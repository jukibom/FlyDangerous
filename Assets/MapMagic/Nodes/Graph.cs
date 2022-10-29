using System;
using System.Reflection;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

using Den.Tools;
using MapMagic.Products;
using MapMagic.Core; //version number
using Den.Tools.Matrices; //get generic type
using MapMagic.Expose;

namespace MapMagic.Nodes
{
	[System.Serializable]
	[HelpURL("https://gitlab.com/denispahunov/mapmagic/wikis/home")]
	[CreateAssetMenu(menuName = "MapMagic/Empty Graph", fileName = "Graph.asset", order = 101)]
	public class Graph : ScriptableObject , ISerializationCallbackReceiver
	{
		[NonSerialized] public Generator[] generators = new Generator[0];  //it's all serialized via callback
		[NonSerialized] public Dictionary<IInlet<object>, IOutlet<object>> links = new Dictionary<IInlet<object>, IOutlet<object>>();
		[NonSerialized] public Group[] groups = new Group[0];
		[NonSerialized] public Noise random = new Noise(12345, 32768); //noise created on serialization as well, nb when changing noise arr length

		[NonSerialized] public Exposed exposed = new Exposed();
		[NonSerialized] public Override defaults = new Override();

		public ulong changeVersion; //increases each time after change from gui. Used to copy function userGraph only when changed.
		public int serializedVersion = 0; //displayed in graph inspector
		public int instanceId; //cached instanceId during serialization

		public static Action<Generator, TileData> OnBeforeNodeCleared;
		public static Action<Generator, TileData> OnAfterNodeGenerated;
		public static Action<Type, TileData, IApplyData, StopToken> OnOutputFinalized; //TODO: rename onAfterFinalize? onBeforeApplyAssign?

		public bool guiShowDependent;
		public bool guiShowShared;
		public bool guiShowExposed;
		public bool guiShowDebug;
		public Vector2 guiMiniPos = new Vector2(20,20);
		public Vector2 guiMiniAnchor = new Vector2(0,0);

		public bool debugGenerate = true;
		public bool debugGenInfo = false;
		public bool debugGraphBackground = true;
		public float debugGraphBackColor = 0.5f;
		public bool debugGraphFps = true;
		public bool drawInSceneView = false;

		public void OnEnable () => instanceId=GetInstanceID(); //GetInstanceID not allowed during serialization

		public static Graph Create (Graph src=null, bool inThread=false)
		{
			Graph graph;

			if (inThread)
				graph = (Graph)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Graph));  //not stable, better create in prepare
			else 
				graph = ScriptableObject.CreateInstance<Graph>();

			//copy source graph
			if (src==null) graph.generators = new Generator[0];
			else 
			{
				graph.generators = (Generator[])Serializer.DeepCopy(src.generators);
				graph.random = src.random;
			}

			return graph;
		}


		#region Node Operations

			public void Add (Generator gen)
			{
				if (ArrayTools.Contains(generators,gen))
						throw new Exception("Could not add generator " + gen + " since it is already in graph");

				//gen.id = NewGenId; //id is assigned on create

				ArrayTools.Add(ref generators, gen);
				//cachedGuidLut = null;
			}


			public void Add (Group grp)
			{
				if (ArrayTools.Contains(groups, grp))
						throw new Exception("Could not add group " + grp + " since it is already in graph");

				ArrayTools.Add(ref groups, grp);
				//cachedGuidLut = null;
			}


			public void Remove (Generator gen)
			{
				if (!ArrayTools.Contains(generators,gen))
					throw new Exception("Could not remove generator " + gen + " since it is not in graph");

				UnlinkGenerator(gen);
				exposed.RemoveUnused(this); //will also remove exposed layers

				ArrayTools.Remove(ref generators, gen);
				//cachedGuidLut = null;
			}


			public void Remove (Group grp)
			{
				if (!ArrayTools.Contains(groups, grp))
					throw new Exception("Could not remove group " + grp + " since it is not in graph");

				ArrayTools.Remove(ref groups, grp);
				//cachedGuidLut = null;
			}

			
			public Generator[] Import (Graph other, bool createOverride=false)
			/// Returns the array of imported generators (they are copy of the ones in souce graph)
			{
				Graph copied = ScriptableObject.CreateInstance<Graph>();
				DeepCopy(other, copied);

				//gens
				ArrayTools.AddRange(ref generators, copied.generators);

				//links
				foreach (var kvp in copied.links)
					links.Add(kvp.Key, kvp.Value);

				//groups
				ArrayTools.AddRange(ref groups, copied.groups);
				
				//ids
				Dictionary<ulong,ulong> replacedIds = CheckFixIds( new HashSet<IUnit>(copied.generators) ); 

				//expose
				copied.exposed.ReplaceIds(replacedIds);
				exposed.AddRange(copied.exposed);

				return copied.generators;
			}


			public Graph Export (HashSet<Generator> gensHash)
			{
				Graph exported = ScriptableObject.CreateInstance<Graph>();

				//gens
				exported.generators = gensHash.ToArray();

				//links
				foreach (var kvp in links)
				{
					if (gensHash.Contains(kvp.Key.Gen) && gensHash.Contains(kvp.Value.Gen))
						exported.links.Add(kvp.Key, kvp.Value);
				}

				//expose
				foreach (Generator gen in exported.generators)
				{
					Exposed.Entry[] genEntries = exposed[gen.Id];
					if (genEntries != null)
						exported.exposed.AddRange(genEntries);
				}

				//copy to duplicate generators and links
				Graph copied = ScriptableObject.CreateInstance<Graph>();
				DeepCopy(exported, copied);

				return copied;
			}


			public Generator[] Duplicate (HashSet<Generator> gens)
			/// Returns the list of duplicated generators
			{
				Graph exported = Export(gens);
				Generator[] expGens = exported.generators;
				Import(exported);

				return expGens;
			}


			public static void Reposition (IList<Generator> gens, Vector2 newCenter)
			/// Moves array of generators center to new position
			{
				Vector2 currCenter = Vector2.zero;
				foreach (Generator gen in gens)
					currCenter += new Vector2(gen.guiPosition.x + gen.guiSize.x/2, gen.guiPosition.y);

				currCenter /= gens.Count;

				Vector2 delta = newCenter - currCenter;

				foreach (Generator gen in gens)
					gen.guiPosition += delta;
			}


			public Dictionary<ulong,ulong> CheckFixIds (HashSet<IUnit> susGens=null)
			// Ensures that ids of generators and layers are never match
			// Will try to change ids in susGens if any (leaving othrs intact)
			// Returns approx dict of what ids were replaced with what. Duplicated ids and 0 are not included in list
			{
				Dictionary<ulong,IUnit> allIds = new Dictionary<ulong,IUnit>(); //not hash set but dict to skip generator if it's his id
				Dictionary<ulong,ulong> oldNewIds = new Dictionary<ulong,ulong>();
				List<ulong> zeroNewIds = new List<ulong>();

				CheckFixIdsRecursively(susGens, allIds, oldNewIds, zeroNewIds);

				if (oldNewIds.Count != 0  )
					Debug.Log($"Changed generators ids on serialize: {oldNewIds.Count + zeroNewIds.Count}");

				return oldNewIds;
			}

			private void CheckFixIdsRecursively (HashSet<IUnit> susGens, Dictionary<ulong,IUnit> allIds, Dictionary<ulong,ulong> oldNewIds, List<ulong> zeroNewIds)
			//populates a dict of renamed ids
			//those ids that were renamed from 0 are added to zeroNewIds
			{
				if (generators == null)
					OnAfterDeserialize(); //in some cases got to de-serialize sub-graphs

				//not-suspicious generators first
				foreach (IUnit unit in AllUnits())
				{
					if (susGens != null  &&  susGens.Contains(unit))
						continue;

					if (allIds.TryGetValue(unit.Id, out IUnit contUnit)  || unit.Id == 0)
					{
						if (contUnit == unit) //is itself
							continue;

						ulong newId = Id.Generate();
						while (allIds.ContainsKey(newId))
							newId = Id.Generate();
						
						if (unit.Id != 0)
							oldNewIds.Add(unit.Id, newId);
						else
							zeroNewIds.Add(newId);

						unit.Id = newId;
					}

					allIds.Add(unit.Id, unit);
				}

				//sub-graph next
				foreach (IBiome biome in UnitsOfType<IBiome>())
				{
					if (biome.SubGraph != null)
						biome.SubGraph.CheckFixIdsRecursively(susGens, allIds, oldNewIds, zeroNewIds);
				}

				//suspicious generator last (after all other generators are marked and won't change)
				if (susGens != null)
					foreach (IUnit unit in AllUnits())
					{
						if (!susGens.Contains(unit))
							continue;

						if (allIds.TryGetValue(unit.Id, out IUnit contUnit) || unit.Id == 0)
						{
							if (contUnit == unit) //is itself
								continue;

							ulong newId = Id.Generate();
							while (allIds.ContainsKey(newId))
								newId = Id.Generate();
						
							if (unit.Id != 0)
								oldNewIds.Add(unit.Id, newId);
							else
								zeroNewIds.Add(newId);

							unit.Id = newId;
						}

						allIds.Add(unit.Id, unit);
					}
			}

		#endregion

		#region Linking

			public void Link (IOutlet<object> outlet, IInlet<object> inlet)
			{
				//unlinking
				if (outlet == null  &&  links.ContainsKey(inlet))
					links.Remove(inlet);

				//linking
				else //if (CheckLinkValidity(outlet, inlet)) 
				{
					if (links.ContainsKey(inlet)) links[inlet] = outlet;
					else links.Add(inlet, outlet);
				}

				//cachedBackwardLinks = null;
			}


			public void Link (IInlet<object> inlet, IOutlet<object> outlet)
			/// The same in case this order is more convenient
				{ Link(outlet, inlet); }


			public bool CheckLinkValidity (IOutlet<object> outlet, IInlet<object> inlet)
			{
				if (Generator.GetGenericType(outlet) != Generator.GetGenericType(inlet))
					return false;

				if (AreDependent(inlet.Gen, outlet.Gen)) //in this order
					return false;

				return true;
			}

			public bool AreDependent (Generator prevGen, Generator nextGen)
			/// Performance: 1000 iterations on TutorialObjects graph take 20-40 ms
			{
				if (prevGen == nextGen)
					return true;

				if (nextGen is IInlet<object> nextInlet  &&  
					links.TryGetValue(nextInlet, out IOutlet<object> nextInletLink)  &&
					AreDependent(prevGen, nextInletLink.Gen) ) 
						return true;

				if (nextGen is IMultiInlet nextMulIn)
				{
					foreach (IInlet<object> nextIn in nextMulIn.Inlets())
					{
						if (links.TryGetValue(nextIn, out IOutlet<object> nextInLink)  &&
							AreDependent(prevGen, nextInLink.Gen) )
								return true;
					}
				}

				if (nextGen is ICustomDependence cusDepGen)
				{
					foreach (Generator priorGen in cusDepGen.PriorGens())
					{
						if (AreDependent(prevGen, priorGen))
							return true;
					}
				}

				return false;
			}

			public bool AreDependent (Generator prevGen, IInlet<object> nextInlet)
			{
				if (links.TryGetValue(nextInlet, out IOutlet<object> parentOut))
				{
					Generator parentGen = parentOut.Gen;
					return AreDependent(prevGen, parentGen);
				}
				else
					return false;
			}


			public bool IsLinked (IInlet<object> inlet) => links.ContainsKey(inlet);
			/// Is this inlet linked to anything


			public IOutlet<object> GetLink (IInlet<object> inlet) => links.TryGetValue(inlet, out IOutlet<object> outlet) ? outlet : null;
			/// Simply gets inlet's link


			public void UnlinkInlet (IInlet<object> inlet)
			{
				if (links.ContainsKey(inlet))
					links.Remove(inlet);
				
				//cachedBackwardLinks = null;
			}


			public void UnlinkOutlet (IOutlet<object> outlet)
			/// Removes any links to this outlet
			{
				List<IInlet<object>> linkedInlets = new List<IInlet<object>>();

				foreach (IInlet<object> inlet in linkedInlets)
					links.Remove(inlet);

				//cachedBackwardLinks = null;
			}


			public void UnlinkGenerator (Generator gen)
			/// Removes all links from and to this generator
			{
				List<IInlet<object>> genLinks = new List<IInlet<object>>(); //both that connected to this gen inlets and outlets

				foreach (var kvp in links)
				{
					IInlet<object> inlet = kvp.Key;
					IOutlet<object> outlet = kvp.Value;

					//unlinking this from others (not needed on remove, but Unlink could be called not only on remove)
					if (inlet.Gen == gen)
						genLinks.Add(inlet);

					//unlinking others from this
					if (outlet.Gen == gen)
						genLinks.Add(inlet);
				}

				foreach (IInlet<object> inlet in genLinks)
					links.Remove(inlet);

				//cachedBackwardLinks = null;
			}


			private List<IInlet<object>> LinkedInlets (IOutlet<object> outlet)
			/// Isn't fast, so using for internal purpose only. For other cases use cachedBackwardsLinks
			{
				List<IInlet<object>> linkedInlets = new List<IInlet<object>>();
				
				foreach (var kvp in links)
					if (kvp.Value == outlet)
						linkedInlets.Add(kvp.Key);

				return linkedInlets;
			}


			public void ThroughLink (Generator gen)
			/// Connects previous gen outlet with next gen inlet maintaining link before removing this gen
			/// This will not unlink generator completely - other inlets may remain
			{
				//choosing the proper inlets and outlets for re-link
				IInlet<object> inlet = null;
				IOutlet<object> outlet = null;

				if (gen is IInlet<object>  &&  gen is IOutlet<object>)
					{ inlet = (IInlet<object>)gen; outlet = (IOutlet<object>)gen; }

				if (gen is IMultiInlet multInGen  &&  gen is IOutlet<object> outletGen)
				{
					Type genericType = Generator.GetGenericType(gen);
					foreach (IInlet<object> genInlet in multInGen.Inlets())
					{
						if (!IsLinked(genInlet)) continue;
						if (Generator.GetGenericType(genInlet) == genericType) inlet = genInlet; //the first inlet of gen type
					}
				}

				if (gen is IInlet<object> inletGen  &&  gen is IMultiOutlet multOutGen)
				{
					Type genericType = Generator.GetGenericType(gen);
					foreach (IOutlet<object> genOutlet in multOutGen.Outlets())
					{
						if (Generator.GetGenericType(genOutlet) == genericType) outlet = genOutlet; //the first outlet of gen type
					}
				}

				if (inlet == null || outlet == null) return;
					
				// re-linking
				List<IInlet<object>> linkedInlets = LinkedInlets(outlet); //other generator's inlet connected to this gen
				if (linkedInlets.Count == 0)
					return;
				
				IOutlet<object> linkedOutlet;
				if (!links.TryGetValue(inlet, out linkedOutlet))
					return;
				
				foreach (IInlet<object> linkedInlet in linkedInlets)
					Link(linkedOutlet, linkedInlet);
			}


			public void AutoLink (Generator gen, IOutlet<object> outlet)
			/// Links with first gen's inlet of the same type as outlet
			{
				Type outletType = Generator.GetGenericType(outlet);

				if (gen is IInlet<object> inletGen)
				{
					if (Generator.GetGenericType(inletGen) == outletType)
						Link(outlet, inletGen);
				}

				else if (gen is IMultiInlet multInGen)
				{
					foreach (IInlet<object> inlet in multInGen.Inlets())
						if (Generator.GetGenericType(inlet) == outletType)
							{ Link(outlet,inlet); break; }
				}
			}

		#endregion


		#region Iterating Nodes

			// Not iteration nodes in subGraphs
			// Using recursive fn calls instead (with Graph in SubGraphs)

			public IEnumerable<Generator> GetGenerators (Predicate<Generator> predicate)
			/// Iterates in all generators that match predicate condition
			{
				int i = -1;
				for (int g=0; g<generators.Length; g++)
				{
					i = Array.FindIndex(generators, i+1, predicate);
					if (i>=0) yield return generators[i];
					else break;
				}
			}


			public Generator GetGenerator (Predicate<Generator> predicate)
			/// Finds first generator that matches condition
			/// Returns null if nothing found (no need to use TryGet)
			{
				int i = Array.FindIndex(generators, predicate);
				if (i>=0) return generators[i]; 
				else return null;
			}


			public IEnumerable<T> GeneratorsOfType<T> ()
			/// Iterates all generators of given type
			{
				for (int g=0; g<generators.Length; g++)
				{
					if (generators[g] is T tGen)
						yield return tGen;
				}
			}


			public int GeneratorsCount (Predicate<Generator> predicate)
			/// Finds the number of generators that match given condition
			{
				int count = 0;

				int i = -1;
				for (int g=0; g<generators.Length; g++)
				{
					i = Array.FindIndex(generators, i+1, predicate);
					if (i>=0) count++;
				}

				return count;
			}


			public bool ContainsGenerator (Generator gen) => GetGenerator(g => g==gen) != null;

			public bool ContainsGeneratorOfType<T> () => GetGenerator(g => g is T) != null;


			public int GeneratorsCount<T> () where T: class
			{
				bool findByType (Generator g) => g is T;
				return GeneratorsCount(findByType);
			}


			public IEnumerable<IUnit> AllUnits ()
			/// Iterates all generators and layers in graph. May iterate twice if layer is input and output
			{
				foreach (Generator gen in generators)
				{
					yield return gen;

					if (gen is IMultiLayer multGen)
						foreach (IUnit layer in multGen.Layers)
							yield return layer;

					if (gen is IMultiInlet inlGen)
						foreach (IInlet<object> layer in inlGen.Inlets())
							yield return layer;

					if (gen is IMultiOutlet outGen)
						foreach (IOutlet<object> layer in outGen.Outlets())
							yield return layer;
				}
			}


			public IEnumerable<T> UnitsOfType<T> ()
			/// Iterates all generators and layers in graph. May iterate twice if layer is input and output
			{
				foreach (Generator gen in generators)
				{
					if (gen is T tgen)
						yield return tgen;

					if (gen is IMultiLayer multGen)
						foreach (IUnit layer in multGen.Layers)
							if (layer is T tlayer)
								yield return tlayer;

					if (gen is IMultiInlet inlGen)
						foreach (IInlet<object> layer in inlGen.Inlets())
							if (layer is T tlayer)
								yield return tlayer;

					if (gen is IMultiOutlet outGen)
						foreach (IOutlet<object> layer in outGen.Outlets())
							if (layer is T tlayer)
								yield return tlayer;
				}
			}


			public IEnumerable<Graph> SubGraphs (bool recursively=false)
			/// Enumerates in all child graphs recursively
			{
				foreach (IBiome biome in UnitsOfType<IBiome>())
				{
					Graph subGraph = biome.SubGraph;
					if (subGraph == null) continue;

					yield return biome.SubGraph;

					if (recursively)
						foreach (Graph subSubGraph in subGraph.SubGraphs(recursively:true))
							yield return subSubGraph;
				}
			}


			public bool ContainsSubGraph (Graph subGraph, bool recursively=false)
			{
				foreach (IBiome biome in UnitsOfType<IBiome>())
				{
					Graph biomeSubGraph = biome.SubGraph;
					if (biomeSubGraph == null) continue;
					if (biomeSubGraph == subGraph) return true;
					if (recursively && biomeSubGraph.ContainsSubGraph(subGraph, recursively:true)) return true;
				}
				
				return false;
			}


			public IEnumerable<Generator> RelevantGenerators (bool isDraft)
			/// All nodes that end chains, have previes, etc - all that should be generated
			{
				for (int g=0; g<generators.Length; g++)
					if (IsRelevant(generators[g], isDraft))
						yield return generators[g];
			}


			public bool IsRelevant (Generator gen, bool isDraft)
			{
				if (gen is OutputGenerator outGen)
				{
					if (isDraft  &&  outGen.OutputLevel.HasFlag(OutputLevel.Draft)) return true;
					if (!isDraft  && outGen.OutputLevel.HasFlag(OutputLevel.Main)) return true;
				}

				else if (gen is IRelevant)
					return true;

				else if (gen is IBiome biomeGen && biomeGen.SubGraph!=null)
					return true;

				else if (gen is IMultiLayer multiLayerGen)
				{
					foreach (IUnit layer in multiLayerGen.Layers)
						if (layer is IBiome)
							return true;
				}

				else if (gen.guiPreview)
					return true;

				return false;
			}

		#endregion


		#region Generate

		//And all the stuff that takes data into account

			public void ClearGenerator (Generator gen, TileData data)
			/// Clears this generator only
			/// Sets gen to non-ready (in any sub-data) and then removes ready state for all dependent from it
			/// Called from MM UI on top level only, but each biome runs this for subgraph recursively
			{
				#if MM_DEBUG
				Log.Add("ClearGenerator (draft:" + data.isDraft + " gen:" + gen.id); 
				#endif

				data.ClearReady(gen); 
				//no need to check if it is contained in graph, just removing ready state from data
				//maybe move to ClearDirectly?

				if (gen is ICustomClear cgen  &&  ContainsGenerator(gen))
					cgen.ClearDirectly(data);

				foreach (ICustomClear acGen in GeneratorsOfType<ICustomClear>())
					acGen.ClearAny(gen, data);
			}


			public bool ClearChanged (TileData data)
			/// Removes ready state if any of prev gens is not ready
			/// Clears all relevants if prior generator is clear
			{
				RefreshInputHashIds();

				//clearing nodes that are not in graph
				data.RemoveNotContained(this);

				Dictionary<Generator,bool> processed = new Dictionary<Generator,bool>();
				//using processed instead of ready since non-ready generators should be cleared too - they might have NonReady-ReadyOutdated-NonReady chanis

				//clearing graph nodes
				bool allReady = true;
				foreach (Generator relGen in RelevantGenerators(data.isDraft))
					allReady = allReady & ClearChangedRecursive(relGen, data, processed);
				
				return allReady;
			}


			private bool ClearChangedRecursive (Generator gen, TileData data, Dictionary<Generator,bool> processed)
			/// Removes ready state if any of prev gens is not ready, per-gen
			{
				if (processed.TryGetValue(gen, out bool processedReady))
					return processedReady;

				bool ready = data.IsReady(gen);
				//don't return if not ready, got to check inlet chains and biomes subs

				if (gen is IInlet<object> inletGen)  
				{
					if (links.TryGetValue(inletGen, out IOutlet<object> precedingOutlet))
					{
						Generator precedingGen = precedingOutlet.Gen;

						bool inletReady; //loading from lut or clearing recursive
						if (!processed.TryGetValue(gen, out inletReady))
							inletReady = ClearChangedRecursive(precedingGen, data, processed);

						ready = ready && inletReady;
					}
				}

				if (gen is IMultiInlet multInGen)
					foreach (IInlet<object> inlet in multInGen.Inlets())
					{
						//the same as inletGen
						if (links.TryGetValue(inlet, out IOutlet<object> precedingOutlet))
						{
							Generator precedingGen = precedingOutlet.Gen;

							bool inletReady; //loading from lut or clearing recursive
							if (!processed.TryGetValue(gen, out inletReady))
								inletReady = ClearChangedRecursive(precedingGen, data, processed);

							ready = ready && inletReady;
							//no break, need to check-clear other layers chains
						}
					}

				if (gen is ICustomDependence customDepGen)
					foreach (Generator priorGen in customDepGen.PriorGens())
					{
						ClearChangedRecursive(priorGen, data, processed);

						if (!data.IsReady(priorGen.id))
							{ ready = true; break; }
					}

				if (gen is ICustomClear cgen)
				{
					cgen.ClearRecursive(data);
					ready = data.IsReady(gen);
				}		

				if (!ready) data.ClearReady(gen);  //maybe move to ClearRecursive?
				processed.Add(gen, ready);
				return ready;
			}


			public void Prepare (TileData data, Terrain terrain, Override ovd=null)
			/// Executed in main thread to perform terrain reads or something
			/// Not recursive just for performance reasons. Prepares only on-ready generators
			{
				if (ovd==null)
					ovd = defaults;

				foreach (Generator gen in generators)
				{
					if (!(gen is IPrepare prepGen)) continue;
					if (data.IsReady(gen.id)) continue;

					//applying override (duplicating gen if needed)
					Generator cGen = gen;
					if (Assigner.IsExposed(gen, exposed))
						cGen = (Generator)Assigner.CopyAndAssign(gen, exposed, ovd);

					//preparing
					((IPrepare)cGen).Prepare(data, terrain);
				}
			}


			public void PrepareRecursive (Generator gen, TileData data, Terrain terrain, Override ovd, HashSet<Generator> processed)
			{
				if (processed.Contains(gen)) //already prepared
					return;

				if (gen is IInlet<object> inletGen)
				{
					if (links.TryGetValue(inletGen, out IOutlet<object> outlet))
						PrepareRecursive(outlet.Gen, data, terrain, ovd, processed);
				}

				if (gen is IMultiInlet multInGen)
				{
					foreach (IInlet<object> inlet in multInGen.Inlets())
						if (links.TryGetValue(inlet, out IOutlet<object> outlet))
							PrepareRecursive(outlet.Gen, data, terrain, ovd, processed);
				}

				if (gen is ICustomDependence customDepGen)
				{
					foreach (Generator priorGen in customDepGen.PriorGens())
						PrepareRecursive(priorGen, data, terrain, ovd, processed);
				}

				//not excluding disabled: they should generate the chain before them (including outputs like Textures)
			}


			public void Generate (TileData data, StopToken stop=null, Override ovd=null)
			{
				#if MM_DEBUG
				Log.Add("Generate (draft:" + data.isDraft + ")", $"{data.area.Coord.x}, {data.area.Coord.z}");
				#endif

				//refreshing link ids lut (only for top level graph)
				RefreshInputHashIds();

				if (ovd==null)
					ovd = defaults;

				#if MM_DEBUG
				if (!debugGenerate) return;
				#endif

				//main generate pass - all changed gens recursively
				foreach (Generator relGen in RelevantGenerators(data.isDraft))
				{
					if (stop!=null && stop.stop) return;
					GenerateRecursive(relGen, data, ovd,  stop:stop); //will not generate if it has not changed
				}
			}


			public void GenerateRecursive (Generator gen, TileData data, Override ovd, StopToken stop=null)
			{
				if (stop!=null && stop.stop) return;
				if (data.IsReady(gen.id)) return;

				//generating inlets recursively
				if (gen is IInlet<object> inletGen)
				{
					if (links.TryGetValue(inletGen, out IOutlet<object> outlet))
						GenerateRecursive(outlet.Gen, data, ovd, stop:stop);
				}

				if (gen is IMultiInlet multInGen)
				{
					foreach (IInlet<object> inlet in multInGen.Inlets())
						if (links.TryGetValue(inlet, out IOutlet<object> outlet))
							GenerateRecursive(outlet.Gen, data, ovd, stop:stop);
				}

				if (gen is ICustomDependence customDepGen)
				{
					foreach (Generator priorGen in customDepGen.PriorGens())
						GenerateRecursive(priorGen, data, ovd:ovd, stop:stop);
				}

				//checking for generating twice
				if (stop!=null && stop.stop) return;
				if (data.IsReady(gen.id))  
					throw new Exception($"Generating twice {gen}, id: {Id.ToString(gen.id)}, draft: {data.isDraft}, stop: {(stop!=null ? stop.stop.ToString() : "null")}");

				//before-generated event
				//if (gen is ICustomGenerate customGen)
				//	customGen.OnBeforeGenerated(this, data, stop);

				//checking if all layers Gen and Id assigned (not necessary, remove)
				if (gen is IMultiLayer layerGen)
					foreach (IUnit layer in layerGen.Layers)
					{
						if (layer.Gen == null) layer.SetGen(gen);
						if (layer.Id == 0) layer.Id = Id.Generate();
					}

				//main generate fn
				long startTime = System.Diagnostics.Stopwatch.GetTimestamp();
				if (stop!=null && stop.stop) return;

				//applying override (duplicating gen if needed)
				Generator cGen = gen;
				if (Assigner.IsExposed(gen, exposed))
					cGen = (Generator)Assigner.CopyAndAssign(gen, exposed, ovd);

				//generating
				#if MM_DEBUG
				//Log.Add($"Generating {cGen.GetType().Name} (draft:{data.isDraft})"); //too many nodes to show
				#endif

				cGen.Generate(data, stop);

				//checking time
				long deltaTime = System.Diagnostics.Stopwatch.GetTimestamp() - startTime;
				if (data.isDraft) gen.draftTime = 1000.0 * deltaTime / System.Diagnostics.Stopwatch.Frequency;
				else gen.mainTime = 1000.0 * deltaTime / System.Diagnostics.Stopwatch.Frequency;

				//marking ready
				if (stop!=null && stop.stop) return;
				data.MarkReady(gen);
				OnAfterNodeGenerated?.Invoke(gen, data);
			}


			public void Finalize (TileData data, StopToken stop=null)
			{
				if (stop!=null && stop.stop) { data.ClearFinalize(); return; } //no need to leave finalize for further generate
				
				while (data.FinalizeMarksCount > 0)
				{
					FinalizeAction action = data.DequeueFinalize();
					//data returns finalize actions with priorities (height first)

					//if (action == MatrixGenerators.HeightOutput200.finalizeAction)
					//{
					//	data.MarkFinalize(ObjectsGenerators.ObjectsOutput.finalizeAction, stop);
					//	data.MarkFinalize(ObjectsGenerators.TreesOutput.finalizeAction, stop);
					//}
					//can't use ObjectsGenerators since they are in the other module. Using OnOutputFinalized event instead.

					if (stop!=null && stop.stop) { data.ClearFinalize(); return; }
					action(data, stop);
				}
			}


			[Obsolete] private IEnumerator Apply (TileData data, Terrain terrain, StopToken stop=null)
			/// Actually not a graph function. Here for the template. Not used.
			{
				if (stop!=null && stop.stop) yield break;
				while (data.ApplyMarksCount != 0)
				{
					IApplyData apply = data.DequeueApply(); //this will remove apply from the list
					
					//applying routine
					if (apply is IApplyDataRoutine)
					{
						IEnumerator e = ((IApplyDataRoutine)apply).ApplyRoutine(terrain);
						while (e.MoveNext()) 
						{
							if (stop!=null && stop.stop) yield break;
							yield return null;
						}
					}

					//applying at once
					else
					{
						apply.Apply(terrain);
						yield return null;
					}
				}

				
				#if UNITY_EDITOR
				if (data.isPreview)
					UnityEditor.EditorWindow.GetWindow<UnityEditor.EditorWindow>("MapMagic Graph");
				#endif

				//OnGenerateComplete.Raise(data);
			}


			public void Purge (Type type, TileData data, Terrain terrain)
			/// Purges the results of all output generators of type
			{
				for (int g=0; g<generators.Length; g++)
				{
					if (!(generators[g] is OutputGenerator outGen)) continue;

					Type genType = outGen.GetType();
					if (genType==type  ||  type.IsAssignableFrom(genType))
						outGen.ClearApplied(data, terrain);
				}

				foreach (Graph subGraph in SubGraphs())
					subGraph.Purge(type, data, terrain);
			}


			private void RefreshInputHashIds ()
			/// For each inlet in graph writes the id of linked outlet
			{
				//foreach (IUnit unit in AllUnits()) if (unit is IInlet<object> inlet)  inlet.LinkedOutletId = 0;  
				//do not set LinkedOutletId beforehand - it might be generating, and some generator will read this id. Caused skipped tiles issue. Better do it after

				foreach (var kvp in links)
				{
					IInlet<object> inlet = kvp.Key;
					IOutlet<object> outlet = kvp.Value;
					inlet.LinkedOutletId = outlet.Id;
					inlet.LinkedGenId = outlet.Gen.id;
				}

				foreach (IUnit unit in AllUnits()) 
					if (unit is IInlet<object> inlet  &&  !links.ContainsKey(inlet))
						inlet.LinkedOutletId = 0; 
			}


			public static IEnumerable<(Graph,TileData)> SubGraphsDatas (Graph rootGraph, TileData rootData)
			/// Includes root level graph-data as well
			{
				yield return (rootGraph, rootData);

				foreach (IBiome biome in rootGraph.UnitsOfType<IBiome>()) //top level first
				{
					Graph subGraph = biome.SubGraph;
					if (subGraph == null)
						continue;

					TileData subData = rootData.GetSubData(biome.Id);
					if (subData == null)
						continue;

					foreach ((Graph,TileData) subSub in SubGraphsDatas(subGraph, subData))
						yield return subSub;
				}
			}

		#endregion


		#region Complexity/Progress

			public float GetGenerateComplexity ()
			/// Gets the total complexity of the graph (including biomes) to evaluate the generate progress
			{
				float complexity = 0;

				for (int g=0; g<generators.Length; g++)
				{
					if (generators[g] is ICustomComplexity)
						complexity += ((ICustomComplexity)generators[g]).Complexity;

					else
						complexity ++;
				}

				return complexity;
			}


			public float GetGenerateProgress (TileData data)
			/// The summary complexity of the nodes Complete (ready) in data (shows only the graph nodes)
			/// No need to combine with GetComplexity since these methods are called separately
			{
				float complete = 0;

				//generate
				for (int g=0; g<generators.Length; g++)
				{
					if (generators[g] is ICustomComplexity)
						complete += ((ICustomComplexity)generators[g]).Progress(data);

					else
					{
						if (data.IsReady(generators[g].id))
							complete ++;
					}
				}

				return complete;
			}


			public float GetApplyComplexity ()
			/// Gets the total complexity of the graph (including biomes) to evaluate the generate progress
			{
				HashSet<Type> allApplyTypes = GetAllOutputTypes();
				return allApplyTypes.Count;
			}


			private HashSet<Type> GetAllOutputTypes (HashSet<Type> outputTypes=null)
			/// Looks in subGraphs recursively
			{
				if (outputTypes == null)
					outputTypes = new HashSet<Type>();

				for (int g=0; g<generators.Length; g++)
					if (generators[g] is OutputGenerator)
					{
						Type type = generators[g].GetType();
						if (!outputTypes.Contains(type))
							outputTypes.Add(type);
					}

				foreach (Graph subGraph in SubGraphs())
					subGraph.GetAllOutputTypes(outputTypes);

				return outputTypes;
			}


			public float GetApplyProgress (TileData data)
			{
				return data.ApplyMarksCount;
			}

		#endregion


		#region Serialization

			
			[SerializeField] private GraphSerializer200Beta serializer200beta = null;
			//[SerializeField] private GraphSerializer199 serializer199 = null;
			//public Serializer.Object[] serializedNodes = new Serializer.Object[0];



			public void OnBeforeSerialize ()
			{ 
				if (generators == null) 
					OnAfterDeserialize(); //trying to load graph once more

				if (generators == null) 
					throw new Exception("Could not save graph data, node array is null. Check if graph was loaded successfully"); 

				if (serializer200beta == null)
					serializer200beta = new GraphSerializer200Beta();
				serializer200beta.Serialize(this);
			}

			public void OnAfterDeserialize () 
			{
//				try 
				{
					if (serializer200beta != null) serializer200beta.Deserialize(this);
					//if (serializer199 != null) serializer199.Deserialize(this);
					//if (serializedNodes!=null && serializedNodes.Length!=0) generators = (Generator[])Serializer.Deserialize(serializedNodes);
				}
/*				
				catch (Exception e) 
				{ 
					generators=null; 
					//Den.Tools.Tasks.CoroutineManager.Enqueue(()=>Debug.LogError("Could not load graph data: " + name + "\n" + e, this));
					throw new Exception("Could not load graph data:\n" + e); 
				} 
*/
			}

			public static void DeepCopy (Graph src, Graph dst)
			{
				dst.name = src.name;
				if (dst.serializer200beta==null) dst.serializer200beta = new GraphSerializer200Beta();
				dst.serializer200beta.Serialize(src); //using dst's serializer (no need to create new one)
				dst.serializer200beta.Deserialize(dst);
			}


		#endregion


		#region Debug


			public static Graph debugGraph; //assign via watch

			public string DebugUnitName (ulong id, bool useGraphName=true, string prevGraphNames=null)
			/// Gets the unit name, number, graph, layer, etc by it's. 
			/// Looks in subgraphs too
			/// Disable use graph name when running from thread
			{
				foreach (IUnit unit in AllUnits())
					if (unit.Id == id)
					{
						string unitName = DebugUnitName(unit);

						if (useGraphName) unitName += $" graph:{name}";
						if (prevGraphNames != null) unitName += $" prev:{prevGraphNames}";

						return unitName;
					}

				int biomeNum = 0;
				foreach (IUnit unit in AllUnits())
					if (unit is IBiome biome)
					{
						string subPrefix;
						if (useGraphName) subPrefix =  $"{prevGraphNames}.{name} (b:{biomeNum})";
						else subPrefix = $"(b:{biomeNum})";

						string subResult = biome.SubGraph.DebugUnitName(id, useGraphName, subPrefix);
						if (subResult != null)
							return subResult;

						biomeNum++;
					}
						
				return null;
			}


			public string[] DebugAllUnits (int subLevel=0)
			{
				List<string> strs = new List<string>();

				foreach (IUnit unit in AllUnits())
					strs.Add( DebugUnitName(unit) + " sub:" + subLevel );

				foreach (IUnit unit in AllUnits())
					if (unit is IBiome biome)
						strs.AddRange( biome.SubGraph.DebugAllUnits(subLevel+1) );

				return strs.ToArray();
			}


			public string DebugUnitName (IUnit unit)
			/// Just returns unit information
			{
				string unitName = $"{unit.GetType().Namespace}.{unit.GetType().Name}";
				string genName = $"{unit.Gen.GetType().Namespace}.{unit.Gen.GetType().Name}";

				int genNum = -1;
				for (int g=0; g<generators.Length; g++)
					if (generators[g] == unit.Gen)
						{ genNum=g; break; }

				int layerNum = -1; int layerCounter = 0;
				if (unit.Gen != unit  &&  unit.Gen is IMultiLayer multLayerGen)
					foreach (IUnit layer in multLayerGen.Layers)
					{
						if (layer == unit)
							{ layerNum = layerCounter; break; }
						layerCounter++;
					}

				int inletNum = -1; int inletCounter = 0;
				if (unit.Gen != unit  &&  unit.Gen is IMultiInlet multInletGen)
					foreach (IUnit inlet in multInletGen.Inlets())
					{
						if (inlet == unit)
							{ inletNum = inletCounter; break; }
						inletCounter++;
					}

				int outletNum = -1; int outletCounter = 0;
				if (unit.Gen != unit  &&  unit.Gen is IMultiOutlet multOutletGen)
					foreach (IUnit outlet in multOutletGen.Outlets())
					{
						if (outlet == unit)
							{ outletNum = outletCounter; break; }
						outletCounter++;
					}

				return $"unit:{unitName}, gen:{genName}, id:{Id.ToString(unit.Id)}, g:{genNum}, l:{layerNum}, i:{inletNum}, o:{outletNum}";
			}

		#endregion
	}
}