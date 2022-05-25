using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Cinemachine;
using Core.Player;
using Den.Tools;
using Gameplay;
using MapMagic.Core;
using Misc;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if !NO_PAID_ASSETS
using GPUInstancer;
#endif

namespace Core.MapData {
    public class LevelLoader : MonoBehaviour {
        public delegate void LevelLoadedAction();

        public GameObject checkpointPrefab;

        private readonly List<AsyncOperation> _scenesLoading = new();
        public LevelData LoadedLevelData { get; private set; } = new();

        public LevelData LevelDataAtCurrentPosition => GenerateLevelData();

        public IEnumerator StartGame(LevelData levelData) {
            LoadedLevelData = levelData;

            var locationSceneToLoad = levelData.location.SceneToLoad;
            var environmentSceneToLoad = levelData.environment.SceneToLoad;
            var musicTrack = levelData.musicTrack;

            // now we can finally start the level load
            _scenesLoading.Add(SceneManager.LoadSceneAsync(environmentSceneToLoad, LoadSceneMode.Additive));
            _scenesLoading.Add(SceneManager.LoadSceneAsync(locationSceneToLoad, LoadSceneMode.Additive));
            _scenesLoading.ForEach(scene => scene.allowSceneActivation = false);

            if (musicTrack != "")
                MusicManager.Instance.PlayMusic(MusicTrack.FromString(musicTrack), true, true, false);
            else
                MusicManager.Instance.StopMusic(true);

            yield return LoadGameScenes();
        }

        public IEnumerator RestartLevel(Action onRestart) {
            var ship = FdPlayer.FindLocalShipPlayer;
            if (ship) {
                // first let's check if this is a terrain world and handle that appropriately
                var mapMagic = FindObjectOfType<MapMagicObject>();

                void DoReset(Vector3 position, Quaternion rotation) {
                    ship.AbsoluteWorldPosition = position;
                    ship.transform.rotation = rotation;
                    ship.Reset();

                    onRestart();
                }

                // the terrain will not be loaded if we teleport there, we need to fade to black, wait for terrain to load, then fade back. This should still be faster than full reload.
                IEnumerator LoadTerrainAndReset(Vector3 position, Quaternion rotation) {
                    ship.AbsoluteWorldPosition = position;
                    ship.transform.rotation = rotation;
                    ship.Reset();

                    yield return new WaitForSeconds(0.1f);

                    // wait for fully loaded local terrain
                    while (mapMagic.IsGenerating()) {
                        var progressPercent = Mathf.Min(100, Mathf.Round(mapMagic.GetProgress() * 100));
                        var loadText = GameObject.FindGameObjectWithTag("DynamicLoadingText").GetComponent<Text>();
                        loadText.text = $"Generating terrain ({progressPercent}%)";

                        yield return null;
                    }

                    // unload the loading screen
                    var unload = SceneManager.UnloadSceneAsync("Loading");
                    while (!unload.isDone) yield return null;

                    DoReset(position, rotation);
                    Game.Instance.FadeFromBlack();
                    yield return new WaitForSeconds(0.7f);
                }

                IEnumerator ResetTrackIfNeeded() {
                    // if there's a track in the game world, start it
                    var track = FindObjectOfType<Track>();
                    if (track) yield return track.StartTrackWithCountdown();

                    var user = ship.User;
                    if (user) user.EnableGameInput();
                }

                var positionToWarpTo = new Vector3 {
                    x = LoadedLevelData.startPosition.x,
                    y = LoadedLevelData.startPosition.y,
                    z = LoadedLevelData.startPosition.z
                };

                var rotationToWarpTo = Quaternion.Euler(LoadedLevelData.startRotation.x,
                    LoadedLevelData.startRotation.y, LoadedLevelData.startRotation.z);

                // if multiplayer free-roam and not the host, warp to the host
                if (Game.Instance.SessionType == SessionType.Multiplayer && LoadedLevelData.gameType.CanWarpToHost && !ship.isHost)
                    FindObjectsOfType<ShipPlayer>().ToList().ForEach(otherShipPlayer => {
                        if (otherShipPlayer.isHost) {
                            var emptyPosition = PositionalHelpers.FindClosestEmptyPosition(otherShipPlayer.AbsoluteWorldPosition, 10);
                            rotationToWarpTo = otherShipPlayer.transform.rotation;
                            positionToWarpTo = emptyPosition + FloatingOrigin.Instance.Origin;
                        }
                    });

                var shipPosition = ship.AbsoluteWorldPosition;
                var distanceToStart = Vector3.Distance(shipPosition, positionToWarpTo);

                // TODO: Make this distance dynamic based on tiles?
                if (mapMagic && ship && distanceToStart > 20000) {
                    yield return ShowLoadingScreen(true);

                    // if there's a track in the game world, clear ghosts
                    var track = FindObjectOfType<Track>();
                    if (track) track.ClearGhosts();

                    yield return LoadTerrainAndReset(positionToWarpTo, rotationToWarpTo);
                    yield return ResetTrackIfNeeded();
                }
                else {
                    // don't need to wait for full scene reload, just reset state and notify subscribers
                    DoReset(positionToWarpTo, rotationToWarpTo);
                    yield return ResetTrackIfNeeded();
                }
            }
            else {
                Debug.LogWarning("No local ship player found for restart action");
            }
        }

        // This is a separate action so that we can safely move to a new active loading scene and fully unload everything
        // before moving to any other map or whatever we need to do.
        // On completion it executes callback `then` with a reference to the loading text.
        public IEnumerator ShowLoadingScreen(bool keepScene = false) {
            // disable user input if we're in-game while handling everything else
            var ship = FdPlayer.FindLocalShipPlayer;
            if (ship != null) {
                ship.User.DisableGameInput();
                ship.User.DisableUIInput();
            }

            Game.Instance.FadeToBlack();
            yield return new WaitForSeconds(0.5f);

            // load loading screen (lol)
            var loadMode = keepScene ? LoadSceneMode.Additive : LoadSceneMode.Single;
            yield return SceneManager.LoadSceneAsync("Loading", loadMode);
        }

        public IEnumerator HideLoadingScreen() {
            // unload the loading screen
            yield return SceneManager.UnloadSceneAsync("Loading");
        }

        // Return a new level data object hydrated with all the information of the current game state
        private LevelData GenerateLevelData() {
            var levelData = new LevelData {
                name = LoadedLevelData.name,
                gameType = LoadedLevelData.gameType,
                location = LoadedLevelData.location,
                musicTrack = LoadedLevelData.musicTrack,
                environment = LoadedLevelData.environment,
                terrainSeed = LoadedLevelData.terrainSeed,
                checkpoints = LoadedLevelData.checkpoints
            };

            var ship = FdPlayer.FindLocalShipPlayer;
            if (ship) {
                var position = ship.AbsoluteWorldPosition;
                var rotation = ship.transform.rotation;
                levelData.startPosition.x = position.x;
                levelData.startPosition.y = position.y;
                levelData.startPosition.z = position.z;
                levelData.startRotation.x = rotation.eulerAngles.x;
                levelData.startRotation.y = rotation.eulerAngles.y;
                levelData.startRotation.z = rotation.eulerAngles.z;
            }

            var track = FindObjectOfType<Track>();
            if (track) {
                var checkpoints = track.Checkpoints;
                levelData.checkpoints = new List<CheckpointLocation>();
                foreach (var checkpoint in checkpoints) {
                    var checkpointLocation = new CheckpointLocation();
                    checkpointLocation.type = checkpoint.Type;
                    checkpointLocation.position = new LevelDataVector3();
                    checkpointLocation.rotation = new LevelDataVector3();

                    var checkpointTransform = checkpoint.transform;
                    var position = checkpointTransform.localPosition;
                    var rotation = checkpointTransform.rotation.eulerAngles;
                    checkpointLocation.position = LevelDataVector3.FromVector3(position);
                    checkpointLocation.rotation = LevelDataVector3.FromVector3(rotation);
                    levelData.checkpoints.Add(checkpointLocation);
                }
            }

            return levelData;
        }

        private IEnumerator LoadGameScenes() {
            // disable all game interactions
            Time.timeScale = 0;

            // grab the load text to draw to
            var loadText = GameObject.FindGameObjectWithTag("DynamicLoadingText").GetComponent<Text>();

            float progress = 0;
            for (var i = 0; i < _scenesLoading.Count; ++i)
                while (_scenesLoading[i].progress < 0.9f) {
                    // this is literally what the unity docs recommend
                    yield return null;

                    progress += _scenesLoading[i].progress;
                    var totalProgress = progress / _scenesLoading.Count;
                    var progressPercent = Mathf.Min(100, Mathf.Round(totalProgress * 100));

                    // set loading text (last scene is always the engine)
                    loadText.text = i == _scenesLoading.Count
                        ? $"Loading Engine ({progressPercent}%)"
                        : $"Loading Assets ({progressPercent}%)";

                    yield return null;
                }

            // all scenes have loaded as far as they can without activation, allow them to activate
            for (var i = 0; i < _scenesLoading.Count; ++i) {
                _scenesLoading[i].allowSceneActivation = true;
                while (!_scenesLoading[i].isDone) yield return null;
            }

            // checkpoint placement
            var track = FindObjectOfType<Track>();
            if (track && LoadedLevelData.checkpoints?.Count > 0)
                LoadedLevelData.checkpoints.ForEach(c => {
                    var checkpointObject = Instantiate(checkpointPrefab, track.transform);
                    var checkpoint = checkpointObject.GetComponent<Checkpoint>();
                    checkpoint.Type = c.type;
                    var checkpointObjectTransform = checkpointObject.transform;
                    checkpointObjectTransform.position = c.position.ToVector3();
                    checkpointObjectTransform.rotation = Quaternion.Euler(
                        c.rotation.x,
                        c.rotation.y,
                        c.rotation.z
                    );
                    checkpoint.transform.parent = track.transform;
                });

            // set floating origin on loading player now to force world components to update position
            var loadingPlayer = FdPlayer.FindLocalLoadingPlayer;
            if (loadingPlayer) loadingPlayer.SetFloatingOrigin();

            // if terrain needs to generate, toggle special logic and wait for it to load all primary tiles
            var mapMagic = FindObjectOfType<MapMagicObject>();
            if (mapMagic) {
                mapMagic.graph.random = new Noise(LoadedLevelData.terrainSeed.GetHashCode(), 32768);

#if !NO_PAID_ASSETS
                // gpu instancer initialisation (paid asset!)
                var cam = FindObjectOfType<CinemachineBrain>(true).gameObject.GetComponent<Camera>();
                var gpuInstancer = FindObjectOfType<GPUInstancerMapMagic2Integration>();
                if (mapMagic && gpuInstancer) {
                    gpuInstancer.floatingOriginTransform = mapMagic.transform;
                    GPUInstancerAPI.SetCamera(cam);
                    gpuInstancer.SetCamera(cam);
                }
#endif

                // our terrain gen may start disabled to prevent painful threading fun
                mapMagic.enabled = true;
                yield return new WaitForEndOfFrame();

                // replace with user seed
                mapMagic.graph.random = new Noise(LoadedLevelData.terrainSeed.GetHashCode(), 32768);
                mapMagic.StartGenerate();
                yield return new WaitForEndOfFrame();

                // wait for fully loaded local terrain
                while (mapMagic.IsGenerating()) {
                    var progressPercent = Mathf.Min(100, Mathf.Round(mapMagic.GetProgress() * 100));

                    // this entity may be destroyed by server shutdown...
                    if (loadText != null) loadText.text = $"Generating terrain ({progressPercent}%)\n\n\nSeed: \"{LoadedLevelData.terrainSeed}\"";

                    yield return new WaitForEndOfFrame();
                }
            }

            _scenesLoading.Clear();
        }
    }
}