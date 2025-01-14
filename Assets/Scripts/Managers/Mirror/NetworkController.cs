﻿using Mirror;
using UnityEngine;
using System.Collections;

public class NetworkController : NetworkBehaviour
{
    private GameObject[] spawnableObjects;
    [SerializeField] public GameObject playerPrefab;

    private void Awake() 
    {
        spawnableObjects = FindObjectOfType<NetworkManager>().spawnPrefabs.ToArray();
    }

    //Spawning object by it's name whith given possition, rotation, velocity and time untill delition if given
    public void NetworkSpawn(string name, Vector3 position, Quaternion rotation, Vector3 velocity, float time = 0)
    {
        if(this.isLocalPlayer)
        {
            CmdSpawn(name, position, rotation, velocity, time);
        }
    }

    [Command]
    private void CmdSpawn(string name, Vector3 position, Quaternion rotation, Vector3 velocity, float time)
    {
        GameObject temporaryObject = null;

        //Goes trough a list of all spawnable objects and finds the right one by name
        foreach (GameObject gameObject in spawnableObjects)
        {
            if (gameObject.name.Equals(name))
            {
                temporaryObject = gameObject;
            }
        }

        //Instantinating the object on the client
        GameObject instantiateObject = Instantiate(temporaryObject, position, rotation);

        Rigidbody rigidbody = instantiateObject.GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.velocity = velocity;
        }

        //Spawning the object on the server
        NetworkServer.Spawn(instantiateObject);

        //Starting the destroy method if time is given
        if (time > 0f)
        {
            StartCoroutine(NetworkDestroy(instantiateObject, time));
        }
    }
    
    //Destroys the object on the server after given time
    public IEnumerator NetworkDestroy(GameObject gameObject, float time)
    {
        yield return new WaitForSeconds(time);

        if(this.isLocalPlayer)
        {
            CmdDestroy(gameObject);
        }
    }

    [Command]
    private void CmdDestroy(GameObject gameObject)
    {
        NetworkServer.Destroy(gameObject);
    }
}
