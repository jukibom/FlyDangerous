using System.Collections;
using System.Collections.Generic;
using Bhaptics.Tact.Unity;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VestHapticClip), true)]
public class VestHapticClipEditor : FileHapticClipEditor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DefaultPropertyUI();
        
        DetailPropertyUi();

        ResetUI();

        GUILayout.Space(20);
        PlayUI();
        
        GUILayout.Space(3);
        SaveAsUI();

        serializedObject.ApplyModifiedProperties();
    }

    private void DetailPropertyUi()
    {
            GUILayout.Space(5);
            var originLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 135f;

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TactFileAngleX"), GUILayout.Width(350f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TactFileOffsetY"), GUILayout.Width(350f));
            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = originLabelWidth;

            GUILayout.Space(5);
    }

}
