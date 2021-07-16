using System;
using Mirror;
using UnityEngine;

namespace Core.Player {
    public class LoadingPlayer : NetworkBehaviour {

        [SyncVar]
        private bool _isLoaded;
        public bool IsLoaded => _isLoaded;
        
        private void Awake() {
            // We want to keep this around when jumping to the loading scene and manually destroy it later.
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() {
            LevelLoader.OnLevelLoaded += OnLevelLoaded;
        }

        private void OnDisable() {
            LevelLoader.OnLevelLoaded -= OnLevelLoaded;
        }

        private void OnLevelLoaded() {
            // store loaded state, inform network layer
            _isLoaded = true;
        }
    }
}