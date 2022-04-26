using Core;

namespace Misc {
    public static class Player {
        public static string LocalPlayerName {
            // if using online services, grab the name from there.
            get {
                if (FdNetworkManager.Instance.HasOnlineServices) {
                    return FdNetworkManager.Instance.OnlineService!.PlayerName;
                }
                return Preferences.Instance.GetString("playerName");
            }
        }

        public static bool IsUsingOnlineName => FdNetworkManager.Instance.HasOnlineServices;
    }
}