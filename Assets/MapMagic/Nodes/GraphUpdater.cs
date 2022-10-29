using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GraphUpdater  
{

				/*#region Updating older versions
					if (serializedVersion < 10) Debug.LogError("MapMagic: trying to load unknow version scene (v." + serializedVersion/10f + "). " +
						"This may cause errors or drastic drop in performance. " +  
						"Delete this MapMagic object and create the new one from scratch when possible."); 

					//loading old serializer
					if (classes.Length==0 && serializer.entities.Count!=0)
					{
						serializer.ClearLinks();
						all = (Generator[])serializer.Retrieve(listNum);
						serializer.ClearLinks();

						OnBeforeSerialize();
						serializer = null; //will not make it null, just 0-length
						OnAfterDeserialize ();
					}

					if (serializedVersion < 200)
					{
//						for (int i=0; i<classes.Length; i++)
//							classes[i] = Update150to200(classes[i]);

						string tmp = "";
						for (int i=0; i<classes.Length; i++)
							tmp += classes[i] + "\n";
						Debug.Log(tmp);
					}
				#endregion*/




				/*List<string> classesList = new List<string>();  classesList.AddRange(classes);
				List<UnityEngine.Object> objectsList = new List<UnityEngine.Object>();  objectsList.AddRange(objects);
				List<float> floatsList = new List<float>();  floatsList.AddRange(floats);

				references.Clear();
				Generator[] newAll = null;
				//try { 
				newAll = (Generator[])CustomSerialization.ReadClass(0, classesList, objectsList, floatsList, references);
				//catch (Exception e) { Debug.LogError ("Error loading graph: " + e); }
				if (newAll != null) all = newAll;
				references.Clear();

				//post-processing generators
				for (int i=0; i<all.Length; i++)
				{
					Generator gen = all[i];

					//adding special arrays
					if (gen is OutputGenerator) ArrayTools.Add(ref outputs, gen as OutputGenerator); 
					if (gen is Group) ArrayTools.Add(ref groups, gen as Group);
					if (gen is Biome) ArrayTools.Add(ref biomes, gen as Biome);
				}
			}


			/*public string Update150to200 (string src)
			{
				string matrixClassName = "Plugins.MatrixWorld";
				string objectClassName = "Plugins.PosTab";
				//TODO: should be MapMagic

				int headStop = src.IndexOf(">") + 1;
				string header = src.Remove(headStop,src.Length-headStop);
				Debug.Log(header + ": Updating v1.5 graph data to v2.0. Check graph consistency after update.");
				
				//inlet classes
				if (src.Contains("<MapMagic.Generator+Input>"))
				{
					if (src.Contains("<type type=MapMagic.Generator+InoutType value=0/>")) //type == Type.Map
					{
						src = src.Replace("<MapMagic.Generator+Input>", "<MapMagic.Generator+Inlet`1[" + matrixClassName + "]>");
						src = src.Replace("</MapMagic.Generator+Input>", "</MapMagic.Generator+Inlet`1[" + matrixClassName + "]>");
					}

					if (src.Contains("<type type=MapMagic.Generator+InoutType value=1/>")) //type == Type.Objects
					{
						src = src.Replace("<MapMagic.Generator+Input>", "<MapMagic.Generator+Inlet`1[" + objectClassName + "]>");
						src = src.Replace("</MapMagic.Generator+Input>", "</MapMagic.Generator+Inlet`1[" + objectClassName + "]>");
					}
				}

				//outlet classes
				if (src.Contains("<MapMagic.Generator+Output>"))
				{
					if (src.Contains("<type type=MapMagic.Generator+InoutType value=0/>")) //type == Type.Map
					{
						src = src.Replace("<MapMagic.Generator+Output>", "<MapMagic.Generator+Outlet`1[" + matrixClassName + "]>");
						src = src.Replace("</MapMagic.Generator+Output>", "</MapMagic.Generator+Outlet`1[" + matrixClassName + "]>");
					}

					if (src.Contains("<type type=MapMagic.Generator+InoutType value=1/>")) //type == Type.Objects
					{
						src = src.Replace("<MapMagic.Generator+Output>", "<MapMagic.Generator+Outlet`1[" + objectClassName + "]>");
						src = src.Replace("</MapMagic.Generator+Output>", "</MapMagic.Generator+Outlet`1[" + objectClassName + "]>");
					}
				}

				//special case for combine generators array
				if (src.StartsWith("<MapMagic.CombineGenerator>") && src.Contains("<<inputs type=MapMagic.Generator+Input[]"))
					src = src.Replace("<inputs type=MapMagic.Generator+Input[]", "<inputs type=MapMagic.Generator+Inlet`1[" + objectClassName + "][]");
				if (src.StartsWith("<MapMagic.Generator+Input[]"))
				{
					while (src.Contains("type=MapMagic.Generator+Input"))
						src = src.Replace("type=MapMagic.Generator+Input", "type=MapMagic.Generator+Inlet`1[" + objectClassName + "]");
					src = src.Replace("MapMagic.Generator+Input[]", "MapMagic.Generator+Inlet`1[" + objectClassName + "][]");
				}

				//object inlet/outlet values
				string[] allPossibleObjectInlets = new string[] { //first generator name, then value name
					"ObjectOutput+Layer",	"input",
					"TreesOutput+Layer",	"input",
					"AdjustGenerator",		"input",
					"CleanUpGenerator",		"input",
					"SplitGenerator",		"input",
					"SubtractGenerator",	"minuendIn",
					"SubtractGenerator",	"subtrahendIn",
					"RarefyGenerator",		"input",
					"CombineGenerator",		"inputs",
					"PropagateGenerator",	"input",
					"StampGenerator",		"positionsIn",
					"BlobGenerator",		"objectsIn",
					"FlattenGenerator",		"objectsIn",
					"ForestGenerator",		"seedlingsIn",
					"ForestGenerator",		"otherTreesIn",
					"SlideGenerator",		"input" };

				string[] allPossibleObjectOutlets = new string[] { //first generator name, then value name
					"ScatterGenerator",		"output",
					"AdjustGenerator",		"output",
					"CleanUpGenerator",		"output",
					"SplitGenerator",		"output",
					"SubtractGenerator",	"minuendOut",
					"RarefyGenerator",		"output",
					"CombineGenerator",		"output",
					"PropagateGenerator",	"output",
					"ForestGenerator",		"treesOut",
					"SlideGenerator",		"output" };

				for (int i=0; i<allPossibleObjectInlets.Length; i++)
				{
					while (src.StartsWith("<MapMagic." + allPossibleObjectInlets[i]) && src.Contains("<" + allPossibleObjectInlets[i+1] + " type=MapMagic.Generator+Input"))  //object output "input" value
						src = src.Replace("<" + allPossibleObjectInlets[i+1] + " type=MapMagic.Generator+Input", "<" + allPossibleObjectInlets[i+1] + " type=MapMagic.Generator+Inlet`1[" + objectClassName + "]");
				}
					
				for (int i=0; i<allPossibleObjectOutlets.Length; i++)
				{
					while (src.StartsWith("<MapMagic." + allPossibleObjectOutlets[i]) && src.Contains("<" + allPossibleObjectOutlets[i+1] + " type=MapMagic.Generator+Output"))  //object output "input" value
						src = src.Replace("<" + allPossibleObjectOutlets[i+1] + " type=MapMagic.Generator+Output", "<" + allPossibleObjectOutlets[i+1] + " type=MapMagic.Generator+Outlet`1[" + objectClassName + "]");
				}

				//portals
				if (src.StartsWith("<MapMagic.Portal>"))
				{
					//src.Replace("<input type=MapMagic.Generator+Input", "<input type=MapMagic.Generator+Inlet");
					//src.Replace("<output type=MapMagic.Generator+Output", "<output type=MapMagic.Generator+Outlet");


					if (src.Contains("<type type=MapMagic.Generator+InoutType value=0/>")) //map type
					{
						if (src.Contains("<form type=MapMagic.Portal+PortalForm value=0/>"))
						{
							src = src.Replace ("<MapMagic.Portal>", "<MapMagic.InletPortal`1[" + matrixClassName + "]>");
							src = src.Replace ("</MapMagic.Portal>", "</MapMagic.InletPortal`1[" + matrixClassName + "]>");
						}
						if (src.Contains("<form type=MapMagic.Portal+PortalForm value=1/>"))
						{
							src = src.Replace ("<MapMagic.Portal>", "<MapMagic.OutletPortal`1[" + matrixClassName + "]>");
							src = src.Replace ("</MapMagic.Portal>", "</MapMagic.OutletPortal`1[" + matrixClassName + "]>");
						}
						src = src.Replace ("<input type=MapMagic.Generator+Input", "<input type=MapMagic.Generator+Inlet`1[" + matrixClassName + "]");
						src = src.Replace ("<output type=MapMagic.Generator+Output", "<output type=MapMagic.Generator+Outlet`1[" + matrixClassName + "]");
					}
					if (src.Contains("<type type=MapMagic.Generator+InoutType value=1/>")) //object type
					{
						if (src.Contains("<form type=MapMagic.Portal+PortalForm value=0/>"))
						{
							src = src.Replace ("<MapMagic.Portal>", "<MapMagic.InletPortal`1[" + objectClassName + "]>");
							src = src.Replace ("</MapMagic.Portal>", "</MapMagic.InletPortal`1[" + objectClassName + "]>");
						}
						if (src.Contains("<form type=MapMagic.Portal+PortalForm value=1/>"))
						{
							src = src.Replace ("<MapMagic.Portal>", "<MapMagic.OutletPortal`1[" + objectClassName + "]>");
							src = src.Replace ("</MapMagic.Portal>", "</MapMagic.OutletPortal`1[" + objectClassName + "]>");
						}
						src = src.Replace ("<input type=MapMagic.Generator+Input", "<input type=MapMagic.Generator+Inlet`1[" + objectClassName + "]");
						src = src.Replace ("<output type=MapMagic.Generator+Output", "<output type=MapMagic.Generator+Outlet`1[" + objectClassName + "]");
					}
				}

				//other inlet/outlet values
				while (src.Contains("type=MapMagic.Generator+Input"))
				{
					src = src.Replace("type=MapMagic.Generator+Input", "type=MapMagic.Generator+Inlet`1[" + matrixClassName + "]");
				}

				while (src.Contains("type=MapMagic.Generator+Output"))
				{
					src = src.Replace("type=MapMagic.Generator+Output", "type=MapMagic.Generator+Outlet`1[" + matrixClassName + "]");
				}

				//replacing matrix link in objects input
				if (src.StartsWith("<MapMagic.Generator+Inlet`1[" + objectClassName + "]>"))
					src.Replace("<link type=MapMagic.Generator+Outlet`1[" + matrixClassName + "]", "<link type=MapMagic.Generator+Outlet`1[" + objectClassName + "]");

				return src;
			}*/
}
