using System;
using System.Collections;
using System.Collections.Generic;
using Den.Tools;
using Engine;
using JetBrains.Annotations;
using MapMagic.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : MonoBehaviour {

    public static Game Instance;

    public GameObject checkpointPrefab;
    
    private LevelData _levelDataAs = new LevelData();
    public LevelData LevelDataAsLoaded => _levelDataAs;
    public LevelData LevelDataCurrent => GenerateLevelData();

    [CanBeNull] private ShipParameters _shipParameters;
    public ShipParameters ShipParameters {
        get => _shipParameters == null 
            ? FindObjectOfType<Ship>()?.Parameters ?? Ship.ShipParameterDefaults
            : _shipParameters;
        set {
            _shipParameters = value;
            var ship = FindObjectOfType<Ship>();
            if (ship) ship.Parameters = _shipParameters;
        }
    }

    public bool IsTerrainMap => _levelDataAs.location == Location.Terrain;
    public string Seed => _levelDataAs.terrainSeed;
    
    [SerializeField] private Animator crossfade;

    // show certain things if first time hitting the menu
    private bool _menuFirstRun = true;
    public bool menuFirstRun => _menuFirstRun;
    private List<AsyncOperation> scenesLoading = new List<AsyncOperation>();

    void Awake() {
        // singleton shenanigans
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    public void StartGame(LevelData levelData, bool dynamicPlacementStart = false) {
        _levelDataAs = levelData;
        HideCursor();

        string mapScene;
        switch (levelData.location) {
            case Location.NullSpace: mapScene = "MapTest"; break;   // used when loading without going via the menu
            case Location.TestSpaceStation: mapScene = "MapTest"; break;
            case Location.Terrain: mapScene = "Terrain"; break;
            default: throw new Exception("Supplied map type (" + levelData.location + ") is not a valid scene.");
        }
        
        // This is a separate action so that we can safely move to a new active loading scene and fully unload everything before moving to any other map
        IEnumerator SwitchToLoadingScreen() {
            
            // disable user input if we're in-game while handling everything else
            var user = FindObjectOfType<User>();
            if (user != null) {
                user.DisableGameInput();
                user.DisableUIInput();
            }

            crossfade.SetTrigger("FadeToBlack");
            yield return new WaitForSeconds(0.5f);
            
            // load loading screen (lol)
            var load = SceneManager.LoadSceneAsync("Loading", LoadSceneMode.Single);
            yield return load;

            var loadText = GameObject.FindGameObjectWithTag("DynamicLoadingText").GetComponent<Text>();

            // now we can finally start the level load
            scenesLoading.Add(SceneManager.LoadSceneAsync(mapScene, LoadSceneMode.Additive));
            scenesLoading.Add(SceneManager.LoadSceneAsync("Player", LoadSceneMode.Additive));
            scenesLoading.ForEach(scene => scene.allowSceneActivation = false);
            StartCoroutine(LoadGameScenes(loadText, dynamicPlacementStart));
        }
        
        StartCoroutine(SwitchToLoadingScreen());
    }

    public void RestartLevel() {
        // Todo: record player initial state and load it here instead of this scene juggling farce which takes ages to load
        StopTerrainGeneration();
        StartGame(_levelDataAs);
    }

    public void QuitToMenu() {
        _menuFirstRun = false;
        StopTerrainGeneration();
        var user = FindObjectOfType<User>();
        user.DisableGameInput();
        
        IEnumerator LoadMenu() {
            FadeToBlack();
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene("Main Menu");
            ResetGameState();
            FadeFromBlack();
            ShowCursor();
        }

        StartCoroutine(LoadMenu());
    }
    public void QuitGame() {
        IEnumerator Quit() {
            FadeToBlack();
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        StartCoroutine(Quit());
    }

    public void ShowCursor() {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideCursor() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }
    
    public void FadeToBlack() {
        crossfade.SetTrigger("FadeToBlack");
    }

    public void FadeFromBlack() {
        crossfade.SetTrigger("FadeFromBlack");
    }

    private void StopTerrainGeneration() {
        var terrainLoader = FindObjectOfType<MapMagicObject>();
        if (terrainLoader) {
            terrainLoader.StopGenerate();
        }
    }
    
    private void ResetGameState() {
        _levelDataAs = new LevelData();
    }

    // Return a new level data object hydrated with all the information of the current game state
    private LevelData GenerateLevelData() {
        var levelData = new LevelData();
        levelData.raceType = _levelDataAs.raceType;
        levelData.location = _levelDataAs.location;
        levelData.terrainSeed = _levelDataAs.terrainSeed;
        levelData.checkpoints = _levelDataAs.checkpoints;

        var ship = FindObjectOfType<Ship>();
        var floatingOrigin = FindObjectOfType<FloatingOrigin>();
        if (ship) {
            var position = ship.transform.position;
            var rotation = ship.transform.rotation.eulerAngles;
            levelData.startPosition.x = position.x;
            levelData.startPosition.y = position.y;
            levelData.startPosition.z = position.z;
            levelData.startRotation.x = rotation.x;
            levelData.startRotation.y = rotation.y;
            levelData.startRotation.z = rotation.z;

            // if floating origin fix is active, overwrite position with corrected world space
            if (floatingOrigin) {
                var origin = floatingOrigin.FocalObjectPosition;
                levelData.startPosition.x = origin.x;
                levelData.startPosition.y = origin.y;
                levelData.startPosition.z = origin.z;
            }
        }

        var track = FindObjectOfType<Track>();
        if (track) {
            var checkpoints = track.Checkpoints;
            levelData.checkpoints = new List<CheckpointLocation>();
            foreach (var checkpoint in checkpoints) {
                var checkpointLocation = new CheckpointLocation();
                checkpointLocation.type = checkpoint.type;
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
    
    IEnumerator LoadGameScenes(Text loadingText, bool dynamicPlacement) {
        
        // disable all game interactions
        Time.timeScale = 0;
        
        float progress = 0;
        for (int i = 0; i < scenesLoading.Count; ++i) {
            while (scenesLoading[i].progress < 0.9f) { // this is literally what the unity docs recommend
                yield return null;
                    
                progress += scenesLoading[i].progress;
                var totalProgress = progress / scenesLoading.Count;
                var progressPercent = Mathf.Min(100, Mathf.Round(totalProgress * 100));
                
                // set loading text (last scene is always the engine)
                loadingText.text = i == scenesLoading.Count
                    ? $"Loading Engine ({progressPercent}%)"
                    : $"Loading Assets ({progressPercent}%)";
                
                yield return null;
            }
        }
            
        // all scenes have loaded as far as they can without activation, allow them to activate
        for (int i = 0; i < scenesLoading.Count; ++i) {
            scenesLoading[i].allowSceneActivation = true;
            while (!scenesLoading[i].isDone) {
                yield return null;
            }
        }
        
        // disable user input now that a valid user has loaded
        var user = FindObjectOfType<User>();
        if (user != null) {
            user.DisableGameInput();
            user.DisableUIInput();
            user.ResetMouseToCentre();
        }
        
        // ship placement
        var ship = FindObjectOfType<Ship>();
        if (ship && !dynamicPlacement) {
            var t = _levelDataAs.startPosition.x;
            ship.transform.position = new Vector3(
                _levelDataAs.startPosition.x,
                _levelDataAs.startPosition.y,
                _levelDataAs.startPosition.z
            );
            ship.transform.rotation = Quaternion.Euler(
                _levelDataAs.startRotation.x,    
                _levelDataAs.startRotation.y,    
                _levelDataAs.startRotation.z    
            );
            
            // debug flight params
            ship.Parameters = ShipParameters;
        }
        
        // checkpoint placement
        var track = FindObjectOfType<Track>();
        if (track && _levelDataAs.checkpoints?.Count > 0) {
            List<Checkpoint> checkpoints = new List<Checkpoint>();
            _levelDataAs.checkpoints.ForEach(c => {
                var checkpointObject = Instantiate(checkpointPrefab, track.transform);
                var checkpoint = checkpointObject.GetComponent<Checkpoint>();
                checkpoint.type = c.type;
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
                checkpoints.Add(checkpoint);
            });

            track.Checkpoints = checkpoints;
        }

        // if terrain needs to generate, toggle special logic and wait for it to load all primary tiles
        var terrainLoader = FindObjectOfType<MapMagicObject>();
        if (_levelDataAs.location == Location.Terrain && terrainLoader) {
            
            // Stop auto-loading with default seed
            terrainLoader.StopGenerate();
            Den.Tools.Tasks.ThreadManager.Abort();
            yield return new WaitForEndOfFrame();
            terrainLoader.ClearAll();
            
            // replace with user seed
            terrainLoader.graph.random = new Noise(_levelDataAs.terrainSeed.GetHashCode(), 32768);
            terrainLoader.StartGenerate();
            
            // wait for fully loaded local terrain
            while (terrainLoader.IsGenerating()) {
                var progressPercent = Mathf.Min(100, Mathf.Round(terrainLoader.GetProgress() * 100));
                loadingText.text = $"Generating terrain ({progressPercent}%)\n\n\nSeed: \"{_levelDataAs.terrainSeed}\"";

                yield return null;
            }
            
            // terrain loaded, if we need to dynamically place the ship let's do that now
            if (dynamicPlacement) {
                // move the player up high and perform 5 raycasts - one from each corner of the ship and one from the centre.
                // move the player to the closest one, height-wise.
                // TODO :| (and remove the set of 2100 from the free roam menu class! You're welcome, future me!)
            }
        }

        // unload the loading screen
        var unload = SceneManager.UnloadSceneAsync("Loading");
        while (!unload.isDone) {
            yield return null;
        }

        // resume the game
        Time.timeScale = 1;
        FadeFromBlack();
        yield return new WaitForSeconds(0.7f);
        
        // if there's a track in the game world, start it (prevent collision starting timer during load for laps)
        if (track) {
            track.TrackReady();
        }

        // enable user input
        if (user != null) {
            user.EnableGameInput();
        }
        scenesLoading.Clear();
    }
}
