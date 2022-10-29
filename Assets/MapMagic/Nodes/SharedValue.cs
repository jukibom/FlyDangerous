using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MapMagic.Nodes
{
	[Serializable]
	public class SharedValuesHolder : ISerializationCallbackReceiver
	{
		[NonSerialized] private Dictionary<(Type type, string name), object> sharedValues = new Dictionary<(Type,string), object> ();


		public SharedValuesHolder () { }
		public SharedValuesHolder (SharedValuesHolder src)
			{ sharedValues = new Dictionary<(Type,string),object>(sharedValues); }


		public object GetValue (Type holderType, string name)
		{
			if (sharedValues.TryGetValue((holderType,name), out object val))
				return val;
			else 
				//throw new Exception("Shared value " + name + " has not been added to holder");
				return null;
		}


		public T GetValue<T> (Type holderType, string name)
		{
			if (sharedValues.TryGetValue((holderType,name), out object val))
				return (T)val;
			else 
				throw new Exception("Shared value " + name + " has not been added to holder"); 
		}


		public void SetValue (Type type, string name, object val)
		{
			(Type type, string name) typeName = (type, name);
			if (sharedValues.ContainsKey(typeName))
				sharedValues[typeName] = val;
			else
				sharedValues.Add(typeName, val);
		}


		public void SetDefaults (object obj)
		{
			Type type = obj.GetType();
			foreach (SharedValueDefaultAttribute defValAtt in type.GetCustomAttributes<SharedValueDefaultAttribute>())
			{
				(Type type, string name) typeName = (type, defValAtt.name);

				//adding if not yet added
				if (!sharedValues.ContainsKey(typeName))
					sharedValues.Add(typeName, defValAtt.val);

				//re-writing if val type changed
				else if (defValAtt.val.GetType() != sharedValues[typeName].GetType())
					sharedValues[typeName] = defValAtt.val;  
			}
		}


		public (Type,string)[] GetTypeNames ()
		{
			(Type, string)[] typeNames = new (Type,string)[sharedValues.Count];
			sharedValues.Keys.CopyTo(typeNames, 0);
			return typeNames;
		}


		Type[] serializedTypes = new Type[0];
		string[] serializedNames = new string[0];
		object[] serializedVals = new object[0];

		public void OnBeforeSerialize ()
		{
			if (serializedTypes.Length != sharedValues.Count) 
			{
				serializedTypes = new Type[sharedValues.Count];
				serializedNames = new string[sharedValues.Count];
				serializedVals = new object[sharedValues.Count];
			}

			int counter = 0;
			foreach (var kvp in sharedValues)
			{
				var typeName = kvp.Key;
				serializedTypes[counter] = typeName.type;
				serializedNames[counter] = typeName.name;
				serializedVals[counter] = kvp.Value;
				counter++;
			}
		}

		public void OnAfterDeserialize ()
		{
			sharedValues.Clear();
			for (int i=0; i<serializedTypes.Length; i++)
				sharedValues.Add((serializedTypes[i], serializedNames[i]), serializedVals[i]);
		}
	}


	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class SharedValueDefaultAttribute : Attribute
	{
		public string name;
		public object val;

		public SharedValueDefaultAttribute(string name, object val) { this.name = name; this.val = val; }
	}

}
