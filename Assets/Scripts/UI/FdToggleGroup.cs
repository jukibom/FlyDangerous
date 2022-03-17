using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    [RequireComponent(typeof(ToggleGroup))]
    public class FdToggleGroup : MonoBehaviour {
        [SerializeField] private string preference;
        [SerializeField] private List<FdToggle> options = new();

        public string Preference => preference;
        public string Value => 
            GetComponent<ToggleGroup>()
                .ActiveToggles()
                .ToList()
                .Find(t => t.isOn)
                .GetComponent<FdToggle>()?.Value ?? throw new Exception("FdToggle script missing on child Toggle!");
    }
}
