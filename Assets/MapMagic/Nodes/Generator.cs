using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.Matrices; //Normalize gen
//using Den.Tools.Segs;
using Den.Tools.Splines;

using MapMagic.Core;
using MapMagic.Products;

namespace MapMagic.Nodes
{
	public interface IUnit
	/// Either Generator or significant (processed by graph Generate) Layer
	{
		Generator Gen { get; } 
		void SetGen (Generator gen);

		ulong Id { get; set; }

		IUnit ShallowCopy();
	}

	public interface IInlet<out T> : IUnit where T:class 
		/// The one that linked with the outlet via graph dictionary
	/// Could be generator or layer itself or a special inlet obj
	{ 
		ulong LinkedOutletId { get; set; } //is set each on clear or generate from graph luts. Just to quickly get inlet product/ready
		ulong LinkedGenId { get; set; } //the same, but gets Outlet.Gen (for ready)
	}

	public interface IOutlet<out T> : IUnit where T:class { }
	/// The one that generates and stores product
	/// Could be generator or layer itself or a special outlet obj

	public interface IMultiInlet
	/// Generator that stores multiple inlets (either layered or standard)
	{ 
		IEnumerable<IInlet<object>> Inlets ();
	}

	public interface IMultiOutlet 
	/// Generator that stores multiple outlets
	{ 
		IEnumerable<IOutlet<object>> Outlets ();
	}

	[Serializable]
	public class Inlet<T> : IInlet<T> where T: class 
	/// The one that is assigned in non-layered multiinlet generators
	{
		[SerializeField] private Generator gen; 
		public Generator Gen { get{return gen;} private set{gen=value;} } //auto-property is not serialized
		public void SetGen (Generator gen) => Gen=gen;

		public ulong id; //don't generate on creating (will cause error on serialization. And no need to generate it on each serialize)
		public ulong Id { get{if (id==0) id=Den.Tools.Id.Generate(); return id;} set{id=value;} }
		public ulong LinkedOutletId { get; set; }  //Assigned every before each clear or generate
		public ulong LinkedGenId { get; set; } 

		public IUnit ShallowCopy() => (Inlet<T>)this.MemberwiseClone(); 
	}

	[Serializable]
	public class Outlet<T> : IOutlet<T> where T: class
	/// The one that is assigned in non-layered multioutlet generators
	{
		[SerializeField] private Generator gen; 
		public Generator Gen { get{return gen;} private set{gen=value;} }
		public void SetGen (Generator gen) => Gen=gen;

		public ulong id; //don't generate on creating (will cause error on serialization)
		public ulong Id { get{if (id==0) id=Den.Tools.Id.Generate(); return id;} set{id=value;} }

		public IUnit ShallowCopy() => (Outlet<T>)this.MemberwiseClone();
	}

	public interface IPrepare
	/// Node has something to make in main thread before generate start in Prepare fn
	{
		void Prepare (TileData data, Terrain terrain);
		ulong Id { get; set; }
	}


	public interface ISceneGizmo 
	/// Displays some gizmo in a scene view
	{ 
		void DrawGizmo(); 
		bool hideDefaultToolGizmo {get;set;} 
	}


	[Flags] public enum OutputLevel { Draft=1, Main=2, Both=3 }  //Both is for gui purpose only

	public abstract class OutputGenerator : Generator
	/// Final output node (height, textures, objects, etc)
	{
		//just to mention: static Finalize (TileData data, StopToken stop);
		//Action<TileData,StopToken> FinalizeAction { get; }
		public abstract void ClearApplied (TileData data, Terrain terrain);
		public abstract OutputLevel OutputLevel {get;}
	}

	/*public interface IOutput
	/// Either output layer or output generator itself
	/// TODO: merge with output generator?
	{
		Generator Gen { get; } 
		void SetGen (Generator gen);
		ulong Id { get; set; }
	}
	*/

	public interface IApplyData
	{
		void Apply (Terrain terrain);
		int Resolution {get;}
	}


	public interface IApplyDataRoutine : IApplyData
	{
		IEnumerator ApplyRoutine (Terrain terrain);
	}


	public interface ICustomComplexity
	/// To implement both Complexity and Progress properties
	{
		float Complexity { get; } //default is 1
		float Progress (TileData data);  //can be different from Complexity if the generator is partly done. Max is Complexity, min 0
	}


	public interface IBiome : IUnit
	/// Passes the current graph commands to sub graph(s)
	{
		Graph SubGraph { get; }
		Expose.Override Override { get; set; }
	}


	public interface ICustomDependence
	/// Makes PriorGens generated before generating this one
	/// Used in Portals, mainly for checking link validity
	{
		IEnumerable<Generator> PriorGens ();
	}

	public interface IRelevant { } 
	/// Should be generated when generating graph

	public interface IMultiLayer
	/// If generator contains layers that should be processed with graph Generate function
	/// In short - if layer contains id generator should be multi-layered
	/// Theoretically it should replace IMultiInlet/IMultiOutlet/IMultiBiome
	{
//		IEnumerable<IUnit> Layers ();
		IList<IUnit> Layers { get; set; }
		bool Inversed { get; } //for gui purpose, to draw mini
		bool HideFirst { get; }
	}

	public interface ICustomClear 
	/// Performs additional actions before clearing (like clearing sub-datas)
	{
		void ClearDirectly (TileData data);  //Called by graph if gen field was changed. Will call data.ClearReady(gen) beforehand anyway
		void ClearRecursive (TileData data);  //Called by graph on clearing recursive (no matter ready or not). Inlets are already cleared to this moment
		void ClearAny (Generator gen, TileData data); //Called at top level graph each time any node changes. Iterating in sub-graph is done with this
	}


	public sealed class GeneratorMenuAttribute : Attribute
	{
		public string menu;
		public int section;
		public string name;
		public string menuName; //if not defined using real name
		public string iconName;
		public bool disengageable;
		public bool disabled;
		public int priority;
		public string helpLink;
		public bool lookLikePortal = false; //brush reads/writes are portals, but should look like gens
		public bool drawInlets = true;
		public bool drawOutlet = true;
		public bool drawButtons = true;
		public bool advancedOptions = false;
		public Type colorType = null; ///> to display the node in the color of given outlet type
		public Type updateType;  ///> The class legacy generator updates to when clicking Update

		//these are assigned on load attribute in gen and should be null by default
		public string nameUpper;
		public float nameWidth; //the size of the nameUpper in pixels
		public Texture2D icon;
		public Type type;
		public Color color;
	}


	[System.Serializable]
	public class Layer : IUnit
	/// Standard values to avoid duplicating them in each of generators layers
	{
		public Generator Gen { get; private set; }
		public void SetGen (Generator gen) => Gen=gen;

		public ulong id; //properties not serialized
		public ulong Id { get{return id;} set{id=value;} } 
		public ulong LinkedOutletId { get; set; }  //if it's inlet. Assigned every before each clear or generate
		public ulong LinkedGenId { get; set; } 

		public IUnit ShallowCopy() => (Layer)this.MemberwiseClone();
	}


	[System.Serializable]
	public abstract class Generator : IUnit
	{
		public bool enabled = true;

		public ulong id; //properties not serialized  //0 is empty id - reassigning it automatically
		public ulong Id { get{return id;} set{id=value;} } 
		public ulong LinkedOutletId { get; set; }  //if it's inlet. Assigned every before each clear or generate
		public ulong LinkedGenId { get; set; } 

		public double draftTime;
		public double mainTime;

		public Vector2 guiPosition;
		public Vector2 guiSize;  //to add this node to group

		public bool guiPreview; //is preview for this generator opened
		public bool guiAdvanced;

		//just to avoid implementing it in each generator
		public Generator Gen { get{ return this; } }
		public void SetGen (Generator gen) { }


		public static Generator Create (Type type)
		///Factory instead of constructor since could not create instance of abstract class
		{
			if (type.IsGenericTypeDefinition) type = type.MakeGenericType(typeof(Den.Tools.Matrices.MatrixWorld)); //if type is open generic type - creating the default matrix world
			
			Generator gen = (Generator)Activator.CreateInstance(type);
			gen.id = Den.Tools.Id.Generate();

			if (gen is IMultiLayer lgen)
				foreach (IUnit layer in lgen.Layers)
					{ layer.SetGen(gen); layer.Id = Den.Tools.Id.Generate(); }

			if (gen is IMultiInlet igen)
				foreach (IUnit layer in igen.Inlets())
					{ layer.SetGen(gen); layer.Id = Den.Tools.Id.Generate(); }

			if (gen is IMultiOutlet ogen)
				foreach (IUnit layer in ogen.Outlets())
					{ layer.SetGen(gen); layer.Id = Den.Tools.Id.Generate(); }

			return gen;
		}

		public IUnit ShallowCopy() => (Generator)this.MemberwiseClone();  //1000 noise generators in 0.1 ms

		public abstract void Generate (TileData data, StopToken stop);
		/// The stuff generator does to read inputs (already prepared), generate, and write product(s). Does not affect previous generators, ready state or event, just the essence of generate


		#region Generic Type

			public static Type GetGenericType (Type type)
			/// Finds out if it's map, objects or splines node/inlet/outlet.
			/// Returns T if type is T, IOutlet<T>, Inlet<T>, or inherited from any of those (where T is MatrixWorls, TransitionsList or SplineSys, or OTHER type)
			{
				Type[] interfaces = type.GetInterfaces();
				foreach (Type itype in interfaces)
				{
					if (!itype.IsGenericType) continue;
					//if (!typeof(IOutlet).IsAssignableFrom(itype) && !typeof(Inlet).IsAssignableFrom(itype)) continue;
					return itype.GenericTypeArguments[0];
				}

				return null;
			}

			//shotcuts to avoid evaluating by type
			public static Type GetGenericType<T> (IOutlet<T> outlet) where T: class  =>  typeof(T);
			public static Type GetGenericType<T> (IInlet<T> inlet) where T: class  =>  typeof(T);
			public static Type GetGenericType (Generator gen) 
			{
				if (gen is IOutlet<object> outlet) return GetGenericType(outlet);
				if (gen is IInlet<object> inlet) return GetGenericType(inlet);
				return null;
			}
			public static Type GetGenericType (IOutlet<object> outlet)
			{
				if (outlet is IOutlet<MatrixWorld>) return typeof(MatrixWorld);
				else if (outlet is IOutlet<TransitionsList>) return typeof(TransitionsList);
				else if (outlet is IOutlet<SplineSys>) return typeof(SplineSys);
				else return GetGenericType(outlet.GetType());
			}
			public static Type GetGenericType (IInlet<object> inlet)
			{
				if (inlet is IInlet<MatrixWorld>) return typeof(MatrixWorld);
				else if (inlet is IInlet<TransitionsList>) return typeof(TransitionsList);
				else if (inlet is IInlet<SplineSys>) return typeof(SplineSys);
				else return GetGenericType(inlet.GetType());
			}

		#endregion


		#region Serialization/Placeholders

			public Type AlternativeSerializationType
			{get{
				//if (this is IInlet<object> 
				return typeof(Placeholders.InletOutletPlaceholder);
			}}

		#endregion
	}
}