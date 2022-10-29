using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine.Profiling;

using Den.Tools;
using MapMagic.Products;

namespace MapMagic.Nodes
{

	[System.Serializable]
	public class Group
	{ 
		public string name = "Group";
		public string comment = "Drag in generators to group them";
		public Color color = new Color(0.625f, 0.625f, 0.625f, 1);

		public Vector2 guiPos;
		public Vector2 guiSize = new Vector2(100,100);
	}
}
