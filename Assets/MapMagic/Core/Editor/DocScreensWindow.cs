using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using MapMagic.Core;
using Den.Tools.GUI;
 
namespace MapMagic.GUI.DocScreens
{
	//[EditoWindowTitle(title = "MapMagic Graph")]  //it's internal Unity stuff
	public class DocScreensWindow : EditorWindow
	{
		Texture2D docScreen;
		List<Texture2D> sources = new List<Texture2D>();

		string folder = "C:\\Unity\\Wiki\\MapMagicWiki";
		string file = "Noise_Intensity";
		int interval = 40;
		float ratio = 4;
		float zoom = 1;

		UI ui = new UI();

		public void OnGUI ()
		{
			ui.Draw(DrawGUI, inInspector:false);
		}


		public void DrawGUI ()
		{
			using (Cell.LineStd) Draw.Field(ref folder, "Folder");
			using (Cell.LineStd) Draw.Field(ref file, "Name");
			using (Cell.LinePx(0))
			{
				using (Cell.LineStd) Draw.Field(ref interval, "Interval");
				using (Cell.LineStd) Draw.Field(ref ratio, "Ratio");
				using (Cell.LineStd) Draw.Field(ref zoom, "Zoom");

				if (Cell.current.valChanged)
					docScreen = Compile(sources);
			}

			using (Cell.LineStd) 
				if (Draw.Button("Append")) Append(withGizmos:false);

			using (Cell.LineStd) 
				if (Draw.Button("Append in With Gizmos")) Append(withGizmos:true);

			using (Cell.LineStd) 
				if (Draw.Button("Remove Last")) { sources.RemoveAt(sources.Count-1); docScreen = Compile(sources); }

			using (Cell.LineStd) 
				if (Draw.Button("Save")) Save();

			using (Cell.LineStd) 
				if (Draw.Button("Clear")  &&  EditorUtility.DisplayDialog("Clear DocScreens", $"Clear {file} without saving?", "Clear", "Cancel"))
					Clear();

			using (Cell.LineStd) 
				if (Draw.Button("Save and Clear")) 
				{
					Save();
					Clear();
				}

			using (Cell.LinePx(UI.current.editorWindow.position.width/ratio))
				Draw.Texture(docScreen);
		}

		public void Append (bool withGizmos=true)
		{
			Texture2D screen = CaptureSceneView(withGizmos);
			sources.Add(screen);

			docScreen = Compile(sources);
		}


		private Texture2D Compile (List<Texture2D> sources)
		{
			if (sources.Count==1) return sources[0];

			int height = (int)(sources[0].height / zoom);
			int width = (int)(height * ratio);
			int numIntervals = sources.Count-1;
			int pureWidth = width - numIntervals*interval;
			int screenWidth = pureWidth / sources.Count;

			//if (screenWidth > sources[0].width)
			//	throw new System.Exception("Scene view ratio error: it's too high, make it wider");

			Texture2D tex = new Texture2D(width, height);
			Color[] empty = new Color[width*height];
			tex.SetPixels(empty);

			for (int s=0; s<sources.Count; s++)
			{
				//if (sources[s].height != height)
				//	throw new System.Exception("Different source height");

				int middle = sources[s].width / 2;
				int blockWidth = Mathf.Min(screenWidth, sources[s].width);
				int start = middle - blockWidth /2;

				int blockHeight = Mathf.Min(height, sources[s].height);

				Color[] colors = sources[s].GetPixels(start, 0, blockWidth, blockHeight);
				tex.SetPixels(
					s*screenWidth + s*interval + screenWidth/2 - blockWidth/2, 
					height/2 - blockHeight/2, 
					blockWidth, 
					blockHeight, 
					colors);
			}

			tex.Apply();
			return tex;
		}

		public void Save ()
		{
			docScreen = Compile(sources);
			byte[] bytes =docScreen.EncodeToPNG();
			string filename = $"{folder}\\{file}.png";
			if (System.IO.File.Exists(filename) && !EditorUtility.DisplayDialog("Overwrite", $"Overwrite {filename}?", "Overwrite", "Cancel"))
				return;
			System.IO.File.WriteAllBytes(filename, bytes);
		}

		public void Clear()
		{
			docScreen = null;
			sources.Clear();
			//name = "Generator_Value_V1_V2_V3";
		}


		public Texture2D CaptureSceneView (bool withGizmos = true)
		{
			SceneView sv = SceneView.lastActiveSceneView;
			Camera cam = sv.camera;

			
			Texture2D gizmosTex = withGizmos ? CamTexToTexture(cam) : null; //previous (before render) cam texture is usually gizmos
			
			cam.Render();
			Texture2D camTex = CamTexToTexture(cam);

			if (withGizmos)
			{
				Color[] gizmosColors = gizmosTex.GetPixels();
				Color[] camColors = camTex.GetPixels();

				for (int i=0; i<camColors.Length; i++)
					camColors[i] = camColors[i]*(1-gizmosColors[i].a) + gizmosColors[i]*gizmosColors[i].a;

				camTex.SetPixels(camColors);
				camTex.Apply();
			}

			return camTex;
		}

		public Texture2D CamTexToTexture (Camera cam)
		{
			RenderTexture rt = cam.targetTexture;

			Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
 
			RenderTexture prevRt = RenderTexture.active;
			RenderTexture.active = rt;
			tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			tex.Apply();
			RenderTexture.active = prevRt;

			return tex;
		}


		#if MM_DOC
		[MenuItem ("Window/MapMagic/DocScreens")]
		#endif
		public static void ShowWindow ()
		{
			DocScreensWindow window = (DocScreensWindow)GetWindow(typeof (DocScreensWindow)); 

			Texture2D icon = TexturesCache.LoadTextureAtPath("MapMagic/Icons/Window");
			window.titleContent = new GUIContent("MapMagic DocScreens", icon);

			window.position = new Rect(100,100,300,200);
		}
	}
}