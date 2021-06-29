using System;
using System.Collections;
using System.Collections.Generic;
using Den.Tools;
using MapMagic.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Engine {
    public class LevelLoader : MonoBehaviour {
        
        private LevelData _levelData = new LevelData();
        private List<AsyncOperation> _scenesLoading = new List<AsyncOperation>();
        public LevelData LoadedLevelData => _levelData;
        public LevelData LevelDataAtCurrentPosition => GenerateLevelData();
        public GameObject checkpointPrefab;

        public void StopTerrainGenerationIfApplicable() {
            var terrainLoader = FindObjectOfType<MapMagicObject>();
            if (terrainLoader) {
                terrainLoader.StopGenerate();
            }
        }
        
        public void ResetLoadedLevelData() {
            _levelData = new LevelData();
        }
        
        public void StartGame(LevelData levelData, ShipParameters shipParameters, bool dynamicPlacementStart = false) {
            _levelData = levelData;

            string location;
            string environment;
            
            // main location loader 
            switch (levelData.location) {
                case Location.NullSpace: location = "MapTest"; break;   // used when loading without going via the menu
                case Location.TestSpaceStation: location = "SpaceStation"; break;
                case Location.TerrainV1: location = "TerrainV1"; break;
                case Location.TerrainV2: location = "TerrainV2"; break;
                default: throw new Exception("Supplied map type (" + levelData.location + ") is not a valid scene.");
            }
            
            // if terrain, include conditions
            switch (levelData.environment) {
                case Environment.PlanetOrbitBottom: environment = "Planet_Orbit_Bottom"; break;
                case Environment.PlanetOrbitTop: environment = "Planet_Orbit_Top"; break;
                case Environment.SunriseClear: environment = "Sunrise_Clear"; break;
                case Environment.NoonClear: environment = "Noon_Clear"; break;
                case Environment.NoonCloudy: environment = "Noon_Cloudy"; break;
                case Environment.NoonStormy: environment = "Noon_Stormy"; break;
                case Environment.SunsetClear: environment = "Sunset_Clear"; break;
                case Environment.SunsetCloudy: environment = "Sunset_Cloudy"; break;
                case Environment.NightClear: environment = "Night_Clear"; break;
                case Environment.NightCloudy: environment = "Night_Cloudy"; break;
                default: environment = "Sunrise_Clear"; break;
            }
            
            StartCoroutine(SwitchToLoadingScreen(loadText => {
                // now we can finally start the level load
                _scenesLoading.Add(SceneManager.LoadSceneAsync(environment, LoadSceneMode.Additive));
                _scenesLoading.Add(SceneManager.LoadSceneAsync(location, LoadSceneMode.Additive));
                _scenesLoading.ForEach(scene => scene.allowSceneActivation = false);
                
                StartCoroutine(LoadGameScenes(loadText, shipParameters, dynamicPlacementStart));
            }));
        }
        
         public void RestartLevel(Action onRestart) {
                var user = FindObjectOfType<User>();
                var ship = FindObjectOfType<Ship>();
        
                Action DoReset = () => {
                    var world = GameObject.Find("World")?.transform;
                    if (world != null) {
                        world.position = Vector3.zero;
                    }
        
                    ship.transform.position = new Vector3 {
                        x = LoadedLevelData.startPosition.x, 
                        y = LoadedLevelData.startPosition.y, 
                        z = LoadedLevelData.startPosition.z
                    };
                    ship.transform.rotation = Quaternion.Euler(
                        LoadedLevelData.startRotation.x, 
                        LoadedLevelData.startRotation.y,
                        LoadedLevelData.startRotation.z
                    );
                    ship.Reset();
        
                    if (onRestart != null) {
                        onRestart();
                    }
                };
        
                IEnumerator ResetTrackIfNeeded() {
                    // if there's a track in the game world, start it
                    var track = FindObjectOfType<Track>();
                    if (track) {
                        yield return track.StartTrackWithCountdown();
                    }
        
                    if (user) {
                        user.EnableGameInput();
                    }
                }
                
                // first let's check if this is a terrain world and handle that appropriately
                var mapMagic = FindObjectOfType<MapMagicObject>();
                if (mapMagic && ship) {
                    ship.AbsoluteWorldPosition(out var shipPosition, out _);
                    var distanceToStart = Vector3.Distance(shipPosition, new Vector3 {
                        x = LoadedLevelData.startPosition.x, 
                        y = LoadedLevelData.startPosition.y, 
                        z = LoadedLevelData.startPosition.z
                    }) ;
        
                    // where do we get this from?
                    if (distanceToStart > 20000) {
                        
                        // the terrain will not be loaded if we teleport there, we need to fade to black, wait for terrain to load, then fade back. This should still be faster than full reload.
                        IEnumerator LoadTerrainAndReset(Text loadingText) {
                            DoReset();
                            yield return new WaitForSeconds(0.5f);
                            
                            // wait for fully loaded local terrain
                            while (mapMagic.IsGenerating()) {
                                var progressPercent = Mathf.Min(100, Mathf.Round(mapMagic.GetProgress() * 100));
                                loadingText.text = $"Regenerating terrain at start position ({progressPercent}%)";
        
                                yield return null;
                            }
                            
                            // unload the loading screen
                            var unload = SceneManager.UnloadSceneAsync("Loading");
                            while (!unload.isDone) {
                                yield return null;
                            }
                            
                            Game.Instance.FadeFromBlack();
                            yield return new WaitForSeconds(0.7f);
        
                            yield return ResetTrackIfNeeded();
                        }
        
                        StartCoroutine(SwitchToLoadingScreen(loadText => {
                            StartCoroutine(LoadTerrainAndReset(loadText));
                        }, true));
                        
                        // loading enumerator started, early return (gross)
                        return;
                    }
                }
        
                // don't need to wait for full scene reload, just reset state and notify subscribers
                DoReset();
                StartCoroutine(ResetTrackIfNeeded());
            }
        
        // Return a new level data object hydrated with all the information of the current game state
        private LevelData GenerateLevelData() {
            var levelData = new LevelData();
            levelData.name = _levelData.name;
            levelData.raceType = _levelData.raceType;
            levelData.location = _levelData.location;
            levelData.environment = _levelData.environment;
            levelData.terrainSeed = _levelData.terrainSeed;
            levelData.checkpoints = _levelData.checkpoints;

            var ship = FindObjectOfType<Ship>();
            // TODO: Is this ship the actual player? (Convert it to a network object!)
            if (ship) {
                ship.AbsoluteWorldPosition(out var outPosition, out var outRotation);
                levelData.startPosition.x = outPosition.x;
                levelData.startPosition.y = outPosition.y;
                levelData.startPosition.z = outPosition.z;
                levelData.startRotation.x = outRotation.eulerAngles.x;
                levelData.startRotation.y = outRotation.eulerAngles.y;
                levelData.startRotation.z = outRotation.eulerAngles.z;
            }

            var track = FindObjectOfType<Track>();
            if (track) {
                var checkpoints = track.Checkpoints;
                levelData.checkpoints = new List<CheckpointLocation>();
                foreach (var checkpoint in checkpoints) {
                    var checkpointLocation = new CheckpointLocation();
                    checkpointLocation.type = checkpoint.Type;
                    checkpointLocation.position = new LevelDataVector3<float>();
                    checkpointLocation.rotation = new LevelDataVector3<float>();
                    
                    var position = checkpoint.transform.localPosition;
                    var rotation = checkpoint.transform.rotation.eulerAngles;
                    checkpointLocation.position.x = position.x;
                    checkpointLocation.position.y = position.y;
                    checkpointLocation.position.z = position.z;
                    checkpointLocation.rotation.x = rotation.x;
                    checkpointLocation.rotation.y = rotation.y;
                    checkpointLocation.rotation.z = rotation.z;
                    levelData.checkpoints.Add(checkpointLocation);
                }
            }
            return levelData;
        }
        
        IEnumerator LoadGameScenes(Text loadingText, ShipParameters shipParameters, bool dynamicPlacement) {
        
            // disable all game interactions
            Time.timeScale = 0;

            float progress = 0;
            for (int i = 0; i < _scenesLoading.Count; ++i) {
                while (_scenesLoading[i].progress < 0.9f) { // this is literally what the unity docs recommend
                    yield return null;
                        
                    progress += _scenesLoading[i].progress;
                    var totalProgress = progress / _scenesLoading.Count;
                    var progressPercent = Mathf.Min(100, Mathf.Round(totalProgress * 100));
                    
                    // set loading text (last scene is always the engine)
                    loadingText.text = i == _scenesLoading.Count
                        ? $"Loading Engine ({progressPercent}%)"
                        : $"Loading Assets ({progressPercent}%)";
                    
                    yield return null;
                }
            }
            
            // all scenes have loaded as far as they can without activation, allow them to activate
            for (int i = 0; i < _scenesLoading.Count; ++i) {
                _scenesLoading[i].allowSceneActivation = true;
                while (!_scenesLoading[i].isDone) {
                    yield return null;
                }
            }

            // ship placement
            var ship = FindObjectOfType<Ship>();
            if (ship) {
                ship.transform.position = new Vector3(
                    _levelData.startPosition.x,
                    _levelData.startPosition.y,
                    _levelData.startPosition.z
                );
                ship.transform.rotation = Quaternion.Euler(
                    _levelData.startRotation.x,    
                    _levelData.startRotation.y,    
                    _levelData.startRotation.z    
                );
                
                // debug flight params
                ship.Parameters = shipParameters;
            }

            // checkpoint placement
            var track = FindObjectOfType<Track>();
            if (track && _levelData.checkpoints?.Count > 0) {
                _levelData.checkpoints.ForEach(c => {
                    var checkpointObject = Instantiate(checkpointPrefab, track.transform);
                    var checkpoint = checkpointObject.GetComponent<Checkpoint>();
                    checkpoint.Type = c.type;
                    var transform = checkpointObject.transform;
                    transform.position = new Vector3(
                        c.position.x,
                        c.position.y,
                        c.position.z
                    );
                    transform.rotation = Quaternion.Euler(
                        c.rotation.x,
                        c.rotation.y,
                        c.rotation.z
                    );
                    checkpoint.transform.parent = track.transform;
                });
                // position the player at the start and initialise all the checkpoints
                track.InitialiseTrack();
            }

            // if terrain needs to generate, toggle special logic and wait for it to load all primary tiles
            // TODO: shift this crap to the TerrainLoader, it makes way more sense over there
            var mapMagic = FindObjectOfType<MapMagicObject>();
            if (mapMagic) {
                // our terrain gen may start disabled to prevent painful threading fun
                mapMagic.enabled = true;
                
                // Stop auto-loading with default seed
                mapMagic.StopGenerate();
                Den.Tools.Tasks.ThreadManager.Abort();
                yield return new WaitForEndOfFrame();
                mapMagic.ClearAll();

                // replace with user seed
                mapMagic.graph.random = new Noise(_levelData.terrainSeed.GetHashCode(), 32768);
                mapMagic.StartGenerate();
                
                // wait for fully loaded local terrain
                while (mapMagic.IsGenerating()) {
                    var progressPercent = Mathf.Min(100, Mathf.Round(mapMagic.GetProgress() * 100));
                    loadingText.text = $"Generating terrain ({progressPercent}%)\n\n\nSeed: \"{_levelData.terrainSeed}\"";

                    yield return null;
                }
                
                // terrain loaded, if we need to dynamically place the ship let's do that now
                if (ship && dynamicPlacement) {
                    // TODO: make this iterate over the corners of the ship:
                    // move the player up high and perform 5 raycasts - one from each corner of the ship and one from the centre.
                    // move the player to the closest one, height-wise.
                    // Additionally, move the ship around in a spiral and perform this operation a number of times.
                    // Move the ship to the lowest position.

                    var shipTransform = ship.transform;
                    
                    // move ship above terrain max
                    shipTransform.position = new Vector3(
                        shipTransform.position.x,
                        10000,
                        shipTransform.position.z
                    );
                    
                    // cast down to get terrain height at this position
                    if (Physics.Raycast(ship.transform.position, Vector3.down, out var hit, 10000)) {
                        shipTransform.position = hit.point;
                        
                        // move ship 25 meters up to compensate for rocks and other crap
                        shipTransform.Translate(0, 25, 0);
                        
                        // store new position in game level data for restarts
                        _levelData.startPosition.x = shipTransform.position.x;
                        _levelData.startPosition.y = shipTransform.position.y;
                        _levelData.startPosition.z = shipTransform.position.z;
                    }
                }
            }

            // unload the loading screen
            var unload = SceneManager.UnloadSceneAsync("Loading");
            while (!unload.isDone) {
                yield return null;
            }

            // set up graphics settings (e.g. camera FoV) + VR status (cameras, radial fog etc)
            Game.Instance.ApplyGraphicsOptions();
            Game.Instance.NotifyVRStatus();

            // resume the game
            Time.timeScale = 1;
            Game.Instance.FadeFromBlack();
            yield return new WaitForSeconds(0.7f);

            // if there's a track in the game world, start it
            if (track) {
                yield return track.StartTrackWithCountdown();
            }

            // enable user input
            var user = FindObjectOfType<User>();
            if (user != null) {
                user.EnableGameInput();
            }

            _scenesLoading.Clear();
        }
        
        // This is a separate action so that we can safely move to a new active loading scene and fully unload everything
        // before moving to any other map or whatever we need to do.
        // On completion it executes callback `then` with a reference to the loading text.
        IEnumerator SwitchToLoadingScreen(Action<Text> then, bool keepScene = false) {
            
            // disable user input if we're in-game while handling everything else
            var user = FindObjectOfType<User>();
            if (user != null) {
                user.DisableGameInput();
                user.DisableUIInput();
            }

            Game.Instance.FadeToBlack();
            yield return new WaitForSeconds(0.5f);
            
            // load loading screen (lol)
            var loadMode = keepScene ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var load = SceneManager.LoadSceneAsync("Loading", loadMode);
            yield return load;

            var loadText = GameObject.FindGameObjectWithTag("DynamicLoadingText").GetComponent<Text>();
            then(loadText);
        }
    }
}