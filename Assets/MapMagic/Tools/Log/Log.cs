using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using UnityEngine;

namespace Den.Tools
{
	public static class Log
	{
		public class Entry : IDisposable
		{
			public string name;
			public string threadName;
			public long startTicks;
			public long disposeTicks;
			public (string,string)[] fieldValues;
			public List<Entry> subs;
			public bool guiExpanded;

			public void Dispose () => Log.DisposeGroup();

			public int Count
			{get{
				int count = 1;
				if (subs!=null) 
					foreach (Entry sub in subs)
						count += sub.Count;
				return count;
			}}

			public IEnumerable<Entry> SubsRecursive ()
			{
				if (subs==null) yield break;
				foreach (Entry sub in subs)
				{
					yield return sub;

					if (sub.subs != null)
						foreach (Entry subSub in sub.SubsRecursive())
							yield return subSub;
				}
			}
		}

		public static bool enabled = false;

		public static Entry root = new Entry() {name="Root"};
		private static Entry activeGroup = root; //not among openedGroups  //TODO: make a dictionary thread->group
		private static List<Entry> openedGroups = new List<Entry>();

		private static Entry tempGroup = new Entry(); //to return when recording disabled

		public const string defaultId = "Default";

		
		public static void AddThreadId (string name) => Add(name, Thread.CurrentThread.ManagedThreadId.ToString());

		public static void Add (string name, string id=defaultId)
		{
			if (!enabled) return;

			Entry entry = new Entry() {name=name, threadName=id};
			
			if (activeGroup.subs == null) activeGroup.subs = new List<Entry>();
			activeGroup.subs.Add(entry);
		}

		public static void Add (string name, string id, object obj)
		{
			if (!enabled) return;

			Entry entry = new Entry() {name=name, threadName=id};
			entry.fieldValues = ReadValues(obj);

			if (activeGroup.subs == null) activeGroup.subs = new List<Entry>();
			activeGroup.subs.Add(entry);
				
		}


		public static Entry Group (string name, string id=defaultId)
		{
			if (!enabled) return tempGroup;

			Entry entry = new Entry() {name=name, threadName=id};

			if (activeGroup.subs == null) activeGroup.subs = new List<Entry>();
			activeGroup.subs.Add(entry);

			openedGroups.Add(activeGroup);
			activeGroup = entry;

			long unityStartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.Ticks;
			long currentTime = DateTime.Now.Ticks; //todo: minimize operations after DateTime.Now
			entry.startTicks = currentTime - unityStartTime;

			return entry;
		}


		private static void DisposeGroup ()
		{
			if (!enabled) return;

			long currentTime = DateTime.Now.Ticks;
			long unityStartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.Ticks;
			activeGroup.disposeTicks = currentTime - unityStartTime;

			activeGroup = openedGroups[openedGroups.Count-1];
			openedGroups.RemoveAt(openedGroups.Count-1);
		}


		private static (string,string)[] ReadValues (object obj)
		{
			Type type = obj.GetType();
			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			(string,string)[] fieldValues = new (string,string)[fields.Length];

			for (int i=0; i<fields.Length; i++)
			{
				string name = fields[i].Name;
				string value = fields[i].GetValue(obj).ToString();

				fieldValues[i] = (name,value);
			}

			return fieldValues;
		}


		public static void Clear () => root.subs.Clear();

		public static int Count => root.Count;

		public static IEnumerable AllEntries ()  //all except root
		{
			foreach (Entry sub in root.SubsRecursive())
				yield return sub;
		}


		public static HashSet<string> UsedThreads ()
		{
			HashSet<string> usedIds = new HashSet<string>();

			foreach (Entry sub in AllEntries())
				usedIds.Add(sub.threadName);

			return usedIds;
		}
	}
}