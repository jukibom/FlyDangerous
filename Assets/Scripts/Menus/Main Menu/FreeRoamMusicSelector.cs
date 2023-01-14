using Audio;
using Misc;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menus.Main_Menu {
    public class FreeRoamMusicSelector : MonoBehaviour, ISelectHandler {
        public void OnSelect(BaseEventData eventData) {
            var musicTrack = FdEnum.FromDropdownSelectionEvent(MusicTrack.List(), eventData);
            MusicManager.Instance.PlayMusic(musicTrack, false, false, true);
        }
    }
}