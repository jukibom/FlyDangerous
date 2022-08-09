using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;



namespace Bhaptics.Tact.Unity
{
    [CustomEditor(typeof(FileHapticClip), true)]
    public class FileHapticClipEditor : HapticClipEditor
    {

        protected FileHapticClip m_targetScript
        {
            get
            {
                return targetScript as FileHapticClip;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DefaultPropertyUI();

            GUILayout.Space(3);
            ResetUI();

            GUILayout.Space(20);
            PlayUI();

            GUILayout.Space(3);
            SaveAsUI();

            serializedObject.ApplyModifiedProperties();
        }



        protected void ReflectUI()
        {
            var clipType = m_targetScript.ClipType;
            if (clipType == HapticDeviceType.Tactosy_arms || clipType == HapticDeviceType.Tactosy_feet || clipType == HapticDeviceType.Tactosy_hands)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IsReflect"), new GUIContent("IsReflect"), GUILayout.Width(350f));
                GUILayout.EndHorizontal();
            }
        }

        protected void DefaultPropertyUI()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Clip Type", GUILayout.Width(100f));

            var clipTypeSerializedObject = serializedObject.FindProperty("ClipType");
            EditorGUILayout.LabelField(clipTypeSerializedObject.enumNames[clipTypeSerializedObject.enumValueIndex]);
            GUILayout.EndHorizontal();

            var originLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = 130f;
            GUI.enabled = false;
            GUILayout.BeginHorizontal();
            var temp = m_targetScript.ClipDurationTime;
            GUIContent customLabel = new GUIContent("└ Duration Time(ms)");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_clipDurationTime"), customLabel, GUILayout.Width(350f));
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUIUtility.labelWidth = 105f;

            ReflectUI();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Intensity"), GUILayout.Width(350f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Duration"), GUILayout.Width(350f));
            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = originLabelWidth;
        }

        protected void SaveAsUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save As *.tact File"))
            {
                SaveAsTactFileFromClip(target);
            }
            GUILayout.EndHorizontal();
        }




        private void SaveAsTactFileFromClip(Object target)
        {
            if (m_targetScript != null)
            {
                var saveAsPath = EditorUtility.SaveFilePanel("Save as *.tact File", @"\download\", m_targetScript.name, "tact");
                if (saveAsPath != "")
                {
                    System.IO.File.WriteAllText(saveAsPath, m_targetScript.JsonValue);
                }
            }
        }
    }
}
