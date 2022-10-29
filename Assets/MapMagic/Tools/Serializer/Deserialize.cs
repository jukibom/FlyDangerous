using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Den.Tools.Serialization
{
	public partial class Serializer
	{
		public static object Deserialize (string str, IList<UnityEngine.Object> unityObjs=null)
		{
			if (str == "null")
				return null;

			object[] splitString = (object[])Tools.SplitBlock(str, ',', '<', '>');
			splitString = (object[])splitString[0]; //initially will get object[1]
			return DeserializeRecursive(splitString, new Dictionary<int,object>(), unityObjs).val;
		}


		private static (string name, object val) DeserializeRecursive (object[] splitString, Dictionary<int,object> deserializedIdsObjs, IList<UnityEngine.Object> unityObjs)
		/// Returns field name with a variable that should be assigned here
		{
			//check adding to deserializedIdsObjs if id!=0 before each return!

			string name = ((string)splitString[0]).Trim('"');


			//finding type
			Type type = null;
			string typeLine = (string)splitString[1];
			//typeLine = typeLine.Replace(';', ',');
			typeLine = typeLine.Trim('"');
			type = Type.GetType(typeLine);

			if (type == null) //not exist anymore
//				throw new Exception("Could not find type for: " + typeLine);
{ Debug.LogError("Could not find type for: " + typeLine); return (name,null); }
//TODO: should be exception

			//id
			int id = 0; //0 is no id
			if (!type.IsPrimitive)
				id = int.Parse( ((string)splitString[2]).TrimStart(new []{'i','d'}) ); 
			
			if (splitString.Length<=3  &&  deserializedIdsObjs.TryGetValue(id, out object serVal)) //already serialized and no additional data to add
				return (name, serVal);			
				//return only if serializedand no data since we can append already loaded objects with other fields (split one object in different ser values)


			//value: most common primitives
			if (type == typeof(float))
				return (name, float.Parse( (string)splitString[3], System.Globalization.CultureInfo.InvariantCulture.NumberFormat)); //this will load 0.X saved floats in Finnish locale (otherwise don'e understand . instead of ,)
			else if (type == typeof(int))
				return (name, int.Parse( (string)splitString[3] ));


			//guid
			else if (type == typeof(Guid))
			{
				Guid guid = Guid.Parse((string)splitString[3]);
				return (name, guid);
			}


			//other primitives
			else if (type.IsPrimitive)
			{
				object val = Convert.ChangeType((string)splitString[3], Type.GetTypeCode(type));
				return (name, val);
			}


			//null
			else if (splitString.Length == 4  &&  splitString[3] is string strFrstVal  &&  strFrstVal == "null")
				return (name, null);


			//string
			else if (type == typeof(string))
			{
				if (deserializedIdsObjs.TryGetValue(id, out object val))
					return (name, val);

				else
				{
					string sval = ((string)splitString[3]).Trim('"');
					deserializedIdsObjs.Add(id, sval);
					return (name, sval);
				}
			}


			//unity object
			else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
			{
				//are serialized as value, with no reference
				//they are wrote to a special array instead. No way to deserialize their GUID - editor only

				if (deserializedIdsObjs.TryGetValue(id, out object val))
					return (name, val);

				string strVal = (string)splitString[3];
				if (strVal == "null")
					return (name, null);
				else
				{
					int index = int.Parse(strVal);
					return (name,unityObjs[index]);
				}
			}


			//reflections
			else if (type.IsSubclassOf(typeof(Type))) //could be MonoType
			{
				Type tVal = Type.GetType( ((string)splitString[3]).Trim('"') );
				if (id!=0) deserializedIdsObjs.Add(id, tVal);
				return (name,tVal);
			}

			else if (type.IsSubclassOf(typeof(MemberInfo)))
			{
				Type mt = Type.GetType(((string)splitString[4]).Trim('"'));
				MemberInfo mi = mt.GetField(((string)splitString[3]).Trim('"'), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (id!=0) deserializedIdsObjs.Add(id, mi);
				return (name, mi);
			}


			//array
			else if (type.IsArray)
			{
				if (deserializedIdsObjs.TryGetValue(id, out object val))
					return (name, val);

				Array array = (Array)Activator.CreateInstance(type, splitString.Length-3);
				deserializedIdsObjs.Add(id, array); //assigning object now in case we will have reference to it (array of itself?)

				Type elementType = type.GetElementType();
				TypeCode elementTypeCode =  Type.GetTypeCode(elementType);

				for (int i=0; i<array.Length; i++)
				{
					object serElement = splitString[i+3];

					if (serElement is string stringElement)
					{
						object elVal = Convert.ChangeType(stringElement, elementTypeCode);
						array.SetValue(elVal, i);
					}

					else if (serElement is object[] arrElement)
					{
						(string elName, object elVal) = DeserializeRecursive(arrElement, deserializedIdsObjs, unityObjs);
						array.SetValue(elVal, i);
					}
				}

				//adding to deserializedIdsObjs in the beginning
				return (name, array);
			}
			

			//classes/structs
			else
			{
				//getting value - do not return since we can add some fields
				if (!deserializedIdsObjs.TryGetValue(id, out object val))
				{
					//if (val is ICustomSerialization customObj)
					//	customObj.PreprocessBeforeDeserialize(serObj, serialized, deserialized);

					val = Activator.CreateInstance(type);
					if (id != 0) deserializedIdsObjs.Add(id, val);  //assigning object now in case we will have reference to it
				}
				
				//for (int f=splitString.Length-1; f>=3; f--) //deserializing from end to start, to pass NewOverridedTests
				for (int f=3; f<splitString.Length; f++)  //deserializing from the beginning to deserialize ordered dicts (amd maybe more)
				{
					(string fieldName, object fieldVal) = DeserializeRecursive( (object[])splitString[f], deserializedIdsObjs, unityObjs );

					MemberInfo member = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					
					if (member==null) //trying to load from baser type (for classes derived from list and dict)
						member = type.BaseType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

					if (member==null) //serialized property for animation curve or other special types
						member = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

					if (member==null) //Unity 2021.2 beta dictionary field names have _ prefix, previous ones - don't
					{
						if (fieldName == "buckets" || fieldName == "entries" || fieldName == "count" || fieldName == "comparer" || fieldName == "version")
							fieldName = "_" + fieldName;

						member = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						if (member==null) member = type.BaseType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					}

					if (member==null)  //if new variable was set in code at this position
					#if !MM_DEBUG
						continue; 
					#else
						throw new Exception ("Deserialize: Field " + fieldName + " not found");
					#endif


					if (member is FieldInfo field)
					{
						if (field.IsNotSerialized) continue; //although it was serialized when data was saved, it could be not serialized now 
						field.SetValue(val, fieldVal);
					}

					else if (member is PropertyInfo prop)
						prop.SetValue(val, fieldVal);
				}

				//adding to deserializedIdsObjs in the beginning
				return (name, val);
			}
				

			throw new Exception("Could not deserialize:\n" + splitString);
		}
	}


}