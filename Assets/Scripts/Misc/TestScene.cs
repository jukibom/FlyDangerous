using System.Collections;
using System.IO;
using Cinemachine;
using Core;
using Core.MapData;
using Core.Player;
using Core.Replays;
using Gameplay;
using MapMagic.Core;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
#if !NO_PAID_ASSETS
using GPUInstancer;
#endif

namespace Misc {
    /**
     * Simple helper class used to get a test environment with a playable ship and working network
     * without having to go through the menus etc.
     */
    public class TestScene : MonoBehaviour {
        public bool shouldShowTestShip;
        public bool shouldRecordSession;
        public bool shouldReplaySession;
        public Transform spawnLocation;
        public ShipPlayer shipPlayerPrefab;

        private void Awake() {
            // load engine if not already 
            if (!FindObjectOfType<Engine>()) SceneManager.LoadScene("Engine", LoadSceneMode.Additive);
        }

        private void Start() {
            IEnumerator StartGame() {
                // allow game state to initialise
                yield return new WaitForEndOfFrame();

#if !NO_PAID_ASSETS
                // gpu instancer fun (paid asset!)
                var cam = FindObjectOfType<CinemachineBrain>().gameObject.GetComponent<Camera>();
                GPUInstancerAPI.SetCamera(cam);
#endif

                // instruct the server to create a ship player immediately on start
                Game.Instance.SessionStatus = SessionStatus.Development;

                // start server and connect to it
                NetworkServer.dontListen = true;
                FdNetworkManager.Instance.StartHost();

                yield return new WaitForEndOfFrame();

                // enable input and position it where this entity is
                var player = FdPlayer.FindLocalShipPlayer;
                if (player) {
                    player.User.EnableGameInput();
                    var pos = spawnLocation.position;
                    var rot = spawnLocation.rotation;
                    player.AbsoluteWorldPosition = pos;
                    player.transform.rotation = rot;
                    Game.Instance.LoadedLevelData.startPosition = new LevelDataVector3(pos.x, pos.y, pos.z);
                    Game.Instance.LoadedLevelData.startRotation = new LevelDataVector3(rot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z);
                }

                // if there's a map magic object going on here, enable it
                var mapMagic = FindObjectOfType<MapMagicObject>();
                if (mapMagic) mapMagic.enabled = true;

                // apply graphics options
                Game.Instance.ApplyGameOptions();

                // if there's a track, initialise it
                var track = FindObjectOfType<Track>();
                if (track) track.InitialiseTrack();

                // create a test other player
                if (shouldShowTestShip) CreateTestSecondShip();

                // record the sessions for testing ghost data
                if (player && shouldRecordSession) {
                    // TODO: move this to game class?
                    var recorder = gameObject.AddComponent<ReplayRecorder>();
                    recorder.StartNewRecording(player.ShipPhysics);
                }

                // playback all the ghosts of the current loaded level
                if (player && shouldReplaySession) {
                    var path = Path.Combine(Replay.ReplayDirectory, Game.Instance.LoadedLevelData.LevelHash());
                    if (Directory.Exists(path)) {
                        var ghostPaths = Directory.GetFiles(path);
                        foreach (var ghostPath in ghostPaths) Game.Instance.LoadGhost(Replay.LoadFromFilepath(ghostPath));
                    }
                }

                // My work here is done
                spawnLocation.gameObject.SetActive(false);
            }

            StartCoroutine(StartGame());
        }

        private void CreateTestSecondShip() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                var testPlayer = Instantiate(shipPlayerPrefab, player.transform.position + new Vector3(0, 0, 10), Quaternion.identity);
            }
        }
    }
}