using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Profiling;

namespace Den.Tools.Tasks
{

	public static class CoroutineManager
	{
		public class Task
		{
			public string name = null;  //not used, for debug purpose
			public int priority;

			public List<Action> actions;
			public int actionNum = 0;

			public IEnumerator routine; //enumerator currently running

			public void Add (Action action)
			{
				if (actions == null)
					actions = new List<Action>();

				actions.Add(action);
			}

			public void Start () { CoroutineManager.Enqueue(this); }
			public void Stop () { CoroutineManager.Stop(this); }

			public bool Enqueued {get{ return queue.Contains(this); }}
			public bool Active {get{ return active==this; }}
		}


		private static List<Task> queue = new List<Task>();
		private static Task active = null;

		public static float timePerFrame = 3;
		private static System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

		public static long updateNum = 0;


		static CoroutineManager ()
		{
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.playModeStateChanged -= AbortOnPlaymodeChange;
			Application.wantsToQuit -= AbortOnExit;
			//UpdateCaller.Update -= UpdateMain;

			UnityEditor.EditorApplication.playModeStateChanged += AbortOnPlaymodeChange; 
			Application.wantsToQuit += AbortOnExit;
			//UpdateCaller.Update += UpdateMain; //updating main actions/routines every frame
			#endif
		}


		public static Task Enqueue (Action action, int priority=0, string name=null)
		{
			Task task = new Task() { actions = new List<Action>() {action}, priority = priority, name = name };
			CoroutineManager.Enqueue(task);
			return task;
		}

		public static Task Enqueue (IEnumerator routine, int priority=0, string name=null)
		{
			Task task = new Task() { routine = routine, priority = priority, name = name };
			CoroutineManager.Enqueue(task);
			return task;
		}

		public static void Enqueue (Task task)
		{
			lock (queue)
			{
				if (queue.Contains(task)) return;  //already enqueued

				if (active == task)  //already running - restarting
				{
					active.routine = null;
					active = null;
				}

				queue.Add(task);
			}
		}

		public static void Dequeue (Task task)
		{
			lock (queue)
				if (queue.Contains(task))
					queue.Remove(task);
		}


		public static void Stop (Task task)
		{
			lock (queue)
			{
				if (queue.Contains(task))
					queue.Remove(task);
			}

			if (active == task)
				active = null;
		}


		public static void Update ()
		{
			updateNum++;

			timer.Reset();
			while (timer.ElapsedMilliseconds < timePerFrame  ||  !timer.IsRunning) //!IsRunning to iterate only once
			{
				if (!timer.IsRunning) timer.Start();

				//taking new task
				if (active == null)
					lock (queue) //new tasks could be added at the end of threads
				{
					if (queue.Count == 0) break;

					int taskNum = GetMaxPriorityNum(queue);
					active = queue[taskNum];
					queue.RemoveAt(taskNum);
				}

				//moving actions
				bool move = false;
				if (active.actions != null  &&  active.actions.Count != 0  &&  active.actionNum < active.actions.Count)
				{
					try  { active.actions[active.actionNum](); }
					catch(Exception e) { throw new Exception("Routine error: " + e); }  
					finally
					{
						active.actionNum ++;
						move = true;
					}
				}

				//moving active routine
				if (!move  &&  active.routine != null)
					move = active.routine.MoveNext();

				if (!move)
				{
					if (active!=null) active.routine = null; 
					active = null; 
				}
			}
			timer.Stop();
		}


		public static int GetMaxPriorityNum (List<Task> list)
		{
			int maxPriority = int.MinValue;
			int maxPriorityNum = -1;

			int listCount = list.Count;
			for (int i=list.Count-1; i>=0; i--) //for FIFO
			{
				int priority = list[i].priority;
				if (priority > maxPriority)
				{
					maxPriority = priority;
					maxPriorityNum = i;
				}
			}
			
			return maxPriorityNum;
		}


		public static void Abort ()
		{
			//clearing queue (in that order so no job will pass from queue to active)
			lock (queue)
				queue.Clear();

			//clearing active
			if (active != null)
			{
				try 
				{
					if (active.routine!=null)
						active.routine.Reset();
				}
				catch (Exception) {} //error on project start/stop
				active.routine = null;
			}
		}

		#if UNITY_EDITOR
		static void AbortOnPlaymodeChange (UnityEditor.PlayModeStateChange state)
		{
			if (state==UnityEditor.PlayModeStateChange.ExitingEditMode || state==UnityEditor.PlayModeStateChange.ExitingPlayMode)
				Abort();
		}
		#endif


		static bool AbortOnExit ()
		{
			Abort(); return true;
		}

		public static bool IsWorking {get{ return queue.Count!=0 || active!=null; }}

		public static bool IsQueueEmpty {get{ return queue.Count==0; }} //useful for checking if there any tasks left from within task

		public static bool IsNameEnqueued (string name) 
		{
			lock (queue)
				if (queue.FindIndex(c=>c.name==name) >= 0)
					return true;
			return false;
		}


		public static bool IsNameActive (string name) 
		{
			if (active != null && active.name == name) return true;
			return false;
		}


		public static string DebugState ()
		/// returns active thread names and queue names to debug
		{
			//debug usually happens on border state, so locking
			string queueNames = "";
			lock (queue)
				queueNames = queue.ToStringMemberwise<Task>(t => t.name);

			return "Coroutine active: " + (active==null ? "null" : active.name) + "\n" + "Coroutine queue: " + queueNames;
		}
	}
}
