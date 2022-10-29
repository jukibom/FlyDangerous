using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Den.Tools
{
	#if MM_ExtendedTerrainInspector
	[CustomEditor(typeof(Terrain))]   
	#endif
	public class ExtendedTerrainInspector : Editor
	{
		private Type inspectorType;
		[NonSerialized] private Editor inspector;

		private Terrain terrain;
		private TerrainCollider terrainCollider;

		private PropertyInfo selectedToolProperty;

		private Action onInspectorGUIAction;
		private Action onInspectorUpdateAction;
		private Action saveInspectorSettingsAction;
		private Action onEnableAction;
		private Action onDisableAction;
		private Action<SceneView> onSceneGUIAction;
		private Action<SceneView> onPreSceneGUIAction;
		private Func<bool> isModificationAliveAction;

		private Bounds strokeBounds;

		//public static WeakEvent<Terrain,Bounds,int> StrokeFinished = new WeakEvent<Terrain,Bounds,int>();  //terrain, stroke bounds and tool number
		public static Action<Terrain,Bounds,int> StrokeFinished;

		public void Init ()
		{
			//inspector type
			Assembly a = Assembly.GetAssembly(typeof(Editor));
			inspectorType = ArrayTools.FindMember(a.GetTypes(), x => x.Name=="TerrainInspector");

			if (inspectorType==null)
				throw new Exception("Could not load TerrainInspector type"); 

			//creating a temporary editor
			if (inspector == null) 
				inspector = CreateEditor(targets, inspectorType); 

			//terrain
			terrain = (Terrain)target;
			terrainCollider = terrain.GetComponent<TerrainCollider>();

			//properties
			selectedToolProperty = inspectorType.GetProperty("selectedTool",  BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

			//methods
			MethodInfo onInspectorGUIMethod = inspectorType.GetMethod("OnInspectorGUI");
			MethodInfo onInspectorUpdateMethod = inspectorType.GetMethod("OnInspectorUpdate",  BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			MethodInfo saveInspectorSettingsMethod = inspectorType.GetMethod("SaveInspectorSettings", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			MethodInfo onEnableMethod = inspectorType.GetMethod("OnEnable");
			MethodInfo onDisableMethod = inspectorType.GetMethod("OnDisable");

			//caching most used methods to actions
			onInspectorGUIAction = (Action)Delegate.CreateDelegate(typeof(Action), inspector, onInspectorGUIMethod); 
			onInspectorUpdateAction = (Action)Delegate.CreateDelegate(typeof(Action), inspector, onInspectorUpdateMethod);
			saveInspectorSettingsAction = (Action)Delegate.CreateDelegate(typeof(Action), inspector, saveInspectorSettingsMethod);
			onEnableAction = (Action)Delegate.CreateDelegate(typeof(Action), inspector, onEnableMethod);
			onDisableAction = (Action)Delegate.CreateDelegate(typeof(Action), inspector, onDisableMethod);
			isModificationAliveAction = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), inspector,	inspectorType.GetMethod("IsModificationToolActive", BindingFlags.Instance | BindingFlags.NonPublic));

			onSceneGUIAction = (Action<SceneView>)Delegate.CreateDelegate(typeof(Action<SceneView>), inspector,	inspectorType.GetMethod("OnSceneGUICallback"));

			//initializing
			//MethodInfo initializeMethod = inspectorType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic);
			//initializeMethod.Invoke(inspector, null);
		}


		public void OnSceneGUI ()
		{
			Event current = Event.current;
			int controlID = GUIUtility.GetControlID("TerrainEditor".GetHashCode(), FocusType.Passive);

			if (!Event.current.alt && 
				current.button == 0 && 
				isModificationAliveAction() )
			{
				Vector3 brushPos = BrushPos();
				Bounds brushBounds = new Bounds(brushPos, Vector3.zero);
				
				float pixelSize = 1f * terrain.terrainData.size.x / Mathf.Min(terrain.terrainData.heightmapResolution, terrain.terrainData.alphamapResolution); //terrain.terrainData.detailResolution);
				brushBounds.Expand( EditorPrefs.GetInt("TerrainBrushSize") * pixelSize );

				switch (Event.current.type)
				{
					case EventType.MouseDown: 
						strokeBounds = brushBounds;
						break;
					case EventType.MouseDrag:
						strokeBounds.Encapsulate(brushBounds);
						break;
					case EventType.MouseUp:
						//saveInspectorSettingsAction(); 
						if (StrokeFinished != null)
							StrokeFinished(terrain, strokeBounds, 1);
						break; 
				}

				Handles.color = Color.red;
				Handles.DrawWireCube(strokeBounds.center, strokeBounds.extents*2);
			}
		}



		public void OnDisable ()  
		{
			onDisableAction(); 
			onDisableAction(); //ondisabling twice to remove onprescenegui delegates (somehow they are added twice)

			DestroyImmediate(inspector); //otherwise it will still be calling onscenegui
			inspector = null; 
		}


		public void OnEnable () 
		{
			if (inspector == null) Init(); 
			onEnableAction();
		}

		public override void OnInspectorGUI ()
		{
			if (inspector == null) Init();
			onInspectorGUIAction();
		}


		public Vector3 BrushPos ()
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			RaycastHit hit;
			if (terrainCollider.Raycast(ray, out hit, Mathf.Infinity))
				return hit.point;
			else
				return strokeBounds.center; //returning somewhere within stroke
		}
	}

}



//properties:
//UnityEditor.TerrainTool	selectedTool, 
//System.Boolean			canEditMultipleObjects, 
//UnityEngine.Object		target, 
//UnityEngine.Object[]		targets, 
//System.Int32				referenceTargetIndex, 
//System.String				targetTitle, 
//UnityEditor.SerializedObject serializedObject, 
//System.Boolean			isInspectorDirty, 
//UnityEditor.IPreviewable	preview, 
//UnityEditor.PropertyHandlerCache propertyHandlerCache, 
//System.String				name, 
//UnityEngine.HideFlags		hideFlags

//methods:
//Single	PercentSlider(UnityEngine.GUIContent, Single, Single, Single), 
//Void		CheckKeys(), 
//Void		LoadBrushIcons(), 
//Void		Initialize(), 
//Void		LoadInspectorSettings(), 
//Void		SaveInspectorSettings(), 
//Void		OnEnable(), 
//Void		OnDisable(), 
//TerrainTool get_selectedTool(), 
//Void		set_selectedTool(TerrainTool), 
//Void		MenuButton(UnityEngine.GUIContent, System.String, Int32), 
//Int32		AspectSelectionGrid(Int32, UnityEngine.Texture[], Int32, UnityEngine.GUIStyle, System.String, Boolean ByRef), 
//Rect		GetBrushAspectRect(Int32, Int32, Int32, Int32 ByRef), 
//Int32		AspectSelectionGridImageAndText(Int32, UnityEngine.GUIContent[], Int32, UnityEngine.GUIStyle, System.String, Boolean ByRef), 
//Void		LoadSplatIcons(), Void LoadTreeIcons(), Void LoadDetailIcons(), 
//Void		ShowTrees(), Void ShowDetails(), Void ShowSettings(), Void ShowRaiseHeight(), Void ShowSmoothHeight(), Void ShowTextures(), Void ShowBrushes(), Void ShowHeightmaps(), Void ShowResolution(), Void ShowRefreshPrototypes(), Void ShowMassPlaceTrees(), Void ShowBrushSettings(), Void ShowSetHeight(), 
//Void		ResizeDetailResolution(UnityEngine.TerrainData, Int32, Int32), 
//Void		ShowUpgradeTreePrototypeScaleUI(), 
//Void		InitializeLightingFields(), 
//Void		RenderLightingFields(), 
//Void		OnInspectorUpdate(), 
//Void		OnInspectorGUI(), 
//UnityEditor.Brush GetActiveBrush(Int32), 
//Boolean	Raycast(Vector2 ByRef, Vector3 ByRef), 
//Boolean	HasFrameBounds(), 
//Bounds	OnGetFrameBounds(), 
//Boolean	IsModificationToolActive(), 
//Boolean	IsBrushPreviewVisible(), 
//Void		DisableProjector(), 
//Void		UpdatePreviewBrush(), 
//Void		OnSceneGUICallback(UnityEditor.SceneView), 
//Void		OnPreSceneGUICallback(UnityEditor.SceneView)