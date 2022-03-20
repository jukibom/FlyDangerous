using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    [RequireComponent(typeof(ToggleGroup))]
    public class FdToggleGroup : MonoBehaviour {
        [SerializeField] private string preference;

        public string Preference => preference;

        public string Value {
            get => GetComponentsInChildren<FdToggle>()
                .ToList()
                .Find(t => t.GetComponent<Toggle>().isOn)
                .GetComponent<FdToggle>().Value;

            set => GetComponentsInChildren<FdToggle>()
                .ToList()
                .Find(t => t.GetComponent<FdToggle>().Value == value)
                .GetComponent<Toggle>()
                .isOn = true;
        }
    }
}