using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using Core.Player;
using Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor {
    public class TrackEditor : EditorWindow {
        private Track _track;

        private int _selectedEnvironmentIndex;


        [MenuItem("Fly Dangerous/Track Editor")]
        public static void ShowWindow() {
            GetWindow(typeof(TrackEditor), false, "Fly Dangerous Track Editor");
        }

        private void OnEnable() {
            SceneManager.activeSceneChanged += SceneChanged;
        }

        private void OnDisable() {
            SceneManager.activeSceneChanged -= SceneChanged;
        }

        private void SceneChanged(Scene _, Scene __) {
            Refresh();
        }

        private void Refresh() {
            _track = FindObjectOfType<Track>();
            if (_track != null) {
                var levelData = _track.Serialize();
                _selectedEnvironmentIndex = Environment.List().ToList().IndexOf(levelData.environment);
            }

            Repaint();
        }

        private void OnGUI() {
            GUILayout.Label("Work in progress don't use me ^_^");
            if (_track == null) {
                GUILayout.Label("No Track in the scene.");
                if (GUILayout.Button("Refresh")) Refresh();
                return;
            }

            GUILayout.Label("Detected track. This tool should be used while the game is running!");
            EditorGUILayout.ObjectField(_track, typeof(Track), true);
            if (GUILayout.Button("Copy Level to Clipboard")) GUIUtility.systemCopyBuffer = _track.Serialize().ToJsonString();
            if (GUILayout.Button("Skip to Next Level")) {
                var ship = FdPlayer.FindLocalShipPlayer;
                if (ship != null) ship.User.InGameUI.GameModeUIHandler.RaceResultsScreen.LoadNextLevel();
            }

            var environment = EditorGUILayout.Popup("Environment", _selectedEnvironmentIndex, GetEnvironments().ToArray());

            if (EditorGUI.EndChangeCheck())
                if (environment != _selectedEnvironmentIndex)
                    SetEnvironmentByIndex(environment);
        }

        private List<string> GetEnvironments() {
            return Environment.List().Select(b => b.Name).ToList();
        }

        private void SetEnvironmentByIndex(int environmentIndex) {
            var sceneToLoad = Environment.List().ToArray()[environmentIndex].SceneToLoad;
            _track.SetEnvironmentByName(sceneToLoad);
        }
    }
}