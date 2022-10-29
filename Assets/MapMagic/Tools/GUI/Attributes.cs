using System;
using System.Reflection;
using System.Collections.Generic;
//using UnityEngine.Profiling;

namespace Den.Tools.GUI
{
	public sealed class ValAttribute : Attribute
	{
		public string name;
		public string cat;
		public bool display = true; //to expose but do not show automatically
		public bool isLeft; //for toggles
		public int priority = 0;
		public Type type;
		public bool allowSceneObject;
		public float min = float.MinValue;
		public float max = float.MaxValue;

		public FieldInfo field;
		public PropertyInfo prop;
		public MethodInfo method;

		public ValAttribute () { }
		public ValAttribute (string name) { this.name=name; }
		public ValAttribute (string name, string cat) { this.name=name; this.cat=cat; }
		public ValAttribute (string name, float min, float max) { this.name=name; this.min=min; this.max=max; }
		public ValAttribute (string name, float min) { this.name=name; this.min=min; }
		public ValAttribute (string name, string cat, float min, float max) { this.name=name; this.cat=cat; this.min=min; this.max=max; }


		[System.NonSerialized] private static readonly Dictionary<Type, ValAttribute[]> attributesCaches = new Dictionary<Type, ValAttribute[]>();

		public static ValAttribute[] GetAttributes (Type type)
		{
			if (attributesCaches.TryGetValue(type, out ValAttribute[] attributes)) return attributes;

			List<ValAttribute> attList = new List<ValAttribute>();

			FieldInfo[] fields = type.GetFields();
			for (int f=0; f<fields.Length; f++)
			{
				ValAttribute valAtt = Attribute.GetCustomAttribute(fields[f], typeof(ValAttribute)) as ValAttribute;
				if (valAtt == null) continue;
					
				valAtt.field = fields[f];
				valAtt.type = fields[f].FieldType;

				attList.Add(valAtt);
			}

			PropertyInfo[] props = type.GetProperties();
			for (int p=0; p<props.Length; p++)
			{
				ValAttribute valAtt = Attribute.GetCustomAttribute(props[p], typeof(ValAttribute)) as ValAttribute;
				if (valAtt == null) continue;
					
				valAtt.prop = props[p];
				valAtt.type = props[p].PropertyType;

				attList.Add(valAtt);
			}

			MethodInfo[] methods = type.GetMethods();
			for (int m=0; m<methods.Length; m++)
			{
				ValAttribute valAtt = Attribute.GetCustomAttribute(methods[m], typeof(ValAttribute)) as ValAttribute;
				if (valAtt == null) continue;
					
				valAtt.method = methods[m];

				attList.Add(valAtt);
			}

			attributes = attList.ToArray();

			//if (attributes != null)
			//	Array.Sort(attributes, (x,y) => 0);//y.priority.CompareTo(x.priority));

			attributesCaches.Add(type, attributes);
			return attributes;
		}


	}

	public sealed class EditorClassAttribute : Attribute
	{
		public static Dictionary<Type,Type> editors = new Dictionary<Type, Type>();

		public EditorClassAttribute (Type type) 
		{
			UnityEngine.Debug.Log(type);
		}
	}
}

/*
namespace Plugins.GUI
{
	public class Val : Attribute
	{
		public string name;
		public string cat;
		public Type type;
		public bool isLeft; //for toggles
		public int priority;
		public bool allowSceneObject;

		public FieldInfo field;
		public PropertyInfo prop;
		public MethodInfo method;

		public Val () { }
		public Val (string name) { this.name=name; }
		public Val (string name, string cat) { this.name=name; this.cat=cat; }


		[System.NonSerialized] private static readonly Dictionary<Type, Val[]> attributesCaches = new Dictionary<Type, Val[]>();

		public static Val[] GetAttributes (Type type)
		{
			if (attributesCaches.TryGetValue(type, out Val[] attributes)) return attributes;

			List<Val> attList = new List<Val>();

			FieldInfo[] fields = type.GetFields();
			for (int f=0; f<fields.Length; f++)
			{
				Val valAtt = Attribute.GetCustomAttribute(fields[f], typeof(Val)) as Val;
				if (valAtt == null) continue;
					
				valAtt.field = fields[f];
				valAtt.type = fields[f].FieldType;

				attList.Add(valAtt);
			}

			PropertyInfo[] props = type.GetProperties();
			for (int p=0; p<props.Length; p++)
			{
				Val valAtt = Attribute.GetCustomAttribute(props[p], typeof(Val)) as Val;
				if (valAtt == null) continue;
					
				valAtt.prop = props[p];
				valAtt.type = props[p].PropertyType;

				attList.Add(valAtt);
			}

			attributes = attList.ToArray();

			//if (attributes != null)
			//	Array.Sort(attributes, (x,y) => 0);//y.priority.CompareTo(x.priority));

			attributesCaches.Add(type, attributes);
			return attributes;
		}


	}
}
*/