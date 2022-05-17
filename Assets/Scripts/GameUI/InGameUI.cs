using System;
using Core;
using Core.ShipModel;
using Game_UI;
using GameUI.GameModes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameUI {
    public enum GameUIMode {
        Pancake,
        VR
    }

    public class InGameUI : MonoBehaviour, IPointerMoveHandler {
        [SerializeField] private Canvas screenSpaceCanvas;
        [SerializeField] private Canvas worldSpaceCanvas;
        [SerializeField] private DebugUI debugUI;
        [SerializeField] private PauseSystem pauseSystem;
        [SerializeField] private ShipStats shipStats;
        [SerializeField] private MouseWidget mouseWidgetWorldSpace;
        [SerializeField] private MouseWidget mouseWidgetScreenSpace;
        [SerializeField] private TargettingSystem targettingSystem;
        [SerializeField] private GameModeUIHandler gameModeUIHandler;
        [SerializeField] private CursorIcon cursor;
        [SerializeField] private Camera vrMouseCamera;

        public DebugUI DebugUI => debugUI;
        public PauseSystem PauseSystem => pauseSystem;
        public ShipStats ShipStats => shipStats;
        public MouseWidget MouseWidgetWorld => mouseWidgetWorldSpace;
        public MouseWidget MouseWidgetScreen => mouseWidgetScreenSpace;
        public TargettingSystem TargettingSystem => targettingSystem;
        public GameModeUIHandler GameModeUIHandler => gameModeUIHandler;

        private void Awake() {
            CursorIsActive(false);
        }

        private void OnEnable() {
            Game.OnPauseToggle += CursorIsActive;
        }

        private void OnDisable() {
            Game.OnPauseToggle -= CursorIsActive;
        }

        public void OnPointerMove(PointerEventData eventData) {
            if (
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    screenSpaceCanvas.GetComponent<RectTransform>(),
                    eventData.position,
                    eventData.enterEventCamera,
                    out var canvasPosition)
            )
                cursor.SetLocalPosition(canvasPosition);
        }

        public void CursorIsActive(bool isActive) {
            cursor.gameObject.SetActive(isActive);
            cursor.SetLocalPosition(Vector2.zero);
        }

        public void OnGameMenuToggle() {
            if (!pauseSystem.IsPaused) pauseSystem.OnGameMenuToggle();
        }

        public void SetMode(GameUIMode mode) {
            var screenSpaceRect = screenSpaceCanvas.GetComponent<RectTransform>();

            switch (mode) {
                case GameUIMode.VR: {
                    screenSpaceCanvas.renderMode = RenderMode.WorldSpace;
                    screenSpaceCanvas.worldCamera = vrMouseCamera;
                    screenSpaceRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                    screenSpaceRect.localRotation = Quaternion.identity;
                    screenSpaceRect.localPosition = new Vector3(0, 0.3f, 0.5f);
                    screenSpaceRect.sizeDelta = new Vector2(1280, 1000);
                    // we rely on the cockpit UI in VR mode!
                    ShipStats.SetStatsVisible(false);
                    break;
                }
                case GameUIMode.Pancake: {
                    var uiCamera = GameObject.FindGameObjectWithTag("UICamera")?.GetComponent<Camera>();
                    if (uiCamera) {
                        screenSpaceCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                        screenSpaceCanvas.worldCamera = uiCamera;
                        screenSpaceRect.localScale = Vector3.one;
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