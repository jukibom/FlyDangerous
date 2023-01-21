using System.Collections;
using System.IO;
using Cinemachine;
using Core;
using Core.MapData;
using Core.MapData.Serializable;
using Core.Player;
using Core.Replays;
using Core.Scores;
using Core.ShipModel;
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
        public ShipGhost shipGhostPrefab;


        private void Start() {
            IEnumerator StartGame() {
                // load engine scene if not already 
                if (!FindObjectOfType<Engine>()) yield return SceneManager.LoadSceneAsync("Engine", LoadSceneMode.Additive);

                // allow game state to initialise
                yield return new WaitForFixedUpdate();

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

                var pos = spawnLocation.position;
                var rot = spawnLocation.rotation;
                Game.Instance.LoadedLevelData.startPosition = new SerializableVector3(pos.x, pos.y, pos.z);
                Game.Instance.LoadedLevelData.startRotation = new SerializableVector3(rot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z);

                yield return new WaitForEndOfFrame();

                // enable input and position it where this entity is
                var player = FdPlayer.FindLocalShipPlayer;
                if (player) {
                    player.User.EnableGameInput();
                    player.SetTransformWorld(pos, rot);
                }

                // if there's a map magic object going on here, enable it
                var mapMagic = FindObjectOfType<MapMagicObject>();
                if (mapMagic) {
                    Game.Instance.LoadedLevelData.location = Location.TerrainV1;
                    mapMagic.enabled = true;
                }

                // apply graphics options
                Game.Instance.ApplyGameOptions();

                // init game mode handlers for free roam
                var track = FindObjectOfType<Track>();
                if (player && track) {
                    Game.Instance.LoadedLevelData.gameType = GameType.FreeRoam;
                    Game.Instance.GameModeHandler.InitialiseGameMode(player, Game.Instance.LoadedLevelData, GameType.FreeRoam.GameMode, player.User.InGameUI,
                        track);
                }
                else if (track == null) {
                    Debug.LogWarning(
                        "No track in the scene! You're in undefined behaviour territory as the game mode has not initialised! Consider adding an @World prefab to the loaded location scene.");
                }

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
                        Debug.Log("Loading ghosts from " + path);
                        foreach (var ghostPath in ghostPaths) Game.Instance.LoadGhost(Replay.LoadFromFilepath(ghostPath));
                    }
                }

                Game.Instance.NotifyVRStatus();

                // Fade in!
                Game.Instance.FadeFromBlack();

                // Start
                Game.Instance.GameModeHandler.Begin();

                // My work here is done
                spawnLocation.gameObject.SetActive(false);
            }

            StartCoroutine(StartGame());
        }

        private void OnApplicationQuit() {
            if (shouldRecordSession) {
                var recorder = GetComponent<ReplayRecorder>();
                recorder.StopRecording();
                recorder.Replay?.Save(new ScoreData());
            }
        }

        private void CreateTestSecondShip() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                Instantiate(shipGhostPrefab, player.transform.position + new Vector3(0, 0, 10), Quaternion.identity);
                var targettingSystem = FindObjectOfType<TargettingSystem>();
                if (targettingSystem) targettingSystem.ResetTargets();
            }
        }
    }
}