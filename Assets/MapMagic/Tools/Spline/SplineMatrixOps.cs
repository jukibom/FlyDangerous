using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Den.Tools;
using Den.Tools.Splines;
using Den.Tools.Matrices;
using Den.Tools.GUI;

namespace Den.Tools.Splines
{
public static class SplineMatrixOps
{
	public static void Stroke (SplineSys spline, MatrixWorld matrix, 
		bool white=false, float intensity=1,
		bool antialiased=false, bool padOnePixel=false)
	/// Draws a line on matrix
	/// White will fill the line with 1, when disabled it will use spline height
	/// PaddedOnePixel works similarly to AA, but fills border pixels with full value (to create main tex for the mask)
	{
		foreach (Line line in spline.lines)
			for (int s=0; s<line.segments.Length; s++)
		{
			int numSteps = (int)(line.segments[s].length / matrix.PixelSize.x * 0.1f + 1);

			Vector3 startPos = line.segments[s].start.pos;
			Vector3 prevCoord = matrix.WorldToPixelInterpolated(startPos.x, startPos.z);
			float prevHeight = white ? intensity : (startPos.y / matrix.worldSize.y);

			for (int i=0; i<numSteps; i++)
			{
				float percent = numSteps!=1 ? 1f*i/(numSteps-1) : 1;
				Vector3 pos = line.segments[s].GetPoint(percent);
				float posHeight = white ? intensity : (pos.y / matrix.worldSize.y);
				pos = matrix.WorldToPixelInterpolated(pos.x, pos.z);

				matrix.Line(
					(Vector2D)prevCoord, 
					(Vector2D)pos, 
					prevHeight,
					posHeight,
					antialised:antialiased,
					paddedOnePixel:padOnePixel,
					endInclusive:i==numSteps-1);

				prevCoord = pos;
				prevHeight = posHeight;
			}
		}
	}


	public static void Silhouette (SplineSys spline, MatrixWorld matrix)
	/// Fills all pixels within closed spline with 1, and all outer pixels with 0
	/// Pixels directly in the spline are filled with 0.5
	/// Internally strokes matrix first
	{
		Stroke (spline, matrix, white:true, intensity:0.5f, antialiased:false, padOnePixel:false);
		Silhouette(spline, matrix, matrix);
	}

	public static void Silhouette (SplineSys spline, MatrixWorld strokeMatrix, MatrixWorld dstMatrix)
	/// Fills all pixels within closed spline with 1, and all outer pixels with 0
	/// Pixels directly in the spline are filled with 0.5
	/// Requires the matrix with line stroked. StrokeMatrix and DstMatrix could be the same
	{
		if (strokeMatrix != dstMatrix)
			dstMatrix.Fill(strokeMatrix);
			//and then using dst matrix only

		CoordRect rect = dstMatrix.rect;
		Coord min = rect.Min; Coord max = rect.Max;

		for (int x=min.x; x<max.x; x++)
			for (int z=min.z; z<max.z; z++)
			{
				int pos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
				
				if (dstMatrix.arr[pos] < 0.01f) //free from stroke and fill
				{
					Vector3 pixelPos = dstMatrix.PixelToWorld(x, z);
					bool handness = spline.Handness(pixelPos) >= 0;

					dstMatrix.PaintBucket(new Coord(x,z), handness ? 0.75f : 0.25f);
				}
			}
	}

	public static void PaintBucket (this MatrixWorld matrix, Coord coord, float val, float threshold=0.0001f, int maxIterations=10)
	/// Like a paintBucket tool in photoshop
	/// Fills all zero (lower than threshold) values with val, until meets borders
	/// Doesnt guarantee filling (areas after the corner could be missed)
	/// Use threshold to change between mask -1 or 0
	{
		CoordRect rect = matrix.rect;
		Coord min = rect.Min; Coord max = rect.Max;

		MatrixOps.Stripe stripe = new MatrixOps.Stripe( Mathf.Max(rect.size.x, rect.size.z) );

		stripe.length = rect.size.x;

		matrix[coord] = -256; //starting mask

		//first vertical spread is one row-only
		MatrixOps.ReadRow(stripe, matrix, coord.x, matrix.rect.offset.z);
		PaintBucketMaskStripe(stripe, threshold);
		MatrixOps.WriteRow(stripe, matrix, coord.x, matrix.rect.offset.z);

		for (int i=0; i<maxIterations; i++) //ten tries, but hope it will end before that
		{
			bool change = false;

			//horizontally
			for (int z=min.z; z<max.z; z++)
			{
				MatrixOps.ReadLine(stripe, matrix, rect.offset.x, z);
				change = PaintBucketMaskStripe(stripe, threshold)  ||  change;
				MatrixOps.WriteLine(stripe, matrix, rect.offset.x, z);
			}

			//vertically
			for (int x=min.x; x<max.x; x++)
			{
				MatrixOps.ReadRow(stripe, matrix, x, matrix.rect.offset.z);
				change = PaintBucketMaskStripe(stripe, threshold)  ||  change;
				MatrixOps.WriteRow(stripe, matrix, x, matrix.rect.offset.z);
			}

			if (!change)
				break;

			//if (i==maxIterations-1 && !change)
			//	Debug.Log("Reached max iterations");
		}

		//filling masked values with val
		for (int i=0; i<matrix.arr.Length; i++)
			if (matrix.arr[i] < -255) matrix.arr[i] = val;
	}

	private static bool PaintBucketMaskStripe (MatrixOps.Stripe stripe, float threshold=0.0001f)
	/// Fills stripe until first unmasked value with -256
	/// Returns true if anything masked
	{
		bool changed = false;

		//to right
		bool masking = false;
		for (int i=0; i<stripe.length; i++)
		{
			if (stripe.arr[i] < -255)
			{
				masking = true;
				continue;
			}

			if (stripe.arr[i] < threshold)
			{
				if (masking) 
				{
					stripe.arr[i] = -256;
					changed = true;
				}
			}
			else
				masking = false;
		}

		//to left
		masking = false;
		for (int i=stripe.length-1; i>=0; i--)
		{
			if (stripe.arr[i] < -255)
			{
				masking = true;
				continue;
			}

			if (stripe.arr[i] < threshold)
			{
				if (masking) 
				{
					stripe.arr[i] = -256;
					changed = true;
				}
			}
			else
				masking = false;
		}

		return changed;
	}


	public static void CombineSilhouetteSpread (Matrix silhouetteMatrix, Matrix spreadMatrix, Matrix dstMatrix)
	/// Blends silhouette and spread the way it's done in Silhouette node, so that silhouette have the spreaded edges
	/// All matrices could be the same
	{
		for (int i=0; i<dstMatrix.count; i++)
		{
			float spread = spreadMatrix.arr[i];
			float silhouette = silhouetteMatrix.arr[i];
			dstMatrix.arr[i] = silhouette > 0.5f ? spread : 1-spread;
		}
	}

	#region Isoline

		[StructLayout(LayoutKind.Sequential)]
		public struct MetaLine 
		{ 
			public Coord c0;  
			public Coord c1;  
			public Coord c2;  
			public Coord c3;
			public byte count;
			public bool closed;  

			public const int length = 256;
			
			public Coord this[int n]
			{
				get 
				{ 
					switch (n)
					{
						case 0: return c0;
						case 1: return c1;
						case 2: return c2;
						case 3: return c3;
						default: throw new Exception($"PixelLine array index ({n}) out of range");
					}
				}
				set 
				{ 
					switch (n)
					{
						case 0: c0 = value; break;
						case 1: c1 = value; break;
						case 2: c2 = value; break;
						case 3: c3 = value; break;
						default: throw new Exception($"PixelLine array index ({n}) out of range");
					}
				}
			}

			public static void AddToList (List<Coord> list, MetaLine line)
			{
				if (line.count==0) return;
				if (line.count>=1) list.Add(line.c0);
				if (line.count>=2) list.Add(line.c1);
				if (line.count>=3) list.Add(line.c2);
				if (line.count==4) list.Add(line.c3);
				if (line.closed) list.Add(line.c0);
			}

			public Coord Last
			{get{
				switch (count)
				{
					case 1: return c0;
					case 2: return c1;
					case 3: return c2;
					case 4: return c3;
					default: throw new Exception($"PixelLine array index ({count-1}) out of range");
				}
			}}

			public MetaLine (Coord c0, Coord c1, Coord c2, Coord c3) { this.c0=c0; this.c1=c1; this.c2=c2; this.c3=c3; count=4; closed=false; }
			public MetaLine (int c0x, int c0z, int c1x, int c1z, int c2x, int c2z, int c3x, int c3z) { c0.x=c0x; c0.z=c0z; c1.x=c1x; c1.z=c1z; c2.x=c2x; c2.z=c2z; c3.x=c3x; c3.z=c3z; count=4; closed=false; }
			public MetaLine (int c0x, int c0z, int c1x, int c1z, int c2x, int c2z) { c0.x=c0x; c0.z=c0z; c1.x=c1x; c1.z=c1z; c2.x=c2x; c2.z=c2z; c3.x=0; c3.z=0; count=3; closed=false; }
			public MetaLine (int c0x, int c0z, int c1x, int c1z) { c0.x=c0x; c0.z=c0z; c1.x=c1x; c1.z=c1z; c2.x=0; c2.z=0; c3.x=0; c3.z=0; count=2; closed=false; }
		}


		public static List<MetaLine> MatrixToMetaLines (Matrix matrix, float threshold)
		/// Create isolines in form of short unwelded meta-lines around each pixel
		{
			Coord size = matrix.rect.size;
			List<MetaLine> lines = new List<MetaLine>();

			for (int x=0; x<size.x; x++)
				for (int z=0; z<size.z; z++)
				{
					int pos = z*size.x + x;
					if (matrix.arr[pos] < threshold) continue;

					int c = 0;
					if (z==size.z-1  ||  matrix.arr[pos+size.z]>=threshold)  c = c | 0b_0000_1000;  //z==size.z-1: out of border pixels have the same fill as this one (so they considered always filled)
					if (z==0  ||  matrix.arr[pos-size.z]>=threshold)  c = c | 0b_0000_0100;
					if (x==0  ||  matrix.arr[pos-1]>=threshold)  c = c | 0b_0000_0010;
					if (x==size.x-1  ||  matrix.arr[pos+1]>=threshold)  c = c | 0b_0000_0001;

					switch (c)
					{
						case 0b_0000_0000:  //if (!top && !bottom && !left && !right)
							lines.Add( new MetaLine(x,z,  x,z+1,  x+1,z+1,  x+1,z) { closed=true } );
							break;

						case 0b_0000_0001:  //else if (!top && !bottom && !left &&  right)
							lines.Add( new MetaLine(x+1,z,  x,z,  x,z+1,  x+1,z+1) );
							break;

						case 0b_0000_0100:  //else if (!top &&  bottom && !left && !right)
							lines.Add( new MetaLine(x,z,  x,z+1,  x+1,z+1,  x+1,z) );
							break;

						case  0b_0000_0010:  //else if (!top && !bottom &&  left && !right)
							lines.Add( new MetaLine(x,z+1,  x+1,z+1,  x+1,z,  x,z) );
							break;

						case  0b_0000_1000:  //else if ( top && !bottom && !left && !right)
							lines.Add( new MetaLine(x+1,z+1,  x+1,z,  x,z,  x,z+1) );
							break;

						case  0b_0000_0011:  //else if (!top && !bottom &&  left &&  right)
							lines.Add( new MetaLine(x,z+1,  x+1,z+1) );
							lines.Add( new MetaLine(x+1,z,  x,z) );
							break;

						case 0b_0000_1100:  //else if ( top &&  bottom && !left && !right)
							lines.Add( new MetaLine(x,z,  x,z+1) );
							lines.Add( new MetaLine(x+1,z+1,  x+1,z) );
							break;

						case 0b_0000_0101:  //else if (!top &&  bottom && !left &&  right)
							lines.Add( new MetaLine(x,z,  x,z+1,  x+1,z+1) );
							break;

						case 0b_0000_0110:  //else if (!top &&  bottom &&  left && !right)
							lines.Add( new MetaLine(x,z+1,  x+1,z+1,  x+1,z) );
							break;

						case 0b_0000_1010:  //else if ( top && !bottom &&  left && !right)
							lines.Add( new MetaLine(x+1,z+1,  x+1,z,  x,z) );
							break;

						case 0b_0000_1001:  //else if ( top && !bottom && !left &&  right)
							lines.Add( new MetaLine(x+1,z,  x,z,  x,z+1) );
							break;
					
						case 0b_0000_0111:  //else if (!top &&  bottom &&  left &&  right)
							lines.Add( new MetaLine(x,z+1,  x+1,z+1) );
							break;

						case 0b_0000_1110:  //else if ( top &&  bottom &&  left && !right)
							lines.Add( new MetaLine(x+1,z+1,  x+1,z) );
							break;

						case 0b_0000_1011:  //else if ( top && !bottom &&  left &&  right)
							lines.Add( new MetaLine(x+1,z,  x,z) );
							break;

						case 0b_0000_1101:  //else if ( top &&  bottom && !left &&  right)
							lines.Add( new MetaLine(x,z,  x,z+1) );
							break;

						case 0b_0000_1111:  //else if (top && bottom && left && right)
							break;

						default:
							throw new Exception("Impossible pixels combination found");
					}
				}

			return lines;
		}

		public static List< List<Coord> > WeldMetaLines (List<MetaLine> metaLines)
		{
			//metaLines start coord lut
			Dictionary<Coord,int> startLut = new Dictionary<Coord,int>(capacity:metaLines.Count);
			int metaLinesCount = metaLines.Count;
			for (int i=0; i<metaLinesCount; i++)
				startLut.Add(metaLines[i].c0, i);

			//meta lines with the start coordinate that is not covered with other metaline end coordinate
			//they will begin a new line
			Stack<Coord> initialCoords = new Stack<Coord>();

			HashSet<Coord> endCoords = new HashSet<Coord>();
			for (int i=0; i<metaLinesCount; i++)
				endCoords.Add(metaLines[i].Last);
			
			for (int i=0; i<metaLinesCount; i++)
				if (!endCoords.Contains(metaLines[i].c0)) initialCoords.Push(metaLines[i].c0);

			//welded lines
			List< List<Coord> > weldedLines = new List< List<Coord> >();
			List<Coord> currentLine = new List<Coord>();

			//welding
			while (startLut.Count != 0)
			{
				//finding first point to weld
				Coord start;
				if (initialCoords.Count != 0)
					start = initialCoords.Pop(); 
				else
					start = startLut.AnyKey(); //if no initial coords left - then only looped lines remain - and then starting anywhere

				for (int i=0; i<metaLinesCount+2; i++) //should not reach maximum
				{
					if (startLut.TryGetValue(start, out int num)) //if has continuation
					{
						MetaLine.AddToList(currentLine, metaLines[num]);
						startLut.Remove(start);
						start = metaLines[num].Last;
						continue;
					}

					else //finishing line
					{
						weldedLines.Add(currentLine);
						currentLine = new List<Coord>();
						break;
					} 

					throw new Exception("Welding reached maximum");
				}
			}

			return weldedLines;
		}

		/*public static List<Vector2D> RelaxLine (List<Vector2D> line, float relax)
		{
			int lineCount = line.Count;

			Vector2D next = 

			for (int n=0; n<lineCount; n++)
			{

			}
		}*/

	#endregion
}

}//namespace
