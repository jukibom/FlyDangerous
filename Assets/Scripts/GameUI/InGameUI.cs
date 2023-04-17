using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.ShipModel;
using FdUI;
using Game_UI;
using GameUI.GameModes;
using Misc;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameUI {
    public class InGameUI : MonoBehaviour, IPointerMoveHandler {
        [SerializeField] private Canvas screenSpaceCanvas;
        [SerializeField] private Canvas worldSpaceCanvas;
        [SerializeField] private DebugUI debugUI;
        [SerializeField] private PauseSystem pauseSystem;
        [SerializeField] private ShipStats shipStats;
        [SerializeField] private MouseWidget mouseWidget;
        [SerializeField] private TargettingSystem targettingSystem;
        [SerializeField] private IndicatorSystem indicatorSystem;
        [SerializeField] private GameModeUIHandler gameModeUIHandler;
        [SerializeField] private CursorIcon cursor;
        [SerializeField] private Camera vrMouseCamera;

        [Tooltip("All images in the UI which respect the users' UI color preference")] [SerializeField]
        private List<Image> colourTintImages;

        [Tooltip("All text in the UI which respect the users' UI color preference")] [SerializeField]
        private List<Text> colourTintTextElements;

        private Camera uiCamera;

        public DebugUI DebugUI => debugUI;
        public PauseSystem PauseSystem => pauseSystem;
        public ShipStats ShipStats => shipStats;
        public MouseWidget MouseWidget => mouseWidget;
        public TargettingSystem TargettingSystem => targettingSystem;
        public IndicatorSystem IndicatorSystem => indicatorSystem;
        public GameModeUIHandler GameModeUIHandler => gameModeUIHandler;

        private void Awake() {
            OnPauseToggle(false);
        }

        private void Start() {
            var cameras = FindObjectsOfType<Camera>(true).ToList();
            uiCamera = cameras.Find(c => c.CompareTag("UICamera"));
        }

        private void OnEnable() {
            Game.OnPauseToggle += OnPauseToggle;
            Game.OnVRStatus += SetVRStatus;
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnPauseToggle -= OnPauseToggle;
            Game.OnVRStatus -= SetVRStatus;
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
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

        public void OnPauseToggle(bool isPaused) {
            cursor.gameObject.SetActive(isPaused);
            cursor.SetLocalPosition(Vector2.zero);
            worldSpaceCanvas.enabled = !isPaused;
        }

        public void OnGameMenuToggle() {
            if (!pauseSystem.IsPaused) pauseSystem.OnGameMenuToggle();
        }

        private void SetVRStatus(bool isVREnabled) {
            // if VR is enabled, we need to swap our active cameras and make UI panels operate in world space
            var screenSpaceRect = screenSpaceCanvas.GetComponent<RectTransform>();

            if (isVREnabled) {
                screenSpaceCanvas.renderMode = RenderMode.WorldSpace;
                screenSpaceCanvas.worldCamera = vrMouseCamera;
                screenSpaceRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                screenSpaceRect.localRotation = Quaternion.identity;
                screenSpaceRect.localPosition = new Vector3(0, 0.3f, 0.5f);
                screenSpaceRect.sizeDelta = new Vector2(1920, 1440f); // 4:3
                // we rely on the cockpit UI in VR mode!
                ShipStats.SetStatsVisible(false);
            }
            else {
                if (uiCamera) {
                    screenSpaceCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    screenSpaceCanvas.worldCamera = uiCamera;
                    screenSpaceRect.localScale = Vector3.one;
                }
                else {
                    throw new Exception("Failed to find UI camera while switching VR mode!");
                }
            }
        }

        private void OnGameSettingsApplied() {
            foreach (var colourTintImage in colourTintImages) {
                var htmlColor = Preferences.Instance.GetString("playerHUDIndicatorColor");
                colourTintImage.color = ColorExtensions.ParseHtmlColor(htmlColor);
            }

            foreach (var colourTintText in colourTintTextElements) {
                var htmlColor = Preferences.Instance.GetString("playerHUDIndicatorColor");
                colourTintText.color = ColorExtensions.ParseHtmlColor(htmlColor);
            }
        }
    }
}