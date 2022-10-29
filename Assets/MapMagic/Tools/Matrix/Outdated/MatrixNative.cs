using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Den.Tools
{
	public class MatrixNative
	{
		[DllImport ("NativePlugins", EntryPoint = "?Test@Matrix@@QEAAHXZ")]
		public static extern int TestCall ();
	}

}
