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

        public DebugUI DebugUI => debugUI;
        public PauseMenu PauseMenu => pauseMenu;
        public ShipStats ShipStats => shipStats;
        public Timers Timers => timers;
        public MouseWidget MouseWidgetWorld => mouseWidgetWorldSpace;
        public MouseWidget MouseWidgetScreen => mouseWidgetScreenSpace;
        public TargettingSystem TargettingSystem => targettingSystem;

        public void SetMode(GameUIMode mode) {
            switch (mode) {
                case GameUIMode.VR: {
                    var pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
                    pauseMenuCanvas.renderMode = RenderMode.WorldSpace;
                    screenSpaceCanvas.renderMode = RenderMode.WorldSpace;
                    var pauseMenuRect = pauseMenuCanvas.GetComponent<RectTransform>();
                    var uiRect = screenSpaceCanvas.GetComponent<RectTransform>();

                    pauseMenuRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                    uiRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

                    pauseMenuRect.localRotation = Quaternion.identity;
                    uiRect.localRotation = Quaternion.identity;

                    pauseMenuRect.localPosition = new Vector3(0, 0.3f, 0.5f);
                    uiRect.localPosition = new Vector3(0, 0.3f, 0.5f);

                    pauseMenuRect.sizeDelta = new Vector2(1280, 1000);
                    uiRect.sizeDelta = new Vector2(1280, 1000);
                    break;
                }
                case GameUIMode.Pancake: {
                    var pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
                    pauseMenuCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    screenSpaceCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    var pauseMenuRect = pauseMenuCanvas.GetComponent<RectTransform>();
                    var uiRect = screenSpaceCanvas.GetComponent<RectTransform>();

                    pauseMenuRect.localScale = Vector3.one;
                    uiRect.localScale = Vector3.one;
                    pauseMenuRect.position = Vector3.zero;
                    pauseMenuRect.sizeDelta = new Vector2(1920, 1080);
                    break;
                }
            }
        }
    }
}