using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Profiling;

namespace Den.Tools.GUI
{
	public struct Padding
	{
		public float left;
		public float right;
		public float top;
		public float bottom;


		public Padding (float left, float right, float top, float bottom)
		{
			this.left = left;
			this.right = right;
			this.top = top;
			this.bottom = bottom;
		}

		public Padding (float hor, float vert)
		{
			this.left = hor;
			this.right = hor;
			this.top = vert;
			this.bottom = vert;
		}

		public Padding (float offset)
		{
			this.left = offset;
			this.right = offset;
			this.top = offset;
			this.bottom = offset;
		}
	}
}