using System.Collections.Generic;
using UnityEngine;

namespace UI {
    public class TabGroup : MonoBehaviour {
        public List<TabButton> tabButtons = new();
        public TabButton defaultTab;

        public void Start() {
            tabButtons.ForEach(tab => tab.Subscribe(this));
            if (defaultTab != null) OnTabSelected(defaultTab);
        }

        public void OnTabSelected(TabButton button) {
            ResetTabs();
            button.SetSelectedState(true);
        }

        private void ResetTabs() {
            tabButtons.ForEach(tab => { tab.SetSelectedState(false); });
        }
    }
}