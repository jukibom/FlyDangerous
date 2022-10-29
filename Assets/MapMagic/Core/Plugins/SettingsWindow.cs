
//using a different assembly to make it compile independently from MapMagic or other assets
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
//using System.Reflection;
using UnityEngine.Profiling;
using UnityEditor.Compilation;

//using Plugins;
//using Plugins.GUI;

//using MapMagic.Core;
//using MapMagic.Nodes;
//using MapMagic.Products;
//using MapMagic.Previews;

namespace MapMagic.GUI
{
	

	//[EditoWindowTitle(title = "MapMagic Settings")]  //it's internal Unity stuff
	public class SettingsWindow : EditorWindow
	{
		private struct Settings : ICloneable
		{
			public bool native;
			public bool debug;
			public bool experimental;

			public bool cts;
			public bool megaSplat;
			public bool microSplat;
			public bool rtp;
			public bool vsPro;

			public bool autoRef;

			public object Clone() => this.MemberwiseClone();
			public void DisableCompatibility () 
				{ cts=false; megaSplat=false; microSplat=false; rtp=false; vsPro=false; }
		}

		private Settings current;
		private Settings changed;

		//UI ui = new UI(); //using standard editor since settings supposed to be compiled before everything else (but not compiled)

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		static void InitializeSettings ()
		/// Initializes settings on first MM import
		{
			BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
			string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
			
			//removing beta marks
			if (symbols.Contains("_MAPMAGIC_BETA"))
				ToggleKeyword(false, "_MAPMAGIC_BETA", ref symbols);
			if (symbols.Contains("_MMNATIVE"))
				ToggleKeyword(false, "_MMNATIVE", ref symbols);

			if (!symbols.Contains("MAPMAGIC2"))
			{
				ToggleKeyword(true, "MAPMAGIC2", ref symbols); //for voxeland and other plugins compatibility

				#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
				ToggleKeyword(true, "MM_NATIVE", ref symbols);
				#endif

				PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
			}

			//ShowNet20Notification();
		}


		public void OnEnable ()
		{
			current = ReadCurrentSettings();
			changed = (Settings)current.Clone();
		}

		public void OnGUI ()
		{
			//ui.Draw(DrawGUI);
			DrawGUI();
		}

		public void DrawGUI ()
		{
			if (EditorApplication.isCompiling)
				EditorGUILayout.HelpBox("Compiling scripts. Please wait until compilation is finished", MessageType.None);

			//using (Cell.LineStd)
			EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.Space();
				using (new EditorGUILayout.VerticalScope())
				{
					BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
					string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

					EditorGUILayout.Space();

					EditorGUILayout.LabelField("Scripting Define Symbols");
					DrawToggle(current.native, ref changed.native, "C++ Native Code");
					DrawToggle(current.debug, ref changed.debug, "Debug Mode");
					DrawToggle(current.experimental, ref changed.experimental, "Experimental Features");

					//Cell.EmptyLinePx(5);
					EditorGUILayout.Space();

					EditorGUI.BeginDisabledGroup(!changed.autoRef);
					if (!changed.autoRef) 
						changed.DisableCompatibility();
					EditorGUILayout.LabelField("Compatibility");
					DrawToggle(current.cts, ref changed.cts, "CTS 2019");
					DrawToggle(current.megaSplat, ref changed.megaSplat, "MegaSplat");
					DrawToggle(current.microSplat, ref changed.microSplat, "MicroSplat");
					DrawToggle(current.rtp, ref changed.rtp, "Relief Terrain Pack");
					DrawToggle(current.vsPro, ref changed.vsPro, "Vegetation Studio Pro");
					EditorGUI.EndDisabledGroup();

					//Cell.EmptyLinePx(4);
					EditorGUILayout.Space();

					#if UNITY_2019_2_OR_NEWER
					DrawToggle(current.autoRef, ref changed.autoRef, "Assemblies Auto Ref");
					EditorGUILayout.HelpBox("Enable MM assemblies Auto Reference for compatibility with these or custom scripts. Disable for faster compile.", MessageType.None);
					#endif

					EditorGUILayout.Space();
					EditorGUILayout.LabelField("*: Fields marked with asterisk are not applied yet.");

					EditorGUILayout.Space();
					using (new EditorGUILayout.HorizontalScope())
					{
						//EditorGUILayout.Space(180);
						//doesnt work in Unity 2019

						EditorGUILayout.Space();
						EditorGUILayout.Space();
						EditorGUILayout.Space();

						if (GUILayout.Button("Apply"))
						{
							ApplySettings(changed);
							current = ReadCurrentSettings();
						}

						if (GUILayout.Button("Cancel"))
							Close();
					}
				}
				EditorGUILayout.Space();

				
				//EditorWindow.focusedWindow.Repaint();
				//AssetDatabase.Refresh();
			}
			EditorGUI.EndDisabledGroup();
		}


		static void DrawToggle (bool currEnabled, ref bool newEnabled, string label)
		{
			if (newEnabled != currEnabled)
				label += "*";

			newEnabled = EditorGUILayout.ToggleLeft(label, newEnabled);
		}


		static Settings ReadCurrentSettings ()
		{
			Settings settings = new Settings();

			BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
			string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

			settings.native = symbols.Contains("MM_NATIVE");
			settings.debug = symbols.Contains("MM_DEBUG");
			settings.experimental = symbols.Contains("MM_EXPERIMENTAL");

			settings.cts = symbols.Contains("CTS_PRESENT");
			settings.megaSplat = symbols.Contains("__MEGASPLAT__");
			settings.microSplat = symbols.Contains("__MICROSPLAT__");
			settings.rtp = symbols.Contains("RTP");
			settings.vsPro = symbols.Contains("VEGETATION_STUDIO_PRO");

			settings.autoRef = 
				GetAutoRef("MapMagic") &&
				GetAutoRef("MapMagic.Editor") &&
				GetAutoRef("Den.Tools") &&
				GetAutoRef("Den.Tools.Editor");

			return settings;
		}


		static void ApplySettings (Settings settings)
		{
			BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
			string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

			ToggleKeyword(settings.native, "MM_NATIVE", ref symbols);
			ToggleKeyword(settings.debug, "MM_DEBUG", ref symbols);
			ToggleKeyword(settings.experimental, "MM_EXPERIMENTAL", ref symbols);

			ToggleKeyword(settings.cts, "CTS_PRESENT", ref symbols);
			ToggleKeyword(settings.megaSplat, "__MEGASPLAT__", ref symbols);
			ToggleKeyword(settings.microSplat, "__MICROSPLAT__", ref symbols);
			ToggleKeyword(settings.rtp, "RTP", ref symbols);
			ToggleKeyword(settings.vsPro, "VEGETATION_STUDIO_PRO", ref symbols);

			SetAutoRef("MapMagic", settings.autoRef);
			SetAutoRef("MapMagic.Editor", settings.autoRef);
			SetAutoRef("Den.Tools", settings.autoRef);
			SetAutoRef("Den.Tools.Editor", settings.autoRef);

			PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
		}


		static void ToggleKeyword (bool val, string keyword, ref string symbols)
		{
			//enabling
			if (val  &&  !symbols.Contains(keyword+";")  &&  !symbols.EndsWith(keyword)) 
			{
				symbols += (symbols.Length!=0? ";" : "") + keyword;
				Debug.Log(keyword + " Enabled");
			}

			//disabling
			if (!val  &&  (symbols.Contains(keyword+";")  ||  symbols.EndsWith(keyword))) 
			{
				symbols = symbols.Replace(keyword,""); 
				symbols = symbols.Replace(";;", ";"); 
				Debug.Log(keyword + " Disabled");
			}
		}


		static bool GetAutoRef (string assName)
		{
			string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assName);
			using (StreamReader reader = new StreamReader(asmdef))
			{
				string asmdefText = reader.ReadToEnd();
				if (asmdefText.Contains("\"autoReferenced\": false"))
					return false;
				else return true;
			}
		}

		static void SetAutoRef (string assName, bool val)
		{
			string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assName);
			string asmdefText;
			using (StreamReader reader = new StreamReader(asmdef))
				asmdefText = reader.ReadToEnd();

			bool getVal = asmdefText.Contains("\"autoReferenced\": false");
			if (val == getVal)
				return;
			
			string newText = "\"autoReferenced\": " + (val? "true":"false");
			if (asmdefText.Contains("\"autoReferenced\": false"))
				asmdefText = asmdefText.Replace("\"autoReferenced\": false", newText);
			else if (asmdefText.Contains("\"autoReferenced\": true"))
				asmdefText = asmdefText.Replace("\"autoReferenced\": true", newText);
			else 
				asmdefText = asmdefText.Replace("}", newText + "\n}");

			using (StreamWriter writer = new StreamWriter(asmdef))
				writer.Write(asmdefText);

			AssetDatabase.Refresh();
		}


		[MenuItem ("Window/MapMagic/Settings")]
		public static void ShowWindow ()
		{
			SettingsWindow window = (SettingsWindow)GetWindow(typeof (SettingsWindow));

			Texture2D icon = Resources.Load("MapMagic/Icons/Window") as Texture2D; 
			window.titleContent = new GUIContent("MapMagic Settings", icon);

			window.position = new Rect(100,100,300,340);
		}

		public static void ShowNet20Notification ()
		{
			//#if !NET_STANDARD_2_0 won't work since editor is always NET_4
			if (PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup) != ApiCompatibilityLevel.NET_4_6  &&
				EditorUtility.DisplayDialog("MapMagic API Compatibility Warning", "MapMagic requires .NET 4.x API Compatibility level. \n"+
					"Do you want to switch compatibility level now? \n\n"+
					"You can switch compatibility level manually in Project Settings -> Player -> Api Compatibility Level",
					"Switch to .NET 4.x",
					"Cancel"))
						PlayerSettings.SetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup, ApiCompatibilityLevel.NET_4_6);
			
		}
	}
}

#endif