using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Linq;
using System.Linq.Expressions;
using Den.Tools;

namespace Den.Tools.GUI
{
	public static class Draw
	{
		const int fieldPadding = 1;
		const int buttonPadding = 1;
		public const int vectorXYWidth = 15; //width of 'width' and 'height' elements in vector field
		public const float vectorXYRelWidth = 0.11f; 
		const int vectorWidthHeightWidth = 42; //width of 'width' and 'height' elements in vector field

		private static Material multiplyMat; //transparent, has no texture, draws over area and fill it with color
		private static Material textureIconMat;
		private static Material textureArrIconMat;
		private static Material textureScrollZoomMat;
		private static Material textureMat;
		private static Material gridMat;
		private static Material textureRawMat;

		private static Cell pressedButton;


		#region Fast Fields

			public static void Label (string label, GUIStyle style=null) //TODO: optimize style?
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Cell.current.special |= Cell.Special.Label;

				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);
				if (style==null) style = UI.current.styles.label;

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true); //BeginDisabledGroup performs some action on (false) anyways - do un-disable in disabled block
				EditorGUI.LabelField(rect, label, style:style);
				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();
			}


			public static float Field (float val, GUIStyle style=null) 
			{
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;

				Cell.current.special |= Cell.Special.Field;

				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);
				GUIStyle cstyle = style!=null ? style : UI.current.styles.field;

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);

				float newVal = val;
				if (!Cell.current.inactive)
					newVal = EditorGUI.FloatField(rect, val, style:cstyle);
					//Generates some garbage (1-2kB, check with profile), but there's nothing I can do about it
				else
					EditorGUI.LabelField(rect, val.ToString(), style:cstyle);

				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				//assigning new value only on enter, tab or mouseclick (delayed mode)
				if (!newVal.Equals(val))
				{
					UI.current.delayedFloat = newVal;
					UI.current.delayedCell = Cell.current;
				}

//				if (Event.current.keyCode == KeyCode.Return  &&  !UI.current.layout)
//						Debug.Log("True");  


				if (UI.current.delayedCell == Cell.current  &&  (UI.FieldLostFocus || UI.current.editorWindow != EditorWindow.focusedWindow))
				{
					UI.current.MarkChanged();
					val = UI.current.delayedFloat;

					if (UI.current.editorWindow!=null) UI.current.editorWindow.Repaint();

					UI.current.delayedCell = null;
				}

				return val;
			}


			public static float Field (float val, string label, float min=float.MinValue, float max=float.MaxValue) 
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }

				Cell.current.special |= Cell.Special.LabelField;

				//using (Cell.RowRel(1-Cell.current.fieldWidth)) 
				Cell cell = Cell.RowRel(1-Cell.current.fieldWidth);
				{
					Label(label);
					if (!Cell.current.inactive)
						val = DragValue(val);
					if (val>max) val=max; //checking after drag since it going to draw field with changed value
					if (val<min) val=min;
				}
				cell.Dispose();

				//using (Cell.RowRel(Cell.current.fieldWidth)) 
				cell = Cell.RowRel(Cell.current.fieldWidth);
				val = Field(val);
				cell.Dispose();

				if (val>max) val=max;
				if (val<min) val=min;
				return val;
			}

			public static void Field (ref float val) { val = Field(val); } 
			public static void Field (ref float val, string label) { val = Field(val, label); }

			public static int Field (int val, GUIStyle style = null) 
			{
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;

				Cell.current.special |= Cell.Special.Field;
				
				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);

				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);
				GUIStyle cstyle = style!=null ? style : UI.current.styles.field;

				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				int newVal = val;
				if (!Cell.current.inactive)
					newVal = (int)EditorGUI.FloatField(rect, val, style:cstyle);
					//Generates some garbage (1-2kB, check with profile), but there's nothing I can do about it
				else
					EditorGUI.LabelField(rect, val.ToString(), style:cstyle);

				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				//assigning new value only on enter, tab or mouseclick (delayed mode)
				if (!newVal.Equals(val))
				{
					UI.current.delayedInt = newVal;
					UI.current.delayedCell = Cell.current;
				}

				if (UI.current.delayedCell == Cell.current  &&  (UI.FieldLostFocus || UI.current.editorWindow != EditorWindow.focusedWindow))
				{
					UI.current.MarkChanged();
					val = UI.current.delayedInt;

					if (UI.current.editorWindow!=null) UI.current.editorWindow.Repaint();

					UI.current.delayedCell = null;
				}

				return val;
			}

			public static int Field (int val, string label, int min=int.MinValue, int max=int.MaxValue) 
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }

				Cell.current.special |= Cell.Special.LabelField;
				
				//using (Cell.RowRel(1-Cell.current.fieldWidth)) 
				Cell cell = Cell.RowRel(1-Cell.current.fieldWidth);
				{
					Label(label);
					val = DragValue(val);
				//	if (val>max) val=max; //checking after drag since it going to draw field with changed value
				//	if (val<min) val=min;
				}
				cell.Dispose();

				//using (Cell.RowRel(Cell.current.fieldWidth)) 
				cell = Cell.RowRel(Cell.current.fieldWidth);
					val = Field(val);
				cell.Dispose();

			//	if (val>max) val=max;
			//	if (val<min) val=min;

				return val;
			}

			public static void Field (ref int val) { val = Field(val); } 
			public static void Field (ref int val, string label) { val = Field(val, label); }

		#endregion


		#region Other Fields

			//simple single field
			public static T Field<T> (T val, Func<Rect,T,T> drawFn) //where T: IEquatable<T>
			{
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;

				Cell.current.special |= Cell.Special.Field;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);

				T newVal = drawFn(rect, val);

				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				if (
					(newVal==null && val!=null) ||
					(newVal!=null && val==null) ||
					(newVal!=null && val!=null && !newVal.Equals(val)) )
				{
					UI.current.MarkChanged();
					val = newVal;
				}

				return val;
			}

			//complex field label+val
			public static T Field<T> (T val, string label, Func<Rect,T,T> drawFn, Func<T,T> dragFn=null) //where T: IEquatable<T>
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }

				Cell.current.special |= Cell.Special.LabelField;
				
				//using (Cell.RowRel(1-Cell.current.fieldWidth)) 
				Cell labelCell = Cell.RowRel(1-Cell.current.fieldWidth);
				{
					Label(label);
					if (dragFn != null) val = dragFn(val);
				}
				labelCell.Dispose();

				//using (Cell.RowRel(Cell.current.fieldWidth)) 
				Cell fieldCell = Cell.RowRel(Cell.current.fieldWidth); //won't work with texture/object fields
				val = Field(val, drawFn);
				fieldCell.Dispose();

				return val;
			}

			public static double Field (double val) { return Field(val, doubleFieldFn); } 
			public static void Field (ref double val) { val = Field(val, doubleFieldFn); } 
			public static double Field (double val, string label) { return Field(val, label, doubleFieldFn, doubleDragFn); }
			public static void Field (ref double val, string label) { val = Field(val, label, doubleFieldFn, doubleDragFn); }
			private static double DoubleFieldFn (Rect rect, double val) { return EditorGUI.DoubleField(rect, val, style:UI.current.styles.field); }
			private static readonly Func<Rect,double,double> doubleFieldFn = (rect,val) => EditorGUI.DoubleField(rect, val, style:UI.current.styles.field);
			private static readonly Func<double,double> doubleDragFn = val => (double)DragValueInternal((float)val, 0.01f);


			public static string Field (string val) { return Field(val, stringFieldFn); } 
			public static void Field (ref string val) { val = Field(val, stringFieldFn); } 
			public static string Field (string val, string label) { return Field(val, label, stringFieldFn); }
			public static void Field (ref string val, string label) { val = Field(val, label, stringFieldFn); }
				//could use return Field(val, EditorGUI.ColorField), but it creates new object each time, and therefore too much garbage
			private static readonly Func<Rect,string,string> stringFieldFn = (rect,val) => EditorGUI.TextField(rect, val, style:UI.current.styles.field);


			//enums
			public static Enum Field (Enum val, bool flags=false) { return Field(val, flags ? flagsFieldFn : enumFieldFn); } 
			public static void Field (ref Enum val, bool flags=false) { val = Field(val, flags ? flagsFieldFn : enumFieldFn); } 
			public static Enum Field (Enum val, string label, bool flags=false) { return Field(val, label, flags ? flagsFieldFn : enumFieldFn); }
			public static void Field (ref Enum val, string label, bool flags=false) { val = Field(val, label, flags ? flagsFieldFn : enumFieldFn); }
			private static Enum EnumFieldFn (Rect rect, Enum val) 
			{ 
				Enum result = EditorGUI.EnumPopup(rect, val, UI.current.styles.enumClose); 
				Vector2 signPos = Cell.current.InternalCenter; signPos.x += Cell.current.finalSize.x/2 - 10; signPos.y-=1;
				Icon(StylesCache.enumSign, signPos);
				return result;
			}
			private static readonly Func<Rect,Enum,Enum> enumFieldFn = EnumFieldFn;
			
			private static Enum FlagsFieldFn (Rect rect, Enum val) 
			{ 
				Enum result = EditorGUI.EnumFlagsField(rect, val, UI.current.styles.enumClose); 
				Vector2 signPos = Cell.current.InternalCenter; signPos.x += Cell.current.finalSize.x/2 - 10; signPos.y-=1;
				Icon(StylesCache.enumSign, signPos);
				return result;
			}
			private static readonly Func<Rect,Enum,Enum> flagsFieldFn = FlagsFieldFn;


			//generic enums
			public static T Field<T> (T val, bool flags=false) where T:Enum { return (T)Field((Enum)val,flags); } 
			public static void Field<T> (ref T val, bool flags=false) where T:Enum { val = (T)Field((Enum)val,flags); } 
			public static T Field<T> (T val, string label, bool flags=false) where T:Enum { return (T)Field((Enum)val, label,flags); }
			public static void Field<T> (ref T val, string label, bool flags=false) where T:Enum { val = (T)Field((Enum)val, label,flags); }

			public static bool Toggle (bool val) { return Field(val, toggleFieldFn); }
			public static void Toggle (ref bool val) { val = Field(val, toggleFieldFn); }
			public static bool Toggle (bool val, string label) { return Field(val, label, toggleFieldFn); }
			public static void Toggle (ref bool val, string label) { val = Field(val, label, toggleFieldFn); }
			private static readonly Func<Rect,bool,bool> toggleFieldFn = (rect,val) => 
			{
				float maxSize = 16;
				if (UI.current.scrollZoom!=null) maxSize *= UI.current.scrollZoom.zoom;
				if (rect.width > maxSize) rect.width = maxSize;
				if (rect.height > maxSize) rect.height = maxSize;
				return EditorGUI.Toggle(rect, val, UI.current.styles.checkbox);
			};


			public static bool ToggleLeft (bool val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				
				using (Cell.RowPx(18)) val = Field(val, toggleFieldFn);
				using (Cell.Row) Label(label);

				return val;
			}

			public static void ToggleLeft (ref bool val, string label) { val = ToggleLeft(val, label); }

			public static void DualLabel (string label, string field) { Field(field, label, dualLabelFieldFn, null); }
			private static string LabelFn (Rect rect, string label) { EditorGUI.LabelField(rect, label, style:UI.current.styles.label); return label; }
			private static readonly Func<Rect,string,string> dualLabelFieldFn = (rect,label) => 
				{ EditorGUI.LabelField(rect, label, style:UI.current.styles.label); return label; };

			public static void IconLabel (string label, Texture2D icon)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return; }
				
				using (Cell.RowPx(18)) Icon(icon);
				using (Cell.Row) Label(label);
			}

			public static T UniversalField<T> (T val) { return (T)UniversalField(val, typeof(T)); }

			public static object UniversalField(object val, Type type)
			{
				if (type == typeof(int)) return Field((int)val);
				else if (type == typeof(float)) return Field((float)val);
				else if (type == typeof(bool)) return Toggle((bool)val);
				else if (typeof(Enum).IsAssignableFrom(type)) return Field((Enum)val);
				else if (type == typeof(string)) return Field((string)val);
				else if (type == typeof(Color)) return Field((Color)val);
				else if (type == typeof(double)) return Field((double)val);
				else if (type == typeof(Vector2)) return Field((Vector2)val);
				else if (type == typeof(Vector3)) return Field((Vector3)val);
				else if (type == typeof(Vector4)) return Field((Vector4)val);
				else if (type == typeof(Vector2D)) return Field((Vector2D)val);
				else if (type == typeof(Rect)) return Field((Rect)val);
				else if (type == typeof(Coord)) return Field((Coord)val);
				else if (type == typeof(CoordRect)) return Field((CoordRect)val);
				else if (type == typeof(Coord3D)) return Field((Coord3D)val);
				else if (type == typeof(Texture2D)) return Field((Texture2D)val, true);
				else if (type == typeof(Transform)) return Field((Transform)val, true);
				else if (type == typeof(GameObject)) return Field((Transform)val, true);
				else if (type == typeof(Material)) return Field((Material)val, true);
				else if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return ObjectField((UnityEngine.Object)val, type, true);
				
				return val;
			}

			public static object UniversalField (object val, Type type, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }

				if (type == typeof(int)) return Field((int)val, label);
				else if (type == typeof(float)) return Field((float)val, label);
				else if (type == typeof(bool)) return Toggle((bool)val, label);
				else if (typeof(Enum).IsAssignableFrom(type)) return Field((Enum)val, label);
				else if (type == typeof(string)) return Field((string)val, label);
				else if (type == typeof(Color)) return Field((Color)val, label);
				else if (type == typeof(double)) return Field((double)val, label);
				else if (type == typeof(Vector2)) return Field((Vector2)val, label);
				else if (type == typeof(Vector3)) return Field((Vector3)val, label);
				else if (type == typeof(Vector4)) return Field((Vector4)val, label);
				else if (type == typeof(Vector2D)) return Field((Vector2D)val, label);
				else if (type == typeof(Rect)) return Field((Rect)val, label);
				else if (type == typeof(Coord)) return Field((Coord)val, label);
				else if (type == typeof(CoordRect)) return Field((CoordRect)val, label);
				else if (type == typeof(Coord3D)) return Field((Coord3D)val, label);
				else if (type == typeof(Texture2D)) return Field((Texture2D)val, label, true);
				else if (type == typeof(Transform)) return Field((Transform)val, label, true);
				else if (type == typeof(GameObject)) return Field((Transform)val, label, true);
				else if (type == typeof(Material)) return Field((Material)val, label, true);
				else if (type == typeof(TerrainLayer)) return Field((TerrainLayer)val, label, true);
				else if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return ObjectField((UnityEngine.Object)val, type, true);
				
				return val;
			}

			public static T UniversalField<T> (T val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				
				Type type = typeof(T);

				using (Cell.RowRel(1-Cell.current.fieldWidth)) 
				{
					Label(label);
					if (type == typeof(int)) val = (T)(object)DragValue((int)(object)val);
					if (type == typeof(float)) val = (T)(object)DragValue((float)(object)val);
				}
				
				using (Cell.RowRel(Cell.current.fieldWidth)) 
					val = (T)UniversalField(val, type);

				return val;
			}

		#endregion


		#region Non-generic Object Fields

			public static UnityEngine.Object Field (UnityEngine.Object val, Type type, bool allowSceneObject)
			{
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;
				if (StylesCache.objectPickerTex == null) return val; //happens on build
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);

				Rect shrinkedRect = new Rect(rect.x, rect.y+1, rect.width, rect.height-2);
				UnityEngine.Object newVal = EditorGUI.ObjectField(shrinkedRect, val, objType:type, allowSceneObjects:allowSceneObject);

				if (StylesCache.isPro)
					EditorGUI.DrawRect(rect, new Color(0.22f, 0.22f, 0.22f));

				if (Event.current.type == EventType.Repaint)
					UI.current.styles.field.Draw(rect, false, false, false, false);

				string name = "None";
				if (newVal != null) name = newVal.name.ToString();
				name += " (" + type.Name + ")";

				Rect labelRect = new Rect(rect.x+2, rect.y, rect.width-22, rect.height);
				EditorGUI.LabelField(labelRect, name, UI.current.styles.label);

				float zoom = UI.current.scrollZoom!=null ? UI.current.scrollZoom.zoom : 1;
				Rect pickerRect = new Rect(rect.x+rect.width - 12*zoom, rect.y+rect.height/2-4*zoom, 8*zoom, 8*zoom);
				UnityEngine.GUI.DrawTexture(pickerRect, StylesCache.objectPickerTex, ScaleMode.ScaleAndCrop);

				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				if (
					(newVal==null && val!=null) ||
					(newVal!=null && val==null) ||
					(newVal!=null && val!=null && !newVal.Equals(val)) )
				{
					UI.current.MarkChanged();
					val = newVal;
				}

				return val;
			}

			public static UnityEngine.Object Field (UnityEngine.Object val, string label, Type type, bool allowSceneObject)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				
				using (Cell.RowRel(1-Cell.current.fieldWidth)) 
				//Cell labelCell = Cell.RowRel(1-Cell.current.fieldWidth);
					Label(label);
				//labelCell.Dispose();

				using (Cell.RowRel(Cell.current.fieldWidth)) 
				//Cell fieldCell = Cell.RowRel(Cell.current.fieldWidth); //won't work with texture/object fields
				val = Field(val, type, allowSceneObject);
				//fieldCell.Dispose();

				return val;
			}

			public static void Field (ref UnityEngine.Object val, Type type, bool allowSceneObject)
				{ val = Field(val, type, allowSceneObject); }

			public static void Field (ref UnityEngine.Object val, string label, Type type, bool allowSceneObject)
				{ val = Field(val, label, type, allowSceneObject); }

		#endregion

		#region Generic Object Fields

			public static T Field<T> (T val, Func<Rect,T,bool,T> drawFn, bool allowSceneObject) where T: UnityEngine.Object
			{
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;

				Cell.current.special |= Cell.Special.Field;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);
				T newVal = drawFn(rect, val, allowSceneObject);
				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				if (
					(newVal==null && val!=null) ||
					(newVal!=null && val==null) ||
					(newVal!=null && val!=null && !newVal.Equals(val)) )
				{
					UI.current.MarkChanged();
					val = newVal;
				}

				return val;
			}

			public static T Field<T> (T val, string label, Func<Rect,T,bool,T> drawFn, bool allowSceneObject) where T: UnityEngine.Object
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }

				Cell.current.special |= Cell.Special.LabelField;
				
				using (Cell.RowRel(1-Cell.current.fieldWidth)) 
				//Cell labelCell = Cell.RowRel(1-Cell.current.fieldWidth);
					Label(label);
				//labelCell.Dispose();

				using (Cell.RowRel(Cell.current.fieldWidth)) 
				//Cell fieldCell = Cell.RowRel(Cell.current.fieldWidth); //won't work with texture/object fields
				val = Field(val, drawFn, allowSceneObject);
				//fieldCell.Dispose();

				return val;
			}


			//known object fields
			public static Texture2D Field (Texture2D val, bool allowSceneObject=false) { return Field(val, textureFieldFn, allowSceneObject); } 
			public static void Field (ref Texture2D val, bool allowSceneObject=false) { val = Field(val, textureFieldFn, allowSceneObject); } 
			public static Texture2D Field (Texture2D val, string label, bool allowSceneObject=false) { return Field(val, label, textureFieldFn, allowSceneObject); }
			public static void Field (ref Texture2D val, string label, bool allowSceneObject=false) { val = Field(val, label, textureFieldFn, allowSceneObject); }
			private static readonly Func<Rect,Texture2D,bool,Texture2D> textureFieldFn = ObjectFieldFn;

			public static TerrainLayer Field (TerrainLayer val, bool allowSceneObject=false) { return Field(val, terrainLayerFieldFn, allowSceneObject); } 
			public static void Field (ref TerrainLayer val, bool allowSceneObject=false) { val = Field(val, terrainLayerFieldFn, allowSceneObject); } 
			public static TerrainLayer Field (TerrainLayer val, string label, bool allowSceneObject=false) { return Field(val, label, terrainLayerFieldFn, allowSceneObject); }
			public static void Field (ref TerrainLayer val, string label, bool allowSceneObject=false) { val = Field(val, label, terrainLayerFieldFn, allowSceneObject); }
			private static readonly Func<Rect,TerrainLayer,bool,TerrainLayer> terrainLayerFieldFn = ObjectFieldFn;

			public static Material Field (Material val, bool allowSceneObject=false) { return Field(val, materialFieldFn, allowSceneObject); } 
			public static void Field (ref Material val, bool allowSceneObject=false) { val = Field(val, materialFieldFn, allowSceneObject); } 
			public static Material Field (Material val, string label, bool allowSceneObject=false) { return Field(val, label, materialFieldFn, allowSceneObject); }
			public static void Field (ref Material val, string label, bool allowSceneObject=false) { val = Field(val, label, materialFieldFn, allowSceneObject); }
			private static readonly Func<Rect,Material,bool,Material> materialFieldFn = ObjectFieldFn;


			public static Transform Field (Transform val, bool allowSceneObject=false) { return Field(val, transformFieldFn, allowSceneObject); } 
			public static void Field (ref Transform val, bool allowSceneObject=false) { val = Field(val, transformFieldFn, allowSceneObject); } 
			public static Transform Field (Transform val, string label, bool allowSceneObject=false) { return Field(val, label, transformFieldFn, allowSceneObject); }
			public static void Field (ref Transform val, string label, bool allowSceneObject=false) { val = Field(val, label, transformFieldFn, allowSceneObject); }
			private static readonly Func<Rect,Transform,bool,Transform> transformFieldFn = ObjectFieldFn;

			public static GameObject Field (GameObject val, bool allowSceneObject=false) { return Field(val, gameObjectFieldFn, allowSceneObject); } 
			public static void Field (ref GameObject val, bool allowSceneObject=false) { val = Field(val, gameObjectFieldFn, allowSceneObject); } 
			public static GameObject Field (GameObject val, string label, bool allowSceneObject=false) { return Field(val, label, gameObjectFieldFn, allowSceneObject); }
			public static void Field (ref GameObject val, string label, bool allowSceneObject=false) { val = Field(val, label, gameObjectFieldFn, allowSceneObject); }
			private static readonly Func<Rect,GameObject,bool,GameObject> gameObjectFieldFn = ObjectFieldFn;

			//unknown object fields
			public static T ObjectField<T> (T val, bool allowSceneObject=false) where T: UnityEngine.Object 
				{ return Field(val, ObjectFieldFn, allowSceneObject); } 
			public static void ObjectField<T> (ref T val, bool allowSceneObject=false) where T: UnityEngine.Object 
				{ val = Field(val, ObjectFieldFn, allowSceneObject); } 
			public static T ObjectField<T> (T val, string label, bool allowSceneObject=false) where T: UnityEngine.Object 
				{ return Field(val, label, ObjectFieldFn, allowSceneObject); } 
			public static void ObjectField<T> (ref T val, string label, bool allowSceneObject=false) where T: UnityEngine.Object 
				{ val = Field(val, label, ObjectFieldFn, allowSceneObject); } 

			private static T ObjectFieldFn<T> (Rect rect, T val, bool allowSceneObject) where T: UnityEngine.Object =>
				(T)ObjectFieldFn(rect, val, typeof(T), allowSceneObject);


			public static UnityEngine.Object ObjectField (UnityEngine.Object val, Type type, bool allowSceneObject=false)
			{
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;

				Cell.current.special |= Cell.Special.Field;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);
				UnityEngine.Object newVal = ObjectFieldFn(rect, val, type, allowSceneObject);
				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				if (
					(newVal==null && val!=null) ||
					(newVal!=null && val==null) ||
					(newVal!=null && val!=null && !newVal.Equals(val)) )
				{
					UI.current.MarkChanged();
					val = newVal;
				}

				return val;
			} 

			public static UnityEngine.Object ObjectField (UnityEngine.Object val, string label, Type type, bool allowSceneObject=false)
			{
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;
				
				using (Cell.RowRel(1-Cell.current.fieldWidth)) Label(label);
				using (Cell.RowRel(Cell.current.fieldWidth)) val = ObjectField(val, type, allowSceneObject);

				return val;
			}

			private static UnityEngine.Object ObjectFieldFn (Rect rect, UnityEngine.Object val, Type type, bool allowSceneObject)
			{
				Rect shrinkedRect = new Rect(rect.x, rect.y+1, rect.width, rect.height-2);
				UnityEngine.Object obj = EditorGUI.ObjectField(shrinkedRect, val, objType:type, allowSceneObjects:allowSceneObject);
				
				if (StylesCache.isPro)
					EditorGUI.DrawRect(rect, new Color(0.22f, 0.22f, 0.22f));

				if (Event.current.type == EventType.Repaint)
					UI.current.styles.field.Draw(rect, false, false, false, false);

				string name = "None";
				if (obj != null) name = obj.name.ToString();
				name += " (" + type.Name + ")";

				Rect labelRect = new Rect(rect.x+2, rect.y, rect.width-22, rect.height);
				EditorGUI.LabelField(labelRect, name, UI.current.styles.label);

				float zoom = UI.current.scrollZoom!=null ? UI.current.scrollZoom.zoom : 1;
				Rect pickerRect = new Rect(rect.x+rect.width - 12*zoom, rect.y+rect.height/2-4*zoom, 8*zoom, 8*zoom);
				UnityEngine.GUI.DrawTexture(pickerRect, StylesCache.objectPickerTex, ScaleMode.ScaleAndCrop);

				return obj;
			}

		#endregion


		#region Color Fields

			public static Color Field (Color color, bool hdr=false) 
			{ 
				if (UI.current.layout) return color;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return color;
				
				Cell.current.special |= Cell.Special.Field;
			
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);

				//if (multiplyMat == null)
				//	multiplyMat = new Material( Shader.Find("Hidden/DPLayout/Multiply") );
				//multiplyMat.SetColor("_Color", color);

				if (UnityEngine.GUI.Button(rect, "", GUIStyle.none))
				{
					Assembly editorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
					Type colorPickerType = editorAssembly.GetType("UnityEditor.ColorPicker");
					MethodInfo[] methods = colorPickerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
					MethodInfo showMethod = methods.Single( m => 
					{
						if (m.Name != "Show") return false;
						ParameterInfo[] parameters = m.GetParameters();
						if (parameters.Length == 4 && parameters[0].ParameterType == typeof(Action<Color>)) return true;
						else return false;
					} );

					Cell currentCell = Cell.current;
					EditorWindow baseWindow = UI.current.editorWindow; //EditorWindow.focusedWindow;
					Action<Color> colorChangedCallback = c => 
					{
						colorChangeCell = currentCell;
						colorChangeColor = c;
						baseWindow.Repaint();
					};
					showMethod.Invoke(null, new object[] {colorChangedCallback, color, true, hdr});
				}

				if (Event.current.type == EventType.Repaint)
					UI.current.styles.field.Draw(rect, false, false, false, false);


				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				if (colorChangeCell==Cell.current)
				{
					colorChangeCell = null;
					color = colorChangeColor;
					UI.current.MarkChanged();
				}

				//drawing color itself after it has been changed
				Rect colorRect = rect.Extended(-1);
				Rect rgbRect = colorRect;
				rgbRect.width -= 10;
				Rect aRect = colorRect;
				aRect.width = 10; 
				aRect.x = rgbRect.position.x+rgbRect.width;

				EditorGUI.DrawRect(rgbRect, new Color(color.r, color.g, color.b));
				EditorGUI.DrawRect(aRect, new Color(color.a, color.a, color.a));

				return color;
			} 

			public static Cell colorChangeCell; //null if color have not changed
			public static Color colorChangeColor;

			public static Color Field (Color val, string label) //where T: IEquatable<T>
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				
				using (Cell.RowRel(1-Cell.current.fieldWidth)) 
				{
					Label(label);

					float sum = val.r + val.g + val.b;
					float newSum = DragValueInternal(sum, 0.005f, exponentiality:1000, sensitivity:50000);
					if (newSum<0) newSum = 0;
					if (newSum>5) newSum = 5; //maximum value when color isn't white
					if (newSum>sum+0.00001f || newSum<sum-0.00001f)
					{
						val.r = val.r/sum * newSum; 
						val.g = val.g/sum * newSum; 
						val.b = val.b/sum * newSum;

						UI.current.MarkChanged();
					}
				}

				using (Cell.RowRel(Cell.current.fieldWidth)) 
					val = Field(val);

				return val;
			}

			public static void Field (ref Color val) { val = Field(val); } 
			public static void Field (ref Color val, string label) { val = Field(val, label); }

		#endregion


		#region Vectors

			public static Vector2 Field (Vector2 val)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }

				Cell.current.special |= Cell.Special.Vector;
				
				Cell.current.fieldWidth = 0.8f;
				using (Cell.LineStd) { val.x = Field(val.x, "X"); Cell.current.special |= Cell.Special.VectorX; } //after field since it should overwrite LabelField
				using (Cell.LineStd) { val.y = Field(val.y, "Y"); Cell.current.special |= Cell.Special.VectorY; }

				return val;
			}

			public static Vector2 Field (Vector2 val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.Row) Label(label);
					using (Cell.RowPx(vectorXYWidth))
					{
						using (Cell.LineStd) { Label("X"); val.x = DragValue(val.x); Cell.current.special |= Cell.Special.VectorX; }
						using (Cell.LineStd) { Label("Y"); val.y = DragValue(val.y); Cell.current.special |= Cell.Special.VectorY; }
					}
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
				{
					using (Cell.LineStd) { val.x = Field(val.x); Cell.current.special |= Cell.Special.VectorX; }
					using (Cell.LineStd) { val.y = Field(val.y); Cell.current.special |= Cell.Special.VectorY; }
				}

				return val;
			}

			public static void Field (ref Vector2 val) { val = Field(val); }
			public static void Field (ref Vector2 val, string label) { val = Field(val, label); }

			public static void Field (ref Vector2 val, string label, string xName, string yName, int xyWidth=15) { val = Field(val, label, xName, yName, xyWidth); }
			public static Vector2 Field (Vector2 val, string label, string xName, string yName, int xyWidth=15)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.Row) Label(label);
					using (Cell.RowPx(xyWidth))
					{
						using (Cell.LineStd) { Label(xName); val.x = DragValue(val.x); Cell.current.special |= Cell.Special.VectorX; }
						using (Cell.LineStd) { Label(yName); val.y = DragValue(val.y); Cell.current.special |= Cell.Special.VectorY; }
					}
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
				{
					using (Cell.LineStd) { val.x = Field(val.x); Cell.current.special |= Cell.Special.VectorX; }
					using (Cell.LineStd) { val.y = Field(val.y); Cell.current.special |= Cell.Special.VectorY; }
				}

				return val;
			}


			public static Vector3 Field (Vector3 val)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				Cell.current.fieldWidth = 0.8f;
				using (Cell.LineStd) { val.x = Field(val.x, "X"); Cell.current.special |= Cell.Special.VectorX; }
				using (Cell.LineStd) { val.y = Field(val.y, "Y"); Cell.current.special |= Cell.Special.VectorY; }
				using (Cell.LineStd) { val.z = Field(val.z, "Z"); Cell.current.special |= Cell.Special.VectorZ; }

				return val;
			}

			public static Vector3 Field (Vector3 val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.Row) Label(label);
					using (Cell.RowPx(vectorXYWidth))
					{
						using (Cell.LineStd) { Label("X"); val.x = DragValue(val.x); Cell.current.special |= Cell.Special.VectorX; }
						using (Cell.LineStd) { Label("Y"); val.y = DragValue(val.y); Cell.current.special |= Cell.Special.VectorY; }
						using (Cell.LineStd) { Label("Z"); val.z = DragValue(val.z); Cell.current.special |= Cell.Special.VectorZ; }
					}
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
				{
					using (Cell.LineStd) { val.x = Field(val.x); Cell.current.special |= Cell.Special.VectorX; }
					using (Cell.LineStd) { val.y = Field(val.y); Cell.current.special |= Cell.Special.VectorY; }
					using (Cell.LineStd) { val.z = Field(val.z); Cell.current.special |= Cell.Special.VectorZ; }
				}

				return val;
			}

			public static void Field (ref Vector3 val) { val = Field(val); }
			public static void Field (ref Vector3 val, string label) { val = Field(val, label); }


			public static Vector4 Field (Vector4 val)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				Cell.current.fieldWidth = 0.8f;
				using (Cell.LineStd) { val.x = Field(val.x, "X"); Cell.current.special |= Cell.Special.VectorX; }
				using (Cell.LineStd) { val.y = Field(val.y, "Y"); Cell.current.special |= Cell.Special.VectorY; }
				using (Cell.LineStd) { val.z = Field(val.z, "Z"); Cell.current.special |= Cell.Special.VectorZ; }
				using (Cell.LineStd) { val.w = Field(val.w, "W"); Cell.current.special |= Cell.Special.VectorW; }

				return val;
			}

			public static Vector4 Field (Vector4 val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.Row) Label(label);
					using (Cell.RowPx(vectorXYWidth))
					{
						using (Cell.LineStd) { Label("X"); val.x = DragValue(val.x); Cell.current.special |= Cell.Special.VectorX; }
						using (Cell.LineStd) { Label("Y"); val.y = DragValue(val.y); Cell.current.special |= Cell.Special.VectorY; }
						using (Cell.LineStd) { Label("Z"); val.z = DragValue(val.z); Cell.current.special |= Cell.Special.VectorZ; }
						using (Cell.LineStd) { Label("W"); val.w = DragValue(val.w); Cell.current.special |= Cell.Special.VectorW; }
					}
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
				{
					using (Cell.LineStd) { val.x = Field(val.x); Cell.current.special |= Cell.Special.VectorX; }
					using (Cell.LineStd) { val.y = Field(val.y); Cell.current.special |= Cell.Special.VectorY; }
					using (Cell.LineStd) { val.z = Field(val.z); Cell.current.special |= Cell.Special.VectorZ; }
					using (Cell.LineStd) { val.w = Field(val.w); Cell.current.special |= Cell.Special.VectorW; }
				}

				return val;
			}

			public static void Field (ref Vector4 val) { val = Field(val); }
			public static void Field (ref Vector4 val, string label) { val = Field(val, label); }


			public static Vector2D Field (Vector2D val)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				Cell.current.fieldWidth = 0.8f;
				using (Cell.LineStd) { val.x = Field(val.x, "X"); Cell.current.special |= Cell.Special.VectorX; }
				using (Cell.LineStd) { val.z = Field(val.z, "Z"); Cell.current.special |= Cell.Special.VectorZ; }

				return val;
			}

			public static Vector2D Field (Vector2D val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.Row) Label(label);
					using (Cell.RowPx(vectorXYWidth))
					{
						using (Cell.LineStd) { Label("X"); val.x = DragValue(val.x); Cell.current.special |= Cell.Special.VectorX; }
						using (Cell.LineStd) { Label("Z"); val.z = DragValue(val.z); Cell.current.special |= Cell.Special.VectorZ; }
					}
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
				{
					using (Cell.LineStd) { val.x = Field(val.x); Cell.current.special |= Cell.Special.VectorX; }
					using (Cell.LineStd) { val.z = Field(val.z); Cell.current.special |= Cell.Special.VectorZ; }
				} 

				return val;
			}

			public static void Field (ref Vector2D val) { val = Field(val); }
			public static void Field (ref Vector2D val, string label) { val = Field(val, label); }


			public static Rect Field (Rect val)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				
				Cell.current.fieldWidth = 0.7f;
				using (Cell.LineStd) val.x = Field(val.x, "Pos X");
				using (Cell.LineStd) val.y = Field(val.y, "Pos Y");
				using (Cell.LineStd) val.width = Field(val.width, "Width");
				using (Cell.LineStd) val.height = Field(val.height, "Height");

				return val;
			}

			public static Rect Field (Rect val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.Row) Label(label);
					using (Cell.RowPx(vectorWidthHeightWidth))
					{
						using (Cell.LineStd) { Label("Pos X"); val.x = DragValue(val.x); }
						using (Cell.LineStd) { Label("Pos Y"); val.y = DragValue(val.y); }
						using (Cell.LineStd) { Label("Width"); val.width = DragValue(val.width); }
						using (Cell.LineStd) { Label("Height"); val.height = DragValue(val.height); }
					}
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
				{
					using (Cell.LineStd) val.x = Field(val.x);
					using (Cell.LineStd) val.y = Field(val.y);
					using (Cell.LineStd) val.width = Field(val.width);
					using (Cell.LineStd) val.height = Field(val.height);
				}

				return val;
			}

			public static void Field (ref Rect val) { val = Field(val); }
			public static void Field (ref Rect val, string label) { val = Field(val, label); }


			public static Coord Field (Coord val)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				Cell.current.fieldWidth = 0.8f;
				using (Cell.LineStd) val.x = Field(val.x, "X");
				using (Cell.LineStd) val.z = Field(val.z, "Z");

				return val;
			}

			public static Coord Field (Coord val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.Row) Label(label);
					using (Cell.RowPx(vectorXYWidth))
					{
						using (Cell.LineStd) { Label("X"); val.x = DragValue(val.x); Cell.current.special |= Cell.Special.VectorX; }
						using (Cell.LineStd) { Label("Z"); val.z = DragValue(val.z); Cell.current.special |= Cell.Special.VectorZ; }
					}
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
				{
					using (Cell.LineStd) { val.x = Field(val.x); Cell.current.special |= Cell.Special.VectorX; }
					using (Cell.LineStd) { val.z = Field(val.z); Cell.current.special |= Cell.Special.VectorZ; }
				}

				return val;
			}

			public static void Field (ref Coord val) { val = Field(val); }
			public static void Field (ref Coord val, string label) { val = Field(val, label); }


			public static CoordRect Field (CoordRect val)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				
				Cell.current.fieldWidth = 0.7f;
				using (Cell.LineStd) val.offset.x = Field(val.offset.x, "Pos X");
				using (Cell.LineStd) val.offset.z = Field(val.offset.z, "Pos Z");
				using (Cell.LineStd) val.size.x = Field(val.size.x, "Size X");
				using (Cell.LineStd) val.size.z = Field(val.size.z, "Size Z");

				return val;
			}

			public static CoordRect Field (CoordRect val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.Row) Label(label);
					using (Cell.RowPx(vectorWidthHeightWidth))
					{
						using (Cell.LineStd) { Label("Pos X"); val.offset.x = DragValue(val.offset.x); }
						using (Cell.LineStd) { Label("Pos Z"); val.offset.z = DragValue(val.offset.z); }
						using (Cell.LineStd) { Label("Size X"); val.size.x = DragValue(val.size.x); }
						using (Cell.LineStd) { Label("Size Z"); val.size.z = DragValue(val.size.z); }
					}
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
				{
					using (Cell.LineStd) val.offset.x = Field(val.offset.x);
					using (Cell.LineStd) val.offset.z = Field(val.offset.z);
					using (Cell.LineStd) val.size.x = Field(val.size.x);
					using (Cell.LineStd) val.size.z = Field(val.size.z);
				}

				return val;
			}


			public static void Field (ref CoordRect val) { val = Field(val); }
			public static void Field (ref CoordRect val, string label) { val = Field(val, label); }


			public static Coord3D Field (Coord3D val)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				Cell.current.fieldWidth = 0.8f;
				using (Cell.LineStd) { val.x = Field(val.x, "X");  Cell.current.special |= Cell.Special.VectorX; }
				using (Cell.LineStd) { val.y = Field(val.y, "Y");  Cell.current.special |= Cell.Special.VectorX; }
				using (Cell.LineStd) { val.z = Field(val.z, "Z");  Cell.current.special |= Cell.Special.VectorX; }

				return val;
			}

			public static Coord3D Field (Coord3D val, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				Cell.current.special |= Cell.Special.Vector;
				
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.Row) Label(label);
					using (Cell.RowPx(vectorXYWidth))
					{
						using (Cell.LineStd) { Label("X"); val.x = DragValue(val.x); Cell.current.special |= Cell.Special.VectorX; }
						using (Cell.LineStd) { Label("Y"); val.y = DragValue(val.y); Cell.current.special |= Cell.Special.VectorY; }
						using (Cell.LineStd) { Label("Z"); val.z = DragValue(val.z); Cell.current.special |= Cell.Special.VectorZ; }
					}
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
				{
					using (Cell.LineStd) { val.x = Field(val.x); Cell.current.special |= Cell.Special.VectorX; }
					using (Cell.LineStd) { val.y = Field(val.y); Cell.current.special |= Cell.Special.VectorY; }
					using (Cell.LineStd) { val.z = Field(val.z); Cell.current.special |= Cell.Special.VectorZ; }
				}

				return val;
			}

		#endregion


		#region Class

			public static bool Class (object obj, string category=null, Action<FieldInfo,Cell> additionalAction=null) 
			/// Draws all values of the class marked with Val attribute (and category)
			/// if additionalAction defined performs it for each fo the fields
			/// Returns true if anything has been drawn, false if empty
			{
				Type type = obj.GetType();

				ValAttribute[] attributes = null;
				attributes = GetCachedVals(type);

				for (int a=0; a<attributes.Length; a++)
				{
					ValAttribute att = attributes[a];

					if (att.field == null) continue; //could be a property
					if (att.cat != category) continue; //null category is a category too. Not drawing all when category is null.

					Cell cell = Cell.LineStd;
					try 
					{ 
						ClassField(att, obj); 
						additionalAction?.Invoke(att.field, cell);
					}
					finally { cell.Dispose(); }
				}

				return attributes.Length != 0;
			}


			public static void ClassField (ValAttribute att, object obj)
			{
				//wrapping GetValue to delegate to avoid boxing/unboxing objects (creates too much garbage)
				//not using dynamic since it's compiled to object anyways

				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return; }
				
				if (att.type == typeof(float))
				{
					if (UI.current.layout) EmptyField();
					else
					{
						float srcVal = Getter<float>.GetGetter(att.field)(obj);
						float dstVal = Field(srcVal, att.name, att.min, att.max);
						if (Cell.current.valChanged)  att.field.SetValue(obj, dstVal); 
					}
				}

				else if (att.type == typeof(int))
				{
					if (UI.current.layout) EmptyField();
					else
					{
						int val = Getter<int>.GetGetter(att.field)(obj);
						val = Field(val, att.name, 
							min:att.min<int.MinValue ? int.MinValue : (int)att.min, 
							max:att.max>int.MaxValue ? int.MaxValue : (int)att.max); //(int)float.Max = -2000..0
						if (Cell.current.valChanged)  att.field.SetValue(obj, val); 
					}
				}

				else if (att.type == typeof(bool))
				{
					bool val = Getter<bool>.GetGetter(att.field)(obj);

					if (!att.isLeft) val = Toggle(val, att.name);
					else val = ToggleLeft(val, att.name);

					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(string))
				{
					if (UI.current.layout) EmptyField();
					else
					{
						string val = Getter<string>.GetGetter(att.field)(obj);
						val = Field(val ?? default, att.name);
						if (Cell.current.valChanged)  att.field.SetValue(obj, val); 
					}
				}

				else if (att.type == typeof(Color))
				{
					if (UI.current.layout) EmptyField();
					else
					{
						Color val = Getter<Color>.GetGetter(att.field)(obj);
						val = Field((Color)val, att.name);
						if (Cell.current.valChanged)  att.field.SetValue(obj, val); 
					}
				}

				else if (att.type == typeof(double))
				{
					if (UI.current.layout) EmptyField();
					else
					{
						double val = Getter<float>.GetGetter(att.field)(obj);
						val = Field(val, att.name);
						if (Cell.current.valChanged)  att.field.SetValue(obj, val); 
					}
				}

				else if (typeof(Enum).IsAssignableFrom(att.type))
				{
					Enum val = (Enum)att.field.GetValue(obj);  //Getter<Enum>.GetGetter(att.field)(obj); Invalid IL code
					val = Field(val, att.name);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(Vector2))
				{
					Vector2 val = Getter<Vector2>.GetGetter(att.field)(obj);
					val = Field(val, att.name);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(Vector3))
				{
					Vector3 val = Getter<Vector3>.GetGetter(att.field)(obj);
					val = Field(val, att.name);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(Vector4))
				{
					Vector4 val = Getter<Vector4>.GetGetter(att.field)(obj);
					val = Field(val, att.name);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(Vector2D))
				{
					Vector2D val = Getter<Vector2D>.GetGetter(att.field)(obj);
					val = Field(val, att.name);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(Rect))
				{
					Rect val = Getter<Rect>.GetGetter(att.field)(obj);
					val = Field(val, att.name);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(Coord))
				{
					Coord val = Getter<Coord>.GetGetter(att.field)(obj);
					val = Field(val, att.name);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(CoordRect))
				{
					CoordRect val = Getter<CoordRect>.GetGetter(att.field)(obj);
					val = Field(val, att.name);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(Coord3D))
				{
					Coord3D val = Getter<Coord3D>.GetGetter(att.field)(obj);
					val = Field(val, att.name);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(Texture2D))
				{
					if (UI.current.layout) EmptyField();
					else
					{		
						Texture2D val = Getter<Texture2D>.GetGetter(att.field)(obj);
						val = Field(val, att.name);
						if (Cell.current.valChanged)  att.field.SetValue(obj, val);
							
					}
				}

				else if (att.type == typeof(Transform))
				{
					if (UI.current.layout) EmptyField();
					else
					{
						Transform val = Getter<Transform>.GetGetter(att.field)(obj);
						val = Field(val ?? default, att.name, allowSceneObject:att.allowSceneObject);
						if (Cell.current.valChanged)  att.field.SetValue(obj, val); 
					}
				}

				else if (att.type == typeof(GameObject))
				{
					if (UI.current.layout) EmptyField();
					else
					{
						GameObject val = Getter<GameObject>.GetGetter(att.field)(obj);
						val = Field(val ?? default, att.name, allowSceneObject:att.allowSceneObject);
						if (Cell.current.valChanged)  att.field.SetValue(obj, val); 
					}
				}

				else if (att.type == typeof(UnityEngine.Object) || typeof(UnityEngine.Object).IsAssignableFrom(att.type))
				{
					UnityEngine.Object val = Getter<UnityEngine.Object>.GetGetter(att.field)(obj);
					val = Field(val ?? default, att.name, att.type, att.allowSceneObject);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val); 
				}

				else if (att.type == typeof(AnimationCurve))
				{
					AnimationCurve val = Getter<AnimationCurve>.GetGetter(att.field)(obj);
					using (Cell.Row) Label(att.name);
					using (Cell.Row) AnimationCurve (val);
					if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, val);
				}

				else if (att.type.IsArray)
				{
					bool opened = true;
					Cell.EmptyLinePx(4);
					using (Cell.LineStd)
						using (new Draw.FoldoutGroup(ref opened, att.name, isLeft:true))
							if (opened)
							{
								Type elType = att.type.GetElementType();
								Array arr = (Array)att.field.GetValue(obj);

								for (int i=0; i<arr.Length; i++)
									using (Cell.LineStd)
									{
										if (elType == typeof(float)) arr.SetValue( Field((float)arr.GetValue(i), i.ToString()), i);
									}

								if (Cell.current.valChanged && !UI.current.layout)  att.field.SetValue(obj, arr);
							}
					Cell.EmptyLinePx(4);
				}

				else if (att.type.IsClass)// || type.IsValueType)
				{
					bool opened = true;
					Cell.EmptyLinePx(4);
					using (Cell.LineStd)
						using (new Draw.FoldoutGroup(ref opened, att.name, isLeft:true))
							if (opened)
							{
								object val =  Getter<object>.GetGetter(att.field)(obj); //field.GetValue(obj);
								Draw.Class(val);
							}
					Cell.EmptyLinePx(4);
				}

				else
				{
					object val = att.field.GetValue(obj);
					if (val != null) DualLabel(att.name, val.ToString());
					else DualLabel(att.name, "null");
				}
			}

			public static class Getter<T>
			{
				static Dictionary<FieldInfo, Func<object,T>> cache = new Dictionary<FieldInfo, Func<object, T>>(); //idea: make weak?

				public static Func<object,T> GetGetter (FieldInfo field)
				{
					//return cached if any
					if (cache.TryGetValue(field, out Func<object,T> getter)) return getter;

					//creating new one
					string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
					DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] { typeof(object) }, true);
					ILGenerator ilgen = setterMethod.GetILGenerator();
					ilgen.Emit(OpCodes.Ldarg_0);
					ilgen.Emit(OpCodes.Ldfld, field);
					ilgen.Emit(OpCodes.Ret); //I have no idea of what I'm doing.jpg
					getter = (Func<object, T>)setterMethod.CreateDelegate(typeof(Func<object, T>));

					cache.Add(field, getter);
					return getter;
				}
			}

			public static void EmptyField ()
			/// Just initializes cells on layout
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return; }

				//Cell cell = Cell.LineStd;

				Cell.EmptyRowRel(1-Cell.current.fieldWidth);
				Cell.EmptyRowRel(Cell.current.fieldWidth);

				//cell.Dispose();
			}

			public static ValAttribute[] GetCachedVals (Type type)
			{
				if (valsCaches.TryGetValue(type, out ValAttribute[] attributes)) return attributes;

				List<ValAttribute> attList = new List<ValAttribute>();

				FieldInfo[] fields = type.GetFields();
				for (int f=0; f<fields.Length; f++)
				{
					ValAttribute valAtt = Attribute.GetCustomAttribute(fields[f], typeof(ValAttribute)) as ValAttribute;
					if (valAtt == null) continue;
					
					valAtt.field = fields[f];
					valAtt.type = fields[f].FieldType;

					attList.Add(valAtt);
				}

				PropertyInfo[] props = type.GetProperties();
				for (int p=0; p<props.Length; p++)
				{
					ValAttribute valAtt = Attribute.GetCustomAttribute(props[p], typeof(ValAttribute)) as ValAttribute;
					if (valAtt == null) continue;
					
					valAtt.prop = props[p];
					valAtt.type = props[p].PropertyType;

					attList.Add(valAtt);
				}

				attributes = attList.ToArray();

				//if (attributes != null)
				//	Array.Sort(attributes, (x,y) => 0);//y.priority.CompareTo(x.priority));

				valsCaches.Add(type, attributes);

				return attributes;
			}

			private static readonly Dictionary<Type, ValAttribute[]> valsCaches = new Dictionary<Type, ValAttribute[]>();
			//don't store attributes cache in runtime code

		#endregion

		#region Fields Caches

			public static void AddFieldToCellObj (FieldInfo field) => UI.current.cellObjs.ForceAdd(field, Cell.current, "Field");
			
			public static void AddFieldToCellObj (Type type, string fieldName)
			{
				FieldInfo field = GetCachedField(type,fieldName);
				if (field == null)
				{
					field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (field == null) Debug.LogError($"Could not find GUI field for {type.Name}:{fieldName}");
					else SetCachedField(field, type, fieldName);
				}

				UI.current.cellObjs.ForceAdd(field, Cell.current, "Field");
			}

			private static FieldInfo GetCachedField (Type type, string fieldName)
			{
				if (fieldsCaches.TryGetValue(type, out var fieldsDict))
				{
					if (fieldsDict.TryGetValue(fieldName, out FieldInfo field))
						return field;
					else 
						return null;
				}
				else 
					return null;
			}

			private static void SetCachedField (FieldInfo field, Type type, string fieldName)
			{
				if (!fieldsCaches.ContainsKey(type))
					fieldsCaches.Add(type, new Dictionary<string,FieldInfo>());
				fieldsCaches[type].ForceAdd(fieldName, field);
			}

			private static readonly Dictionary<Type, Dictionary<string,FieldInfo>> fieldsCaches = new Dictionary<Type, Dictionary<string,FieldInfo>>();

		#endregion


		#region Editor

			[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
			public sealed class EditorAttribute : Attribute
			{
				public Type type;
				public string cat;

				public EditorAttribute (Type type) { this.type = type; }
				public EditorAttribute (Type type, string cat) { this.type = type; this.cat = cat; }
			}


			public static bool Editor (dynamic obj, object[] args=null, string cat=null)
			/// Draws special class editor. Returns false if no editor found
			{
				if (cachedEditors == null)
					PopulateCachedEditors();

				Type type = obj.GetType();
				cachedEditors.TryGetValue((type, cat), out Delegate editorAction);
				if (editorAction != null) Invoke(editorAction, obj, args);

				return editorAction != null;
			}

			private static void Invoke<T> (Delegate action, T obj, object[] args)  
			/// Can't invoke delegate directly, so using generic wrapper 
			/// Providing a T obj will let it know what type to use
			{
				if (action is Action<T,object[]>)
					((Action<T,object[]>)action).Invoke(obj, args);

				else
					((Action<T>)action).Invoke(obj);
					// SNIPPET: Calling delegate of any type
			}
			
			/*private static Action<T> GetEditor<T>(T ignored) { return GetEditor<T>(); } //just to call generic  (SNIPPET generic dynamic)
			public static Action<T> GetEditor<T> ()
			{
				if (cachedEditors == null)
					PopulateCachedEditors();

				if (cachedEditors.TryGetValue(typeof(T), out Delegate editorAction)) return (Action<T>)editorAction;
				else return null;
			}*/

			private static Dictionary<(Type,string),Delegate> cachedEditors = null;




			private static void PopulateCachedEditors ()
			{
				cachedEditors = new Dictionary<(Type,string),Delegate>();
				Dictionary<EditorAttribute,MethodInfo> methodsDict = GetAllMethodsWithAttribute<EditorAttribute>();

				foreach (var kvp in methodsDict)
				{
					EditorAttribute editorAtt = kvp.Key;
					MethodInfo methodInfo = kvp.Value;

					if (cachedEditors.ContainsKey((editorAtt.type, editorAtt.cat))) continue;

					string methodName = methodInfo.ReflectedType.FullName + "." + methodInfo.Name;
					DynamicMethod editorMethod = new DynamicMethod(methodInfo.Name, typeof(void), new Type[1] { methodInfo.ReturnType });

					//Type delegateType = Expression.GetDelegateType(Type[] {methodInfo.GetParameters());
					Delegate action = CreateDelegate(methodInfo); //Delegate.CreateDelegate(typeof(Action<dynamic>), null, methodInfo);
					cachedEditors.Add((editorAtt.type, editorAtt.cat), action);
				}
			}


			static Delegate CreateDelegate (MethodInfo methodInfo)  
			/// SNIPPET: Creating delegate of any type from MethodInfo
			{
				ParameterInfo[] pars = methodInfo.GetParameters();

				Type[] parTypes = new Type[pars.Length + 1]; //one for return type
				for (int i=0; i<pars.Length; i++)
					parTypes[i] = pars[i].ParameterType;
				parTypes[parTypes.Length-1] = methodInfo.ReturnType;

				var delegateType = Expression.GetDelegateType(parTypes);
				return Delegate.CreateDelegate(delegateType, null, methodInfo);
			}


			private static Dictionary<T,MethodInfo> GetAllMethodsWithAttribute<T> () where T: Attribute
			{
				Dictionary<T,MethodInfo> dict = new Dictionary<T, MethodInfo>();
				string aName = typeof(Draw).Assembly.FullName;

				foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
				{
					bool isDependent = false;
					foreach (AssemblyName dan in a.GetReferencedAssemblies())
						if (dan.FullName == aName) { isDependent = true; break; }
					#if UNITY_2019_2_OR_NEWER //don't know if it will affect anything, but doint it just not to ruin everything
					if (!isDependent) continue;
					#else
					if (!isDependent && a.FullName!=aName) continue;
					#endif

					foreach(Type t in a.GetTypes())
					{
						//if (!t.IsAbstract || !t.IsSealed) continue;

						foreach(MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public))
							foreach(Attribute att in m.GetCustomAttributes())
							{
								if (att is T tatt)
								{
									if (dict.ContainsKey(tatt)) 
										Debug.LogError("Editor method is defined twice. Attach to debug."); //don't throw exception
									else dict.Add(tatt, m);
								}
							}
					}
				}

				return dict;
			}

		#endregion


		#region Other Elements

			public static bool IsButtonPrePressed =>  Cell.current == pressedButton;
			/// Should be called in the same cell with Draw.Button

			public static bool Button (string label, bool visible=true, GUIStyle style=null, MouseCursor cursor=0) 
			{
				if (UI.current.layout) return false;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return false;
				
				if (Event.current.type==EventType.MouseDown  &&  Event.current.button==0  &&  Cell.current.Contains(UI.current.mousePos)  &&  !Cell.current.disabled)
				{
					pressedButton = Cell.current;
					UI.current.editorWindow?.Repaint(); 
				}

				if (visible  &&  Event.current.type == EventType.Repaint)
				{
					if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);
					Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:buttonPadding);

					if (cursor != 0 && !Cell.current.disabled) UnityEditor.EditorGUIUtility.AddCursorRect (rect, cursor);
					if (style == null) style = UI.current.styles.button;
					style.Draw(rect, new GUIContent(label), isHover:false, isActive:false, on:pressedButton==Cell.current, hasKeyboardFocus:false);
					if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();
				}

				if (pressedButton == Cell.current  &&  Event.current.rawType == EventType.MouseUp)
				{
					pressedButton = null;
					UI.current.editorWindow?.Repaint(); 

					if (Event.current.button==0  &&  Cell.current.Contains(UI.current.mousePos)  &&  !Cell.current.inactive)
					{
						UI.current.MarkChanged();
						return true;
					}
				}

				return false;
			}


			public static bool Button (bool visible=true, MouseCursor cursor=0) 
			{
				return Button("", visible:visible, cursor:cursor);
			}


			public static bool Button (Texture2D icon, float iconScale=1, bool visible=true, MouseCursor cursor=0) 
			{
				bool pressed = Button(visible:visible, cursor:cursor);
				if (icon != null) Icon(icon, scale:iconScale);
				return pressed;
			}


			public static bool Button (string label, Texture2D icon, int iconWidth=20, float iconScale=1, bool visible=true, MouseCursor cursor=0) 
			{
				bool pressed = Button(visible:visible, cursor:cursor);
				using (Cell.RowPx(iconWidth)) Icon(icon, scale:iconScale);
				using (Cell.Row) Label(label);
				return pressed;
			}


			public static bool CheckButton (bool val, bool visible = true) 
			{
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:buttonPadding);

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);

				bool newVal;
				if (visible) newVal = UnityEngine.GUI.Toggle(rect, val, "", UI.current.styles.button);
				else newVal = UnityEngine.GUI.Toggle(rect, val, "", GUIStyle.none);

				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				if (val != newVal) 
				{ 
					UI.current.MarkChanged(); 
					val = newVal; 
				}
				return val;
			}


			public static void CheckButton (ref bool val, bool visible = true) 
				{ val = CheckButton(val, visible:visible); }


			public static bool CheckButton (bool val, string label, bool visible=true) 
			{
				val = CheckButton(val, visible:visible);
				Label(label);
				return val;
			}

			public static void CheckButton (ref bool val, string label, bool visible=true)
				{ val = CheckButton(val, label, visible:visible); }


			public static bool CheckButton (bool val, Texture2D iconOff, Texture2D iconOn, float iconScale=1, bool visible=true) 
			{
				val = CheckButton(val, visible:visible);
				Icon(val ? iconOff : iconOn, scale:iconScale);
				return val;
			}

			public static bool CheckButton (bool val, Texture2D icon, bool visible=true) 
			{
				val = CheckButton(val, visible:visible);
				Icon(icon);
				return val;
			}

			public static void CheckButton (ref bool val, Texture2D icon, bool visible=true) 
			{
				val = CheckButton(val, visible:visible);
				Icon(icon);
			}

			public static void CheckButton (ref bool val, Texture2D iconOff, Texture2D iconOn, bool visible=true) 
				{ val = CheckButton(val, iconOff, iconOn, visible:visible); }


			public static bool CheckButton (bool val, string label, Texture2D iconOff, Texture2D iconOn, int iconWidth=20, bool visible=true) 
			{
				val = CheckButton(val, visible:visible);
				using (Cell.RowPx(iconWidth)) Icon(val ? iconOff : iconOn);
				using (Cell.Row) Label(label);
				return val;
			}


			public static void CheckButton (ref bool val, string label, Texture2D iconOff, Texture2D iconOn, int iconWidth=20, bool visible=true) 
				{ val = CheckButton(val, label, iconOff, iconOn, iconWidth:iconWidth, visible:visible); }


			public static void Element (GUIStyle style)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				if (Event.current.type != EventType.Repaint) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				style.Draw(rect, false, false, false ,false);
			}

			public static void Element (Rect rect, GUIStyle style)
			{
				if (UI.current.layout) return;
				//if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				if (Event.current.type != EventType.Repaint) return;
				
				Rect scrRect = UI.current.scrollZoom!=null ? UI.current.scrollZoom.ToScreen(rect) : rect;

				style.Draw(scrRect, false, false, false ,false);
			}

			public static void Element (GUIStyle style, int padding)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				if (Event.current.type != EventType.Repaint) return;
				
				Rect scrRect = Cell.current.GetRect(UI.current.scrollZoom, padding); 

				style.Draw(scrRect, false, false, false ,false);
			}


			public static void Icon (Texture icon, Vector2 center, Color color = new Color(), float scale=1)
			/// Draws an icon in the given coord in native resolution (for zoom 1). Independent from cell
			{
				if (UI.current.layout) return;
				if (icon == null) return; //happens when performing a build
				
				float zoom = UI.current.scrollZoom != null  ?  UI.current.scrollZoom.zoom  :  1;

				Rect rect = new Rect(center.x - icon.width/2f*scale, center.y - icon.height/2f*scale, icon.width*scale, icon.height*scale);
				if (UI.current.scrollZoom != null) 
					rect = UI.current.scrollZoom.ToScreen(rect.position, rect.size);

				//non-cell releated rect, so using alternative optimize
				if (UI.current.optimizeElements)
				{
					Vector2 min = rect.min; Vector2 max = rect.max;
					if (max.x < -1 || max.y < -1 || min.x > Screen.width+1 || max.y > Screen.height+1) return; //Screen.width should give the window size
				}

				if (color.a<0.001f) UnityEngine.GUI.DrawTexture(rect, icon, ScaleMode.ScaleAndCrop);
				else UnityEngine.GUI.DrawTexture(rect, icon, ScaleMode.ScaleAndCrop, true, 0, color, 0,0);
			}

			public static void Icon  (Texture icon, Color color = new Color(), float scale=1)
			/// Draws an icon in the center of cell in native resolution (for zoom 1)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;

				Icon(icon, Cell.current.InternalCenter, color, scale);
			}


			public static void Texture (Texture texture, Material mat = null, ScaleMode scaleMode = ScaleMode.ScaleToFit)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				if (texture == null) texture = StylesCache.blankTex;
				if (texture == null) return; //doesn't load texture after build for some reason

				if (mat != null) 
				{
					if (scaleMode != ScaleMode.ScaleToFit) UnityEditor.EditorGUI.DrawPreviewTexture(rect, texture, mat, scaleMode); //leaves 1 pixel from top and bottom for some reason, so using special case for mat only
					else UnityEditor.EditorGUI.DrawPreviewTexture(rect, texture, mat); 
				}
				else UnityEngine.GUI.DrawTexture(rect, texture, ScaleMode.ScaleAndCrop); //UnityEditor.EditorGUI.DrawPreviewTexture(rect, texture, );
			}

			public static void ColorizedTexture (Texture2D texture, Color color)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				if (texture == null) texture = StylesCache.blankTex;

				UnityEngine.GUI.DrawTexture(rect, texture, ScaleMode.ScaleAndCrop, false, 1, color, 0,0); //UnityEditor.EditorGUI.DrawPreviewTexture(rect, texture, );
			}


			public static void ScrollableTexture (Texture2D texture, ScrollZoom scrollZoom)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				if (Event.current.type == EventType.ScrollWheel && Cell.current.Contains(UI.current.mousePos))
					{ scrollZoom.Zoom(Event.current.mousePosition-rect.position); Event.current.Use(); }

				if (
					Event.current.type == EventType.MouseDown  &&  Event.current.button ==scrollZoom.scrollButton  &&  Cell.current.Contains(UI.current.mousePos) ||
					Event.current.rawType == EventType.MouseUp ||
					scrollZoom.isScrolling)
						scrollZoom.Scroll();

				//float rectAspect = rect.width / rect.height;
				//float textureAspect = texture.width / texture.height;
				//float scrollYfactor = textureAspect / rectAspect;

				if (textureScrollZoomMat == null) textureScrollZoomMat = new Material( Shader.Find("Hidden/DPLayout/TextureScrollZoom") );
				textureScrollZoomMat.SetFloat("_Scale", scrollZoom.zoom);
				textureScrollZoomMat.SetFloat("_OffsetX", scrollZoom.scroll.x / rect.size.x);
				textureScrollZoomMat.SetFloat("_OffsetY",  (1-scrollZoom.scroll.y) / rect.size.y + 1);
				textureScrollZoomMat.SetTexture("_DispTex", texture);
				textureScrollZoomMat.SetFloat("_CellSizeX", rect.size.x);
				textureScrollZoomMat.SetFloat("_CellSizeY",  rect.size.y);
				Shader.SetGlobalInt("_IsLinear", UnityEditor.PlayerSettings.colorSpace==ColorSpace.Linear ? 1 : 0);

				UnityEditor.EditorGUI.DrawPreviewTexture(rect, texture, textureScrollZoomMat, ScaleMode.StretchToFill);
			}


			public static void TextureIcon (Texture2D texture, int borderRadius = 3)
			/// Draws a rounded texture preview at the cell background (no offset)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				if (textureIconMat == null) textureIconMat = new Material( Shader.Find("Hidden/DPLayout/TextureIcon") );
				textureIconMat.SetFloat("_Borders", 1/rect.width);
				Shader.SetGlobalInt("_IsLinear", UnityEditor.PlayerSettings.colorSpace==ColorSpace.Linear ? 1 : 0);

				if (texture == null) texture = StylesCache.blankTex;

				UnityEditor.EditorGUI.DrawPreviewTexture(rect, texture, textureIconMat);
			}


			public static void TextureIcon (Texture2DArray textureArr, int index, int borderRadius = 3)
			/// Draws a rounded texture preview at the cell background (no offset)
			{
				if (textureArr == null) 
					{ TextureIcon(null, borderRadius); return; }

				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				if (textureArrIconMat == null) textureArrIconMat = new Material( Shader.Find("Hidden/DPLayout/TextureArrayIcon") );
				textureArrIconMat.SetFloat("_Borders", 1/rect.width);
				textureArrIconMat.SetFloat("_Index", index);
				textureArrIconMat.SetTexture("_MainTexArr", textureArr);
				Shader.SetGlobalInt("_IsLinear", UnityEditor.PlayerSettings.colorSpace==ColorSpace.Linear ? 1 : 0);

				UnityEditor.EditorGUI.DrawPreviewTexture(rect, StylesCache.blankTex, textureArrIconMat);
			}


			public static void MatrixPreviewTexture (Texture2D texture, bool colorize=false, bool relief=false, float min=0, float max=1, int margins=0)
			{
				if (textureRawMat==null) textureRawMat = new Material(Shader.Find("Hidden/MapMagic/TexturePreview"));

				textureRawMat.SetFloat("_Colorize", colorize ? 1 : 0);
				textureRawMat.SetFloat("_Relief", relief ? 1 : 0);
				textureRawMat.SetFloat("_MinValue", min);
				textureRawMat.SetFloat("_MaxValue", max);
				textureRawMat.SetInt("_Margins", margins);

				Texture(texture, textureRawMat);
			}


			public static void MatrixPreviewReliefSwitch (ref bool colorize, ref bool relief)
			{
				using (Cell.Full)
				{
					Cell.EmptyRow();
					using (Cell.RowPx(12))
					{
						Cell.EmptyLine();
						using (Cell.LinePx(12))
						{
							Texture2D icon;
							if (colorize && relief) icon = UI.current.textures.GetTexture("DPUI/TexCh/BlackWhite");
							else icon = UI.current.textures.GetTexture("DPUI/TexCh/ColRelief");

							if (Draw.Button(icon, visible:false))
								{ colorize = !colorize; relief = colorize; }
						}
						Cell.EmptyLinePx(3);
					}
					Cell.EmptyRowPx(3);
				}
			}


			public static void ProgressBar (float val, Color color)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				Draw.Element(UI.current.styles.progressBarBackground);

				ProgressBarGauge(val, StylesCache.progressBarFill);
			}


			public static void ProgressBarGauge (float val, Texture2D fillTex=null, Color color=new Color())
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				if (fillTex == null) fillTex = StylesCache.progressBarFill;

				if (multiplyMat == null) multiplyMat = new Material( Shader.Find("Hidden/DPLayout/Multiply") );

				Rect gaugeRect = rect;
				gaugeRect.width = gaugeRect.width * val;
				gaugeRect.width = Mathf.RoundToInt(gaugeRect.width);
				if (gaugeRect.width < 1) return;

				if (color.a < 0.00001f) color = new Color(0.4f, 0.65f, 1f);
				multiplyMat.SetColor("_Color", color);

				UnityEditor.EditorGUI.DrawPreviewTexture(gaugeRect, fillTex);
				UnityEditor.EditorGUI.DrawPreviewTexture(gaugeRect, fillTex, multiplyMat);
			}


			public static void URL (string label, string url)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);

				GUIStyle style = UI.current.styles.url;
				if (UnityEngine.GUI.Button(rect, label, style)) Application.OpenURL(url); 
				UnityEditor.EditorGUIUtility.AddCursorRect (rect, UnityEditor.MouseCursor.Link);

				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();
			}


			public static void Helpbox (string label, MessageType messageType = MessageType.None)
			{
				//Element(UI.current.styles.foldoutBackground);
				//Label(label, UI.current.styles.helpBox);

				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);

				UnityEditor.EditorGUI.HelpBox(rect, label, messageType);
			}


			/*public static Enum EnumField (Enum val) 
			{ 
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;
			
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);

				if (enumStyle == null) 
				{
					Texture2D tex = Resources.Load("DPUI/Backgrounds/Enum") as Texture2D;
					enumStyle = new GUIStyle(); 
					enumStyle.normal.background = tex;
					enumStyle.border = new RectOffset(1,1,1,1);
					enumStyle.overflow.bottom += 1;
				}

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);

				Enum newVal = EditorGUI.EnumPopup(rect, val, enumStyle);

				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				if (!val.Equals(newVal))
				{
					UI.current.MarkChanged();
					val = newVal;
				}

				return val;
			} 
			public static void EnumField (ref Enum val) { val = EnumField(val); } 

			public static Enum EnumField (Enum val, string label) 
			{ 
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }

				using(Cell.RowRel(1-Cell.current.fieldWidth))
					Label(label);

				using (Cell.RowRel(Cell.current.fieldWidth)) 
					val = EnumField(val);

				return val;
			}
			public static void EnumField (ref Enum val, string label) { val = EnumField(val, label); }*/


			public static int PopupSelector (int selectedIndex, string[] displayedOptions) 
			{
				if (UI.current.layout) return selectedIndex;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return selectedIndex;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:buttonPadding);

				if (Cell.current.disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);
				int newIndex = UnityEditor.EditorGUI.Popup(rect, selectedIndex, displayedOptions, UI.current.styles.enumClose);
				if (Cell.current.disabled) UnityEditor.EditorGUI.EndDisabledGroup();

				Vector2 signPos = Cell.current.InternalCenter; signPos.x += Cell.current.finalSize.x/2 - 10; signPos.y-=1;
				Icon(StylesCache.enumSign, signPos);

				if (!newIndex.Equals(selectedIndex))
				{
					UI.current.MarkChanged();
					selectedIndex = newIndex;
				}

				return selectedIndex;
			}


			public static void PopupSelector (ref int selectedIndex, string[] displayedOptions)
				{ selectedIndex = PopupSelector(selectedIndex, displayedOptions); }


			public static int PopupSelector (int selectedIndex, string[] displayedOptions, string label) 
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return selectedIndex; }

				using (Cell.RowRel(1-Cell.current.fieldWidth)) Label(label);
				using (Cell.RowRel(Cell.current.fieldWidth)) selectedIndex = PopupSelector(selectedIndex, displayedOptions);

				return selectedIndex;
			}


			public static void PopupSelector (ref int selectedIndex, string[] displayedOptions, string label) =>
				selectedIndex = PopupSelector(selectedIndex, displayedOptions, label);

			public static T PopupSelector<T> (T selectedItem, T[] allItems, string[] displayedOptions, string label)
			{
				int index = allItems.Find(selectedItem);
				int newIndex = PopupSelector(index, displayedOptions);

				if (newIndex != index  &&  newIndex >= 0)
					return allItems[newIndex];
				else
					return selectedItem;
			}

			public static T PopupSelector<T> (ref T selectedItem, T[] allItems, string[] displayedOptions, string label) =>
				selectedItem = PopupSelector(selectedItem, allItems, displayedOptions, label);


			public static void TypeSelector<T> (ref T obj, string label, ref Type[] allTypes, ref string[] allNames, bool allAssemblies=false)
			{
				if (allTypes == null)
				{
					allTypes = typeof(T).Subtypes(allAssemblies:allAssemblies);

					allNames = new string[allTypes.Length];
					for (int i=0; i<allNames.Length; i++)
						allNames[i] = allTypes[i].Name;
				}

				int selectedNum = obj != null ? allTypes.Find(obj.GetType()) : -1;
				Draw.PopupSelector(ref selectedNum, allNames, label);

				if (Cell.current.valChanged)
					obj = (T)Activator.CreateInstance(allTypes[selectedNum]);
			}


			public static void SilhouetteLabel (string label)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);
				
				rect.x+=1; rect.y+=1; EditorGUI.LabelField(rect, label, UI.current.styles.blackLabel);
				rect.y-=2; EditorGUI.LabelField(rect, label, UI.current.styles.blackLabel);
				rect.x-=2;  EditorGUI.LabelField(rect, label, UI.current.styles.blackLabel);
				rect.y+=2; EditorGUI.LabelField(rect, label, UI.current.styles.blackLabel);
				rect.x+=1; rect.y-=1; //EditorGUI.LabelField(rect, label, UI.current.styles.whiteLabel);

				rect.x+=1; EditorGUI.LabelField(rect, label, UI.current.styles.blackLabel);
				rect.x-=2; EditorGUI.LabelField(rect, label, UI.current.styles.blackLabel);
				rect.x+=1; rect.y-=1;  EditorGUI.LabelField(rect, label, UI.current.styles.blackLabel);
				rect.y+=2; EditorGUI.LabelField(rect, label, UI.current.styles.blackLabel);
				rect.y-=1; EditorGUI.LabelField(rect, label, UI.current.styles.whiteLabel);
			}


			public static void BackgroundRightLabel (string label, GUIStyle style=null, GUIStyle backStyle=null, float rightOffset=4)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);
				if (style==null) style = UI.current.styles.label;

				float width = style.CalcSize( new GUIContent(label) ).x;
				rect.x += rect.width - width;
				rect.width = width;
				rect.x -= rightOffset * (UI.current.scrollZoom!=null ? UI.current.scrollZoom.zoom : 0);

				Rect backRect = rect;
				backRect.yMin += 2 * (UI.current.scrollZoom!=null ? UI.current.scrollZoom.zoom : 0);
				backRect.yMax -= 2 * (UI.current.scrollZoom!=null ? UI.current.scrollZoom.zoom : 0);
				backRect.x -= 1 * (UI.current.scrollZoom!=null ? UI.current.scrollZoom.zoom : 0);
				backRect.width += 3 * (UI.current.scrollZoom!=null ? UI.current.scrollZoom.zoom : 0);
				
				if (backStyle != null) 
				{
					if (Event.current.type == EventType.Repaint)	
						backStyle.Draw(backRect, false, false, false, false);
				}

				else EditorGUI.DrawRect(backRect, new Color(0,0,0,0.8f));
				
				EditorGUI.LabelField(rect, label, style:style);
			}


			public static string EditableLabel (string label, GUIStyle style=null)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return label; }
				
				Cell editLabelCell = Cell.current;

				using (Cell.Row)
				{
					if (style == null) style = UI.current.styles.middleLabel;

					if (activeEditLabelCell != editLabelCell)  //non-editable
						Label(label, style);  
					else 
						label = LabelEditField(label, style);
				}

				using (Cell.RowPx(20))
				{
					if (Button(StylesCache.pencilTex, visible:false, cursor:UnityEditor.MouseCursor.Link))
						activeEditLabelCell = editLabelCell;
				}

				return label;
			}


			public static void EditableLabel (ref string label)
				{ label = EditableLabel(label); }


			public static string EditableLabelRight (string label, GUIStyle style=null)
			/// Same editable label, but placed to the right of pencil icon
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return label; }
				
				Cell editLabelCell = Cell.current;

				using (Cell.RowPx(20))
				{
					if (Button(StylesCache.pencilTex, visible:false, cursor:UnityEditor.MouseCursor.Link))
						activeEditLabelCell = editLabelCell;
				}

				using (Cell.Row)
				{
					if (style == null) style = UI.current.styles.middleLabel;

					if (activeEditLabelCell != editLabelCell)  //non-editable
						Label(label, style);  
					else 
						label = LabelEditField(label, style);
				}

				return label;
			}


			public static void EditableLabelRight (ref string label, GUIStyle style=null)
				{ label = EditableLabelRight(label, style); }


			public static void SearchLabel (ref string label, GUIStyle style=null, bool forceFocus=false)
				{ label = SearchLabel(label, style, forceFocus); }


			public static string SearchLabel (string label, GUIStyle style=null, bool forceFocus=false)
			{
				GUIStyle backStyle = UI.current.textures.GetElementStyle("DPUI/Backgrounds/RoundField");
				Draw.Element(backStyle);

				using (Cell.Row)
				{
					Cell editCell = Cell.current;

					//enabling edit
					if (Cell.current.Contains(UI.current.mousePos)  &&  UI.current.mouseButton==0)
						activeEditLabelCell = Cell.current;

					UnityEditor.EditorGUIUtility.AddCursorRect (Cell.current.GetRect(UI.current.scrollZoom), UnityEditor.MouseCursor.Text);

					using (Cell.RowPx(20))
						Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/ZoomSmall"), scale:0.5f);

					using (Cell.Row)
					{
						if (style == null) style = UI.current.styles.middleBlackLabel;

						if (forceFocus || activeEditLabelCell == editCell)  //editable
							label = LabelEditField(label, style);
						else 
							Label(label, style);  
					}
				}

				using (Cell.RowPx(22))
				{
					if (Button(UI.current.textures.GetTexture("DPUI/Icons/Close"), iconScale:0.5f, visible:false))
					{
						UI.current.MarkChanged();
						label = "";
						UnityEditor.EditorGUI.FocusTextInControl(null); 
					}
				}

				return label;
			}


			public static string LabelEditField (string label, GUIStyle style=null)
			{
				if (UI.current.layout) return label;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return label;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);

				if (style == null) style = UnityEditor.EditorStyles.label;

				//editing
				UnityEngine.GUI.SetNextControlName("LayerFoldoutNextFocus"); //to focus in text right after pressing edit button
				string newLabel = UnityEditor.EditorGUI.TextField(rect, label, style:style);
				UnityEditor.EditorGUI.FocusTextInControl("LayerFoldoutNextFocus"); 
				UI.current.editorWindow?.Repaint();  

				//exit editing
				if (Event.current.keyCode==KeyCode.KeypadEnter || Event.current.keyCode==KeyCode.Return || Event.current.keyCode==KeyCode.Escape || //if enter or esc
					(Event.current.type==EventType.MouseDown && !rect.Contains(Event.current.mousePosition))) //if clicked somewhere else
						activeEditLabelCell = null;

				if (newLabel != label)
				{
					UI.current.MarkChanged();
					label = newLabel;
				}

				return label;
			}

			public static Cell activeEditLabelCell; //to know what label we are currently editing


			public static void Grid (
				Color color,
				Color background = new Color(),
				int cellsNumX = 4,
				int cellsNumY = 4,
				bool fadeWithZoom = true)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);
				Vector2 cellSize = new Vector2(rect.width / cellsNumX, rect.height / cellsNumY);
				DrawGrid(rect, cellSize, new Vector2(), color, background, 0.5f, 1,fadeWithZoom);
			}


			public static void StaticGrid (Rect displayRect,
				float cellSize, 
				Color color,
				Color background = new Color(),
				bool fadeWithZoom = true)
			/// Draws grid in window-related rect. Moves with zoom. Useful for backgrounds
			{
				if (UI.current.layout) return;
				
				//float dpiFactor = UI.current.DpiScaleFactor;
				//displayRect.width = (int)(float)(displayRect.width*dpiFactor + 0.5f) / dpiFactor;

				Vector2 dispCellSize = new Vector2(cellSize, cellSize);
				if (UI.current.scrollZoom != null)
					dispCellSize *= UI.current.scrollZoom.zoom;
				//dispCellSize.x = (int)(float)(dispCellSize.x+0.5f);
				//dispCellSize.y = (int)(float)(dispCellSize.y+0.5f);

				Vector2 dispOffset;
				if (UI.current.scrollZoom != null)
					dispOffset = new Vector2(-UI.current.scrollZoom.scroll.x+displayRect.x, UI.current.scrollZoom.scroll.y-displayRect.height-displayRect.y); //-1 makes the line pass through 0-1 pixel
				else
					dispOffset = new Vector2(displayRect.x, -displayRect.height-displayRect.y);
				dispOffset.x += dispCellSize.x*10000;
				dispOffset.y += dispCellSize.y*10000;  //hiding the line pass through 0-1 pixel in 10000 cells away
				
				//dispOffset.x = (int)(float)(dispOffset.x+0.5f);
				//dispOffset.y = (int)(float)(dispOffset.y+0.5f);

				//displayRect.position *= dpiFactor;
				//displayRect.size *= dpiFactor;

				DrawGrid(displayRect, dispCellSize, dispOffset, color, background, 1, 0, fadeWithZoom);
			}


			private static void DrawGrid (Rect displayRect,
				Vector2 cellSize, Vector2 cellOffset,
				Color color, Color background,
				float lineOpacity, float bordersOpacity,
				bool fadeWithZoom)
			/// Draws grid in window-related rect
			{
				if (UI.current.layout) return;
				if (StylesCache.blankTex == null) return;  //happens when performing a build
				
				if (background.a == 0 && background.r == 0 && background.g == 0 && background.b == 0)
					background = new Color(color.r, color.g, color.b, 0); //to avoid blacking on fadeOnZoom

				if (gridMat == null) gridMat = new Material( Shader.Find("Hidden/DPLayout/Grid") );

				if (fadeWithZoom)
				{	
					float clampZoom = UI.current.scrollZoom!=null ? UI.current.scrollZoom.zoom : 1;
					if (clampZoom > 1) clampZoom = 1;
					color = color*clampZoom  +  background*(1-clampZoom);
				}

				float dpiFactor = UI.current.DpiScaleFactor;

				gridMat.SetColor("_Color", color);
				gridMat.SetColor("_Background", background);

				gridMat.SetFloat("_CellSizeX", cellSize.x * dpiFactor);
				gridMat.SetFloat("_CellSizeY", cellSize.y * dpiFactor);
				gridMat.SetFloat("_CellOffsetX", cellOffset.x * dpiFactor); 
				gridMat.SetFloat("_CellOffsetY", cellOffset.y * dpiFactor);

				gridMat.SetFloat("_LineOpacity", lineOpacity);
				gridMat.SetFloat("_BordersOpacity", bordersOpacity);

				gridMat.SetVector("_ViewRect", new Vector4(displayRect.x, displayRect.y, displayRect.size.x, displayRect.size.y) * dpiFactor);

				UnityEditor.EditorGUI.DrawPreviewTexture(displayRect, StylesCache.blankTex, gridMat, ScaleMode.StretchToFill);
			}


			public static void StaticAxis (Rect displayRect, int pos, bool isVertical, Color color)
			/// Infinite horizontal or vertical line
			/// Draws 1-pixel rect in base coordinates (or nothing if it is out of cell)
			/// Used to draw axis
			/// StaticGrid and StaticAxis use displayRect (like (0, 0, Screen.width-toolbarWidth, Screen.height)) insteado of cell
			{
				if (UI.current.layout) return;
				
				Rect lineRect = isVertical ?
					new Rect (
						pos*UI.current.scrollZoom.zoom + UI.current.scrollZoom.scroll.x,
						displayRect.y,
						1,
						displayRect.height) :
					new Rect (
						displayRect.x,
						pos*UI.current.scrollZoom.zoom + UI.current.scrollZoom.scroll.y,
						displayRect.width,
						1);

				UnityEditor.EditorGUI.DrawRect(lineRect, color);
			}


			public static void Histogram (float[] histogram, Vector4 color, Vector4 backColor)
			{
				Material histogramMat = UI.current.textures.GetMaterial("Hidden/DPLayout/Histogram");
				histogramMat.SetFloatArray("_Histogram", histogram);
				histogramMat.SetVector("_Backcolor", backColor);
				histogramMat.SetVector("_Forecolor", color);
				histogramMat.SetInt("_HistogramLength", histogram.Length);
				
				Draw.Texture(null, histogramMat);
			}


			public static bool LayerChevron (
				int num,
				ref int expanded) 
			{
				int newexpanded = expanded;
				if (LayerChevron(expanded==num))
					newexpanded = num;
				else 
					{ if (expanded==num) newexpanded = -1; }

				if (expanded != newexpanded)
				{
					expanded = newexpanded;
					UI.RemoveFocusOnControl(); //otherwise still editing field in the other layer, and it got the previous value
					return true;
				}

				return false;
			}


			public static bool LayerChevron (bool expanded) 
			{
				return CheckButton(expanded, 
					UI.current.textures.GetTexture("DPUI/Chevrons/Down"),  
					UI.current.textures.GetTexture("DPUI/Chevrons/Left"),
					iconScale:0.5f,
					visible:false);
			}


			public static void Equalizer (float[] arr, int length=-1)
			{
				if (length < 0)
					length = arr.Length;

				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return; }

				for (int i=0; i<arr.Length; i++)
					using (Cell.Row) EqualizerElement(ref arr[i]);
			}


			public static void EqualizerElement (ref float val)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Texture2D diamonTex = StylesCache.diamonTex;

				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				Rect lineRect = new Rect(rect.x+rect.width/2-1, rect.y, 2, rect.height);
				UnityEditor.EditorGUI.DrawRect(lineRect, Color.gray);

				Vector2 iconCenter = new Vector2(rect.x+rect.width/2, rect.y + (1-val)*rect.height);
				Rect iconRect = new Rect(iconCenter.x-diamonTex.width/2, iconCenter.y-diamonTex.height/2, diamonTex.width, diamonTex.height);
				UnityEngine.GUI.DrawTexture(iconRect, diamonTex, ScaleMode.ScaleAndCrop);

				if (DragDrop.TryDrag(Cell.current, UI.current.mousePos))
				{
					float newVal = 1-((DragDrop.initialMousePos.y + DragDrop.totalDelta.y - rect.y) / rect.height);
					if (newVal > 1) newVal = 1;
					if (newVal < 0) newVal = 0;
					if (val != newVal) UI.current.MarkChanged();
					val = newVal;

					UnityEditor.EditorGUI.LabelField( new Rect(iconRect.x-15, iconRect.y+iconRect.height, iconRect.width+30, 18), val.ToString() );

					iconRect = new Rect(iconCenter.x-diamonTex.width/2, iconCenter.y-diamonTex.height/2, diamonTex.width, diamonTex.height);
				}
				DragDrop.TryStart(Cell.current, UI.current.mousePos, iconRect);
				DragDrop.TryRelease(Cell.current, UI.current.mousePos);

				UnityEngine.GUI.DrawTexture(iconRect, diamonTex, ScaleMode.ScaleAndCrop);
			}


			public static float FieldDragIcon (float val, Texture2D icon=null) 
			/// MM1-style field with a drag slider cursor icon
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return val; }
				
				Draw.Element(UI.current.styles.field, padding:fieldPadding);

				using (Cell.RowPx(20)) 
				{
					if (icon==null) icon = UI.current.textures.GetTexture("DPUI/Icons/Slider");
					Icon(icon);
					val = DragValue(val);
				}

				using (Cell.Row)
					val = Field(val, style:UI.current.styles.label);

				return val;
			}

			public static void FieldDragIcon (ref float val, Texture2D icon=null) { val = FieldDragIcon(val,icon); }


			public static float IconField (float val, string label, Texture2D icon)
			{
				using (Cell.RowRel(1-Cell.current.fieldWidth))
				{
					using (Cell.RowPx(icon.width)) Icon(icon);
					using (Cell.Row) Label(label);
					val = DragValue(val);
				}
				using (Cell.RowRel(Cell.current.fieldWidth))
					val = Field(val);

				return val;
			}

			public static void IconField (ref float val, string label, Texture2D icon) { val = IconField(val, label, icon); }


			public static void Mesh (Mesh mesh, Material mat, bool clip=true)
			/// Draws a mesh within a cell, presuming mesh fits in 0-1 bounds
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				Matrix4x4 prs = Matrix4x4.TRS(
					new Vector3 (rect.position.x, rect.position.y+rect.height, 0), 
					new Quaternion(0.7071067811865475f, 0, 0, 0.7071067811865475f), 
					new Vector3(rect.size.x, 0, rect.size.y) );

				if (mat.HasProperty("_ClipRect"))
				{
					if (clip)
					{
						float dpiScaleFactor = UI.current.DpiScaleFactor;

						Rect clipRect = rect;

						
						clipRect.position *= dpiScaleFactor;
						clipRect.size *= dpiScaleFactor;
						clipRect.position += UI.current.subWindowRect.position * dpiScaleFactor;

						Rect containerRect = GetRootVisualContainerRect(UI.current.editorWindow);
						containerRect.position *= dpiScaleFactor;
						containerRect.size *= dpiScaleFactor;
						clipRect.position += containerRect.position;

						clipRect = CoordinatesExtensions.Intersect(containerRect, clipRect);

						//clipping by window
						//hacky there, but I'm tired of that stuff. Seems to be no way to get proper clip rect to shader, neither get it by code
					//	Rect containerRect = GetRootVisualContainerRect(UI.current.editorWindow);
					//	Rect windowRect = new Rect(0,0, containerRect.width, containerRect.height); //new Rect(10,10,1000,1000);
					//	clipRect = CoordinatesExtensions.Intersect(windowRect, clipRect);
					//	clipRect.y += containerRect.y + UI.current.subWindowRect.y;
					//	clipRect.x += containerRect.x;
						
						mat.SetVector("_ClipRect", new Vector4(clipRect.x, clipRect.y, clipRect.xMax, clipRect.yMax)); 
					}
					else mat.SetVector("_ClipRect", new Vector4(0, 0, -1, -1)); 
				}

				mat.SetPass(0);
				Graphics.DrawMeshNow(mesh, prs);
			}

			private static Rect GetRootVisualContainerRect (EditorWindow window)
			{
				#if UNITY_2019_1_OR_NEWER
				return window.rootVisualElement.layout;
				#else
				if (rvcLayoutProp == null)
				{
					Type winType = UI.current.editorWindow.GetType();
					rvcProp = winType.GetProperty("rootVisualContainer", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				object rvc = rvcProp.GetValue(UI.current.editorWindow);
				if (rvcLayoutProp == null)
				{
					Type rvcType = rvc.GetType();
					rvcLayoutProp = rvcType.GetProperty("layout", BindingFlags.Instance | BindingFlags.Public);
				}
				return (Rect)rvcLayoutProp.GetValue(rvc);
				#endif
			}

			#if !UNITY_2019_1_OR_NEWER
			private static PropertyInfo rvcProp = null;
			private static PropertyInfo rvcLayoutProp = null;
			#endif

			public static void Rect (Color color)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				EditorGUI.DrawRect(rect, color);
			}


			public static void Rect (Rect rect, Color color)
			{
				if (UI.current.layout) return;
				
				rect = UI.current.scrollZoom.ToScreen(rect);

				EditorGUI.DrawRect(rect, color);
			}


			public static void ToolbarSeparator ()
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect rect = Cell.current.GetRect(UI.current.scrollZoom);

				EditorGUI.DrawRect(rect, StylesCache.isPro ? Color.black : new Color(0.57f, 0.57f, 0.57f, 1));
			}


			public static void DebugRect (int level=0)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				DebugRect(new Rect(Cell.current.worldPosition, Cell.current.finalSize), level);
			}

			public static void DebugRect (Rect rect, int level=0)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				if (UI.current.scrollZoom != null)
					rect = UI.current.scrollZoom.ToScreen(rect.position, rect.size); 

				Color color = new Color(level/3f, 1-level/5f, 0, 0.5f);

				if (rect.width < 0 || rect.height < 0)
					color = new Color(0, 0, 0, 1f);

				if (multiplyMat == null)
					multiplyMat = new Material( Shader.Find("Hidden/DPLayout/Multiply") );
				multiplyMat.SetColor("_Color", color);

				EditorGUI.DrawPreviewTexture(rect, StylesCache.debugTex);
			}

			public static void DebugRect (Cell cell, int level=0)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				DebugRect(new Rect(cell.worldPosition, cell.finalSize), level);
			}

			public static void DebugRectRecursive (Cell cell, int level=0)
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				DebugRect(new Rect(cell.worldPosition, cell.finalSize), level);

				if (cell.subCells != null)
				{
					int childCount = cell.subCells.Count;
					for (int i=0; i<childCount; i++)
					{
						Cell child = cell.subCells[i];
						DebugRectRecursive(child, level+1);
					}
				}
			}

			public static void DebugRectRecursive ()
				{ DebugRectRecursive(Cell.current); }

			public static void DebugMousePos ()
			{
				GetCursorPos(out Vector2Int hMousePos);
				Vector2 hwMousePos = new Vector2(hMousePos.x, hMousePos.y);
				hwMousePos -= UI.current.editorWindow.position.position;
				hwMousePos.y -= 20;
				Rect hwMouseRect = new Rect(hwMousePos.x-3, hwMousePos.y-3, 6,6);
				EditorGUI.DrawRect(hwMouseRect, Color.green);

				Rect mouseRect = new Rect(Event.current.mousePosition.x-3, Event.current.mousePosition.y-3, 6,6);
				EditorGUI.DrawRect(mouseRect, Color.red);
			}


		#endregion


		#region Animation Curve

			private static AnimationCurve windowCurveRef = null;

			private static Type curveWindowType;
			private static Type GetCurveWindowType ()
			{
				if (curveWindowType == null) curveWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.CurveEditorWindow");
				return curveWindowType;
			}

			public static void AnimationCurve (
				AnimationCurve src, 
				Rect ranges=new Rect(), 
				Color color = new Color())
			{
				if (UI.current.layout) return;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;
				
				Rect displayRect = Cell.current.GetRect(UI.current.scrollZoom);

				if (ranges.width < Mathf.Epsilon && ranges.height < Mathf.Epsilon) { ranges.width = 1; ranges.height = 1; }
				if (color.a == 0) color = Color.white;

				//recording undo on change if the curve editor window is opened (and this current curve is selected)
				try
				{
					Type curveWindowType = GetCurveWindowType();
					if (UI.current.editorWindow != null  &&  EditorWindow.focusedWindow.GetType() == curveWindowType)
					{
						AnimationCurve windowCurve = curveWindowType.GetProperty("curve").GetValue(EditorWindow.focusedWindow, null) as AnimationCurve;
						if (windowCurve == src)
						{
							if (windowCurveRef == null) windowCurveRef = windowCurve.Copy();
							if (!windowCurve.IdenticalTo(windowCurveRef))
							{
								Keyframe[] tempKeys = windowCurve.keys;
								windowCurve.keys = windowCurveRef.keys;
								UI.current.MarkChanged();

								windowCurve.keys = tempKeys;

								windowCurveRef = windowCurve.Copy();
							}
						}
					}
					else windowCurveRef = null;
				}
				catch {};

				if (Event.current.type!=EventType.MouseDown || Event.current.button!=1 || EditorWindow.focusedWindow.GetType()==curveWindowType )
				//hack to allow right clicking on curve to expose it
					EditorGUI.CurveField(displayRect, src, color, ranges); 
			}

		#endregion


		#region Foldout

			public static void Foldout (ref bool src, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return; }

				src = Draw.CheckButton(src, visible:false);

				using (Cell.Row)
					Draw.Label(label, UI.current.styles.boldLabel);

				using (Cell.RowPx(15))
					src = CheckButton(src, 
						UI.current.textures.GetTexture("DPUI/Chevrons/SmallDown"),  
						UI.current.textures.GetTexture("DPUI/Chevrons/SmallLeft"),
						visible:false);
			}


			public static void FoldoutLeft (ref bool src, string label)
			{
				if (UI.current.optimizeCells && !UI.current.IsInWindow())
					{ Cell.current.Skip(); return; }

				src = Draw.CheckButton(src, visible:false);

				using (Cell.RowPx(10))
					src = CheckButton(src, 
						UI.current.textures.GetTexture("DPUI/Chevrons/SmallDown"),  
						UI.current.textures.GetTexture("DPUI/Chevrons/SmallRight"),
						visible:false);

				using (Cell.Row)
					Draw.Label(label, UI.current.styles.boldLabel);
			}



			public struct FoldoutGroup : IDisposable
			{
				Cell outCell;
				Cell innerCell;

				public FoldoutGroup (ref bool opened, string label, bool isLeft=false, GUIStyle style=null, bool backgroundWhileClosed=false, int padding=3)
				{
					outCell = null;
					innerCell = null;

					if (style == null) 
						style = UI.current.styles.foldoutBackground;

					if (backgroundWhileClosed || opened) Draw.Element(style);

					//Cell.EmptyLinePx(3);
					using (Cell.LineStd) 
						using (Cell.Padded(padding,0,0,0))
						{
							Cell.current.trackChange = false;

							if (isLeft) Draw.FoldoutLeft(ref opened, label);
							else Draw.Foldout(ref opened, label);
						}

					if (opened)
					{
						outCell = Cell.Line;
						innerCell = Cell.Padded(padding + (isLeft? 10 : 0), 3, 0, 0); //Cell.Padded(padding + (isLeft? 10 : 0), 3, 0, 0);

						//Do stuff
					}	
				}

				public void Dispose ()
				{
					//Do stuff

					Cell.EmptyLinePx(3);
					if (innerCell != null) innerCell.Dispose();
					if (outCell != null) outCell.Dispose();

				}
			}

			//usage example:
			//	using (Cell.LineStd) 
			//	{
			//		using (new Draw.FoldoutGroup(ref opened, "Foldout"))
			//		if (opened)
			//		{
			//			//do stuff
			//		}
			//	}

		#endregion

		#region Scroll

			public struct ScrollGroup : IDisposable
			{
				Cell innerCell;
				ScrollZoom backupScrollZoom;
				Vector2 backupMousePos;

				public const float scrollWidth = 15;

				public ScrollGroup (ref float scroll, bool enabled=true)
				// if not enabled - skipping the scroll section
				{
					if (!enabled)
						{innerCell=null; backupScrollZoom = UI.current.scrollZoom; backupMousePos = UI.current.mousePos; return; }

					float guizoom = UI.current.scrollZoom != null ? UI.current.scrollZoom.zoom : 1;
					Vector2 guiscroll = UI.current.scrollZoom != null ? UI.current.scrollZoom.scroll : Vector2.zero;

					Rect cellRect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);
					Vector2 offset = Cell.current.worldPosition * guizoom;
					Vector2 size = Cell.current.finalSize * guizoom;

					backupScrollZoom = UI.current.scrollZoom;
					UI.current.scrollZoom = new ScrollZoom();
					UI.current.scrollZoom.zoom = backupScrollZoom.zoom;

					backupMousePos = UI.current.mousePos;
					UI.current.mousePos.y += scroll;
					//Event.current.mousePosition = new Vector2(Event.current.);

					innerCell = Cell.Custom(Vector2.zero, Cell.current.finalSize); //Cell.Custom(0,0,size.x-20,0); //Cell.Full;
					innerCell.pixelSize.x -= scrollWidth;
					innerCell.MakeStatic();
					innerCell.isLayouted = false;

					Rect internalRect = new Rect(offset, innerCell.InternalRect.size*guizoom);

					if (!UI.current.layout)
						scroll = UnityEngine.GUI.BeginScrollView(cellRect, new Vector2(0,scroll), internalRect, alwaysShowHorizontal:false, alwaysShowVertical:true).y;

				}


				public void Dispose ()
				{
					if (innerCell != null)
					{
						if (!UI.current.layout)
							UnityEngine.GUI.EndScrollView();

						UI.current.scrollZoom = backupScrollZoom;
						UI.current.mousePos = backupMousePos;
						innerCell.Dispose();
					}
				}
			}

		#endregion

		#region DragValue

			public static float DragValue (float val) 
			{ 
				float newVal = DragValueInternal(val, 0.01f); 

				if (newVal > val + 0.000001f  ||  newVal < val - 0.000001f)
					UI.current.MarkChanged();
				
				return newVal;
			}

			public static int DragValue (int val) 
			{ 
				int newVal = (int)DragValueInternal(val, 1f, exponentiality:1000, sensitivity:5000); 

				if (newVal != val)
					UI.current.MarkChanged();
				
				return newVal;
			}

			public static double DragValue (double val) 
			{ 
				double newVal = (double)DragValueInternal((float)val, 0.01f); 

				if (newVal > val + 0.000001  ||  newVal < val - 0.000001)
					UI.current.MarkChanged();
				
				return newVal;
			}


			private static float DragValueInternal (float val, float minStep, float exponentiality=1, float sensitivity=1000f)
			{
				if (UI.current.layout) return val;
				if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;
				if (Cell.current.inactive) return val;

				Cell cell = Cell.current;
				if (cell.disabled) return val;

				Rect rect = Cell.current.GetRect(UI.current.scrollZoom, padding:fieldPadding);
				rect.height += 2;

				//cursor
				UnityEditor.EditorGUIUtility.AddCursorRect (rect, UnityEditor.MouseCursor.SlideArrow);

				//dragging
				float newVal = val;
	
				if (DragDrop.TryStart(cell,  rect))
				{
					DragDrop.group = "DragField";
					origDragValue = val;

					//ChartWindow.Clear();
					//ChartWindow.Evaluate(x=>RaiseValue(origDragValue, x), 0, 10, 0.01f); 
				}

				if (DragDrop.TryDrag(cell))
				{
					float delta = DragDrop.totalDelta.x;

					delta = SwapCursor ((int)delta);

					newVal = RaiseValue(origDragValue, delta, exponentiality, sensitivity);
					newVal = RoundValue(newVal);

					UI.current.editorWindow?.Repaint(); 
					UI.RemoveFocusOnControl();
				}
						
				if (DragDrop.TryRelease(cell))
				{
					#if UNITY_EDITOR_WIN
					swapTimes = 0;
					#endif

					UI.RemoveFocusOnControl();

					//UI.current.MarkChanged(); //changing on relase too to re-generate chunks in fullres
				}

				return newVal;
			}

			public static float origDragValue;

			public static float RaiseValue (float initialVal, float deltaSteps, float exponentiality=1, float sensitivity=1000f) //actually un-sensitivity
			{
				//converting exponential value to linear number of steps
				float sign = initialVal>=0? 1 : -1;
				float absVal = initialVal*sign;
				float steps = Mathf.Log(absVal+exponentiality, 2f) * sign; 
				
				//modifying steps num
				steps += deltaSteps/sensitivity; 

				//converting back to exponential
				sign = steps>=0? 1 : -1;
				float absSteps = steps*sign;
				return (Mathf.Pow(2f,absSteps)-exponentiality) * sign; 
			}

			public static float RoundValue (float val, float minStep=0.01f)
			{
				int sign = val>=0? 1 : -1;
				float absVal = val*sign;
			
				int step = 100;
				if (absVal > 10) step = 10;
				if (absVal > 100) step = 1;

				absVal = 1f*((int)(absVal*step)) / step;
			
				return (float)absVal*sign;
			}


			[DllImport("user32.dll")]
			public static extern bool GetCursorPos(out Vector2Int lpPoint);

			[DllImport("user32.dll")]
			public static extern bool SetCursorPos(int x, int y);

			#if UNITY_EDITOR_WIN
			private static int swapTimes = 0;
			#endif

			public static float SwapCursor (int mouseX)
			{
				#if UNITY_EDITOR_WIN
				Vector2Int screenMousePos;
				GetCursorPos(out screenMousePos);

				mouseX += swapTimes*Screen.currentResolution.width;

				if (screenMousePos.x == 0)
				{
					swapTimes --;
					SetCursorPos(Screen.currentResolution.width-2, screenMousePos.y);
				}

				if (screenMousePos.x == Screen.currentResolution.width-1)
				{
					swapTimes ++;
					SetCursorPos(1, screenMousePos.y);
				}
				#endif

				return mouseX;
			}

			public static void MoveCursorRight (float speed=10)
			/// Slowly moves cursor to the right. Just to record videos
			/// Speed - pixels per second
			{
				double currentTime = (DateTime.Now-DateTime.Today).TotalMilliseconds;
				double deltaTime = currentTime - moveCursorTime;

				GetCursorPos(out Vector2Int mousePos);
				mousePos.x += (int)(deltaTime / 100f * speed + 0.5f);
				SetCursorPos(mousePos.x, mousePos.y);

				moveCursorTime = currentTime;
			}
			private static double moveCursorTime = -1;

		#endregion
	}
}
