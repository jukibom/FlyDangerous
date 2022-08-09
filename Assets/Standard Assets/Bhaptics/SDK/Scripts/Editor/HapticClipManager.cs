using System;
using UnityEngine;
using System.IO;
using UnityEditor;
using Application = UnityEngine.Application;


namespace Bhaptics.Tact.Unity
{
    [ExecuteInEditMode]
    public class HapticClipManager : ScriptableObject
    {

        private static HapticDeviceType GetMappedDeviceType(string clipType)
        {
            if (clipType == "")
            {
                return HapticDeviceType.None;
            }
            switch (clipType)
            {
                case BhapticsUtils.TypeHead:
                case BhapticsUtils.TypeTactal:
                    return HapticDeviceType.Tactal;

                case BhapticsUtils.TypeVest:
                case BhapticsUtils.TypeTactot:
                    return HapticDeviceType.TactSuit;

                case BhapticsUtils.TypeTactosy:
                case BhapticsUtils.TypeTactosy2:
                    return HapticDeviceType.Tactosy_arms;

                case BhapticsUtils.TypeHand:
                    return HapticDeviceType.Tactosy_hands;

                case BhapticsUtils.TypeFoot:
                    return HapticDeviceType.Tactosy_feet;

                case BhapticsUtils.TypeGlove:
                    return HapticDeviceType.TactGlove;

                default:
                    return HapticDeviceType.None;
            }
        }


        public static void RefreshTactFiles()
        {
            string extension = "*.tact";
            string[] tactFiles = Directory.GetFiles("Assets/", extension, SearchOption.AllDirectories);
            for (int i = 0; i < tactFiles.Length; ++i)
            {
                var fileName = Path.GetFileNameWithoutExtension(tactFiles[i]);
                EditorUtility.DisplayProgressBar("Hold on", "Converting " + fileName + ".tact -> "
                                                + fileName + ".asset (" + i + " / " + tactFiles.Length + ")", i / (float)tactFiles.Length);
                CreateTactClip(tactFiles[i]);
            }
            EditorUtility.ClearProgressBar();
        }


        private static void CreateTactClip(string tactFilePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(tactFilePath);
                if (fileName == null)
                {
                    BhapticsLogger.LogError("File name is null. Path: ");
                    return;
                }
                string json = LoadJsonStringFromFile(tactFilePath);
                var file = CommonUtils.ConvertJsonStringToTactosyFile(json);
                // var fileHash = GetHash(tactFilePath);
                var clipPath = tactFilePath.Replace(".tact", ".asset");

                if (File.Exists(clipPath))
                {
                    clipPath = GetUsableFileName(clipPath);
                    if (clipPath == null)
                    {
                        BhapticsLogger.LogError("File duplicated. Path: " + tactFilePath);
                        return;
                    }
                }
                clipPath = ConvertToAssetPathFromAbsolutePath(clipPath);

                HapticDeviceType type = GetMappedDeviceType(file.Project.Layout.Type);

                FileHapticClip tactClip;
                if (type == HapticDeviceType.TactSuit)
                {
                    tactClip = CreateInstance<VestHapticClip>();
                } else if (type == HapticDeviceType.Tactal)
                {
                    tactClip = CreateInstance<HeadHapticClip>();
                } else if (type == HapticDeviceType.Tactosy_arms)
                {
                    tactClip = CreateInstance<ArmsHapticClip>();
                } else if (type == HapticDeviceType.Tactosy_hands)
                {
                    tactClip = CreateInstance<HandsHapticClip>();
                }else if (type == HapticDeviceType.Tactosy_feet)
                {
                    tactClip = CreateInstance<FeetHapticClip>();
                }else if (type == HapticDeviceType.TactGlove)
                {
                    tactClip = CreateInstance<GloveHapticClip>();
                }
                else
                {
                    tactClip = CreateInstance<FileHapticClip>();
                }

                tactClip.JsonValue = json;
                tactClip.ClipType = type;

                File.Delete(tactFilePath);
                AssetDatabase.CreateAsset(tactClip, clipPath);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                BhapticsLogger.LogError("Failed to read tact file. Path: " + tactFilePath + "\n" + e.Message);
            }
        }

        private static string GetUsableFileName(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }
            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            if (name == null || extension == null)
            {
                return null;
            }
            for (int i = 1; i < 1000; ++i)
            {
                var res = path.Replace(name + extension, name + " " + i + extension);
                if (!File.Exists(res))
                {
                    return res;
                }
            }
            return null;
        }

        private static string ConvertToAssetPathFromAbsolutePath(string absolutePath)
        {
            absolutePath = absolutePath.Replace("\\", "/");
            if (absolutePath.StartsWith(Application.dataPath))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            else if (absolutePath.StartsWith("Assets/"))
            {
                return absolutePath;
            }
            else
            {
                BhapticsLogger.LogError("Path is not absolutePath");
                return null;
            }
        }

        private static string LoadJsonStringFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return json;
        }

        [MenuItem("Bhaptics/.tact files -> HapticClips")]
        private static void OnClickRefreshAssetFiles()
        {
            RefreshTactFiles();
        }

        [MenuItem("Bhaptics/HapticClips -> .tact files")]
        private static void OnClickRefreshTactFiles()
        {
            var saveAsPath = EditorUtility.SaveFolderPanel("Save as *.tact File", @"\download\", "");

            if (!string.IsNullOrEmpty(saveAsPath))
            {
                var allInstances = GetAllInstances<FileHapticClip>();

                foreach (var ins in allInstances)
                {
                    var path = saveAsPath + @"\" + Path.GetDirectoryName(AssetDatabase.GetAssetPath(ins.GetInstanceID()));
                    path = path.Replace("Assets/", "");

                    if (!Directory.Exists(path))
                    {
                        //if it doesn't, create it
                        Directory.CreateDirectory(path);
                    }

                    File.WriteAllText(path + "\\" + ins.name + ".tact", ins.JsonValue);
                }

                BhapticsLogger.LogInfo(".tact files saved count: {0}\n path: {1}", allInstances.Length ,saveAsPath);
            }
            else
            {
                BhapticsLogger.LogError("Folder not selected.");
            }
        }

        public static T[] GetAllInstances<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);  //FindAssets uses tags check documentation for more info
            T[] a = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++)         //probably could get optimized 
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return a;

        }
    }
}
