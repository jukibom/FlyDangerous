using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Den.Tools
{
	public class TimerWindow : EditorWindow
	{
		const int toolbarHeight = 18;
		const int scrollWidth = 15;
		const int lineHeight = 18;
		const float namesWidthPercent = 0.4f;
		Vector2 scrollPosition;
		int selectedLine = 1;

		Timer.TimerInstance groupTimer;
		bool groupEnabled = false;

		bool prevEnabled;

		public void OnEnable () 
		{
			Timer.OnHistoryAdded -= Repaint;
			Timer.OnHistoryAdded += Repaint;
		}

		public void OnGUI () 
		{
			//toolbar/header
			if (Event.current.type == EventType.Repaint)
				EditorStyles.toolbar.Draw(new Rect(0,0,position.width, toolbarHeight), new GUIContent(), 0);
			
			//Record
			Timer.enabled = EditorGUI.Toggle(new Rect(5,0,50,toolbarHeight), Timer.enabled, style:EditorStyles.toolbarButton);
			EditorGUI.LabelField(new Rect(9,1,50,toolbarHeight), "Record", style:EditorStyles.miniBoldLabel);

			//Group
			bool newGroupEnabled = EditorGUI.Toggle(new Rect(55,0,50,toolbarHeight), groupEnabled, style:EditorStyles.toolbarButton);
			if (newGroupEnabled && groupEnabled) //just pressed
			{
				prevEnabled = Timer.enabled;
				Timer.enabled = true;
				groupTimer = Timer.Start("Record Group " + Timer.history.Count);
				groupEnabled = true;
			}
			if (!newGroupEnabled && groupEnabled)
			{
				groupTimer.Dispose();
				groupEnabled = false;
				Timer.enabled = prevEnabled;
			}
			EditorGUI.LabelField(new Rect(63,1,50,toolbarHeight), "Group", style:EditorStyles.miniBoldLabel);

			//Clear
			if (UnityEngine.GUI.Button(new Rect(105,0,50,toolbarHeight), "Clear", style:EditorStyles.toolbarButton))
				Timer.history.Clear();

			DrawHeaderValues(new Rect(0, 0, position.width-scrollWidth, toolbarHeight));

			//calculating lines number
			int historyCount = Timer.history.Count;
			int linesCount = historyCount;
			for (int h=0; h<historyCount; h++)
				linesCount += Timer.history[h].GetExpandedCount();

			//drawing timers
			scrollPosition = UnityEngine.GUI.BeginScrollView(
				position:new Rect(0, toolbarHeight, position.width, position.height-toolbarHeight), 
				scrollPosition:scrollPosition, 
				viewRect:new Rect(0, 0, position.width-scrollWidth, linesCount*lineHeight),
				alwaysShowHorizontal:false,
				alwaysShowVertical:true);

			int lineNum = 0;
			for (int h=0; h<historyCount; h++)
			{
				Timer.TimerInstance timer = Timer.history[h];
				DrawTimer(new Rect(0,lineNum*lineHeight,position.width-scrollWidth, lineHeight), timer, ref lineNum, 0, h);
			}

			UnityEngine.GUI.EndScrollView();
		}


		public void DrawHeaderValues (Rect rect)
		{
			float namesWidth = rect.width * namesWidthPercent;
			float valuesWidth = rect.width - namesWidth;
			float rowWidth = valuesWidth / 7;

			//if (Event.current.type == EventType.Repaint)
			//	for (int i=0; i<5; i++)
			//		EditorStyles.toolbarButton.Draw(new Rect(rect.x+rowWidth*i+namesWidth, rect.y, rowWidth+1, rect.height), 
			//			isHover:false, isActive:false, on:false, hasKeyboardFocus:false);

			Rect row = new Rect(rect.x+namesWidth, rect.y, rowWidth+1, rect.height);
			if (Event.current.type == EventType.Repaint) 
				EditorStyles.toolbarButton.Draw(row, isHover:false, isActive:false, on:false, hasKeyboardFocus:false);
			EditorGUI.LabelField(new Rect(row.x+2, row.y+1, row.width, row.height), "Calls", style:EditorStyles.miniLabel);

			row.x += rowWidth;
			if (Event.current.type == EventType.Repaint) 
				EditorStyles.toolbarButton.Draw(row, isHover:false, isActive:false, on:false, hasKeyboardFocus:false);
			EditorGUI.LabelField(new Rect(row.x+2, row.y+1, row.width, row.height), "Total", style:EditorStyles.miniLabel);

			row.x += rowWidth;
			if (Event.current.type == EventType.Repaint) 
				EditorStyles.toolbarButton.Draw(row, isHover:false, isActive:false, on:false, hasKeyboardFocus:false);
			EditorGUI.LabelField(new Rect(row.x+2, row.y+1, row.width, row.height), "Self", style:EditorStyles.miniLabel);

			row.x += rowWidth;
			if (Event.current.type == EventType.Repaint) 
				EditorStyles.toolbarButton.Draw(row, isHover:false, isActive:false, on:false, hasKeyboardFocus:false);
			EditorGUI.LabelField(new Rect(row.x+2, row.y+1, row.width, row.height), "Average", style:EditorStyles.miniLabel);

			row.x += rowWidth;
			if (Event.current.type == EventType.Repaint) 
				EditorStyles.toolbarButton.Draw(row, isHover:false, isActive:false, on:false, hasKeyboardFocus:false);
			EditorGUI.LabelField(new Rect(row.x+2, row.y+1, row.width, row.height), "Fastest", style:EditorStyles.miniLabel);

			row.x += rowWidth;
			if (Event.current.type == EventType.Repaint) 
				EditorStyles.toolbarButton.Draw(row, isHover:false, isActive:false, on:false, hasKeyboardFocus:false);
			EditorGUI.LabelField(new Rect(row.x+2, row.y+1, row.width, row.height), "Slowest", style:EditorStyles.miniLabel);

			row.x += rowWidth;
			if (Event.current.type == EventType.Repaint) 
				EditorStyles.toolbarButton.Draw(row, isHover:false, isActive:false, on:false, hasKeyboardFocus:false);
			EditorGUI.LabelField(new Rect(row.x+2, row.y+1, row.width, row.height), "Fast*Num", style:EditorStyles.miniLabel);
		}


		public void DrawTimer (Rect rect, Timer.TimerInstance timer, ref int lineNum, int offset=0, int num=-1)
		{
			float namesWidth = rect.width * namesWidthPercent;
			float valuesWidth = rect.width - namesWidth;
			float rowWidth = valuesWidth / 7;
			float numWidth = 25;
			float tabWidth = 10;

			if (rect.y - scrollPosition.y >= 0  &&  rect.y - scrollPosition.y < position.height)
			{

				//selection
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
				{
					selectedLine = lineNum;
					Repaint();
				}

				if (selectedLine == lineNum && Event.current.type == EventType.Repaint)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorStyles.helpBox.Draw(rect, new GUIContent(), 0);
					EditorGUI.EndDisabledGroup();
				}

				//disabling if fastest value is too low
				EditorGUI.BeginDisabledGroup(timer.fastest < 10);

				//name
				if (num>=0)
					EditorGUI.LabelField(new Rect(rect.x, rect.y, numWidth, rect.height), num.ToString());

				float namePos = offset*tabWidth + numWidth;
				Rect nameRect = new Rect(rect.x+namePos, rect.y, namesWidth-namePos, rect.height);
				if (timer.subTimers != null)
					timer.expanded = EditorGUI.Foldout(nameRect, timer.expanded, timer.name);
				else
					EditorGUI.LabelField(new Rect(nameRect.x+12, nameRect.y, nameRect.width-12, nameRect.height), timer.name);

				//values
				Rect row = new Rect(rect.x+namesWidth, rect.y, rowWidth, rect.height);
				EditorGUI.LabelField(row, timer.calls.ToString());

				row.x += rowWidth;
				EditorGUI.LabelField(row, Timer.TicksToMilliseconds(timer.total).ToString("0.000"));

				row.x += rowWidth;
				EditorGUI.LabelField(row, Timer.TicksToMilliseconds(timer.SelfTime()).ToString("0.000"));

				row.x += rowWidth;
				EditorGUI.LabelField(row, Timer.TicksToMilliseconds(timer.total / timer.calls).ToString("0.000"));

				row.x += rowWidth;
				EditorGUI.LabelField(row, Timer.TicksToMilliseconds(timer.fastest).ToString("0.000"));

				row.x += rowWidth;
				EditorGUI.LabelField(row, Timer.TicksToMilliseconds(timer.slowest).ToString("0.000"));

				row.x += rowWidth;
				EditorGUI.LabelField(row, Timer.TicksToMilliseconds(timer.fastest * timer.calls).ToString("0.000"));

				EditorGUI.EndDisabledGroup();
			}

			//sub-timers
			int origLineNum = lineNum;
			lineNum++;
			if (timer.expanded && timer.subTimers!=null)
			{
				for (int i=0; i<timer.subTimers.Count; i++)
					DrawTimer(new Rect(rect.x, rect.y+lineHeight*(lineNum-origLineNum), rect.width, rect.height), timer.subTimers[i], ref lineNum, offset+1);
			}
		}


		[MenuItem ("Window/Timers")]
		public static void ShowEditor ()
		{
			EditorWindow.GetWindow<TimerWindow>("Timers");
		}
	}
}