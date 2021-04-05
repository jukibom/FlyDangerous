using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI {
    public class TabGroup : MonoBehaviour {
        public List<TabButton> tabButtons = new List<TabButton>();
        public TabButton defaultTab;

        public void Start() {
            tabButtons.ForEach(tab => tab.Subscribe(this));
            if (this.defaultTab != null) {
                this.OnTabSelected(this.defaultTab);
            }
        }

        public void OnTabSelected(TabButton button) {
            this.ResetTabs();
            button.SetSelectedState(true);
        }

        private void ResetTabs() {
            tabButtons.ForEach(tab => { tab.SetSelectedState(false); });
        }
    }
}