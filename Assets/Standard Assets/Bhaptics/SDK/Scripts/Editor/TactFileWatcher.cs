using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Bhaptics.Tact.Unity
{
    public class TactFileWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                BhapticsLogger.LogDebug("Import Assets");
                HapticClipManager.RefreshTactFiles();
            }
            foreach (string str in deletedAssets)
            {
                BhapticsLogger.LogDebug("Delete Assets");
                if (Directory.GetFiles("Assets/", "*.tact", SearchOption.AllDirectories).Length != 0)
                {
                    HapticClipManager.RefreshTactFiles();
                }
            }
        }
    }
}
