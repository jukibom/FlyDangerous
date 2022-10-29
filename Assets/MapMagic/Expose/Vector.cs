using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Den.Tools;

namespace MapMagic.Expose
{
	public partial class Calculator
	{
		public struct Vector
		/// Uniform interface for all Vector2, Vector3, Coord, etc
		/// Calculator performs operations not on floats, ints, etc, but on these vectors instead
		{
			public float x;
			public float y;
			public float z;
			public float w;

			public UnityEngine.Object uobj;  //yep, vector has a unity object. Just to allow Calculator output it

			private Vector (float x, float y, float z, float w) { this.x=x; this.y=y; this.z=z; this.w=w; uobj=null; }


			public static explicit operator Vector(float f) => new Vector(f, f, f, f);
			public static explicit operator Vector(int i) => new Vector(i, i, i, i);
			public static explicit operator Vector(double d) => new Vector((float)d, (float)d, (float)d, (float)d);
			public static explicit operator Vector(Vector2 v) => new Vector(v.x, v.y, v.y, 0); //for Vector2 and Vector2D y and z should go to same
			public static explicit operator Vector(Vector2D v) => new Vector(v.x, v.z, v.z, 0);
			public static explicit operator Vector(Coord c) => new Vector(c.x, c.z, c.z, 0);
			public static explicit operator Vector(Vector3 v) => new Vector(v.x, v.y, v.z, 0);
			public static explicit operator Vector(Vector4 v) => new Vector(v.x, v.y, v.z, v.w);
			public static explicit operator Vector(Color c) => new Vector(c.r, c.g, c.b, c.a);
			public static explicit operator Vector(bool b) => b ? new Vector(1,1,1,1) : new Vector(0,0,0,0);
			public static explicit operator Vector(UnityEngine.Object o) 
			{ 
				Vector vec = o!=null ? new Vector(1,1,1,1) : new Vector(0,0,0,0);
				vec.uobj = o;
				return vec;
			}

			public static explicit operator float(Vector v) => v.x;
			public static explicit operator int(Vector v) => Mathf.RoundToInt(v.x);
			public static explicit operator double(Vector v) => v.x;
			public static explicit operator Vector2(Vector v) => new Vector2(v.x, v.y);
			public static explicit operator Vector2D(Vector v) => new Vector2D(v.x, v.z);
			public static explicit operator Coord(Vector v) => new Coord( Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.z) );
			public static explicit operator Vector3(Vector v) => new Vector3(v.x, v.y, v.z);
			public static explicit operator Vector4(Vector v) => new Vector4(v.x, v.y, v.z, v.w);
			public static explicit operator Color(Vector v) => new Color(v.x, v.y, v.z, v.w);
			public static explicit operator bool(Vector v) => v.x>0.00001f;
			public static explicit operator UnityEngine.Object(Vector v) 
			{ 
				if (Mathf.Abs(v.x)<0.00001f &&  Mathf.Abs(v.y)<0.00001f  &&  Mathf.Abs(v.z)<0.00001f  &&  Mathf.Abs(v.w)<0.00001f)
					return null;
				else return v.uobj;
			}


			public Vector (object obj)
			{
				switch (obj)
				{
					case float fobj: this = (Vector)fobj; break;
					case int iobj: this = (Vector)iobj; break;
					case double dobj: this = (Vector)dobj; break;
					case Vector2 v2obj: this = (Vector)v2obj; break;
					case Vector2D v2dobj: this = (Vector)v2dobj; break;
					case Coord cdobj: this = (Vector)cdobj; break;
					case Vector3 v3obj: this = (Vector)v3obj; break;
					case Vector4 v4obj: this = (Vector)v4obj; break;
					case Color cobj: this = (Vector)cobj; break;
					case bool bobj: this = (Vector)bobj; break;
					case UnityEngine.Object uobj: this = (Vector)uobj; break;
					default: this=new Vector(); break;
				}
			}

			public object Convert (Type type)
			/// Casts Vector to given type
			/// Takes a specified channel for int, float and bool
			/// dynamic requires CSharp assembly
			{
				if (type==typeof(float)) return (float)this;
				if (type==typeof(int)) return (int)this;
				if (type==typeof(double)) return (double)this;
				if (type==typeof(Vector2)) return (Vector2)this;
				if (type==typeof(Vector2D)) return (Vector2D)this;
				if (type==typeof(Coord)) return (Coord)this;
				if (type==typeof(Vector3)) return (Vector3)this;
				if (type==typeof(Vector4)) return (Vector4)this;
				if (type==typeof(Color)) return (Color)this;
				if (type==typeof(bool)) return (bool)this;
				if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return (UnityEngine.Object)this;
				if (typeof(Enum).IsAssignableFrom(type)) return (int)this;
				return null;
			}


			public object ConvertToChannel (object wholeVal, int channel, Type type)
			/// Sets the channel of Vector3, Vector4 to vec value, converted to float
			/// Actually converts object to vec, sets channel, then converts back
			{
				Vector wholeVec = new Vector(wholeVal);
				wholeVec[channel] = (float)this;
				return wholeVec.Convert(type);
			}


			public void Unify (int channel)
			/// Makes all 4 channels = specified channel
			/// Then can convert to float - this way can take one channel
			{
				float val = this[channel];
				x = val; y=val; z=val; w=val;
			}


			public float this[int i]
			{
				get {
					switch (i)
					{
						case 0: return x;
						case 1: return y;
						case 2: return z;
						case 3: return w;
						default: return 0;
					}
				}

				set {
					switch (i)
					{
						case 0: x = value; break;
						case 1: y = value; break;
						case 2: z = value; break;
						case 3: w = value; break;
					}
				}
			}

			public static Vector operator + (Vector c1, Vector c2) { c1.x+=c2.x; c1.y+=c2.y; c1.z+=c2.z; c1.w+=c2.w; return c1; }
			public static Vector operator - (Vector c1, Vector c2) { c1.x-=c2.x; c1.y-=c2.y; c1.z-=c2.z; c1.w-=c2.w; return c1; }
			public static Vector operator * (Vector c1, Vector c2) { c1.x*=c2.x; c1.y*=c2.y; c1.z*=c2.z; c1.w*=c2.w; return c1; }
			public static Vector operator / (Vector c1, Vector c2) 
			{ 
				if (c2.x!=0) c1.x/=c2.x; 
				if (c2.y!=0) c1.y/=c2.y; 
				if (c2.z!=0) c1.z/=c2.z; 
				if (c2.w!=0) c1.w/=c2.w; 
				return c1; 
			}
			public static Vector operator ^ (Vector c1, Vector c2)
				{ c1.x=(float)Math.Pow(c1.x,c2.x); c1.y=(float)Math.Pow(c1.y,c2.y); c1.z=(float)Math.Pow(c1.z,c2.z); c1.w=(float)Math.Pow(c1.w,c2.w); return c1; }
				//not XOR, but exponent
	
		}
	}
}