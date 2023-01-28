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

            if (musicTrack != null)
                MusicManager.Instance.PlayMusic(musicTrack, true, true, false);
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
                    ship.SetTransformWorld(position, rotation);
                    ship.Reset();
                    onRestart();
                }

                // the terrain will not be loaded if we teleport there, we need to fade to black, wait for terrain to load, then fade back. This should still be faster than full reload.
                IEnumerator LoadTerrainAndReset(Vector3 position, Quaternion rotation) {
                    ship.SetTransformWorld(position, rotation);
                    ship.Reset();

                    yield return new WaitForSeconds(0.1f);

                    // wait for fully loaded local terrain
                    var loadText = GameObject.FindGameObjectWithTag("DynamicLoadingText").GetComponent<Text>();
                    while (mapMagic.IsGenerating()) {
                        try {
                            var progressPercent = Mathf.Min(100, Mathf.Round(mapMagic.GetProgress() * 100));
                            loadText.text = $"Generating terrain ({progressPercent}%)";
                        }
                        catch {
                            // ignored because it only updates loading text and weird race condition errors here in map magic and out of my control
                        }

                        yield return null;
                    }

                    // unload the loading screen
                    var unload = SceneManager.UnloadSceneAsync("Loading");
                    while (!unload.isDone) yield return null;

                    DoReset(position, rotation);
                    Game.Instance.FadeFromBlack();
                    yield return new WaitForSeconds(0.7f);
                }

                var positionToWarpTo = LoadedLevelData.startPosition.ToVector3();
                var rotationToWarpTo = Quaternion.Euler(LoadedLevelData.startRotation.ToVector3());

                // if multiplayer free-roam and not the host, warp to the host
                if (Game.Instance.SessionType == SessionType.Multiplayer && LoadedLevelData.gameType.GameMode.CanWarpToHost && !ship.isHost)
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
                    yield return LoadTerrainAndReset(positionToWarpTo, rotationToWarpTo);
                }
                else {
                    // don't need to wait for full scene reload, just reset state and notify subscribers
                    DoReset(positionToWarpTo, rotationToWarpTo);
                }

                // Restart the scene
                Game.Instance.GameModeHandler.Restart();
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
            var track = FindObjectOfType<Track>();
            var player = FdPlayer.LocalShipPlayer;
            if (track && player) return track.Serialize(player.AbsoluteWorldPosition, player.transform.rotation);

            // failed to find, this function has maybe been called in menu or invalid loaded state
            Debug.LogError("Failed to find required components to serialise level data!");
            return new LevelData();
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

            var track = FindObjectOfType<Track>();
            if (track) track.Deserialize(LoadedLevelData);

            // set floating origin on loading player now to force world components to update position
            var loadingPlayer = FdPlayer.FindLocalLoadingPlayer;
            if (loadingPlayer) loadingPlayer.SetFloatingOrigin();

            // if terrain needs to generate, toggle special logic and wait for it to load all primary tiles
            var mapMagic = FindObjectOfType<MapMagicObject>();
            if (mapMagic && LoadedLevelData.terrainSeed != null) {
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
                foreach (var terrainTile in mapMagic.tiles.All()) terrainTile.StartGenerate(mapMagic.graph);
                yield return new WaitForEndOfFrame();

                // wait for fully loaded local terrain
                while (mapMagic.IsGenerating()) {
                    try {
                        var progressPercent = Mathf.Min(100, Mathf.Round(mapMagic.GetProgress() * 100));

                        // this entity may be destroyed by server shutdown...
                        if (loadText != null) loadText.text = $"Generating terrain ({progressPercent}%)\n\n\nSeed: \"{LoadedLevelData.terrainSeed}\"";
                    }
                    catch {
                        // ignored because it only updates loading text and weird race condition errors here in map magic and out of my control
                    }

                    yield return new WaitForEndOfFrame();
                }
            }

            _scenesLoading.Clear();
        }
    }
}