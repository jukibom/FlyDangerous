using System;
using System.Linq;
using Core;
using Menus.Options;
using UnityEngine;

namespace Menus.Main_Menu {
    public class MultiplayerNoticeMenu : MenuBase {
        
        [SerializeField] private MultiPlayerMenu multiPlayerMenu;
        [SerializeField] private ServerBrowserMenu serverBrowserMenu;

        public void Continue() {
            // progress but set the cancel operation to the caller of this menu
            if (FdNetworkManager.Instance.HasMultiplayerServices) // we have some online services hooked up, load the game browser
                Progress(serverBrowserMenu, true, true, caller);
            else // revert to old-school
                Progress(multiPlayerMenu,true, true, caller);
        }
    }
}