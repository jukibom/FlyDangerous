using System;
using Core.ShipModel;
using Game_UI;
using UnityEngine;

namespace GameUI {
    public enum GameUIMode {
        Pancake,
        VR
    }

    public class InGameUI : MonoBehaviour {
        [SerializeField] private Canvas screenSpaceCanvas;
        [SerializeField] private Canvas worldSpaceCanvas;
        [SerializeField] private DebugUI debugUI;
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private ShipStats shipStats;
        [SerializeField] private Timers timers;
        [SerializeField] private MouseWidget mouseWidgetWorldSpace;
        [SerializeField] private MouseWidget mouseWidgetScreenSpace;
        [SerializeField] private TargettingSystem targettingSystem;
        [SerializeField] private Transform gameModeUI;
        [SerializeField] private Camera vrMouseCamera;

        public DebugUI DebugUI => debugUI;
        public PauseMenu PauseMenu => pauseMenu;
        public ShipStats ShipStats => shipStats;
        public Timers Timers => timers;
        public MouseWidget MouseWidgetWorld => mouseWidgetWorldSpace;
        public MouseWidget MouseWidgetScreen => mouseWidgetScreenSpace;
        public TargettingSystem TargettingSystem => targettingSystem;
        public Transform GameModeUI => gameModeUI;

        public void SetMode(GameUIMode mode) {
            var pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
            var pauseMenuRect = pauseMenuCanvas.GetComponent<RectTransform>();
            var uiRect = screenSpaceCanvas.GetComponent<RectTransform>();

            switch (mode) {
                case GameUIMode.VR: {
                    pauseMenuCanvas.renderMode = RenderMode.WorldSpace;
                    screenSpaceCanvas.renderMode = RenderMode.WorldSpace;
                    pauseMenuCanvas.worldCamera = vrMouseCamera;
                    screenSpaceCanvas.worldCamera = vrMouseCamera;

                    pauseMenuRect.pivot = new Vector2(0.5f, 0.5f);

                    pauseMenuRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                    uiRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

                    pauseMenuRect.localRotation = Quaternion.identity;
                    uiRect.localRotation = Quaternion.identity;

                    pauseMenuRect.localPosition = new Vector3(0, 0.3f, 0.5f);
                    uiRect.localPosition = new Vector3(0, 0.3f, 0.5f);

                    pauseMenuRect.sizeDelta = new Vector2(1440, 1080);
                    uiRect.sizeDelta = new Vector2(1280, 1000);

                    ShipStats.SetStatsVisible(false);
                    break;
                }
                case GameUIMode.Pancake: {
                    var uiCamera = GameObject.FindGameObjectWithTag("UICamera")?.GetComponent<Camera>();
                    if (uiCamera) {
                        pauseMenuCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                        screenSpaceCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                        pauseMenuCanvas.worldCamera = uiCamera;
                        screenSpaceCanvas.worldCamera = uiCamera;

                        pauseMenuRect.localScale = Vector3.one;
                        uiRect.localScale = Vector3.one;
                        pauseMenuRect.position = Vector3.zero;
                        pauseMenuRect.sizeDelta = new Vector2(1920, 1080);
                    }
                    else {
                        throw new Exception("Failed to find UI camera while switching VR mode!");
                    }

                    break;
                }
            }
        }
    }
}