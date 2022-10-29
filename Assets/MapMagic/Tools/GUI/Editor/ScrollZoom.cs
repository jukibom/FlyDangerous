using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Den.Tools.GUI
{
	[System.Serializable]
	public class ScrollZoom
	{
		public float zoom = 1;
		//public int zoomStage = 0; //number of scroll wheel ticks from zoom 1
		public float zoomStep = 0.0625f;
		public float minZoom = 0.25f;
		public float maxZoom = 2;
		public bool allowZoom = false; //read externally before zooming
		
		public int scrollWheelStep = 18;

		public Vector2 scroll = new Vector2(0, 0);
		public bool isScrolling = false;
		private	Vector2 clickPos = new Vector2(0,0);
		private Vector2 clickScroll = new Vector2(0,0);
		public int scrollButton = 2;
		public bool roundScroll = true;
		public bool allowScroll = false;


		public void Zoom () 
		{ 
			if (!allowZoom) return;
			Zoom(Event.current.mousePosition); 
		}

		public void Zoom (Vector2 pivot)
		/// Zooms with mouse wheel
		{

			if (Event.current == null) return;

			//reading control
			#if UNITY_EDITOR_OSX
			bool control = Event.current.command;
			#else
			bool control = Event.current.control;
			#endif

			float delta = 0;
			if (Event.current.type == EventType.ScrollWheel) delta = Event.current.delta.y / 3f;
			//else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && control) delta = Event.current.delta.y / 15f;
			//else if (control && Event.current.alt && Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Equals) delta --;
			//else if (control && Event.current.alt && Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Minus) delta ++;
			if (Mathf.Abs(delta) < 0.001f) return;

			//calculating current zoom stage - number of scroll wheel ticks from zoom 1
			int zoomStage = 0;
			if (zoom < 0.999f)
				zoomStage = -(int)((1-zoom) / zoomStep);
			else if (zoom > 1.0001f)
			{
				float tempZoom = 1;
				while (tempZoom < zoom)
				{
					tempZoom += zoomStep * (int)(tempZoom / 2 + 1) * 2;
					zoomStage ++;
				}
			}

			//new zoom	
			if (delta > 0) zoomStage--;
			if (delta < 0) zoomStage++;

			float newZoom = 1;
			if (zoomStage < 0)
			{
				int minStage = -(int)((1-minZoom) / zoomStep);
				if (zoomStage < minStage) zoomStage = minStage;
				newZoom = 1 - zoomStep*(-zoomStage);
			}
			else if (zoomStage > 0)
			{
				for (int i=0; i<zoomStage; i++)
				{
					float val = zoomStep * (int)(newZoom / 2 + 1) * 2;
					if (newZoom + val > maxZoom + 0.0001f)
						break;
					newZoom += val;
				}
			}

			Zoom(newZoom, pivot);
		}

		public void Zoom (float zoomVal, Vector2 pivot)
		/// Zooms to zoomVal
		/// Pivot is usually mouse position
		{
			//record mouse position in worldspace
			Vector2 worldMousePos = (pivot - scroll) / zoom;

			//changing zoom
			float zoomChange = zoomVal - zoom;
			zoom = zoomVal;

			//scrolling around pivot
			scroll -= worldMousePos * zoomChange;
			
			#if UNITY_EDITOR
			//UnityEditor.EditorWindow.focusedWindow?.Repaint();
			UI.current?.editorWindow?.Repaint(); 
			#endif

			if (roundScroll) RoundScroll();
		}


		public void ScrollWheel(int step = 3)
		{
			float delta = 0;
			if (Event.current.type == EventType.ScrollWheel) delta = Event.current.delta.y / 3f;
			scroll.y -= delta * scrollWheelStep * step;
		}


		public void Scroll()
		{
			if (!allowScroll) return;

			if (Event.current.type == EventType.MouseDown  &&  Event.current.button == scrollButton)
			{
				clickPos = Event.current.mousePosition;
				clickScroll = scroll;
				isScrolling = true;
			}

			if (Event.current.type == EventType.MouseDown  &&  Event.current.button == 0  &&  Event.current.alt)  //alternative way to scroll
			{
				clickPos = Event.current.mousePosition;
				clickScroll = scroll;
				isScrolling = true;
			}

			if (UI.MouseUp) //(Event.current.rawType == EventType.MouseUp  ||  Event.current.rawType == EventType.MouseLeaveWindow) 
			{
				isScrolling = false;
			}
			
			if (isScrolling)
				Scroll(clickScroll + Event.current.mousePosition - clickPos);
		}


		public void Scroll (Vector2 newScroll)
		{
			scroll = newScroll; 

			if (roundScroll) RoundScroll();

			UI.RemoveFocusOnControl(); //disabling text typing
			UI.current?.editorWindow?.Repaint();
		}


		public void RoundScroll ()
		{
			float dpiFactor = UI.current?.DpiScaleFactor ?? 1;
			if (scroll.x < 0) scroll.x--;	scroll.x = (int)(float)(scroll.x*dpiFactor + 0.50002f) / dpiFactor;   //adding epsilon to prevent position flickering on clear numbers
			if (scroll.y < 0) scroll.y--;	scroll.y = (int)(float)(scroll.y*dpiFactor + 0.50002f) / dpiFactor;
		}


		public Vector2 GetWindowCenter (Vector2 windowSize)
		/// returns the center of the screen(window) in base coordinates
		{
			//return (windowSize/2 - scroll) / zoom;
			return  (windowSize/2) - scroll/zoom;
		}

		public void FocusWindowOn (Vector2 center, Vector2 windowSize)
		{
			//Scroll( -center*zoom + windowSize/2 );
			Scroll( (windowSize/2 - center) * zoom );
		}


		#region ToScreen/ToInternal

			public Vector2 ToScreen(Vector2 pos)
			{
				return new Vector2(pos.x * zoom + scroll.x, pos.y * zoom + scroll.y);
			}

			public Rect ToScreen (Vector2 pos, Vector2 size, bool pixelPerfect=true)
			{
				return ToScreen(pos.x, pos.y, size.x, size.y, pixelPerfect);
			}

			public Rect ToScreen (Rect rect, bool pixelPerfect=true)
			{
				return ToScreen(rect.x, rect.y, rect.width, rect.height, pixelPerfect);
			}

			public Rect ToScreen (double minX, double minY, double sizeX, double sizeY,  bool pixelPerfect=false, bool sizePerfect=false)
			{
				float dpiFactor = UI.current.DpiScaleFactor;
				minX*=dpiFactor; minY*=dpiFactor; sizeX*=dpiFactor; sizeY*=dpiFactor;

				minX = minX*zoom + scroll.x*dpiFactor;
				minY = minY*zoom + scroll.y*dpiFactor;
				sizeX *= zoom;
				sizeY *= zoom;

				if (pixelPerfect)
				{
					double maxX = minX + sizeX;
					double maxY = minY + sizeY;

					if (minX < 0) minX--;	minX = (int)(minX + 0.50002f);  //adding epsilon to prevent position flickering on clear numbers
					if (minY < 0) minY--;	minY = (int)(minY + 0.50002f);
					if (maxX < 0) maxX--;	maxX = (int)(maxX + 0.50002f);
					if (maxY < 0) maxY--;	maxY = (int)(maxY + 0.50002f);

					sizeX = maxX - minX;
					sizeY = maxY - minY;
				}

				if (sizePerfect)
				{
					if (minX < 0) minX--;	minX = (int)(minX + 0.50002f);  //adding epsilon to prevent position flickering on clear numbers
					if (minY < 0) minY--;	minY = (int)(minY + 0.50002f);
					if (sizeX < 0) sizeX--;	sizeX = (int)(sizeX + 0.50002f);
					if (sizeY < 0) sizeY--;	sizeY = (int)(sizeY + 0.50002f);
				}

				return new Rect((float)minX/dpiFactor, (float)minY/dpiFactor, (float)sizeX/dpiFactor, (float)sizeY/dpiFactor);
			}


			public Rect ToInternal(Rect rect)
			{
				Vector2 offset = new Vector2(
					(rect.x - scroll.x) / zoom, 
					(rect.y - scroll.y) / zoom );
				Vector2 size = new Vector2(
					rect.width / zoom, 
					rect.height / zoom);
				return new Rect(offset, size);
			}

			public Vector2 ToInternal(Vector2 pos)
			{
				return new Vector2 (
					(pos.x - scroll.x) / zoom,
					(pos.y - scroll.y) / zoom );
			}

			public float ToInternal(float val)
			{
				return val / zoom;
			}


			public Vector2 RoundToZoom (Vector2 vec)
			/// Queer thing
			{
				vec.x /= zoom;
				vec.x = Mathf.Round(vec.x);
				vec.x *= zoom;

				vec.y /= zoom;
				vec.y = Mathf.Round(vec.y);
				vec.y *= zoom;

				return vec;
			}


		#endregion








		/*public Rect ToDisplay(Rect rect)
		{
			return new Rect(rect.x * zoom + scroll.x, rect.y * zoom + scroll.y, rect.width * zoom, rect.height * zoom);
		}
		
		public Rect ToDisplay(float offsetX, float offsetY, float sizeX, float sizeY)
		{
			return new Rect(offsetX * zoom + scroll.x, offsetY * zoom + scroll.y, sizeX * zoom, sizeY * zoom);
		}

		public Rect ToDisplay(Float2 pos, Float2 size)
		{
			if (paddingBox==null) return new Rect(
				(int)( pos.x * zoom + scroll.x  + 0.5f), 
				(int)( pos.y * zoom + scroll.y  + 0.5f), 
				(int)( size.x * zoom  + 0.5f), 
				(int)( size.y * zoom  + 0.5f) );

			else 
			{
				Padding padding = (Padding)paddingBox;
				return new Rect(
				(int)( (pos.x + padding.left) * zoom + scroll.x  + 0.5f), 
				(int)( (pos.y + padding.top) * zoom + scroll.y   + 0.5f), 
				(int)( (size.x - (padding.left+padding.right)) * zoom  + 0.5f), 
				(int)( (size.y - (padding.top+padding.bottom)) * zoom  + 0.5f));
			}
		}

		public Rect ToInternal(Rect rect)
		{
			return new Rect((rect.x - scroll.x) / zoom, (rect.y - scroll.y) / zoom, rect.width / zoom, rect.height / zoom);
		}

		public Vector2 ToInternal(Vector2 pos)
		{
			return (pos - scroll) / zoom;
		} //return new Vector2( (pos.x-scroll.x)/zoom, (pos.y-scroll.y)/zoom); }


		public void Focus(Cell cell, Vector2 pos)
		{
			throw new System.NotImplementedException();
		}*/
	}
}
