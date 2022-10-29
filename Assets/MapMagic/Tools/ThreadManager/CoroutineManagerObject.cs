using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode] //to call onEnable, then it will subscribe to editor update
public class CoroutineManagerObject : MonoBehaviour
/// More of a snippet on how to update coroutines
{
	public int timePerFrame = 30;

	public void OnEnable ()
	{
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.update += Update;	
		#endif
	}

	public void Update () 
	{ 
		Den.Tools.Tasks.CoroutineManager.timePerFrame = timePerFrame;
		Den.Tools.Tasks.CoroutineManager.Update();
	}



}
