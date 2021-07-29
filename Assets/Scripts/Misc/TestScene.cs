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
        
        private void Start() {
            IEnumerator StartGame() {
                
                // start server and connect to it
                FdNetworkManager.Instance.StartOfflineServer();
                yield return new WaitForEndOfFrame();
                FdNetworkManager.Instance.StartMainGame(null);
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
