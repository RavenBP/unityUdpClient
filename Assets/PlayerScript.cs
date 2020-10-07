using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [SerializeField]
    public float speed = 500;

    [SerializeField]
    Rigidbody rigidBody;

    public string playerID;
    public NetworkMan networkMan { get; set; }
    public bool isPlayer = false;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 movementVector;

    private void Start()
    {
        InvokeRepeating("UpdatePosition", 1, 0.03f);
    }

    private void Update()
    {
        if (isPlayer == true)
        {
            movementVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f) * speed * Time.deltaTime;
            rigidBody.velocity = movementVector;
        }
    }

    void UpdatePosition()
    {
        if (isPlayer == true)
        {
            networkMan.SendPlayerVectors(rigidBody.position, this.rotation);
        }
    }
}
