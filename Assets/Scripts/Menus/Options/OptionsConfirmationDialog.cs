using Menus.Main_Menu;
using UnityEngine;

namespace Menus.Options {
    public class OptionsConfirmationDialog : MenuBase {
        [SerializeField] private OptionsMenu optionsMenu;

        public void Discard() {
            optionsMenu.Cancel(true);
            PlayApplySound();
            Hide();
        }

        public void Save() {
            optionsMenu.Apply();
            PlayApplySound();
            Hide();
        }
    }
}