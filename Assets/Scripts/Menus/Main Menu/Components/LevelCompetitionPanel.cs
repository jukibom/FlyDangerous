using Core.MapData;
using UnityEngine;

namespace Menus.Main_Menu.Components {
    public class LevelCompetitionPanel : MonoBehaviour {
        [SerializeField] private GhostList ghostList;
        public void PopulateGhostsForLevel(Level level) {
            ghostList.PopulateGhostsForLevel(level);
        }
    }
}
