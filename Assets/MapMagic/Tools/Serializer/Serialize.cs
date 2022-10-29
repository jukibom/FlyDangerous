using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Tests.Editor")]

namespace Den.Tools.Serialization
{
	public partial class Serializer
	{
		// Serailization format:
		// <FieldName, <TypeFullName, AssemblyName>, Id, SubField1, SubField2, ..., SubFieldX>
		// id is the number of serialized/deserialized class in serializedObjsIds dict

		// Serialized foo object string
		//	  < class0, <Den.Tools.CoordRect, Tools>, 0,
		//		< offset, <Den.Tools.Coord, Tools>, 0,
		//		  <x, System.Single, 0, 3 >,
		//		  <z, System.Single, 0, 42 > >,
		//		< size, <Den.Tools.CoordWithRef, Tools>, 0,
		//		  <x, System.Single, 3 >,
		//		  <z, System.Single, 42 >,
		//		  <reference, <SomeType, Tools>, 1 > >,
		//		< selfReference, ref, 0 >,
		//		< vector, <UnityEngine.Vector2, UnityEngine.CoreModule>, 3.1415, 42.2 >,
		//		< floatVal, System.Single, 3.1415 >,
		//		< floatArr, System.Single[], <1,2,3,4,5> >,
		//		< refArr, ref[], <1,1,1,1,1> >,
		//		< vectorArr, 
		//		  <UnityEngine.Vector2[], UnityEngine.CoreModule>, 
		//		  < 1, <UnityEngine.Vector2, UnityEngine.CoreModule>, 3.1415, 42.2 >,
		//		  < 2, <UnityEngine.Vector2, UnityEngine.CoreModule>, 3.1415, 42.2 > > >,
		//	  < class1,
		//		<SomeType, Tools>,
		//		<x, System.Single, 3> >


		// TODO: 
		// Find out what is the best way to serialize referenced objects:
		// Ideas: serialize all as it is (with inlined class), assign class ids, and then just link to inlined classes (how to deserialize?)

		public static string Serialize (object val)
		/// Serializes all to string without Unity assets
		{
			if (val==null) 
				return "null";

			return SerializeRecursive("root", val, new Dictionary<object,int>(), null);
		}


		public static string Serialize (object val, out UnityEngine.Object[] unityObjs)
		/// This call will serialize all with assets. UnityObjs array should be saved as well.
		{
			if (val==null) 
				{ unityObjs = new UnityEngine.Object[0]; return "null"; }

			Dictionary<object,int> serializedObjsIds = new Dictionary<object,int>();
			List<UnityEngine.Object> unityObjsList = new List<UnityEngine.Object>();

			string str = SerializeRecursive("root", val, serializedObjsIds, unityObjsList);

			unityObjs = unityObjsList.ToArray();
			return str;
		}


		private static string SerializeRecursive (string name, object val, 
			Dictionary<object,int> serializedObjsIds, //all ids are 1-based. 0 means "don't look up - it's value or null"
			List<UnityEngine.Object> unityObjs, 
			string offset="")
		// If serializedVals defined will assign index to reference values. Otherwise will set them to null.
		// Will expand serialized object with new reference objects
		// BTW fastest way to create string is "x"+"y"+"z". Interpolated string $"{x}{y}{z}" just a bit slower, but more convenient to use (6.38ms vs 6.48ms). StringBuilder, Format, Concat, etc are slower.
		{
			//using ifs instead of switch since we need to check them all in this order, and some are same (like IList and Array)
			//name should be in " since some names (automatic) have < and > in them

			//null
			if (val == null)
			{
				string typeName = "System.Object"; //nullType!=null ? TypeName(nullType) : "System.Object"; //all nulls are System.Object no matter of what they expected to be
				return $"{offset}< \"{name}\", {typeName}, id0, null >";
			}

			//most common types
			switch (val)
			{
				case float fVal: return $"{offset}< \"{name}\", System.Single, id0, {fVal.ToString("R")} >";  //The round-trip ("R") format specifier attempts to ensure that a numeric value that is converted to a string is parsed back into the same numeric value
				case double dVal: return $"{offset}< \"{name}\", System.Double, id0, {dVal.ToString("R")} >"; //same for double
				case int iVal: return $"{offset}< \"{name}\", System.Int32, id0, {iVal.ToString()} >";
			}

			//loading type only now (to speed up all above)
			Type type = val.GetType(); //at this point val should not be null
			//bool isClass = type.IsClass; //IsClass is _extremely_ long operation (10 times longer than IsPrimitive, 100 times than GetType)			

			//already serialized (no mater of it's array, string, other class)
			if (serializedObjsIds!=null  &&  serializedObjsIds.TryGetValue(val, out int existingId))
				return $"{offset}< \"{name}\", \"{TypeName(type)}\", id{existingId} >";

			//string
			if (val is string)
			{
				int id = serializedObjsIds.Count + 1; 
				serializedObjsIds.Add(val, id); //checking if it was serialized in the beginning

				return $"{offset}< \"{name}\", System.String, id{id}, \"{val}\" >"; 
			}

			//guid
			if (val is Guid gVal) 
				return $"{offset}< \"{name}\", System.Guid, id0, {gVal.ToString()} >";
			
			//other primitives (IsPrimitive is rather slow)
			if (type.IsPrimitive)
				return $"{offset}< \"{name}\", {type.FullName}, id0, {val} >";


			//unity object
			if (val is UnityEngine.Object)
			{
				//are serialized as value, with no reference
				//they are wrote to a special array instead. No way to serialize their GUID - editor only

				UnityEngine.Object uObj = (UnityEngine.Object)val;

				if (unityObjs==null)
					return $"{offset}<  \"{name}\", \"{TypeName(type)}\", id0, null >";

				if (uObj == (UnityEngine.Object)null)
					return $"{offset}<  \"{name}\", \"{TypeName(type)}\", id0, null >";
				//doesnt work in deserialize thread, so here is workaround (but seems to be working in serialize):
				//if (type == typeof(UnityEngine.Object)) 
				//yet don't know a case when pure Object could be assigned, so considering it as null

				if (unityObjs.Contains(uObj, out int index))
					return $"{offset}<  \"{name}\", \"{TypeName(type)}\", id0, {index} >";

				else
				{
					unityObjs.Add(uObj);
					return $"{offset}<  \"{name}\", \"{TypeName(type)}\", id0, {unityObjs.Count-1} >"; //unityObjs.Count-1+1 actually
				}
			}


			//reflections
			if (val is MemberInfo mi)
			{
				int id = serializedObjsIds.Count + 1; 
				serializedObjsIds.Add(val, id); //checking if it was serialized in the beginning

				if (val is Type typeVal)
					return $"{offset}< \"{name}\", \"{TypeName(type)}\", id{id}, \"{TypeName(typeVal)}\" >";
			
				else 
					return $"{offset}< \"{name}\", \"{TypeName(type)}\", id{id}, \"{mi.Name}\", \"{TypeName(mi.DeclaringType)}\" >";
			}

			//array
			if (val is Array array)
			{
				Builder builder = new Builder(name, val, serializedObjsIds, offset);
				using (builder)
				{
					Type elementType = type.GetElementType();

					if (elementType.IsPrimitive)
					{
						builder.AddSpace(); //space after id
						if (elementType == typeof(float)) //R case for float
							for (int i=0; i<array.Length; i++)
								builder.Add(((float)array.GetValue(i)).ToString("R")); 

						else //other primitive types
							for (int i=0; i<array.Length; i++)
								builder.Add(array.GetValue(i).ToString()); 
					}

					else
						for (int i=0; i<array.Length; i++)
						{
							object subVal = array.GetValue(i);
							Type subType = subVal!=null ? subVal.GetType() : elementType; //need to serialize real type, field type might be object or interface 

							string subSer = SerializeRecursive(i.ToString(), subVal, serializedObjsIds, unityObjs, offset+"  ");

							builder.AddLine(subSer); 
						}
				}
				return builder.ToString();
			}


			//class/struct
			//else //if not returned on anything else
			{
				Builder builder = new Builder(name, val, serializedObjsIds, offset:offset, addId:!type.IsValueType);
				using (builder)
				{
					if (val is ISerializationCallbackReceiver serReciever)
						serReciever.OnBeforeSerialize(); 

					//FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					foreach (MemberInfo member in Fields(type,val))
					{
						object subVal = null; Type subNullType = null; 

						if (member is FieldInfo field)
							{ subVal = field.GetValue(val); subNullType = field.FieldType; } //Emit only available in editor
						else if (member is PropertyInfo prop)
							{ subVal = prop.GetValue(val); subNullType = prop.PropertyType; }

						string subSer = SerializeRecursive(member.Name, subVal, serializedObjsIds, unityObjs, offset+"  ");//, nullType:subNullType);

						builder.AddLine(subSer);
					}
				}
				return builder.ToString();
			}
		}


		private static IEnumerable<MemberInfo> Fields (Type type, object val)
		/// Iterates proper fields for an object depending on it's type
		/// For most cases it will be type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		/// TODO: cache fields by type
		{
			Type baseType = type.BaseType;
			if (baseType != typeof(object)  &&  baseType != typeof(ValueType))
				foreach (FieldInfo field3 in Fields(baseType, val))
					yield return field3;

			BindingFlags bind = 
				BindingFlags.DeclaredOnly |  //only top-level fields, other ones were listed before
				BindingFlags.Public | 
				BindingFlags.NonPublic | 
				BindingFlags.Instance;

			if (val is IList  &&  baseType == typeof(object)) //list itself, not it's inherited type
			{
				yield return type.GetField("_items", bind);
				yield return type.GetField("_size", bind); 
				yield return type.GetField("_version", bind); //the number of change made with list
			}

			else if (val is IDictionary  &&  baseType == typeof(object))
			{
				if (type.GetField("_buckets", bind) != null)
				{
					yield return type.GetField("_buckets", bind);
					yield return type.GetField("_entries", bind);
					yield return type.GetField("_count", bind);
					yield return type.GetField("_comparer", bind);
					yield return type.GetField("_version", bind);
				}
				else
				{
					yield return type.GetField("buckets", bind);
					yield return type.GetField("entries", bind);
					yield return type.GetField("count", bind);
					yield return type.GetField("comparer", bind);
					yield return type.GetField("version", bind);
				}
			}

			else if (val is AnimationCurve  &&  baseType == typeof(object))
			{
				yield return type.GetProperty("keys", bind);
				yield return type.GetProperty("preWrapMode", bind);
				yield return type.GetProperty("postWrapMode", bind);
			}

			else
			{
				FieldInfo[] fields = type.GetFields(bind);

				foreach (FieldInfo field in fields)
				{
					if (field.IsNotSerialized) continue;
					if (field.IsLiteral) continue; //leaving constant fields blank
					if (field.FieldType.IsPointer) continue; //skipping pointers - obviously they make Unity crash (at first glance pointer addresses will change after deserialization, but could be used to perform a deep copy)

					yield return field;
				}
			}
		}


		internal class Builder : IDisposable
		/// Disposable StringBuilder
		{
			StringBuilder builder;

			public Builder (string name, object val, Dictionary<object,int> serializedObjsIds, string offset="", bool addId=true)
			{
				int id = 0;
				if (addId)
				{
					id = serializedObjsIds.Count + 1; 
					serializedObjsIds.Add(val, id); //checking if it was serialized in the beginning of SerializeREcursive
				}

				if (val is ISerializationCallbackReceiver serReciever)
					serReciever.OnBeforeSerialize(); 

				builder = new StringBuilder();
				builder.Append($"{offset}< \"{name}\", \"{TypeName(val.GetType())}\", id{id}");
			}

			public void Dispose ()
			{
				builder.Append(" >");
			}

			public void AddLine (string s) 
			// Not confuse with StringBuilder.AppendLine! Here line symbol goes first, and adds comma as well
			{
				builder.AppendLine(","); //, and \n goes before line. This way it makes , after id and does not make , at the end
				builder.Append(s);
			}

			public void Add (string s) { builder.Append(","); builder.Append(s); }

			public void AddSpace () { builder.Append(" "); }

			public override string ToString () => builder.ToString();
		}


		private static string TypeName (Type type, char div=',', bool withAssembly=true)
		/// Returns properly formated type
		/// Generics non-asm qualified aw well - without culture, version, etc
		/// Using custom char div instead of comma
		/// TODO: cache type names
		{
			/*string declaringTypeName = "";
			Type declaringType = type.DeclaringType;
			while (declaringType != null)
			{
				declaringTypeName = $"{declaringType.Name}+{declaringTypeName}"; //+ after declaring type
				declaringType = declaringType.DeclaringType;
			}

			string str = $"{type.Namespace}.{declaringTypeName}{type.Name}{div} {type.Assembly.GetName().Name}";*/
			//not reliable

			string typeName = type.ToString();

			if (type.IsArray)
			{
				typeName = $"{TypeName(type.GetElementType(), div, withAssembly:false)}[]";
				
				if (withAssembly)
					typeName = $"{typeName}{div} {type.Assembly.GetName().Name}";

				return typeName;
			}
			
			if (type.IsGenericType)
			{
				/*typeName = typeName.Substring(0, typeName.IndexOf('`', 0)); //note it's not the standard apostrophe

				Type[] subTypes = type.GenericTypeArguments;
				typeName += $"`{subTypes.Length}[";*/

				typeName = typeName.Substring(0, typeName.IndexOf('[', 0)+1);
				
				Type[] subTypes = type.GenericTypeArguments;
				for (int s=0; s<subTypes.Length; s++)
				{
					typeName += $"[{TypeName(subTypes[s])}]";
					if (s!=subTypes.Length-1)
						typeName += div;
				}

				typeName += ']';
			}

			if (withAssembly)
				typeName = $"{typeName}{div} {type.Assembly.GetName().Name}";

			return typeName;
		}
	}
}