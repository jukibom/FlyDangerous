using System.Collections;
using Core;
using Core.Player;
using GameUI;
using Menus.Main_Menu;
using Menus.Options;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menus.Pause_Menu {
    public class PauseMenu : MenuBase {
        [SerializeField] private OptionsMenu optionsPanel;
        [SerializeField] private Text copyConfirmationText;
        [SerializeField] private Text seedText;

        [SerializeField] private PauseSystem pauseSystem;

        [SerializeField] private Button resumeButton;

        [SerializeField] private Button restartButton;

        [SerializeField] private Button optionsButton;

        [SerializeField] private Button quitButton;

        public void OnEnable() {
            // multiplayer specific UI changes
            var player = FdPlayer.FindLocalShipPlayer;
            if (player && Game.Instance.SessionType == SessionType.Multiplayer) {
                // in free roam, restart for clients is changed to warping to the leader (on non-host client)
                if (!player.isHost && Game.Instance.LoadedLevelData.gameType.CanWarpToHost) restartButton.GetComponent<UIButton>().label.text = "WARP TO HOST";
                quitButton.GetComponent<UIButton>().label.text = "LEAVE GAME";
                if (player.isHost) quitButton.GetComponent<UIButton>().label.text = "RETURN TO LOBBY";
            }

            seedText.text = Game.Instance.IsTerrainMap ? "SEED: " + Game.Instance.Seed : "";
        }

        public void Resume() {
            Cancel();
            pauseSystem.Resume();
        }

        public void Restart() {
            PlayApplySound();
            pauseSystem.Restart();
        }

        public void Options() {
            Progress(optionsPanel);
        }

        public void Quit() {
            PlayApplySound();
            pauseSystem.Quit();
        }

        public void CopyLocationToClipboard() {
            PlayApplySound();
            GUIUtility.systemCopyBuffer = Game.Instance.LevelDataAtCurrentPosition.ToJsonString();
            var copyConfirmTransform = copyConfirmationText.transform;
            copyConfirmTransform.localPosition = new Vector3(copyConfirmTransform.localPosition.x, 55, copyConfirmTransform.position.z);
            copyConfirmationText.color = new Color(1f, 1f, 1f, 1f);

            IEnumerator FadeText() {
                while (copyConfirmationText.color.a > 0.0f) {
                    copyConfirmationText.color = new Color(1f, 1f, 1f, copyConfirmationText.color.a - Time.unscaledDeltaTime);

                    var localPosition = gameObject.transform.localPosition;
                    copyConfirmTransform.localPosition = new Vector3(
                        localPosition.x + 160,
                        copyConfirmationText.gameObject.transform.localPosition.y + Time.unscaledDeltaTime * 20,
                        localPosition.z
                    );
                    yield return null;
                }
            }

            StartCoroutine(FadeText());
        }

        // if the user quick-closes with B button or ESC or whatever
        public override void OnCancel(BaseEventData eventData) {
            Resume();
        }
    }
}