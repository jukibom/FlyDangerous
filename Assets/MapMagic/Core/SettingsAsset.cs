using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapMagic.Core
{
	public class SettingsAsset : ScriptableObject
	{
		public static SettingsAsset instance;

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] 
		static void Load ()
		{
			/*SettingsAsset loadedInstance = Resources.Load<SettingsAsset>("MapMagicSettings");

			if (loadedInstance==null)
			{
				loadedInstance = 
			}*/
		}
	}
}
