#if !DISABLESTEAMWORKS
using System;
using System.IO;
using Steamworks;
using UnityEngine;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamFileStore : IOnlineFile {
        public string Filename { get; }
        public MemoryStream Data { get; }
        public SteamFileStore(RemoteStorageDownloadUGCResult_t downloadResult) {
            Filename = downloadResult.m_pchFileName;
            
            byte[] data = new byte[downloadResult.m_nSizeInBytes];
            SteamRemoteStorage.UGCRead(downloadResult.m_hFile, data, downloadResult.m_nSizeInBytes, 0, EUGCReadAction.k_EUGCRead_Close);
            Data = new MemoryStream(data, false);
        }

        ~SteamFileStore() {
            Debug.Log("Free replay file memory");
            Data.Flush();
            Data.Close();
        }
    }
}
#endif