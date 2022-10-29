using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Den.Tools
{
	public static class Timer
	{
		#if UNITY_EDITOR_WIN
		[DllImport("Kernel32.dll")]
		private static extern int QueryPerformanceCounter(ref long count);
		[DllImport("Kernel32.dll")]
		private static extern int QueryPerformanceFrequency(ref long frequency);
		#endif


		public struct TimerInstance : IDisposable  //struct to avoid adding to GC while profiling
		{
			public string name;

			public int calls;
			public long total;
			public long fastest;
			public long slowest;
			public long current;
			public bool logAfter;
			public void AddCurrent (long t) => current += t; //to use as struct

			public bool expanded; //is expanded in gui

			public List<TimerInstance> subTimers;

			public void AddTime (long delta)
			{
				total += delta;
				fastest += delta;
				slowest += delta;

				int subsCount = subTimers.Count;
				for (int i=0; i<subsCount; i++)
					subTimers[i].AddTime(delta);
			}

			public override int GetHashCode () { return name.GetHashCode(); }
			public string Log () { return Log("", 0); }
			public string Log (string result, int tab) 
			{
				for (int i=0; i<tab; i++) result += "\t";
				result += name + " calls:" + calls + 
					" total:" + TicksToMilliseconds(total).ToString("0.000") + 
					" fastest:" + TicksToMilliseconds(fastest).ToString("0.000") +
					" average:" + TicksToMilliseconds(total/calls).ToString("0.000") + "\n"; 
				if (subTimers != null)
					for (int i=0; i<subTimers.Count; i++) result += subTimers[i].Log(result, tab+1);
				return result;
			}

			public int GetExpandedCount ()
			{
				if (subTimers == null || !expanded) return 0;

				int subsCount = subTimers.Count;
				int count = subsCount;
				for (int i=0; i<subsCount; i++)
					count += subTimers[i].GetExpandedCount();

				return count;
			}

			/*public long SubsTime ()
			{
				if (subTimers == null) return total;
				else
				{
					long sum = 0;
					int subsCount = subTimers.Count;
					for (int i=0; i<subsCount; i++)
						sum += subTimers[i].SubsTime();
					return sum;
				}
			}*/

			public long SelfTime ()
			{
				if (subTimers == null) return total;

				long subsTime = 0;
				int subsCount = subTimers.Count;
				for (int i=0; i<subsCount; i++)
					subsTime += subTimers[i].total;

				return total - subsTime;
			}

			public void Dispose ()
			{
				Stop(this);

				if (logAfter) 
					Debug.Log(name + " total:" + TicksToMilliseconds(total).ToString("0.000"));
			}
		}
	

		public static bool enabled;

		public static long startTime;

		public static TimerInstance[] active = new TimerInstance[maxActiveCount]; //TODO: use array to avoid GC
		public static int activeCount = 0;
		const int maxActiveCount = 1000;

		public static List<TimerInstance> history = new List<TimerInstance>();

		public static TimerInstance temp = new TimerInstance();
		
		public static Action OnHistoryAdded;

		public static void PauseActive (long stopTime)
		{
			long delta = stopTime - startTime;

			//if (delta < 0) 
			//	throw new System.Exception("Negative timer value");

			for (int i=0; i<activeCount; i++)
				active[i].current += delta;
		}

		public static TimerInstance Start (string name)
		{
			long stopTime = 0;
			#if UNITY_EDITOR_WIN
			QueryPerformanceCounter(ref stopTime);
			#else
			stopTime = System.Diagnostics.Stopwatch.GetTimestamp();
			#endif
			return Start(name, stopTime, false);
		}

		public static TimerInstance Start (string name, bool logAfter)
		{
			long stopTime = 0;
			#if UNITY_EDITOR_WIN
			QueryPerformanceCounter(ref stopTime);
			#else
			stopTime = System.Diagnostics.Stopwatch.GetTimestamp();
			#endif
			return Start(name, stopTime, logAfter);
		}

		public static TimerInstance Start (string name, long stopTime, bool logAfter)
		{
			//stopping active timers
			PauseActive(stopTime);

			//profiling
			UnityEngine.Profiling.Profiler.BeginSample(name);

			if (!Timer.enabled) { return temp; }

			TimerInstance timer;

			//root timer
			if (activeCount == 0) 
				timer = new TimerInstance() { name=name, fastest=long.MaxValue };

			else
			{
				TimerInstance lastActive = active[activeCount-1];

				//try finding in active
				if (lastActive.subTimers != null)
					timer = lastActive.subTimers.Find(t => t.name==name);

				//creating new if not found
				else
				{
					timer = new TimerInstance() { name=name, fastest=long.MaxValue };
					if (lastActive.subTimers == null) lastActive.subTimers = new List<TimerInstance>();
					lastActive.subTimers.Add(timer);
				}
			}

			//activate
			if (activeCount == maxActiveCount-1)
				throw new Exception("Max Timer counter is reached");
			active[activeCount] = timer;
			activeCount++;

			//starting again
			#if UNITY_EDITOR_WIN
			QueryPerformanceCounter(ref startTime);
			#else
			startTime = System.Diagnostics.Stopwatch.GetTimestamp();
			#endif

			timer.logAfter = logAfter;
			return timer;
		}


		public static void Stop (TimerInstance timer)
		{
			//stopping active timers
			long stopTime = 0;
			#if UNITY_EDITOR_WIN
			QueryPerformanceCounter(ref stopTime);
			#else
			stopTime = System.Diagnostics.Stopwatch.GetTimestamp();
			#endif

			//profiling
			UnityEngine.Profiling.Profiler.EndSample();

			if (!Timer.enabled) return;

			PauseActive(stopTime);

			//check if the timer is active
			if (activeCount == 0)
				throw new System.Exception("Trying to stop timer when there are no active timers running");
			//if (active[activeCount-1] != timer)
			//	throw new System.Exception("Trying to stop non-active timer");

			//removing timer from active
			//active.RemoveAt(activeCount-1);
			activeCount--;

			//writing time
			timer.calls ++;
			timer.total += timer.current;
			if (timer.current < timer.fastest) timer.fastest = timer.current;
			if (timer.current > timer.slowest) timer.slowest = timer.current;
			timer.current = 0;

			//if it is root - moving to history
			if (enabled && activeCount == 0)
			{
				history.Add(timer);
				if (OnHistoryAdded != null) OnHistoryAdded();
			}

			//starting all again
			#if UNITY_EDITOR_WIN
			QueryPerformanceCounter(ref startTime);
			#else
			startTime = System.Diagnostics.Stopwatch.GetTimestamp();
			#endif
		}


		public static double TicksToMilliseconds (long rawTicks) 
		{
			long frequency = 0;
			#if UNITY_EDITOR_WIN
			QueryPerformanceFrequency(ref frequency);
			#else
			frequency = System.Diagnostics.Stopwatch.Frequency;
			#endif

			
			return 1.0 * rawTicks / frequency * 1000;

			/*double dticks;
			if (Stopwatch.IsHighResolution)
			{
				dticks = rawTicks;
				//dticks = rawTicks / Stopwatch.Frequency;
			}
			else
				dticks = rawTicks;

			return dticks / 10000;*/
		}


		public static void Calibrate ()
		{
			long minTme = long.MaxValue;
			for (int i=0; i<10; i++)
			{
				long startTime = 0;
				#if UNITY_EDITOR_WIN
				QueryPerformanceCounter(ref startTime);
				#else
				startTime = System.Diagnostics.Stopwatch.GetTimestamp();
				#endif

				for (int j=0; j<10000; j++)
				{
					using (Timer.Start("Calibrate")) { }
					using (Timer.Start("Calibrate")) { }
					using (Timer.Start("Calibrate")) { }
					using (Timer.Start("Calibrate")) { }
					using (Timer.Start("Calibrate")) { }
					
					using (Timer.Start("Calibrate")) { }
					using (Timer.Start("Calibrate")) { }
					using (Timer.Start("Calibrate")) { }
					using (Timer.Start("Calibrate")) { }
					using (Timer.Start("Calibrate")) { }
				}
				long stopTime = 0;
				#if UNITY_EDITOR_WIN
				QueryPerformanceCounter(ref stopTime);
				#else
				stopTime = System.Diagnostics.Stopwatch.GetTimestamp();
				#endif

				long delta = stopTime - startTime;
				if (delta < minTme) minTme = delta;
			}
			refinement = minTme/100000;
			history.Clear();
			UnityEngine.Debug.Log("Timer calibrated with refinement " + refinement);
		}
		public static long refinement = 0;
	}
}
