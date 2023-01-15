using System.Collections.Generic;
using UnityEngine;

namespace FdUI {
    public class TabGroup : MonoBehaviour {
        public List<TabButton> tabButtons = new();
        public TabButton defaultTab;

        public delegate void TabSelectedAction(string tabId);

        public event TabSelectedAction OnTabSelected;


        public void Start() {
            tabButtons.ForEach(tab => tab.Subscribe(this));
            if (defaultTab != null) SelectTab(defaultTab);
        }

        public void SelectTab(TabButton button) {
            ResetTabs();
            button.SetSelectedState(true);
            OnTabSelected?.Invoke(button.TabId);
        }

        private void ResetTabs() {
            tabButtons.ForEach(tab => { tab.SetSelectedState(false); });
        }
    }
}