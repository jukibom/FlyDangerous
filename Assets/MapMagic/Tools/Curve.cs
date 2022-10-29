using System;
using UnityEngine;
using System.Collections;

namespace Den.Tools
{
	[Serializable]
	public class AnimCurve
	{
		[Serializable]
		public struct Point
		{
			public float time;
			public float val;
			public float inTangent;
			public float outTangent;

			public Point (Keyframe key) { time=key.time; val=key.value; inTangent=key.inTangent; outTangent=key.outTangent; }
			public Point (float time, float val, float inTangent, float outTangent) 
				{ this.time=time; this.val=val; this.inTangent=inTangent; this.outTangent=outTangent; }
		}

		public Point[] points = new Point[0];

		public AnimCurve () { }
		public AnimCurve (AnimationCurve animCurve) { ReadAnimCurve(animCurve); }
		public AnimCurve (Point[] points) { this.points = points; }

		public void ReadAnimCurve (AnimationCurve animCurve)
		{
			if (points.Length != animCurve.keys.Length) 
				points = new Point[animCurve.keys.Length];

			for (int p=0; p<points.Length; p++) 
				points[p] = new Point(animCurve.keys[p]);
		}

		public void WriteToAnimCurve (AnimationCurve animCurve)
		{
			if (points.Length != animCurve.keys.Length) 
				animCurve.keys = new Keyframe[points.Length];

			for (int p=0; p<points.Length; p++) 
			{
				Point point = points[p];
				animCurve.keys[p] = new Keyframe(point.time, point.val, point.inTangent, point.outTangent);
			}
		}

		public float Evaluate (float time, int segment=-1)
		{
			if (time <= points[0].time) return points[0].val;
			if (time >= points[points.Length-1].time) return points[points.Length-1].val;

			for (int p=0; p<points.Length-1; p++)
			{
				if (time > points[p].time && time <= points[p+1].time)
				{
					Point prev = points[p];
					Point next = points[p+1];

					float delta = next.time - prev.time;
					float relativeTime = (time - prev.time) / delta;

					float timeSq = relativeTime * relativeTime;
					float timeCu = timeSq * relativeTime;
     
					float a = 2*timeCu - 3*timeSq + 1;
					float b = timeCu - 2*timeSq + relativeTime;
					float c = timeCu - timeSq;
					float d = -2*timeCu + 3*timeSq;

					return a*prev.val + b*prev.outTangent*delta + c*next.inTangent*delta + d*next.val;
				}
				else continue;
			}

			return 0;
		}
	}

	[Serializable]
	public class Curve
	{
		[Serializable]
		public class Node
		{
			public Vector2 pos;

			public float inTangent;
			public float outTangent;

			public bool linear;

			public Node () { }
			public Node (Vector2 pos) { this.pos=pos; inTangent=0; outTangent=0; linear = false; }
			public Node (Vector2 pos, float inTangent, float outTangent) 
				{ this.pos=pos; this.inTangent=inTangent; this.outTangent=outTangent; linear = false; }
			public Node (Node src) { pos=src.pos; inTangent=src.inTangent; outTangent=src.outTangent; linear=src.linear; }
		}

		public Node[] points = new Node[0];
		public float[] lut = new float[256];

		public Curve () { }
		public Curve (AnimationCurve animCurve) { ReadAnimCurve(animCurve); Refresh(); }
		public Curve (Vector2 pos1, Vector2 pos2) { points = new Node[] { new Node(pos1), new Node(pos2) }; Refresh(); }
		public Curve (Node[] points) { this.points = points; }
		public Curve (Curve src)
		{
			points = new Node[src.points.Length];
			Copy(src,this);
		}

		public static void Copy (Curve src, Curve dst)
		{
			if (dst.points.Length != src.points.Length) 
				dst.points = new Node[src.points.Length];

			for (int i=0; i<src.points.Length; i++)
				dst.points[i] = new Node(src.points[i]);
		}

		public Vector2[] GetPositions ()
		{
			Vector2[] positions = new Vector2[points.Length];

			for (int i=0; i<points.Length; i++)
				positions[i] = points[i].pos;

			return positions;
		}

		public void ReadAnimCurve (AnimationCurve animCurve)
		{
			if (points.Length != animCurve.keys.Length) 
				points = new Node[animCurve.keys.Length];

			for (int p=0; p<points.Length; p++) 
				points[p] = new Node( new Vector2(animCurve.keys[p].time,animCurve.keys[p].value), animCurve.keys[p].inTangent, animCurve.keys[p].outTangent );
		}

		public void WriteToAnimCurve (AnimationCurve animCurve)
		{
			if (points.Length != animCurve.keys.Length) 
				animCurve.keys = new Keyframe[points.Length];

			for (int p=0; p<points.Length; p++) 
			{
				Node point = points[p];
				animCurve.keys[p] = new Keyframe(point.pos.x, point.pos.y, point.inTangent, point.outTangent);
			}
		}

		public float EvaluatePrecise (float time)
		{
			if (time <= points[0].pos.x) return points[0].pos.y;
			if (time >= points[points.Length-1].pos.x) return points[points.Length-1].pos.y;

			for (int p=0; p<points.Length-1; p++)
			{
				if (time > points[p].pos.x && time <= points[p+1].pos.x)
				{
					float delta = points[p+1].pos.x - points[p].pos.x;
					float relativeTime = (time - points[p].pos.x) / delta;

					return EvaluatePrecise(points[p], points[p+1], relativeTime);
				}
			}

			return 0;
		}


		public static float EvaluatePrecise (Node prev, Node next, float x)
		{
			if (prev.linear && next.linear)
				return prev.pos.y * (1-x)  +  next.pos.y * x;

			float delta = next.pos.x - prev.pos.x;


			float pp = 3*x*x - 2*x*x*x;
			float posInterpolation = prev.pos.y * (1-pp)  +  next.pos.y * pp;

			float pt = x*(1-x) * x;
			float it = x*(1-x) * (1-x);  //x*(2*x - x*x -1);
																
			float tanInterpolation = prev.outTangent*it  +  (-next.inTangent)*pt; //unity outTangent is inverted and match inTangent
			posInterpolation += tanInterpolation*delta;


			//bias values to set the changeable length tangents
			/*float nBias = next.linear ? 0 : 1;
			float pBias = prev.linear ? 0 : 1;

			float pp = (x*x*nBias) * (1-x) +  (1-i*i*pBias) * x;
			float posInterpolation = prev.pos.y * (1-pp)  +  next.pos.y * pp;

			float pt = x*x*x - x*x;
			float it = i*i*i - i*i;
			float tanInterpolation = (-prev.outTangent)*it*pBias  +  next.inTangent*pt*nBias; //unity outTangent is inverted and match inTangent
			posInterpolation += tanInterpolation*delta;*/


			/*float pPosFactor = prev.linear ? 1  :  1 + x - 2*x*x;
			float nPosFactor = next.linear ? 1  :  3*x - 2*x*x;
			
			float pTanFactor = prev.linear ? 0  :  x*(1-x);
			float nTanFactor = next.linear ? 0  :  x*(1-x);

			float prevVal = prev.linear ?
				prev.pos.y :
				prev.pos.y * (1 + x - 2*x*x)  +  prev.outTangent*delta * x*(1-x);

			float nextVal = next.linear ?
				next.pos.y :
				next.pos.y * (3*x - 2*x*x)  +  (-next.inTangent)*delta * x*(1-x);

			return prevVal*(1-x) + nextVal*x;*/


			/*if (prev.linear)
			{
				float linInterpolation = prev.pos.y*(1-x)  +  next.pos.y*x;
				posInterpolation = linInterpolation*(1-x) + posInterpolation*x;
			}
			else if (next.linear)
			{
				float linInterpolation = prev.pos.y*(1-x)  +  next.pos.y*x;
				posInterpolation = posInterpolation*(1-x) + linInterpolation*x;
			}*/


			return posInterpolation;
		}


		public void UpdateLut ()
		/// Fills the lut array with baked curve evaluations
		{
			for (int i=0; i<lut.Length; i++)
				lut[i] = EvaluatePrecise(1f*i / (lut.Length-1));
				//TODO: can speed this up
		}


		public float EvaluateLuted (float input)
		/// Using the baked evaluations to quickly get value
		{
			float step = 1f / (lut.Length-1);

			int prevNum = (int)(input/step);
			int nextNum = prevNum+1;

			float prevX = prevNum * step;
			float percent = (input-prevX) / step;

			float prevY = lut[prevNum];
			float nextY = lut[nextNum];
			return prevY*(1-percent) + nextY*percent;
		}


		public void ResetNodeTangents (int num)
		{
			Vector2 pos = points[num].pos;
			float tan = 0;

			Vector2 prevPos = points[num-1].pos;
			Vector2 nextPos = points[num+1].pos;

			float prevTan =  (pos.y - prevPos.y) / (pos.x - prevPos.x);
			float nextTan =  (pos.y - nextPos.y) / (pos.x - nextPos.x);

			float p = (pos.x - prevPos.x) / (nextPos.x - prevPos.x);

			p = (pos-prevPos).magnitude / ((pos-nextPos).magnitude + (pos-prevPos).magnitude);

			p = 2*p*p*p - 3*p*p  +  2*p;

			tan = prevTan*(1-p) + nextTan*p;

			//photoshop-style tangents
			//float avgTan = (nextPos.y - prevPos.y) / (nextPos.x - prevPos.x);
			//tan = avgTan;

			points[num].outTangent = tan;
			points[num].inTangent = tan; //in Unity animcurve in and out tangents are not inverted
		}


		public void ResetFirstTangent ()
		{
			//should be done after all of the other tangents set up
			if (points.Length == 2) { points[0].outTangent = 1; return; }

			Vector2 pos = points[0].pos;
			Vector2 nPos = points[1].pos;

			float delta = nPos.x - pos.x;
			Vector2 nTanDir = new Vector2(1, points[1].inTangent);
			Vector2 aimPos = nPos - nTanDir*delta/2;
			float tan = (pos.y - aimPos.y) / (pos.x - aimPos.x);  //next tan only

			points[0].outTangent = tan;
		}

		public void ResetLastTangent ()
		{
			if (points.Length == 2) { points[points.Length-1].inTangent = 1; return; }

			Vector2 pos = points[points.Length-1].pos;
			Vector2 pPos = points[points.Length-2].pos;

			float delta = pPos.x - pos.x;
			Vector2 nTanDir = new Vector2(1, points[1].inTangent);
			Vector2 aimPos = pPos - nTanDir*delta/2;
			float tan = (aimPos.y-pos.y) / (aimPos.x - pos.x);  //prev tan only

			points[points.Length-1].inTangent = tan;
		}

		public void Refresh (bool updateLut = true)
		{
			points[0].linear = true;
			points[points.Length-1].linear = true;

			for (int i=1; i<points.Length-1; i++)
				ResetNodeTangents(i);

			ResetFirstTangent();
			ResetLastTangent();
			//ResetBeginEndTangents();

			if (updateLut)
				UpdateLut();
		}
	}



	public class CurveBeizer
	{
		public class Node
		{
			public Vector2 pos;
			public Vector2 inTangent;
			public Vector2 outTangent;

			public enum TangentMode { Linear, Smooth, Correlated, Manual }
			public TangentMode tangentMode = TangentMode.Smooth;

			public Node () { }
			public Node (Vector2 pos) { this.pos = pos; }
			public Node (Vector2 pos, Vector2 inTangent, Vector2 outTangent)
				{ this.pos = pos; this.inTangent = inTangent; this.outTangent = outTangent; }

			public static Vector2 GetPoint (Node start, Node end, float p)
			{
				float ip = 1f-p;
				return ip*ip*ip*start.pos + 3*p*ip*ip*(start.pos+start.outTangent) + 3*p*p*ip*(end.pos+end.inTangent) + p*p*p*end.pos;
			}
		}

		public Node[] points = new Node[0];
		public float[] lut = new float[256]; //prepared array to get Y value quickly. Note that conatins last point, so use (lut.Length-1) when getting segment

		public CurveBeizer () { }
		public CurveBeizer (Vector2 pos1, Vector2 pos2) { points = new Node[] { new Node(pos1), new Node(pos2) }; }
		public CurveBeizer (Node[] points) { this.points = points; }

		/*public float Evaluate (float time, int segment=-1)
		/// returns spline Y coordinate using input X
		{
			if (time <= points[0].pos.x) return points[0].pos.y;
			if (time >= points[points.Length-1].pos.x) return points[points.Length-1].pos.y;

			for (int i=0; i<points.Length-1; i++)
			{
				if (time > points[i].pos.x && time <= points[i+1].pos.x)
				{
					//segment nodes
					Node start = points[i]; 
					Node end = points[i+1];

					//x percent
					float delta = end.pos.x - start.pos.x; 
					float p = (time - start.pos.x) / delta;
					float ip = 1f-p;

					//evaluating
					Vector2 pos = ip*ip*ip*start.pos + 3*p*ip*ip*(start.pos+start.outTangent) + 3*p*p*ip*(end.pos+end.inTangent) + p*p*p*end.pos;
					return pos.y;
				}
				else continue;
			}

			return 0;
		}*/

		public float Evaluate (float input)
		{
			float step = 1f / (lut.Length-1);

			int prevNum = (int)(input/step);
			int nextNum = prevNum+1;

			float prevX = prevNum * step;
			float percent = (input-prevX) / step;

			float prevY = lut[prevNum];
			float nextY = lut[nextNum];
			return prevY*(1-percent) + nextY*percent;
		}


		/*public Vector2[][] TestLut (int steps=10)
		{
			//pass 1: evaluating beizer curves
			Vector2[][] perSegmentPoses = new Vector2[points.Length][];

			int stepsPerSegment = steps / points.Length + 3;
			if (stepsPerSegment > steps) stepsPerSegment = steps; //if only one segment

			for (int s=0; s<points.Length-1; s++)
			{
				Vector2[] poses = new Vector2[stepsPerSegment];
				perSegmentPoses[s] = poses;

				Node start = points[s];
				Node end = points[s+1];

				for (int i=0; i<stepsPerSegment; i++)
				{
					float p = 1f*i / (stepsPerSegment-1);
					float ip = 1f-p;
					poses[i] = ip*ip*ip*start.pos + 3*p*ip*ip*(start.pos+start.outTangent) + 3*p*p*ip*(end.pos+end.inTangent) + p*p*p*end.pos;
				}
			}

			return perSegmentPoses;
		}*/


		public float[] GenerateLut (int steps=256)
		{
			float[] lut = new float[steps];
			GenerateLut(lut);
			return lut;
		}

		public float[] GenerateLut () { GenerateLut(lut); return lut; }

		public float[] GenerateLut (float[] lut)
		{
			int steps = lut.Length;

			//pass 1: evaluating beizer curves
			Vector2[][] perSegmentPoses = new Vector2[points.Length][];

			int stepsPerSegment = steps / points.Length + 3;
			if (stepsPerSegment > steps) stepsPerSegment = steps; //if only one segment

			for (int s=0; s<points.Length-1; s++)
			{
				Vector2[] poses = new Vector2[stepsPerSegment];
				perSegmentPoses[s] = poses;

				Node start = points[s];
				Node end = points[s+1];

				for (int i=0; i<stepsPerSegment; i++)
				{
					float p = 1f*i / (stepsPerSegment-1);
					float ip = 1f-p;
					poses[i] = ip*ip*ip*start.pos + 3*p*ip*ip*(start.pos+start.outTangent) + 3*p*p*ip*(end.pos+end.inTangent) + p*p*p*end.pos;
					//IDEA: replace with float operations to speed up
				}
			}

			//pass 2: interpolating poses depending on X position
			for (int i=0; i<steps-1; i++)
			{
				float xPos = 1f*i / (steps-1);
				
				//finding proper segment
				int segNum = 0;
				for (int s=0; s<points.Length-1; s++)
				{
					if (xPos > points[s].pos.x && xPos <= points[s+1].pos.x)
						{ segNum = s; break; }
				}

				//finding proper sub-segment (pos) number
				Vector2[] poses = perSegmentPoses[segNum];
				int num = 0;
				for (int p=0; p<poses.Length-1; p++)
				{
					if (xPos > poses[p].x && xPos <= poses[p+1].x) 
						{ num = p; break; }
				}

				//interpolating pos
				float pStart = poses[num].x;
				float pEnd = poses[num+1].x;
				float pRange = pEnd - pStart;
				float percent = (xPos - pStart) / pRange;
				float val = poses[num].y*(1-percent) + poses[num+1].y*percent;

				//out-of-range
				if (xPos < points[0].pos.x) val = points[0].pos.y;
				if (xPos > points[points.Length-1].pos.x) val = points[points.Length-1].pos.y;

				lut[i] = val;
			}
			
			//setting the final lut val to the last position
			lut[lut.Length-1] = points[points.Length-1].pos.y;

			return lut;
		}

		

		public void RefreshNodeTangents (int num)
		{
			Node node = points[num];

			Vector2 prevPos = num!=0 ? points[num-1].pos : points[num].pos;
			Vector2 nextPos = num!=points.Length-1 ? points[num+1].pos : points[num].pos;
			Vector2 pos = points[num].pos;

			//start / end
			if (num == 0 || num == points.Length-1)
			{
				node.inTangent = new Vector2(0,0);
				node.outTangent = new Vector2(0,0);
			}

			//linear
			if (node.tangentMode == Node.TangentMode.Linear)
			{
				node.inTangent = (prevPos*0.333f + node.pos*0.667f) - node.pos;
				node.outTangent = (nextPos*0.333f + node.pos*0.667f) - node.pos;
			}

			//auto
			if (node.tangentMode == Node.TangentMode.Smooth)
			{
				Vector2 prevNormalized = (prevPos-pos).normalized + pos;
				Vector2 nextNormalized = (nextPos-pos).normalized + pos;

				Vector2 startDirNorm = (nextNormalized - prevNormalized).normalized;
				Vector2 endDirNorm = (prevNormalized - nextNormalized).normalized;

				node.outTangent = startDirNorm * (nextPos.x-pos.x)/2; //( (pos-nextPos).magnitude * 0.333f );
				node.inTangent = endDirNorm * (pos.x-prevPos.x)/2; //( (pos-prevPos).magnitude * 0.333f );
			}
		}

		public void Refresh ()
		{
			for (int i=0; i<points.Length; i++)
				RefreshNodeTangents(i);

			//lut
			GenerateLut();
		}

		public void ReadAnimCurve (AnimationCurve animCurve)
		{
			if (points.Length != animCurve.keys.Length) 
				points = new Node[animCurve.keys.Length];

			for (int p=0; p<points.Length; p++) 
				points[p] = new Node(new Vector2(animCurve.keys[p].time, animCurve.keys[p].value), new Vector2(-0.1f,0), new Vector2(0.1f,0));

			Refresh();
		}


	}
}