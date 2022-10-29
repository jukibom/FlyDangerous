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

namespace MapMagic.Nodes.GUI
{
	public static class GraphEditorActions
	/// Editor implementation (with undo, messages, mouse pos, stuff) of the graph actions
	{
		public static void RemoveGenerators (this Graph graph, HashSet<Generator> selected, Generator clicked)
		{
			if (selected==null  ||  selected.Count==0  ||  !selected.Contains(clicked))
			//if clicked not on selected - removing it instead
				selected = new HashSet<Generator>() { clicked };

			RemoveGenerators(graph, selected);
		}


		public static void RemoveGenerators (this Graph graph, HashSet<Generator> selected)
		{
			CheckAndClearApplied(selected);

			GraphWindow.RecordCompleteUndo();

			foreach (Generator sgen in selected)
			{
				if (graph.ContainsGenerator(sgen))
					graph.Remove(sgen);
			}

			GraphWindow.current.Focus();
			GraphWindow.current.Repaint();

			GraphWindow.current.RefreshMapMagic();
		}


		public static void EnableDisableGenerators (this Graph graph, HashSet<Generator> selected, Generator clicked)
		{
			if (selected==null  ||  selected.Count==0  ||  !selected.Contains(clicked))
			//if clicked not on selected - disabling it instead
				selected = new HashSet<Generator>() { clicked };

			EnableDisableGenerators(graph, selected);
		}


		public static void EnableDisableGenerators (this Graph graph, HashSet<Generator> selected)
		/// Finds an active state and enables/disables nodes
		/// If all of the selected generators are disabled - enables them. If any is enabled - disables
		{
			bool enable = true; //do wee need to enable or disable gens?
			foreach (Generator sgen in selected)
				if (sgen.enabled)
					{ enable = false; break; }

			if (!enable) 
				CheckAndClearApplied(selected);

			foreach (Generator sgen in selected)
			{
				sgen.enabled = !sgen.enabled;
				GraphWindow.current?.RefreshMapMagic(sgen);
			}

			GraphWindow.current.Focus();
			GraphWindow.current.Repaint();
		}


		private static void CheckAndClearApplied (HashSet<Generator> gens)
		/// Checks whether gens contain an output, displays message window to clear it, and clears if selected Yes 
		{
			bool hasOutput = false;
			foreach (Generator gen in gens)
				if (gen is OutputGenerator)
					{ hasOutput=true; break; }

			if (hasOutput  &&  
				GraphWindow.current.mapMagic != null  &&
				GraphWindow.current.mapMagic is MapMagicObject mapMagicObject  &&
				EditorUtility.DisplayDialog("Clear Applied", "Would you like to clear generated and applied results on terrain as well?", "Clear", "Keep"))
			{
				mapMagicObject.ResetTerrains();
				//foreach (Generator gen in gens)
				//	if (gen is OutputGenerator outGen)
				//		GraphWindow.current.mapMagic.Purge(outGen);
			}
		}


		public static void CreateGenerator (this Graph graph, Type type, Vector2 mousePos)
		{
			GraphWindow.RecordCompleteUndo();

			Generator gen = Generator.Create(type);
			gen.guiPosition = new Vector2(mousePos.x-GeneratorDraw.nodeWidth/2, mousePos.y-GeneratorDraw.headerHeight/2);
			graph.Add(gen);

			GraphWindow.current.Focus(); //to return focus from right-click menu
			GraphWindow.current.Repaint();

			//GraphWindow.RefreshMapMagic();
			//no need to refresh since added node isn't linked anywhere
		}


		public static void DuplicateGenerator (this Graph graph, Generator gen, ref HashSet<Generator> selected)
		{
			if (selected==null || selected.Count==0)
				DuplicateGenerator(graph, gen);

			else if (selected.Contains(gen)) //removing selected only if gen is among them
				DuplicateGenerators(graph, ref selected);
		}

		public static void DuplicateGenerator (this Graph graph, Generator gen)
		{
			HashSet<Generator> gens = new HashSet<Generator>();
			gens.Add(gen);
			DuplicateGenerators(graph, ref gens);
		}

		public static void DuplicateGenerators (this Graph graph, ref HashSet<Generator> gens)
		/// Changes the selected hashset
		{
			GraphWindow.RecordCompleteUndo();

			if (gens.Count == 0) return;

			Generator[] duplicatedGens = graph.Duplicate(gens);

			//placing under source generators
			Rect selectionRect = new Rect(duplicatedGens[0].guiPosition, Vector2.zero);
			foreach (Generator gen in duplicatedGens)
				selectionRect = selectionRect.Encapsulate( new Rect(gen.guiPosition, gen.guiSize) );
			foreach (Generator gen in duplicatedGens)
				gen.guiPosition.y += selectionRect.size.y + 20;

			gens.Clear();
			gens.AddRange(duplicatedGens);
			
			GraphWindow.current.Focus(); //to return focus from right-click menu
			GraphWindow.current.Repaint();

			GraphWindow.current.RefreshMapMagic();
		}
	}
}