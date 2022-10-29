using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Profiling;

namespace Den.Tools.GUI
{
	public class StylesCache
	{
		#if UNITY_2019_3_OR_NEWER
		public const int defaultFontSize = 12;
		#else
		public const int defaultFontSize = 11;
		#endif

		public const int defaultToolbarFontSize = 9;

		private bool initializedAsPro; //is currently initialized as pro skin
		public static bool isPro; //= UnityEditor.EditorGUIUtility.isProSkin
		
		public Color FontColor
		{get{
			return isPro ?  new Color(1, 1, 1, 0.7f) : Color.black;
		}}


		public GUIStyle label; 
		public GUIStyle boldLabel; 
		public GUIStyle centerLabel; 
		public GUIStyle boldMiddleCenterLabel; 
		public GUIStyle middleLabel; 
		public GUIStyle middleBlackLabel;
		public GUIStyle rightLabel; 
		public GUIStyle topLabel; 
		public GUIStyle middleCenterLabel; 
		public GUIStyle smallLabel;
		public GUIStyle tinyLabel;
		public GUIStyle bigLabel;
		public GUIStyle blackLabel;
		public GUIStyle whiteLabel;
		public GUIStyle whiteMiddleLabel;
		public GUIStyle url;
		public GUIStyle foldout;
		public GUIStyle foldoutBackground;
		public GUIStyle foldoutOpaque;
		public GUIStyle field;
		public GUIStyle checkbox;
		public GUIStyle button; 
		public GUIStyle enumClose;
		public GUIStyle toolbar;
		public GUIStyle toolbarButton;
		public GUIStyle toolbarHoverButton;
		public GUIStyle toolbarField;
		public GUIStyle helpBox;
		public GUIStyle progressBarBackground;

		public static Texture2D blankTex;
		public static Texture2D pencilTex;
		public static Texture2D diamonTex;
		public static Texture2D debugTex;
		public static Texture2D progressBarFill;
		public static Texture2D enumSign;
		//public static Texture2D toggleTex;
		public static Texture2D objectPickerTex;


		public void CheckInit () 
		{
			isPro = UnityEditor.EditorGUIUtility.isProSkin;

			if (label == null  ||  initializedAsPro != isPro  ||  bigLabel.font == null) 
				Init();
		}

		public void Init ()
		{
			lastZoom = 1; //resetting zoom cache since all styles will be rescaled

			label = new GUIStyle(UnityEditor.EditorStyles.label); 
			label.normal.textColor = label.focused.textColor = label.active.textColor = FontColor; //no focus
			label.fontSize = defaultFontSize;

			boldLabel = new GUIStyle(label);
			boldLabel.fontStyle = FontStyle.Bold;

			centerLabel = new GUIStyle(label);
			centerLabel.alignment = TextAnchor.LowerCenter;

			middleLabel = new GUIStyle(label);
			middleLabel.alignment = TextAnchor.MiddleLeft;

			middleBlackLabel = new GUIStyle(label);
			middleBlackLabel.alignment = TextAnchor.MiddleLeft;
			middleBlackLabel.active.textColor = middleBlackLabel.normal.textColor =  middleBlackLabel.focused.textColor = Color.black;

			rightLabel = new GUIStyle(label);
			rightLabel.alignment = TextAnchor.MiddleRight;

			topLabel = new GUIStyle(label);
			topLabel.alignment = TextAnchor.UpperLeft;

			middleCenterLabel = new GUIStyle(label);
			middleCenterLabel.alignment = TextAnchor.MiddleCenter;
			
			boldMiddleCenterLabel = new GUIStyle(boldLabel);
			boldMiddleCenterLabel.alignment = TextAnchor.MiddleCenter;
		
			smallLabel = new GUIStyle(label);
			smallLabel.fontSize = 9;

			tinyLabel = new GUIStyle(label);
			tinyLabel.fontSize = 6;

			bigLabel = new GUIStyle(label);
			bigLabel.font = Font.CreateDynamicFontFromOSFont("Verdana", 16);
			bigLabel.fontSize = 16;
			//style.fontStyle = FontStyle.Bold; 
			bigLabel.alignment = TextAnchor.MiddleLeft;
			bigLabel.contentOffset = new Vector2(0,-2);

			blackLabel = new GUIStyle(label);
			blackLabel.active.textColor = blackLabel.normal.textColor =  blackLabel.focused.textColor = Color.black;

			whiteLabel = new GUIStyle(label);
			whiteLabel.active.textColor = whiteLabel.normal.textColor =  whiteLabel.focused.textColor = Color.white;

			whiteMiddleLabel = new GUIStyle(whiteLabel);
			whiteMiddleLabel.alignment = TextAnchor.MiddleCenter;
			
			url = new GUIStyle(label);
			url.normal.textColor = new Color(0.3f, 0.5f, 1f);
		
			foldout = new GUIStyle(UnityEditor.EditorStyles.foldout);  
			foldout.fontSize = defaultFontSize;
			foldout.fontStyle = FontStyle.Bold; 
			foldout.focused.textColor = foldout.active.textColor = foldout.onActive.textColor = FontColor;

			foldoutBackground = new GUIStyle();
			Texture2D foldoutBackTex = TexturesCache.LoadTextureAtPath("DPUI/Backgrounds/Foldout");  
			foldoutBackground.normal.background = foldoutBackTex;
			foldoutBackground.border = new RectOffset(4,4,4,4);

			foldoutOpaque = new GUIStyle(foldoutBackground);
			Texture2D foldoutOpaqueTex = TexturesCache.LoadTextureAtPath("DPUI/Backgrounds/FoldoutOpaque");
			foldoutOpaque.normal.background = foldoutOpaqueTex;

			field = new GUIStyle(UnityEditor.EditorStyles.numberField);
			field.fontSize = defaultFontSize;
			Texture2D fieldTexture = TexturesCache.LoadTextureAtPath("DPUI/Backgrounds/Field");
			#if !UNITY_2019_3_OR_NEWER
			field.normal.background = field.active.background = field.focused.background = fieldTexture;
			field.border = new RectOffset(4,4,4,4);
			#endif
			field.normal.textColor = field.focused.textColor = field.active.textColor = FontColor;

			checkbox = new GUIStyle(UnityEditor.EditorStyles.numberField);
			checkbox.fontSize = defaultFontSize;
			Texture2D toggleCheckedTex = TexturesCache.LoadTextureAtPath("DPUI/Toggle/Checked");
			Texture2D toggleCheckedActiveTex = TexturesCache.LoadTextureAtPath("DPUI/Toggle/CheckedActive");
			Texture2D toggleUncheckedTex = TexturesCache.LoadTextureAtPath("DPUI/Toggle/Unchecked");
			Texture2D toggleUncheckedActiveTex = TexturesCache.LoadTextureAtPath("DPUI/Toggle/UncheckedActive");
			checkbox.normal.background = checkbox.focused.background = toggleUncheckedTex;
			checkbox.onNormal.background = checkbox.onFocused.background = toggleCheckedTex;
			checkbox.onActive.background = toggleCheckedActiveTex;
			checkbox.active.background = toggleUncheckedActiveTex;
			checkbox.border = new RectOffset(1,1,1,1);
			checkbox.overflow = new RectOffset(-1,-1,-1,-1);
			checkbox.normal.textColor = checkbox.focused.textColor = checkbox.active.textColor = FontColor;

			button = new GUIStyle("Button"); 
			button.fontSize = defaultFontSize;
			button.normal.textColor = button.focused.textColor = button.active.textColor = FontColor; //no focus
			Texture2D buttonTexture = TexturesCache.LoadTextureAtPath("DPUI/Backgrounds/Button");
			Texture2D buttonPressedTexture = TexturesCache.LoadTextureAtPath("DPUI/Backgrounds/ButtonPressed");
			Texture2D buttonActiveTexture = TexturesCache.LoadTextureAtPath("DPUI/Backgrounds/ButtonActive");
			button.normal.background = buttonTexture;
			button.onNormal.background = buttonPressedTexture;
			button.border = new RectOffset(2,2,2,2);
			button.overflow.bottom = 0;

			//#if !UNITY_2019_3_OR_NEWER
			enumClose = new GUIStyle(button);
			enumClose.alignment = TextAnchor.LowerLeft;
			enumClose.fontSize = defaultFontSize; //defaultToolbarFontSize;
			enumClose.overflow.bottom = 0;
			//#else
			//enumClose = new GUIStyle(UnityEditor.EditorStyles.popup);
			//#endif

			toolbar = new GUIStyle(UnityEditor.EditorStyles.toolbar);
			toolbar.overflow = new RectOffset(0,0,0,0);
			toolbar.padding = new RectOffset(0,0,0,0);
			toolbar.fixedHeight = 0;

			toolbarButton = new GUIStyle(UnityEditor.EditorStyles.toolbarButton);  
			toolbarButton.overflow = new RectOffset(0,0,1,0);
			//style.padding = new RectOffset(0,0,10,0);
			//style.margin = new RectOffset(0,0,10,0);
			toolbarButton.fixedHeight = 0;
			toolbarButton.fontSize = defaultToolbarFontSize;

			toolbarHoverButton = new GUIStyle(button);
			toolbarHoverButton.border = toolbarHoverButton.border;
			toolbarHoverButton.normal.background = toolbarHoverButton.normal.background;
			//style.active.background = style.normal.background;
			toolbarHoverButton.normal.textColor = toolbarHoverButton.focused.textColor = toolbarHoverButton.active.textColor = FontColor; //no focus
			toolbarHoverButton.fontSize = defaultToolbarFontSize;

			toolbarField = new GUIStyle(field);  
			toolbarField.alignment = TextAnchor.MiddleLeft;
			//toolbarField.fontSize = (int)(defaultToolbarFontSize);

			helpBox = new GUIStyle(UnityEditor.EditorStyles.helpBox); 
			helpBox.fontSize = defaultToolbarFontSize;
			
			progressBarBackground = new GUIStyle(UnityEditor.EditorStyles.helpBox); 
			progressBarBackground.normal.background = TexturesCache.LoadTextureAtPath("DPUI/ProgressBar/Background");
			progressBarBackground.border = new RectOffset(1,1,1,1);

			blankTex = TexturesCache.LoadTextureAtPath("DPUI/Backgrounds/White");
			pencilTex = TexturesCache.LoadTextureAtPath("DPUI/Icons/Pencil");
			diamonTex = TexturesCache.LoadTextureAtPath("DPUI/Icons/Circle");
			debugTex = TexturesCache.LoadTextureAtPath("DPUI/Backgrounds/Gradient");
			progressBarFill = TexturesCache.LoadTextureAtPath("DPUI/ProgressBar/Fill");
			enumSign = TexturesCache.LoadTextureAtPath("DPUI/Icons/Enum");
			//toggleTex = TexturesCache.LoadTextureAtPath("DPUI/Icons/Toggle");
			objectPickerTex = TexturesCache.LoadTextureAtPath("DPUI/Icons/ObjectPicker");

			PopulateFontSizes();
			initializedAsPro = isPro;
		}


		public static void Reload ()
		{
			System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(StylesCache).TypeHandle);
		}

		private Dictionary<GUIStyle,float> fontsSizes = new Dictionary<GUIStyle, float>();
		private void PopulateFontSizes ()
		{
			FieldInfo[] fields = typeof(StylesCache).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i=0; i<fields.Length; i++)
			{
				if (fields[i].FieldType != typeof(GUIStyle)) continue;
				GUIStyle style = fields[i].GetValue(this) as GUIStyle;
				if (style.fontSize == 0) continue;
				fontsSizes.Add(style, style.fontSize);
			}
		}

		private float lastZoom = 1;
		public void Resize (float zoom=-1)
		{
			if (zoom == -1) zoom = lastZoom;
			if (zoom == lastZoom) return;
			lastZoom = zoom;
			
			foreach (var kvp in fontsSizes)
			{
				GUIStyle style = kvp.Key;

				if (fontsSizes.TryGetValue(style, out float fs)) 
					style.fontSize = Mathf.RoundToInt(fs * zoom);
			}
		}

		private Dictionary<string,GUIStyle> styles = new Dictionary<string,GUIStyle>();
		//private Dictionary<GUIStyle,float> customFontsSize = new Dictionary<GUIStyle, float>();
	}
}
