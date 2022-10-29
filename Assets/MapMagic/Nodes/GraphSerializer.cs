using System;
using System.Reflection;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

using Den.Tools;
using MapMagic.Products;
using MapMagic.Core; //version number
using MapMagic.Expose;

namespace MapMagic.Nodes
{
	public interface IGraphSerializer
	{
		void Serialize (Graph graph);
		void Deserialize (Graph graph);
	}

	[Serializable]
	public class GraphSerializer200Beta : IGraphSerializer
	{
		public Serializer.Object[] serializedData = new Serializer.Object[0];

		private class SerializedDataProt
		{
			public SemVer ver;
			public Generator[] generators;
			//public Dictionary<IInlet<object>, IOutlet<object>> links;
			public IInlet<object>[] linkInlets;
			public IOutlet<object>[] linkOutlets;
			public Group[] groups;
			public int seed = 12345;

			public Exposed exposed = new Exposed();
			public Override defaults = new Override();
		}

		public void Serialize (Graph graph)
		{
			SerializedDataProt prot = new SerializedDataProt() {
				ver = MapMagicObject.version,
				generators = graph.generators,
				linkInlets = graph.links.Keys.ToArray(),
				linkOutlets = graph.links.Values.ToArray(),
				groups = graph.groups,
				seed = graph.random.Seed,
				exposed = graph.exposed,
				defaults = graph.defaults};

				serializedData = Serializer.Serialize(prot, onAfterSerialize:AfterSerialize);
		}

		public void Deserialize (Graph graph) 
		{
			if (serializedData != null)
			{
				object obj = Serializer.Deserialize(serializedData, onBeforeDeserialize:BeforeDeserialize);

				SerializedDataProt prot = (SerializedDataProt)Serializer.Deserialize(serializedData, onBeforeDeserialize:BeforeDeserialize); 
				CheckNullGenerators(prot.generators);
				CheckNullLinks(ref prot.linkInlets, ref prot.linkOutlets); 
				
				graph.generators = prot.generators;
				graph.links = new Dictionary<IInlet<object>, IOutlet<object>>();
				graph.links.AddRange(prot.linkInlets, prot.linkOutlets);
				graph.groups = prot.groups;

				graph.exposed = prot.exposed;
				if (graph.exposed == null) graph.exposed = new Exposed();

				graph.defaults = prot.defaults;
				if (graph.defaults == null) graph.defaults = new Override();

				graph.random = new Noise(prot.seed, 32768);

				//if (graph.serializedVersion < 210)
					graph.CheckFixIds();

				CheckInletsGensAssigned(graph);
			}
		}


		public static void CheckInletsGensAssigned (Graph graph)
		{
			foreach (Generator gen in graph.generators)
			{
				if (gen is IMultiInlet multiInlet)
					foreach (IInlet<object> inlet in multiInlet.Inlets())
						if (inlet.Gen==null) 
						{
							Debug.Log ($"Generator {gen} inlet Gen is not assigned. Fixing.");
							inlet.SetGen(gen);
						}

				if (gen is IMultiOutlet multiOutlet)
					foreach (IOutlet<object> outlet in multiOutlet.Outlets())
						if (outlet.Gen==null) 
						{
							Debug.Log ($"Generator {gen} outlet Gen is not assigned. Fixing.");
							outlet.SetGen(gen);
						}
			}
		}


		private static void CheckNullGenerators (Generator[] gens)
		{
			for (int g=0; g<gens.Length; g++)
			{
				if (gens[g] == null)
					gens[g] = new Placeholders.Placeholder();
			}
		}


		private static void CheckNullLinks (ref IInlet<object>[] inlets, ref IOutlet<object>[] outlets)
		{
			if (!inlets.ContainsNull() && !outlets.ContainsNull())
				return;

			List<IInlet<object>> newInlets = new List<IInlet<object>>();
			List<IOutlet<object>> newOutlets = new List<IOutlet<object>>();

			int count = inlets.Length;
			for (int i=0; i<count; i++)
			{
				if (inlets[i] != null  &&  outlets[i] != null)
				{
					newInlets.Add(inlets[i]);
					newOutlets.Add(outlets[i]);
				}
			}

			inlets = newInlets.ToArray();
			outlets = newOutlets.ToArray();
		}


		private static void AfterSerialize (object obj, Serializer.Object serObj)
		{
			if (obj is Generator)
				serObj.altType = typeof(Placeholders.InletOutletPlaceholder).AssemblyQualifiedName;

			//if (obj is Placeholders.Placeholder placeholder)
			//{
			//	serObj.type = placeholder.origType;
			//}
			//actually done in placeholder itself

			#if !UNITY_2019_2_OR_NEWER
			//inlets/outlets generics
			if (serObj.type.Contains("Den.Tools.Matrices.MatrixWorld, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
				serObj.type = serObj.type.Replace("Den.Tools.Matrices.MatrixWorld, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Den.Tools.Matrices.MatrixWorld, Tools, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
			if (serObj.type.Contains("Den.Tools.TransitionsList, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
				serObj.type = serObj.type.Replace("Den.Tools.TransitionsList, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Den.Tools.TransitionsList, Tools, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
			if (serObj.type.Contains("Den.Tools.Splines.SplineSys, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
				serObj.type = serObj.type.Replace("Den.Tools.Splines.SplineSys, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Den.Tools.Splines.SplineSys, Tools, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

			/*if (serObj.type == "MapMagic.Nodes.Inlet`1[[Den.Tools.Matrices.MatrixWorld, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
				serObj.type =  "MapMagic.Nodes.Inlet`1[[Den.Tools.Matrices.MatrixWorld, Tools, Version=0.0.0.0]], MapMagic, Version=0.0.0.0";
			if (serObj.type == "MapMagic.Nodes.Inlet`1[[Den.Tools.TransitionsList, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
				serObj.type =  "MapMagic.Nodes.Inlet`1[[Den.Tools.TransitionsList, Tools, Version=0.0.0.0]], MapMagic, Version=0.0.0.0";
			if (serObj.type == "MapMagic.Nodes.Inlet`1[[Den.Tools.Splines.SplineSys, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
				serObj.type =  "MapMagic.Nodes.Inlet`1[[Den.Tools.Splines.SplineSys, Tools, Version=0.0.0.0]], MapMagic, Version=0.0.0.0";
			if (serObj.type == "MapMagic.Nodes.Outlet`1[[Den.Tools.Matrices.MatrixWorld, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
				serObj.type =  "MapMagic.Nodes.Outlet`1[[Den.Tools.Matrices.MatrixWorld, Tools, Version=0.0.0.0]], MapMagic, Version=0.0.0.0";
			if (serObj.type == "MapMagic.Nodes.Outlet`1[[Den.Tools.TransitionsList, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
				serObj.type =  "MapMagic.Nodes.Outlet`1[[Den.Tools.TransitionsList, Tools, Version=0.0.0.0]], MapMagic, Version=0.0.0.0";
			if (serObj.type == "MapMagic.Nodes.Outlet`1[[Den.Tools.Splines.SplineSys, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
				serObj.type =  "MapMagic.Nodes.Outlet`1[[Den.Tools.Splines.SplineSys, Tools, Version=0.0.0.0]], MapMagic, Version=0.0.0.0";
			if (serObj.type == "MapMagic.Nodes.Inlet`1[[Den.Tools.TransitionsList, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][], Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
				serObj.type =  "MapMagic.Nodes.Inlet`1[[Den.Tools.TransitionsList, Tools, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][], MapMagic, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";*/
				
			serObj.type = ModifyTypeName(serObj.type);
			if (serObj.altType != null && serObj.altType.Length != 0)
				serObj.altType = ModifyTypeName(serObj.altType);

			if (serObj.special != null  &&  serObj.special.Contains(", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")  &&  serObj.special.Contains(", MapMagic.Nodes."))
				serObj.special = serObj.special.Replace(", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", ", MapMagic, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

			#endif
		}

		private static string ModifyTypeName (string typeName)
		{
			if (typeName.StartsWith("MapMagic."))
				typeName = typeName.Replace(", Assembly-CSharp, ", ", MapMagic, ");
			if (typeName.StartsWith("Den.Tools."))
				typeName = typeName.Replace(", Assembly-CSharp, ", ", Tools, ");	
			return typeName;
		}


		private static void BeforeDeserialize (Serializer.Object serObj)
		{
			if (serObj.type == "MapMagic.Nodes.Exposed, MapMagic, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
				serObj.type = null; //resetting obj to null

//if (serObj.type!=null && serObj.type.Contains("MapMagic.Expose.Override")) //temporary
//	serObj.type = null;

			if (serObj.type == null)  //for some reason type null after trying to reset expose
				return;	

			foreach (var kvp in classRenames)
			{
				if (serObj.type.Contains(kvp.Key))
					serObj.type = serObj.type.Replace(kvp.Key, kvp.Value);
			}

			#if !UNITY_2019_2_OR_NEWER
			if (serObj.type.Contains(", MapMagic, "))
				serObj.type = serObj.type.Replace(", MapMagic, ", ", Assembly-CSharp, ");
			if (serObj.type.Contains(", Tools, "))
				serObj.type = serObj.type.Replace(", Tools, ", ", Assembly-CSharp, "); 
			if (serObj.altType != null)
			{
				if (serObj.altType.Contains(", MapMagic, "))
					serObj.altType = serObj.altType.Replace(", MapMagic, ", ", Assembly-CSharp, ");
				if (serObj.altType.Contains(", Tools, "))
					serObj.altType = serObj.altType.Replace(", Tools, ", ", Assembly-CSharp, ");
			}
			
			if (serObj.special != null)
			{
				//"contrast, MapMagic.Nodes.MatrixGenerators.Contrast200, MapMagic, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
				if (serObj.special.Contains(", MapMagic, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
					serObj.special = serObj.special.Replace(", MapMagic, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", ", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
			}
			
			#endif
		}

		private static readonly Dictionary<string,string> classRenames = new Dictionary<string, string>() {
			{"Plugins.", "Den.Tools." },
			{"MapMagic.Nodes.ObjectsGenerators.PositioningSettings", "MapMagic.Nodes.PositioningSettings" },
			{", Tools, Version", ", Den.Tools, Version"}, //Tools assembly rename
			{"MapMagic.Nodes.ObjectsGenerators.BiomeBlend", "MapMagic.Nodes.BiomeBlend" },
			{"MapMagic.Nodes.MatrixGenerators.MicroSplatOutput200, Assembly-CSharp,", "MapMagic.Nodes.MatrixGenerators.MicroSplatOutput200, MapMagic.MicroSplat,"},
			{"MapMagic.Nodes.MatrixGenerators.MicroSplatOutput200, Assembly-CSharp-firstpass,", "MapMagic.Nodes.MatrixGenerators.MicroSplatOutput200, MapMagic.MicroSplat,"}
			};
	}
}