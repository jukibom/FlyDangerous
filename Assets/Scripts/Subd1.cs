using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Subd1 : MonoBehaviour
{
    public MeshFilter MF;

    // Start is called before the first frame update
    void Start()
    {
        Mesh m = MF.mesh;
        MeshHelper.Subdivide(m);
        MeshHelper.Subdivide(m);
        MeshHelper.Subdivide(m);
        MF.mesh = m;
        m = null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
