using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine.Profiling;

using Den.Tools;

using MapMagic.Core;
using MapMagic.Products;

namespace MapMagic.Nodes
{
	public static class Placeholders
	{
		[Serializable]
		public class SerObject
		/// nearly copy of Serializer.Object, but has some fields missed intentionally to allow serialization
		{
			public string type = null;
			public string[] fields = null;
			public Serializer.Value[] values = null;

			public static explicit operator Serializer.Object (SerObject src)
			{
				Serializer.Object dst = new Serializer.Object();
				dst.type = src.type;
				dst.fields = src.fields;
				dst.values = src.values;
				return dst;
			}

			public static explicit operator SerObject (Serializer.Object src)
			{
				SerObject dst = new SerObject();
				dst.type = src.type;
				dst.fields = src.fields;
				dst.values = src.values;
				return dst;
			}
		}

		public abstract class GenericPlaceholder : Generator,  Serializer.ICustomSerialization
		{
			public string origType;

			[NonSerialized] public string[] origFields = new string[0];
			[NonSerialized] public Serializer.Value[] origValues = new Serializer.Value[0];

			//TODO: does not serialize UnityObject references
			//TODO: does not serialize layers or multi-inlet links

			public override void Generate (TileData data, StopToken stop) { }


			public void PreprocessBeforeDeserialize (Serializer.Object serObj, Serializer.Object[] allSerialized, object[] allDeserialized) 
			/// Loading placeholder
			/// Reading serObj and converting it to placeholder values
			{
				origType = serObj.type;

				List<string> fieldsList = new List<string>();
				List<Serializer.Value> valuesList = new List<Serializer.Value>();

				for (int v=0; v<serObj.values.Length; v++)
				{
					if (serObj.values[v].t != 255) //if not reference
					{
						fieldsList.Add(serObj.fields[v]);
						valuesList.Add(serObj.values[v]);
					}

					/*else if (serObj.values[v] >= 0) //if not null
					{
						 Serializer.Object fieldObj = allSerialized[serObj.values[v]];
						 Type fieldType = Type.GetType(fieldObj.type);
						 if (fieldType.IsGenericType &&
							 IsInletType(fieldType) ) 
								Debug.Log(fieldType);
					}*/
				}

				origFields = fieldsList.ToArray();
				origValues = valuesList.ToArray();
			}


			public void PostprocessAfterSerialize (Serializer.Object serObj, Dictionary<object,Serializer.Object> allSerialized) 
			/// Storing original instead of placeholder
			/// Writing serObj 
			{
				serObj.type = origType;

				ArrayTools.Append(ref serObj.fields, origFields);
				ArrayTools.Append(ref serObj.values, origValues); 
			}
		}

		public static bool IsInletType (Type type) => 
			type.GetInterfaces().Find(i => i.GetGenericTypeDefinition() == typeof(IInlet<>))  >=  0;

		[GeneratorMenu (name = "Unknown", iconName="GeneratorIcons/Generator")]
		public class InletOutletPlaceholder : GenericPlaceholder, IInlet<object>, IOutlet<object> { }

		[GeneratorMenu (name = "Unknown", iconName="GeneratorIcons/Generator")]
		public class InletPlaceholder : GenericPlaceholder, IInlet<object> { }

		[GeneratorMenu (name = "Unknown", iconName="GeneratorIcons/Generator")]
		public class OutletPlaceholder : GenericPlaceholder, IOutlet<object> { }

		[GeneratorMenu (name = "Unknown", iconName="GeneratorIcons/Generator")]
		public class Placeholder : GenericPlaceholder { }


		/*[GeneratorMenu (name = "Unknown", iconName="GeneratorIcons/Generator")]
		public class MultiInletOutletPlaceholder : GenericPlaceholder, IMultiInlet, IMultiOutlet 
		{ 
			
		}*/
	}
}