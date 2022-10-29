using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Den.Tools
{
	[Serializable]
	public static class Serializer
	{
		public interface IAlternativeType 
		{
			Type AlternativeSerializationType { get; }
		}

		public interface ICustomSerialization
		{
			void PostprocessAfterSerialize (Object serObj, Dictionary<object,Object> allSerialized);
			void PreprocessBeforeDeserialize (Object serObj, Object[] allSerialized, object[] allDeserialized);
		}

		public sealed class NoCopyAttribute : Attribute { }

		[Serializable]
		[StructLayout(LayoutKind.Explicit, Size=16)]
		public struct Value
		{
			[SerializeField][FieldOffset(0)] public byte t;
			[SerializeField][FieldOffset(8)] long v; //the only field serialized. Otherwise all values will be written

			[NonSerialized][FieldOffset(8)] bool boolVal;
			[NonSerialized][FieldOffset(8)] byte byteVal;
			[NonSerialized][FieldOffset(8)] sbyte sbyteVal;
			[NonSerialized][FieldOffset(8)] char charVal;
			[NonSerialized][FieldOffset(8)] decimal decimalVal;
			[NonSerialized][FieldOffset(8)] double doubleVal;
			[NonSerialized][FieldOffset(8)] float floatVal;
			[NonSerialized][FieldOffset(8)] int intVal;
			[NonSerialized][FieldOffset(8)] uint uintVal;
			[NonSerialized][FieldOffset(8)] long longVal; 
			[NonSerialized][FieldOffset(8)] ulong ulongVal;
			[NonSerialized][FieldOffset(8)] short shortVal;
			[NonSerialized][FieldOffset(8)] ushort ushortVal;

			[NonSerialized][FieldOffset(8)] int reference; //same as intVal

			//reference t is 255


			//using value as a reference int number
			public static implicit operator Value(int i) { return new Value() { t=255, intVal=i }; }
			public static implicit operator int(Value v) 
			{  
				if (v.t != 255) 
					throw new Exception("Value t:" + v.t  + " v:" + v.v + " is used like a reference");
				return v.intVal;
			}


			//using as an universal value
			public void Set (object obj, Type type)
			{
				if (type == typeof(int))		{ intVal = (int)obj;  t = 1; }
				else if (type == typeof(float))	{ floatVal = (float)obj; t = 2; }
				else if (type == typeof(bool))	{ boolVal = (bool)obj; t = 3; }
				else if (type == typeof(byte))	{ byteVal = (byte)obj; t = 4; }
				else if (type == typeof(char))	{ charVal = (char)obj; t = 5; }
				else if (type == typeof(uint))	{ uintVal = (uint)obj; t = 6; }
				else if (type == typeof(sbyte))	{ sbyteVal = (sbyte)obj; t = 7; }
				else if (type == typeof(decimal)) { decimalVal = (decimal)obj; t = 8; }
				else if (type == typeof(double)){ doubleVal = (double)obj; t = 9; }
				else if (type == typeof(long))	{ longVal = (long)obj; t = 10; }
				else if (type == typeof(ulong))	{ ulongVal = (ulong)obj; t = 11; }
				else if (type == typeof(short))	{ shortVal = (short)obj; t = 12; }
				else if (type == typeof(ushort)){ ushortVal = (ushort)obj; t = 13; }

				else throw new Exception("Could not set value for " + type);
			}

			public object Get (Type type) // use stored type instead
			{
				if (type == typeof(bool)) return boolVal;
				else if (type == typeof(byte)) return byteVal;
				else if (type == typeof(sbyte)) return sbyteVal;
				else if (type == typeof(char)) return charVal;
				else if (type == typeof(decimal)) return decimalVal;
				else if (type == typeof(double)) return doubleVal;
				else if (type == typeof(float)) return floatVal;
				else if (type == typeof(int)) return intVal;
				else if (type == typeof(uint)) return uintVal;
				else if (type == typeof(long)) return longVal;
				else if (type == typeof(ulong)) return ulongVal;
				else if (type == typeof(short)) return shortVal;
				else if (type == typeof(ushort)) return ushortVal;
				else throw new Exception("Could not get value for " + type);
			}

			public object Get () // use stored type instead
			{
				switch (t)
				{
					case 1: return intVal;
					case 2: return floatVal;
					case 3: return boolVal;
					case 4: return byteVal;
					case 5: return charVal;
					case 6: return uintVal;
					case 7: return sbyteVal;
					case 8: return decimalVal;
					case 9: return doubleVal;
					case 10: return longVal;
					case 11: return ulongVal;
					case 12: return shortVal;
					case 13: return ushortVal;
					default: throw new Exception("Could not get value for " + t);
				}
			}

			public bool CheckType (Type type)
			{
				if (t==0) return false;

				switch (t)
				{
					case 1: return type==typeof(int);
					case 2: return type==typeof(float);
					case 3: return type==typeof(bool);
					case 4: return type==typeof(byte);
					case 5: return type==typeof(char);
					case 6: return type==typeof(uint);
					case 7: return type==typeof(sbyte);
					case 8: return type==typeof(decimal);
					case 9: return type==typeof(double);
					case 10: return type==typeof(long);
					case 11: return type==typeof(ulong);
					case 12: return type==typeof(short);
					case 13: return type==typeof(ushort);
					case 255: return !type.IsPrimitive;
					default: throw new Exception("Could not get value for " + t);
				}
			}


			public override bool Equals (object obj) 
			{ 
				if (!(obj is Value)) return false;
				return t==((Value)obj).t && v==((Value)obj).v;
			}
			public override int GetHashCode () { return base.GetHashCode(); }

		}


		[Serializable]
		public class Object
		{
			public int refId = -1;  //number of this object in <object,Object> dictionary. -1 is null
			public string type = null;
			public string altType = null;
			public string[] fields = null;
			public Value[] values = null;
			public string special = null; //to serialize strings, Types, FieldInfos
			public UnityEngine.Object uniObj = null; //to serialize Unity Objects

			public static Object Null {get{ return new Object() { refId=-1, type=null }; }}
		}



		public static Object[] Serialize (object obj, Action<object,Object> onAfterSerialize=null)
		/// Stores objects in Object[] array
		{
			Dictionary<object,Object> serializedDict = new Dictionary<object, Object>();
			SerializeObject(obj, serializedDict, onAfterSerialize:onAfterSerialize);

			Object[] serialized = null;
			CopyObjectsToArray(serializedDict, ref serialized);

			return serialized;
		}


		public static object Deserialize (Object[] serialized, Action<Object> onBeforeDeserialize=null)
		/// Reads objects from stored array
		{
			if (serialized.Length == 0) return null;
			object[] deserialized = new object[serialized.Length];
			return DeserializeObject(0, serialized, deserialized, onBeforeDeserialize:onBeforeDeserialize);
		}


		public static object DeepCopy (object obj)
		{
			Dictionary<object,Object> serializedDict = new Dictionary<object, Object>();
			SerializeObject(obj, serializedDict);

			Object[] serialized = null;
			CopyObjectsToArray(serializedDict, ref serialized);

			object[] deserialized = new object[serialized.Length];
			return DeserializeObject(0, serialized, deserialized);
		}


		public static object DeepCopyPreservingRefs (object obj, Dictionary<object,object> srcDstRefs)
		{
			//serializing
			Dictionary<object,Object> serializedDict = new Dictionary<object, Object>();
			SerializeObject(obj, serializedDict);

			//manually copy to array with dst reused
			object[] sources = new object[serializedDict.Count]; //to append src-dst dict easily
			Object[] serialized = new Object[serializedDict.Count];
			object[] reused = new object[serializedDict.Count]; //the objects from dst that should be used

			foreach (var kvp in serializedDict)
			{
				object src = kvp.Key;
				Object ser = kvp.Value;
				srcDstRefs.TryGetValue(src, out object dst);

				if (serialized[ser.refId] != null)
					throw new Exception("Objects with the same id: " + ser.refId);

				sources[ser.refId] = src;
				serialized[ser.refId] = ser;
				reused[ser.refId] = dst;
			}
			
			//deserializing
			object[] deserialized = new object[serialized.Length];
			object result = DeserializeObject(0, serialized, deserialized, reused);

			//appending src-dst dict
			srcDstRefs.Clear(); //to get rid of unused references. Added later. Not sure if this line will break all, but it's definetely needed.
			for (int i=0; i<sources.Length; i++)
			{
				if (!srcDstRefs.ContainsKey(sources[i]))
					srcDstRefs.Add(sources[i], deserialized[i]);
				
				else if (srcDstRefs[sources[i]] != deserialized[i])
					throw new Exception("Deserialized and srcDstRefs mismatch");
			}


			return result;
		}


		private static void CopyObjectsToArray (Dictionary<object,Object> serializedDict, ref Object[] serialized)
		/// Will copy Objects using their refIds (not randomly like standard dict.Values). Will check id duplicates btw
		{
			if (serialized==null  ||  serialized.Length!=serializedDict.Count) serialized = new Object[serializedDict.Count];
			else { for (int i=0; i<serialized.Length; i++) serialized[i] = null; }

			foreach (var kvp in serializedDict)
			{
				Object serObj = kvp.Value;

				if (serialized[serObj.refId] != null)
					throw new Exception("Objects with the same id: " + serObj.refId);

				serialized[serObj.refId] = serObj;
			}
		}


		private static Object SerializeObject (object obj, Dictionary<object,Object> serialized, bool skipNoCopyAttribute=false, Action<object,Object> onAfterSerialize=null)
		///If skipNotCopyAttribute will skip all fields marked with NoCopy attribute
		{
			//null
			if (obj == null)
				return Object.Null; 

			//if (obj is UnityEngine.Object && (UnityEngine.Object)obj == (UnityEngine.Object)null)
			//	return Object.Null; 
			//in some cases treats other graph as null. Better skip nulls on deserialize

			//already serialized
			if (serialized.TryGetValue(obj, out Object serObj)) return serObj;

			//type ignored
			//if (obj is ) return Object.Null;

			serObj = new Object();
			serialized.Add(obj, serObj); //adding before calling any other Serialize to prevent infinite loops
			serObj.refId = serialized.Count - 1;

			Type type = obj.GetType();
			serObj.type = type.AssemblyQualifiedName;

			//string
			if (type == typeof(string))
				serObj.special = (string)obj;

			//guid
			if (type == typeof(Guid))
				serObj.special = ((Guid)obj).ToString();

			//unity object
			//else if (type.IsSubclassOf(typeof(UnityEngine.Object))  &&  !type.IsSubclassOf(typeof(UnityEngine.ScriptableObject)))
			else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
				serObj.uniObj = (UnityEngine.Object)obj;

			//serializer
			else if (type == typeof(Object[]))
				throw new Exception("Serializer is trying to serialize serializer objects. This is causing infinite loop.");

			//reflections
			else if (type.IsSubclassOf(typeof(MemberInfo)))
			{
				if (type.IsSubclassOf(typeof(Type))) //could be MonoType
					serObj.special = ((Type)obj).AssemblyQualifiedName;

				else 
				{
					MemberInfo mi = (MemberInfo)obj;
					serObj.special = mi.Name + ", " + mi.DeclaringType.AssemblyQualifiedName;
				}
			}

			//primitives (if they were assigned to object field)
			else if (type.IsPrimitive)
			{
				serObj.values = new Value[1];
				serObj.values[0].Set(obj,type);
			}

			//animation curve
			else if (type == typeof(AnimationCurve))
			{
				AnimationCurve curve = (AnimationCurve)obj;

				serObj.values = new Value[3];
				serObj.values[0] = SerializeObject(curve.keys, serialized, onAfterSerialize:onAfterSerialize).refId;
				serObj.values[1].Set((int)curve.preWrapMode, typeof(int));
				serObj.values[2].Set((int)curve.postWrapMode, typeof(int));
			}

			//array
			else if (type.IsArray)
			{
				Array array = (Array)obj;

				serObj.fields = null;
				serObj.values = new Value[array.Length];

				Type elementType = type.GetElementType();
				bool elementTypeIsPrimitive = elementType.IsPrimitive;

				for (int i=0; i<array.Length; i++)
				{
					object val = array.GetValue(i);

					Value serVal = new Value();
					if (elementTypeIsPrimitive) serVal.Set(val, elementType);
					else serVal = SerializeObject(val, serialized, onAfterSerialize:onAfterSerialize).refId;
						
					serObj.values[i] = serVal;
				}
			}

			//another more relable Unity Object null check
			else if (type == typeof(UnityEngine.Object))
			{
				FieldInfo ptrField = type.GetField("m_CachedPtr", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				IntPtr ptr = (IntPtr)ptrField.GetValue(obj);
				if (ptr == IntPtr.Zero)
					return Object.Null;
			}

			//class/struct
			else
			{
				if (obj is ISerializationCallbackReceiver)
					((ISerializationCallbackReceiver)obj).OnBeforeSerialize(); 

				if (obj is IAlternativeType altTypeObj)
					serObj.altType = altTypeObj.AlternativeSerializationType.AssemblyQualifiedName;

				FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				//special cases when derive from list - adding base class values
				if (obj is IList || obj is IDictionary) 
					 ArrayTools.Append(ref fields, type.BaseType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

				List<FieldInfo> usedFields = new List<FieldInfo>();
				for (int f=0; f<fields.Length; f++)
				{
					FieldInfo field = fields[f];

					if (field.IsLiteral) continue; //leaving constant fields blank
					if (field.FieldType.IsPointer) continue; //skipping pointers (they make unity crash. Maybe require unsafe)
					if (field.IsNotSerialized) continue;

					if (skipNoCopyAttribute)
					{
						object[] noCopyAttributes = field.GetCustomAttributes(typeof(NoCopyAttribute), false);
						if (noCopyAttributes.Length != 0) continue;  //SOMEDAY: make NoCopy a serializer control attribute with values inside
					}

					usedFields.Add(field);
				}
				int usedFieldsCount = usedFields.Count;

				serObj.fields = new string[usedFieldsCount];
				serObj.values = new Value[usedFieldsCount];
				
				for (int f=0; f<usedFieldsCount; f++)
				{
					FieldInfo field = usedFields[f];

					serObj.fields[f] = field.Name;

					object val = field.GetValue(obj);

					Value serVal = new Value();
					if (field.FieldType.IsPrimitive) serVal.Set(val, field.FieldType);
					else serVal = SerializeObject(val, serialized, onAfterSerialize:onAfterSerialize).refId;
						
					serObj.values[f] = serVal;
				}

				if (obj is ICustomSerialization customObj)
					customObj.PostprocessAfterSerialize(serObj, serialized);
			}

			onAfterSerialize?.Invoke(obj, serObj);

			return serObj;
		}


		private static object DeserializeObject (int refId, Object[] serialized, object[] deserialized, object[] reuse = null, Action<Object> onBeforeDeserialize=null)
		/// Loading object from serialized list.
		/// Will try to get object from reuse at the same position first. Note that reuse exists before the serialization and deserialized objs have no fields loaded, but to the end deserialised and reuse members are equal.
		{
			//null
			if (refId < 0) 
				return null;

			//already deserialized
			if (deserialized[refId] != null) return deserialized[refId];

			Object serObj = serialized[refId];
			onBeforeDeserialize?.Invoke(serObj);

			if (serObj.type == null  ||  serObj.refId<0) //refid could be changed on onBeforeDeserialize
				return null;
			
			Type type = Type.GetType(serObj.type);

			//trying to load alternative type
			if (type == null  &&  serObj.altType != null)
				type = Type.GetType(serObj.altType);

			//if still no type - maybe it's from the other assembly?
			if (type == null  &&  serObj.type.Contains(","))
			{
				string shortName = serObj.type.Substring(0, serObj.type.IndexOf(','));
				type = Type.GetType(shortName);
				if (type == null)
				{
					foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
					{
						type = ass.GetType(shortName);
						if (type!=null) 
							break;
					}
				}
			}

			//string    
			if (type == typeof(string))
				return serObj.special;

			//guid
			if (type == typeof(Guid) && serObj.special.Length!=0)
				return Guid.Parse(serObj.special);

			//trying to load from core module
			#if !UNITY_2019_2_OR_NEWER
			if (type == null)
				type = Type.GetType($"{serObj.type}, UnityEngine.CoreModule");
			if (type == null)
				type = Type.GetType($"{serObj.type}, UnityEngine.TerrainModule");
			if (type == null)
				type = Type.GetType($"{serObj.altType}, UnityEngine.CoreModule");
			#endif

			//not exist anymore
			if (type == null)
			{
				#if MM_DEBUG
					Debug.LogError("Could not find type for: " + serObj.type); 
				return null;
				#else
					throw new Exception("Could not find type for: " + serObj.type);
				#endif
			}

			//unity object
			//else if (type.IsSubclassOf(typeof(UnityEngine.Object))  &&  !type.IsSubclassOf(typeof(UnityEngine.ScriptableObject)))
			else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
			{
				//if (serObj.uniObj == (UnityEngine.Object)null) return null; //to avoid casting "null" to Texture2D or something
				//doesnt work in deserialize thread, so here is workaround:
				if (serObj.uniObj.GetType() == typeof(UnityEngine.Object)) 
					return null;
				//yet don't know a case when pure Object could be assigned, so considering it as null

				return serObj.uniObj;
			}

			//serializer

			//reflections
			else if (type.IsSubclassOf(typeof(MemberInfo)))
			{
				if (type.IsSubclassOf(typeof(Type))) //could be MonoType
					return Type.GetType(serObj.special);

				else 
				{
					int d = serObj.special.IndexOf(", ");
					string mn = serObj.special.Substring(0, d);
					string tn = serObj.special.Substring(d+2);

					Type mt = Type.GetType(tn);
					return mt.GetField(mn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				}
			}

			//primitives (if they were assigned to object field)
			else if (type.IsPrimitive)
				return serObj.values[0].Get();

			//animation curve
			else if (type == typeof(AnimationCurve))
			{
				AnimationCurve curve;
				if (reuse==null || reuse[refId]==null) curve = new AnimationCurve();
				else curve = (AnimationCurve)reuse[refId];

				deserialized[refId] = curve;

				curve.keys = (Keyframe[])DeserializeObject(serObj.values[0], serialized, deserialized, reuse, onBeforeDeserialize);
				curve.preWrapMode = (WrapMode)serObj.values[1].Get();
				curve.postWrapMode = (WrapMode)serObj.values[2].Get();

				return curve;
			}

			//array
			else if (type.IsArray)
			{
				Type elementType = type.GetElementType();
				bool elementTypeIsPrimitive = elementType.IsPrimitive;

				if (serObj.values.Length>0 && !serObj.values[0].CheckType(elementType))
					throw new Exception("Value was saved as a reference, but loading as a primitive (or vice versa)");

				Array array;
				if (reuse==null || reuse[refId]==null) 
					array = (Array)Activator.CreateInstance(type, serObj.values.Length);
				else array = (Array)reuse[refId];

				deserialized[refId] = array;

				for (int i=0; i<array.Length; i++)
				{
					Value serVal = serObj.values[i];

					if (elementTypeIsPrimitive) 
						array.SetValue( serVal.Get(), i);

					else if (serVal >= 0) 
					{
						object val = DeserializeObject(serVal, serialized, deserialized, reuse, onBeforeDeserialize);
						array.SetValue(val, i);
					}
				}

				return array;
			}


			//class/struct
			else
			{
				object obj;
				if (reuse==null || reuse[refId]==null) 
					//obj = FormatterServices.GetUninitializedObject(type);  // will not set default values
					obj = Activator.CreateInstance(type); // require constructor. Key/Value collection doesnt have one. Dictionaries not serialized
				else obj = reuse[refId];

				if (obj is ICustomSerialization customObj)
					customObj.PreprocessBeforeDeserialize(serObj, serialized, deserialized);

				deserialized[refId] = obj;

				if (serObj.values == null)
					return null;
					//happens on de-serializing null Unity objects

				for (int v=0; v<serObj.values.Length; v++)
				{
					FieldInfo field = type.GetField(serObj.fields[v], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

					//special cases when derive from list - adding base class values
					if (field==null && (obj is IList || obj is IDictionary) )
						field = type.BaseType.GetField(serObj.fields[v], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					
					if (field==null) continue; //if new variable was set in code at this position

					if (field.IsNotSerialized) continue; //although it was serialized when data was saved, it could be not serialized now 
					//if (!serObj.values[v].CheckType(field.FieldType))
					//	throw new Exception("Value was saved as a reference, but loading as a primitive (or vice versa)");

					Value serVal = serObj.values[v];

					if (field.FieldType.IsPrimitive)
					{
						if (!serVal.CheckType(field.FieldType)) continue; //in case 'new' keywork of other type was used on value
						field.SetValue(obj, serVal.Get());
					}

					else if (serVal >= 0) //if -1 then leaving null/default in field  
					{
						object val = DeserializeObject(serVal, serialized, deserialized, reuse, onBeforeDeserialize);
						if (val!=null  &&  val.GetType() != field.FieldType  &&  !field.FieldType.IsAssignableFrom(val.GetType())) 
							{ Debug.LogWarning($"Serializer: Could not convert {val.GetType()} to {field.FieldType}. Using default value"); continue; }
						field.SetValue(obj, val);
					}
				}

				if (obj is ISerializationCallbackReceiver)
					((ISerializationCallbackReceiver)obj).OnAfterDeserialize(); 

				return obj;
			}
		}


		public enum ClassMatch { None, ShouldMatch, ShouldDiffer }
		public static bool CheckMatch (object src, object dst, HashSet<object> chkd, ClassMatch checkIfSameReference=ClassMatch.None)
		/// Checks if all fields are equal to test the serialization/copy results
		/// sameReference checks if classes AreSame
		{
			if (chkd==null) chkd = new HashSet<object>();
		
			if (src==null && dst==null) return true;
			if (src==null && dst!=null) throw new Exception("Src is null while dst isn't"); //return false;
			if (src!=null && dst==null) throw new Exception("Dst is null while src isn't"); //return false;
		
			Type type = src.GetType();

			if (type.IsPrimitive) { if (!src.Equals(dst)) throw new Exception("Primitives not equal"); }
			else if (type == typeof(string)) { if (src!=dst) throw new Exception("String not equal"); }
			else if (type.IsSubclassOf(typeof(Type))) { if (src!=dst) throw new Exception("Types not equal"); }
			else if (type.IsSubclassOf(typeof(FieldInfo))) { if (src!=dst) throw new Exception("Field Infos not equal"); }
			else if (type.IsSubclassOf(typeof(UnityEngine.Object))) { if (src!=dst) throw new Exception("Unity objects not equal"); }
			else if (type == typeof(AnimationCurve))
			{
				if (checkIfSameReference==ClassMatch.ShouldDiffer && src==dst)
					throw new Exception("Value references match");
			
				if (checkIfSameReference==ClassMatch.ShouldMatch && src!=dst)
					throw new Exception("Value references differ");

				AnimationCurve srcCurve = (AnimationCurve)src;
				AnimationCurve dstCurve = (AnimationCurve)dst;
				if (srcCurve.keys.Length != dstCurve.keys.Length) throw new Exception("Anim Curve length differ"); //return false;
				for (int i=0; i<srcCurve.keys.Length; i++)
					if (srcCurve.keys[i].time != dstCurve.keys[i].time  ||  srcCurve.keys[i].value != dstCurve.keys[i].value) return false;

				return true;
			}
			else if (type.IsArray || type.BaseType.IsArray)
			{
				if (checkIfSameReference==ClassMatch.ShouldDiffer && src==dst)
					throw new Exception("Value references match");
			
				if (checkIfSameReference==ClassMatch.ShouldMatch && src!=dst)
					throw new Exception("Value references differ");

				Array srcArray = (Array)src;
				Array dstArray = (Array)dst;

				if (chkd.Contains(srcArray)) return true;
				chkd.Add(srcArray);

				if (srcArray.Length != dstArray.Length) return false;

				for (int i=0; i<dstArray.Length; i++)
					if (!CheckMatch(srcArray.GetValue(i), dstArray.GetValue(i), chkd, checkIfSameReference)) return false;

				return true;
			}
			else
			{
				if (checkIfSameReference==ClassMatch.ShouldDiffer && src==dst)
					throw new Exception("Value references match");
			
				if (checkIfSameReference==ClassMatch.ShouldMatch && !src.Equals(dst))
					throw new Exception("Value references differ");
			
				if (chkd.Contains(src)) return true;
				chkd.Add(src);

				FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				for (int i=0; i<fields.Length; i++)
				{
					if (fields[i].IsLiteral) continue; //leaving constant fields blank
					if (fields[i].FieldType.IsPointer) continue; //skipping pointers (they make unity crash. Maybe require unsafe)
					if (fields[i].IsNotSerialized) continue;

					object srcVal = fields[i].GetValue(src);
					object dstVal = fields[i].GetValue(dst);

					bool match = CheckMatch(srcVal, dstVal, chkd, checkIfSameReference);
					if (!match) return false;
				}
				return true;
			}

			return true;
		}
	}

}