using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class FPC_Shader : MonoBehaviour
{
    public GameObject parent;

    void Update()
    {
        if (gameObject.transform.hasChanged)
        {
            List<GameObject> gameObjects = new List<GameObject>();
            parent.GetChildGameObjects(gameObjects);

            for (int i = 0; i < gameObjects.Count; i++)
            {
                gameObjects[i].GetComponent<MeshRenderer>().material.SetVector("_World", gameObject.transform.position * -1);
            }
            gameObject.transform.hasChanged = false;
        }
    }
}
