using System;
using UnityEngine;

namespace Den.Tools.Splines
{ 



	[System.Serializable]
	public struct Node
	{
		public Vector3 pos;
		public Vector3 dir;

		public enum TangentType { auto, linear, correlated, broken }
		public TangentType type;
			

		#if UNITY_EDITOR
		public bool selected;
		public bool dispSelected; //to display node as selected when selecting by frame
		public bool freezed; //node could not be selected, moved and displayed with SceneGUI. Used to hide junction nodes.
		#endif


		public static (Vector3,Vector3) AutoTangents (Vector3 prevPos, Vector3 thisPos, Vector3 nextPos)
		{
			Vector3 outDir = nextPos - thisPos;  
			float outDirLength = outDir.magnitude;
			if (outDirLength > 0.00001f) //prevPos match with pos, usually on first segment
				outDir /= outDirLength;
			else
				outDir = new Vector3();

			Vector3 inDir = prevPos - thisPos;
			float inDirLength = inDir.magnitude;
			if (inDirLength > 0.00001f)
				inDir /= inDirLength;
			else
				inDir = new Vector3();

			Vector3 newInDir = (inDir - outDir).normalized;
			Vector3 newOutDir = -newInDir; //(outDir - inDir).normalized;

			inDir = newInDir.normalized * inDirLength * 0.35f;
			outDir = newOutDir.normalized * outDirLength * 0.35f;

			return (inDir,outDir);
		}

		public static (Vector3,Vector3) LinearTangents (Vector3 prevPos, Vector3 thisPos, Vector3 nextPos)
		{
			return (
				(prevPos - thisPos) * 0.333f, 
				(nextPos - thisPos) * 0.333f  );
		}

		public static Vector3 LinearTangent (Vector3 thisPos, Vector3 nextPos) //for first and last segments
			{ return (nextPos - thisPos) * 0.333f; }


		public static Vector3 CorrelatedOutTangent (Vector3 inDir, Vector3 outDir)
			{ return -inDir.normalized * outDir.magnitude; }

		public static Vector3 CorrelatedInTangent (Vector3 inDir, Vector3 outDir)
			{ return -outDir.normalized * inDir.magnitude; }

		/*public static (Vector3,Vector3) AlignTangents (Vector3 prevPos, Vector3 thisPos, Vector3 nextPos)
		{
			switch (type)
			{
					case TangentType.auto: 
					{
						outDir = nextPos - pos;  
						float outDirLength = outDir.magnitude;
						if (outDirLength > 0.00001f) //prevPos match with pos, usually on first segment
							outDir /= outDirLength;
						else
							outDir = new Vector3();

						inDir = prevPos - pos;
						float inDirLength = inDir.magnitude;
						if (inDirLength > 0.00001f)
							inDir /= inDirLength;
						else
							inDir = new Vector3();

						Vector3 newInDir = inDir - outDir;
						Vector3 newOutDir = outDir - inDir;

						inDir = newInDir.normalized * inDirLength * 0.35f;
						outDir = newOutDir.normalized * outDirLength * 0.35f;

						break;
					}

					case TangentType.linear: 
					{
						outDir = (nextPos - pos) * 0.333f;
						inDir = (prevPos - pos) * 0.333f;
						break;
					}

					case TangentType.correlated:
					{
						float inDirLength = inDir.magnitude;
						float outDirLength = outDir.magnitude;

						if (inDirLength > 0) inDir /= inDirLength;
						if (outDirLength > 0) outDir /= outDirLength;
					
						outDir = outDir-inDir / 2;
						inDir = -outDir;

						outDir *= outDirLength; inDir *= inDirLength;

						break;
					}

					//do nothing for broken
				}
			}
			*/
	}


}
