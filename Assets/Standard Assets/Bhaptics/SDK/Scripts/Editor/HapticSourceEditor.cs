using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;



namespace Bhaptics.Tact.Unity
{
	[CustomEditor(typeof(HapticSource))]
	public class HapticSourceEditor : Editor
	{
        private HapticSource targetScript;
        private SerializedObject serialized;



        public virtual void OnEnable()
        {
            targetScript = target as HapticSource;
            serialized = new SerializedObject(targetScript);
        }

        public override void OnInspectorGUI()
        {
            serialized.Update();

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(targetScript), typeof(HapticSource), false);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(serialized.FindProperty("clip"), new GUIContent("Haptic Clip"));

            SerializedProperty playOnAwake = serialized.FindProperty("playOnAwake");
            playOnAwake.boolValue = EditorGUILayout.Toggle("Play On Awake", targetScript.playOnAwake);

            GUILayout.BeginHorizontal();
            SerializedProperty loop = serialized.FindProperty("loop");
            loop.boolValue = EditorGUILayout.Toggle("Loop", targetScript.loop);

            if (targetScript.loop)
            {
                SerializedProperty loopDelaySeconds = serialized.FindProperty("loopDelaySeconds");
                loopDelaySeconds.floatValue = EditorGUILayout.FloatField("Loop Delay Seconds", targetScript.loopDelaySeconds);
            }
            GUILayout.EndHorizontal();

            serialized.ApplyModifiedProperties();
        }
    }
}