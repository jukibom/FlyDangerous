using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;



public class BhapticsUnityPackageWindow : EditorWindow
{
    private string packageDirectoryPath = "/Bhaptics/SDK/Examples/ExamplePackages/";

    private string oculusPackageFileName = "BhapticsOculusExample";
    private string avProVideoPackageFileName = "BhapticsAVProVideoExample";


    [MenuItem("Bhaptics/Import example UnityPackages")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(BhapticsUnityPackageWindow));
    }

    void OnGUI()
    {
        GUILayout.Label("Example of an Input on VR", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Oculus"))
        {
            var path = Application.dataPath + packageDirectoryPath + oculusPackageFileName + ".unitypackage";

            if (File.Exists(path))
            {
                AssetDatabase.ImportPackage(path, true);
            }
            else
            {
                Debug.LogError("File path error; " + path);
            }
        }

        GUILayout.Space(20);

        GUILayout.Label("Example of Haptic with Video", EditorStyles.boldLabel);

        if (GUILayout.Button("AVPro Video"))
        {
            var path = Application.dataPath + packageDirectoryPath + avProVideoPackageFileName + ".unitypackage";

            if (File.Exists(path))
            {
                AssetDatabase.ImportPackage(path, true);
            }
            else
            {
                Debug.LogError("File path error; " + path);
            }
        }
    }
}
