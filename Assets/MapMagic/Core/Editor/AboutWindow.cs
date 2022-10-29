using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;

using MapMagic.Core;
using MapMagic.Nodes;
using MapMagic.Products;
using MapMagic.Previews;

namespace MapMagic.Core.GUI
{
	//[EditoWindowTitle(title = "MapMagic Graph")]  //it's internal Unity stuff
	public class AboutWindow : EditorWindow
	{
		UI ui = new UI();

		public void OnGUI ()
		{
			ui.Draw(DrawGUI, inInspector:false);
		}


		public void DrawGUI ()
		{
			using (Cell.Line)
				DrawAbout();
		}

		public static void DrawAbout ()
		{
			using (Cell.RowPx(100))
				Draw.Icon(UI.current.textures.GetTexture("MapMagic/Icons/AssetBig"), scale:0.5f);

			using (Cell.Row)
			{
				string versionName = MapMagicObject.version.ToString();
				//versionName = versionName[0]+"."+versionName[1]+"."+versionName[2];
				using (Cell.LineStd) Draw.Label("MapMagic " + versionName);
				using (Cell.LineStd) Draw.Label("by Denis Pahunov");

				Cell.EmptyLinePx(10);

				using (Cell.LineStd) Draw.URL(" - Online Documentation", "https://gitlab.com/denispahunov/mapmagic/wikis/home");
				using (Cell.LineStd) Draw.URL(" - Video Tutorials", url:"https://www.youtube.com/playlist?list=PL8fjbXLqBxvbsJ56kskwA2tWziQx3G05m");
				using (Cell.LineStd) Draw.URL(" - Forum Thread", url:"https://forum.unity.com/threads/released-mapmagic-2-infinite-procedural-land-generator.875470/");
				using (Cell.LineStd) Draw.URL(" - Issues / Ideas", url:"http://mm2.idea.informer.com");
			}
		}


		[MenuItem ("Window/MapMagic/About")]
		public static void ShowWindow ()
		{
			AboutWindow window = (AboutWindow)GetWindow(typeof (AboutWindow));

			Texture2D icon = TexturesCache.LoadTextureAtPath("MapMagic/Icons/Window");
			window.titleContent = new GUIContent("About MapMagic", icon);

			window.position = new Rect(100,100,300,200);
		}
	}
}