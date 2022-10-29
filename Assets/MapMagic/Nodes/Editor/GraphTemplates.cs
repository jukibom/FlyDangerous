using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;
using MapMagic.Core;
using MapMagic.Core.GUI;
using MapMagic.Expose.GUI;

namespace MapMagic.Nodes.GUI
{
	public static class GraphTemplates
	{
			//empty graph is created viaCreateAssetMenuAttribute,
			//but unfortunately there's only one attribute per class

			[MenuItem("Assets/Create/MapMagic/Simple Graph", priority = 102)]
			static void MenuCreateMapMagicGraph(MenuCommand menuCommand)
			{
				ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, 
					ScriptableObject.CreateInstance<TmpCallbackReciever>(), 
					"MapMagic Graph.asset", 
					TexturesCache.LoadTextureAtPath("MapMagic/Icons/AssetBig"), 
					null);
			}

			class TmpCallbackReciever : UnityEditor.ProjectWindowCallback.EndNameEditAction
			{
				public override void Action(int instanceId, string pathName, string resourceFile)
				{
					Graph graph = CreateTemplate();
					graph.name = System.IO.Path.GetFileName(pathName);
					AssetDatabase.CreateAsset(graph, pathName);

					ProjectWindowUtil.ShowCreatedAsset(graph);

					GraphInspector.allGraphsGuids = new HashSet<string>(AssetDatabase.FindAssets("t:Graph"));
				} 
			}

			public static Graph CreateTemplate ()
			{
				Graph graph = GraphInspector.CreateInstance<Graph>();

				MatrixGenerators.Noise200 noise = (MatrixGenerators.Noise200)Generator.Create(typeof(MatrixGenerators.Noise200));
				graph.Add(noise);
				noise.guiPosition = new Vector2(-270,-100);

				MatrixGenerators.Erosion200 erosion = (MatrixGenerators.Erosion200)Generator.Create(typeof(MatrixGenerators.Erosion200));
				graph.Add(erosion);
				erosion.guiPosition = new Vector2(-70,-100);
				graph.Link(erosion, noise);

				MatrixGenerators.HeightOutput200 output = (MatrixGenerators.HeightOutput200)Generator.Create(typeof(MatrixGenerators.HeightOutput200));
				graph.Add(output);
				output.guiPosition = new Vector2(130, -100);
				graph.Link(output, erosion);

				return graph;
			}


			#if MM_DEBUG
			[MenuItem("Assets/Create/MapMagic/PerfTest Graph", priority = 102)]
			static void MenuCreatePerfTestGraph(MenuCommand menuCommand)
			{
				ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, 
					ScriptableObject.CreateInstance<TmpCallbackRecieverBig>(), 
					"PefrfTest Graph.asset", 
					TexturesCache.LoadTextureAtPath("MapMagic/Icons/AssetBig"), 
					null);
			}

			class TmpCallbackRecieverBig : UnityEditor.ProjectWindowCallback.EndNameEditAction
			{
				public override void Action(int instanceId, string pathName, string resourceFile)
				{
					Graph graph = CreateBig();
					graph.name = System.IO.Path.GetFileName(pathName);
					AssetDatabase.CreateAsset(graph, pathName);

					ProjectWindowUtil.ShowCreatedAsset(graph);

					GraphInspector.allGraphsGuids = new HashSet<string>(AssetDatabase.FindAssets("t:Graph"));
				} 
			}

			public static Graph CreateBig ()
			{
				Graph graph = GraphInspector.CreateInstance<Graph>();

				/*for (int j=0; j<10; j++)
				{
					MatrixGenerators.Noise200 noise = (MatrixGenerators.Noise200)Generator.Create(typeof(MatrixGenerators.Noise200), graph);
					graph.Add(noise);
					noise.guiPosition = new Vector2(-270,-100 + j*200);

					MatrixGenerators.Terrace200 terrace = null;
					for (int i=0; i<98; i++)
					{
						MatrixGenerators.Terrace200 newTerrace = (MatrixGenerators.Terrace200)Generator.Create(typeof(MatrixGenerators.Terrace200), graph);
						graph.Add(newTerrace);
						newTerrace.guiPosition = new Vector2(-70 + 200*i,-100 + j*200);
						if (i==0) graph.Link(newTerrace, noise);
						else graph.Link(newTerrace, (IOutlet<object>)terrace);
						terrace = newTerrace;
					}

					MatrixGenerators.HeightOutput200 output = (MatrixGenerators.HeightOutput200)Generator.Create(typeof(MatrixGenerators.HeightOutput200), graph);
					graph.Add(output);
					output.guiPosition = new Vector2(130 + 200*98, -100 + j*200);
					graph.Link(output, terrace);
				}*/

				return graph;
			}
			#endif
	}
}