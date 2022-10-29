using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Den.Tools
{

	public static class AssemblePlugins
	{



		[MenuItem("Assets/Assemble MapMagic")]
		private static void AssembleMapMagic ()
		{
			string path = EditorUtility.SaveFolderPanel("Assemble MapMagic To", "", "MapMagic");

			//Directory.Delete(path, true);
			//Debug.Log(AssetDatabase.proj
		}


		private static void CopyFile (string fromFolder, string toFolder, string name)
		{
			
		}


		private static void CopyFolder (string fromFolder, string toFolder, string name)
		{

		}
	}

}
