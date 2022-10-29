using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerate))]
public class MapGeneratoreditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerate mapgen = (MapGenerate)target;

        if (DrawDefaultInspector() && mapgen.autoupdate)
        {
            mapgen.drawmapineditor();

        }

        if (GUILayout.Button("generate"))
        {
            mapgen.drawmapineditor();
        }
    }
}
