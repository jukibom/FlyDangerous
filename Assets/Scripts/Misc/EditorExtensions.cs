#if UNITY_EDITOR
using UnityEditor;

namespace Misc {
    public static class EditorExtensions {
        public static class ForceReserializeAll
        {
            [MenuItem("Tools/Force Reserialize All")]
            public static void Run()
            {
                AssetDatabase.ForceReserializeAssets();
            }
        }
    }
}
#endif