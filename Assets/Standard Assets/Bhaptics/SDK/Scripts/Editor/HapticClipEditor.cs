using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Callbacks;


namespace Bhaptics.Tact.Unity
{
    [CustomEditor(typeof(HapticClip), true)]
    public class HapticClipEditor : Editor
    {
        protected readonly string PlayAutoConfig = "BHAPTICS-HAPTICCLIP-PLAYAUTO";

        protected HapticClip targetScript;
        protected bool isAutoPlay;

        

        void OnEnable()
        {
            targetScript = target as HapticClip;

            isAutoPlay = PlayerPrefs.GetInt(PlayAutoConfig, 1) == 0;

            if (isAutoPlay)
            {
                targetScript.Play();
            }
        }

        void OnDisable()
        {
            if (isAutoPlay)
            {
                targetScript.Stop();
            }
        }

        protected void PlayUI()
        {
            GUILayout.BeginHorizontal();
            if (targetScript == null)
            {
                BhapticsLogger.LogInfo("hapticClip null");
                GUILayout.EndHorizontal();
                return;
            }

            if (GUILayout.Button("Play"))
            {
                targetScript.Play();
            }
            if (GUILayout.Button("Stop"))
            {
                targetScript.Stop();
            }
            GUILayout.EndHorizontal();
        }

        protected void ResetUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Values"))
            {
                targetScript.ResetValues();
            }
            GUILayout.EndHorizontal();
        }

        [OnOpenAsset(1)]
        public static bool OnOpenTactClip(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj is FileHapticClip)
            {
                (obj as FileHapticClip).Play();
                return true;
            }
            return false;
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
        }

        public override void OnPreviewSettings()
        {
            if (GUILayout.Button(new GUIContent(isAutoPlay ? "AUTO PLAY ON" : "AUTO PLAY OFF", isAutoPlay ? "Turn auto play off" : "Turn auto play on"), "preButton"))
            {
                var currentPlayAutoValue = PlayerPrefs.GetInt(PlayAutoConfig, 1);
                currentPlayAutoValue = (currentPlayAutoValue + 1) % 2;
                PlayerPrefs.SetInt(PlayAutoConfig, currentPlayAutoValue);
                isAutoPlay = PlayerPrefs.GetInt(PlayAutoConfig, 1) == 0;
                if (isAutoPlay)
                {
                    targetScript.Play();
                }
            }
        }
    }
}