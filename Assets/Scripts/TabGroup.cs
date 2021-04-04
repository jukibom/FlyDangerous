using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabGroup : MonoBehaviour
{
    public List<TabButton> tabButtons = new List<TabButton>();

    public void Subscribe(TabButton button) {
        tabButtons.Add(button);
        OnTabSelected(button);
    }

    public void OnTabSelected(TabButton button) {
        this.ResetTabs();
        button.SetSelectedState(true);
    }

    private void ResetTabs() {
        tabButtons.ForEach(tab => {
            tab.SetSelectedState(false);
        });
    }
}
