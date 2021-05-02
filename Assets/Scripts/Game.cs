using System.Collections;
using System.Collections.Generic;
using Den.Tools;
using MapMagic.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : MonoBehaviour {

    public static Game Instance;

    public string seed = "123456";
    public bool isTerrainMap = false;
    
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

    public void StartGame(string mapScene) {
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
            StartCoroutine(LoadGameScenes(loadText));
        }
        
        StartCoroutine(SwitchToLoadingScreen());
    }

    public void RestartLevel() {
        // Todo: record player initial state and load it here instead of this scene juggling farce which takes ages to load
        StopTerrainGeneration();
        StartGame(SceneManager.GetActiveScene().name);
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
        isTerrainMap = false;
    }
    
    IEnumerator LoadGameScenes(Text loadingText) {
        
        float progress = 0;
        float totalProgress = 0;
        for (int i = 0; i < scenesLoading.Count; ++i) {
            while (scenesLoading[i].progress < 0.9f) { // this is literally what the unity docs recommend
                yield return null;
                    
                progress += scenesLoading[i].progress;
                totalProgress = progress / scenesLoading.Count;

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
        
        // if terrain needs to generate, toggle special logic and wait for it to load all primary tiles
        var terrainLoader = FindObjectOfType<MapMagicObject>();
        if (terrainLoader) {
            terrainLoader.StopGenerate();
            terrainLoader.graph.random = new Noise(seed.GetHashCode(), 32768);
            terrainLoader.StartGenerate();
            while (terrainLoader.IsGenerating()) {
                var progressPercent = Mathf.Min(100, Mathf.Round(terrainLoader.GetProgress() * 100));
                loadingText.text = $"Generating terrain ({progressPercent}%)\n\n\nSeed: \"{seed}\"";

                yield return null;
            }
        }
        
        // unload the loading screen
        var unload = SceneManager.UnloadSceneAsync("Loading");
        while (!unload.isDone) {
            yield return null;
        }
        
        // disable user input while fading back out (pause screen can pause fade animation!)
        var user = FindObjectOfType<User>();
        if (user != null) {
            user.DisableGameInput();
            user.DisableUIInput();
        }
        
        FadeFromBlack();
        yield return new WaitForSeconds(0.6f);

        // enable user input
        if (user != null) {
            user.EnableGameInput();
        }
        scenesLoading.Clear();
    }
}
