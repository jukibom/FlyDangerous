using System;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;

namespace Core.Player {
    public class LoadingPlayer : NetworkBehaviour {

        [CanBeNull]
        public static LoadingPlayer FindLocal => 
            Array.Find(FindObjectsOfType<LoadingPlayer>(), loadingPlayer => loadingPlayer.isLocalPlayer);

        [SyncVar] public string playerName;

        [SyncVar] public bool isHost;
            
        [SyncVar]
        private bool _isLoaded;
        public bool IsLoaded => _isLoaded;
        
        private void Start() {
            // We want to keep this around when jumping to the loading scene and manually destroy it later.
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() {
            LevelLoader.OnLevelLoaded += OnLevelLoaded;
        }

        private void OnDisable() {
            LevelLoader.OnLevelLoaded -= OnLevelLoaded;
        }
        
        // On local client start
        public override void OnStartAuthority() {
            CmdSetPlayerName(Preferences.Instance.GetString("playerName"));
        }

        private void OnLevelLoaded() {
            // store loaded state, inform network layer
            _isLoaded = true;
        }

        [Command]
        public void CmdSetPlayerName(string name) {
            if (name == "") {
                name = "UNNAMED SCRUB";
            }

            playerName = name;
        }
    }
}