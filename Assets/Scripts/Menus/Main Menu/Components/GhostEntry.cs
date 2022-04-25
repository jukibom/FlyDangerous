using Core.Replays;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class GhostEntry : MonoBehaviour {
        [SerializeField] public Text playerName;
        [SerializeField] public Text entryDate;
        [SerializeField] public Text score;
        [SerializeField] public Checkbox checkbox;
        public Replay replay;
    }
}