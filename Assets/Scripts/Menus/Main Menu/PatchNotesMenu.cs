using System;
using System.Linq;
using Core;
using Menus.Options;
using UnityEngine;

namespace Menus.Main_Menu {
    public class PatchNotesMenu : MenuBase {
        protected void Start() {
            GetComponentsInChildren<ExpandingLine>(true).ToList().First().IsOpen = true;
        }

        public void ClosePanel() {
            Cancel();
        }
    }
}