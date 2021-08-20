using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Player;
using MapMagic.Core;
using UnityEngine;

namespace Misc {
    /**
     * Simple helper class used to get a test environment with a playable ship and working network
     * without having to go through the menus etc.
     */
    public class TestScene : MonoBehaviour {

        [SerializeField]
        private ShipPlayer shipPlayerPrefab; 
        
        private void Start() {
            IEnumerator StartGame() {
                Game.Instance.SessionStatus = SessionStatus.Development;
                
                // start server and connect to it
                NetworkServer.dontListen = true;
                FdNetworkManager.Instance.StartHost();

                yield return new WaitForEndOfFrame();
                
                // enable input and position it where this entity is
                var player = ShipPlayer.FindLocal;
                if (player) {
                    player.User.EnableGameInput();
                    player.AbsoluteWorldPosition = transform.position;
                }
                
                // if there's a map magic object going on here, enable it
                var mapMagic = FindObjectOfType<MapMagicObject>();
                if (mapMagic) {
                    mapMagic.enabled = true;
                }
                
                // My work here is done
                gameObject.SetActive(false);
            }

            StartCoroutine(StartGame());
        }
    }
}
