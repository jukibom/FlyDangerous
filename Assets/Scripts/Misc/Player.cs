using Core;

namespace Misc {
    public static class Player {
        public static string LocalPlayerName =>
            // if using online services, grab the name from there.
            FdNetworkManager.Instance.HasOnlineServices ? FdNetworkManager.Instance.OnlineService!.PlayerName : Preferences.Instance.GetString("playerName");

        public static bool IsUsingOnlineName => FdNetworkManager.Instance.HasOnlineServices;
    }
}