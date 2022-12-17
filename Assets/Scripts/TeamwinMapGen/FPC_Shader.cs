using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class FPC_Shader : MonoBehaviour
{
    public GameObject parent;
    public Shader shader;

    void Update()
    {
        if (gameObject.transform.hasChanged)
        {
            Vector3 Offset = gameObject.transform.position * -1;
            /*
            List<GameObject> gameObjects = new List<GameObject>();
            parent.GetChildGameObjects(gameObjects);
            
            for (int i = 0; i < gameObjects.Count; i++)
            {
                gameObjects[i].GetComponent<MeshRenderer>().material.SetVector("_World", Offset);
                List<GameObject> subgameObjects = new List<GameObject>();
                gameObjects[i].GetChildGameObjects(subgameObjects);
                for (int j = 0; j < subgameObjects.Count; j++)
                {
                    subgameObjects[j].GetComponent<MeshRenderer>().material.SetVector("_World", Offset);
                }
            }
            */
            gameObject.transform.hasChanged = false;
            int Id = Shader.PropertyToID("_World");

            Shader.SetGlobalVector(Id, Offset+new Vector3(12,12,12));

        }
    }
}
