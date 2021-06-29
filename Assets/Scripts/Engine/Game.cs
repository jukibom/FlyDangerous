using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Den.Tools;
using Engine;
using JetBrains.Annotations;
using MapMagic.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Environment = Engine.Environment;

public class Game : MonoBehaviour {

    public static Game Instance;

    public delegate void RestartLevelAction();
    public delegate void GraphicsSettingsApplyAction();
    public delegate void VRToggledAction(bool enabled);
    public static event RestartLevelAction OnRestart;
    public static event GraphicsSettingsApplyAction OnGraphicsSettingsApplied;
    public static event VRToggledAction OnVRStatus;
    
    [SerializeField] private InputActionAsset playerBindings;
    [SerializeField] private ScriptableRendererFeature ssao;
    [CanBeNull] private ShipParameters _shipParameters;
    private Vector3 _hmdPosition;
    private Quaternion _hmdRotation;
    private LevelLoader _levelLoader;

    // The level data most recently used to load a map
    public LevelData LoadedLevelData => _levelLoader.LoadedLevelData;
    
    // The level data hydrated with the current player position and track layout
    public LevelData LevelDataAtCurrentPosition => _levelLoader.LevelDataAtCurrentPosition;
    
    private bool _isVREnabled = false;
    public bool IsVREnabled {
        get => _isVREnabled;
    }

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

    public bool IsTerrainMap => 
        _levelLoader.LoadedLevelData.location == Location.TerrainV1 || 
        _levelLoader.LoadedLevelData.location == Location.TerrainV2;
    public string Seed => _levelLoader.LoadedLevelData.terrainSeed;
    
    [SerializeField] private Animator crossfade;

    // show certain things if first time hitting the menu
    private bool _menuFirstRun = true;
    public bool menuFirstRun => _menuFirstRun;

    void Awake() {
        // singleton shenanigans
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
            return;
        }
    }

    public void Start() {
        // must be a level loader in the scene
        _levelLoader = FindObjectOfType<LevelLoader>();
        
        // if there's a user object when the game starts, enable input (usually in the editor!)
        FindObjectOfType<User>()?.EnableGameInput();
        LoadBindings();
        ApplyGraphicsOptions();
        
        // We use a custom canvas cursor to work in VR and pancake
        Cursor.visible = false;

        // check for command line args
        var args = System.Environment.GetCommandLineArgs();
        if (args.ToList().Contains("-vr") || args.ToList().Contains("-VR")) {
            EnableVR();
        }
    }

    private void OnDestroy() {
        DisableVRIfNeeded();
    }

    private void OnApplicationQuit() {
        DisableVRIfNeeded();
    }

    public void LoadBindings() {
        var bindings = Preferences.Instance.GetString("inputBindings");
        if (!string.IsNullOrEmpty(bindings)) {
            playerBindings.LoadBindingOverridesFromJson(bindings);
        }
    }

    public void ApplyGraphicsOptions() {
        if (OnGraphicsSettingsApplied != null) {
            OnGraphicsSettingsApplied();
        }
        
        var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        urp.renderScale = Preferences.Instance.GetFloat("graphics-render-scale");
        var msaa = Preferences.Instance.GetString("graphics-anti-aliasing");
        switch (msaa) {
            case "8x": urp.msaaSampleCount = 8;
                break;
            case "4x": urp.msaaSampleCount = 4;
                break;
            case "2x": urp.msaaSampleCount = 2;
                break;
            case "none":
            case "default":
                urp.msaaSampleCount = 0;
                break;
        }
        ssao.SetActive(Preferences.Instance.GetBool("graphics-ssao"));
    }

    public void EnableVR() {
        IEnumerator StartXR() {
            yield return UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.InitializeLoader();
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.StartSubsystems();
            _isVREnabled = true;
            NotifyVRStatus();
        }

        StartCoroutine(StartXR());
    }

    public void DisableVRIfNeeded() {
        if (IsVREnabled) {
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.StopSubsystems();
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            _isVREnabled = false;
            NotifyVRStatus();
        }
    }

    public void ResetHMDView(XRRig xrRig, Transform targetTransform) {
        var before = xrRig.transform.position;
        xrRig.MoveCameraToWorldLocation(targetTransform.position);
        xrRig.MatchRigUpCameraForward(targetTransform.up, targetTransform.forward);
        _hmdRotation = xrRig.transform.rotation;
        _hmdPosition += xrRig.transform.position - before;
    }

    public void StartGame(LevelData levelData, bool dynamicPlacementStart = false) {
        LockCursor();
        _levelLoader.StartGame(levelData, _shipParameters, dynamicPlacementStart);
    }

    public void RestartLevel() {
        _levelLoader.RestartLevel(() => {
            if (OnRestart != null) OnRestart();
        });
    }

    public void QuitToMenu() {
        _menuFirstRun = false;
        _levelLoader.StopTerrainGenerationIfApplicable();
        var user = FindObjectOfType<User>();
        user.DisableGameInput();
        
        IEnumerator LoadMenu() {
            FadeToBlack();
            yield return new WaitForSeconds(0.5f);
            yield return SceneManager.LoadSceneAsync("Main Menu");
            _levelLoader.ResetLoadedLevelData();
            ApplyGraphicsOptions();
            NotifyVRStatus();
            FadeFromBlack();
            FreeCursor();
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

    public void FreeCursor() {
        Cursor.lockState = CursorLockMode.None;
    }

    public void LockCursor() {
        Cursor.lockState = CursorLockMode.Confined;
    }
    
    public void FadeToBlack() {
        crossfade.SetTrigger("FadeToBlack");
    }

    public void FadeFromBlack() {
        crossfade.SetTrigger("FadeFromBlack");
    }

    public void NotifyVRStatus() {
        if (OnVRStatus != null) {
            OnVRStatus(IsVREnabled);
        }
        
        // if user has previously applied a HMD position, reapply
        if (IsVREnabled) {
            var xrRig = FindObjectOfType<XRRig>();
            if (xrRig) {
                xrRig.transform.rotation = _hmdRotation;
                xrRig.transform.localPosition = xrRig.transform.localPosition + _hmdPosition;
            }
        }
    }
}
