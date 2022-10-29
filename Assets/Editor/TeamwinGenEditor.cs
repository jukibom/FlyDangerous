using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TeamwinTerr))]

public class TeamwinGenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TeamwinTerr mapgen = (TeamwinTerr)target;

        if (DrawDefaultInspector())
        {
            mapgen.GenerateTerrain();

        }

        if (GUILayout.Button("subdivide"))
        {
            mapgen.subdmesh();
        }
    }
}
