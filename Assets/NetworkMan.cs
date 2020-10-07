using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    public GameObject playerObj;
    Queue<string> newClient;
    Dictionary<string, GameObject> listOfClients;
    string myID;

    // Start is called before the first frame update
    void Start()
    {
        newClient = new Queue<string>();
        listOfClients = new Dictionary<string, GameObject>();
        udp = new UdpClient();
        udp.Connect("35.170.246.64", 12345);
        //udp.Connect("localhost", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }


    public enum commands
    {
        NEW_CLIENT,
        UPDATE,
        LIST_CLIENT,
        DROP_CLIENT,
        MY_ID
    };

    [Serializable]
    public class Message
    {
        public commands cmd;
        public Player player;
    }

    [Serializable]
    public class Player
    {
        public string id;
        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }
        public receivedColor color;
        public Vector3 position;
        public Vector3 rotation;
    }

    [Serializable]
    public class NewPlayer
    {
        [Serializable]
        public struct SingleClient
        {
            public string id;
        }
        public SingleClient[] player;
    }

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;

    [Serializable]
    public class PlayerVectors // Used for sending vector information to server
    {
        public Vector3 position;
        public Vector3 rotation;
    }

    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;

        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);

        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        //Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try
        {
            switch (latestMessage.cmd)
            {
                case commands.NEW_CLIENT:
                    NewPlayer newPlayer = JsonUtility.FromJson<NewPlayer>(returnData);
                    Debug.Log("NEW CLIENT");
                    foreach (var player in newPlayer.player)
                    {
                        newClient.Enqueue(player.id);
                    }
                    break;
                case commands.UPDATE:
                    Debug.Log("UPDATE");
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.LIST_CLIENT:
                    Debug.Log("LIST");
                    NewPlayer playerList = JsonUtility.FromJson<NewPlayer>(returnData);
                    foreach (var player in playerList.player)
                    {
                        newClient.Enqueue(player.id);
                    }
                    break;
                case commands.DROP_CLIENT:
                    Debug.Log("DROP CLIENT");
                    NewPlayer deleteList = JsonUtility.FromJson<NewPlayer>(returnData);
                    foreach (var player in deleteList.player)
                    {
                        listOfClients.Remove(player.id);
                    }
                    break;
                case commands.MY_ID:
                    Debug.Log("GOT OWN ID");
                    NewPlayer myself = JsonUtility.FromJson<NewPlayer>(returnData);
                    newClient.Enqueue(myself.player[0].id);
                    myID = myself.player[0].id;
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers(string playerID)
    {
        if (listOfClients.ContainsKey(playerID) == true)
        {
            return;
        }

        GameObject newPlayer = (GameObject)Instantiate(playerObj, new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0.0f), transform.rotation);
        newPlayer.GetComponent<PlayerScript>().playerID = playerID;
        newPlayer.GetComponent<PlayerScript>().networkMan = this;

        // Ensure proper player is linked with correct ID
        if (myID == playerID)
        {
            newPlayer.GetComponent<PlayerScript>().isPlayer = true;
        }

        listOfClients.Add(playerID, newPlayer);
    }

    void UpdatePlayers()
    {
        if (newClient.Count > 0)
        {
            string id = newClient.Dequeue();
            SpawnPlayers(id);
        }

        foreach (Player player in lastestGameState.players)
        {
            Color color = new Color(player.color.R, player.color.G, player.color.B);
            listOfClients[player.id].GetComponent<Renderer>().material.SetColor("_Color", color);
            listOfClients[player.id].transform.position = player.position;
            listOfClients[player.id].transform.eulerAngles = player.rotation;
        }
    }

    void DestroyPlayers()
    {
        PlayerScript[] playerScripts = GameObject.FindObjectsOfType<PlayerScript>();
        foreach (PlayerScript playerScript in playerScripts)
        {
            if (listOfClients.ContainsKey(playerScript.playerID) == false)
            {
                Destroy(playerScript.gameObject);
            }
        }
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    public void SendPlayerVectors(Vector3 playerPosition, Vector3 playerRotation)
    {
        PlayerVectors playerVectors = new PlayerVectors();
        playerVectors.position = playerPosition;
        playerVectors.rotation = playerRotation;

        string jsonString = JsonUtility.ToJson(playerVectors);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(jsonString);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        UpdatePlayers();
        DestroyPlayers();
    }
}
