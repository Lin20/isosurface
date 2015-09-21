using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;

namespace Isosurface.DualMarchingSquares
{
	public class Cell
	{
		public float[] Values { get; set; }
		public Vector2[] Positions { get; set; }
		public Vector2[] Normals { get; set; }

		private static int[] Bitmasks = { 1, 8, 2, 4 };

		public Cell()
		{
			Values = new float[4];
			Positions = new Vector2[4];
			Normals = new Vector2[4];
		}

		public void Polygonize(List<VertexPositionColorNormal> vertices, int size)
		{
			int bitmask = 0;
			for (int i = 0; i < 4; i++)
			{
				if (Sampler.Sample(Positions[i]) < 0)
					bitmask |= Bitmasks[i];
			}
			if (bitmask == 0 || bitmask == 15)
				return;

			List<Vector2> vs = new List<Vector2>();

			switch (bitmask)
			{
				case 0x0:
					break;

				case 0x1:
					vs.Add(GetPosition(0, 1));
					vs.Add(GetPosition(2, 0));
					break;

				case 0x2:
					vs.Add(GetPosition(3, 2));
					vs.Add(GetPosition(2, 0));
					break;

				case 0x3:
					vs.Add(GetPosition(3, 2));
					vs.Add(GetPosition(0, 1));
					break;

					//stop cc
				case 0x4:
					vs.Add(GetPosition(3, 2));
					vs.Add(GetPosition(1, 3));
					break;

				case 0x5:
					vs.Add(GetPosition(0, 2));
					vs.Add(GetPosition(3, 2));
					
					vs.Add(GetPosition(0, 1));
					vs.Add(GetPosition(1, 3));
					break;

				case 0x6:
					vs.Add(GetPosition(0, 2));
					vs.Add(GetPosition(1, 3));
					break;

				case 0x7:
					vs.Add(GetPosition(0, 1));
					vs.Add(GetPosition(1, 3));
					break;

				case 0x8:
					vs.Add(GetPosition(0, 1));
					vs.Add(GetPosition(1, 3));
					break;

				case 0x9:
					vs.Add(GetPosition(0, 2));
					vs.Add(GetPosition(1, 3));
					break;

				case 0xA:
					vs.Add(GetPosition(0, 2));
					vs.Add(GetPosition(0, 1));
					
					vs.Add(GetPosition(3, 2));
					vs.Add(GetPosition(1, 3));
					break;

				case 0xB:
					vs.Add(GetPosition(3, 2));
					vs.Add(GetPosition(1, 3));
					break;

				case 0xC:
					vs.Add(GetPosition(0, 1));
					vs.Add(GetPosition(3, 2));
					break;

				case 0xD:
					vs.Add(GetPosition(0, 2));
					vs.Add(GetPosition(3, 2));
					break;

				case 0xE:
					vs.Add(GetPosition(0, 1));
					vs.Add(GetPosition(2, 0));
					break;

				case 0xF:
					break;
			}

			for (int i = 0; i < vs.Count; i += 2)
			{
				VertexPositionColorNormal v0 = new VertexPositionColorNormal(new Vector3(vs[i], 0) * size, Color.Blue, Vector3.One);
				VertexPositionColorNormal v1 = new VertexPositionColorNormal(new Vector3(vs[i + 1], 0) * size, Color.Blue, Vector3.One);
				vertices.Add(v0);
				vertices.Add(v1);
			}
		}

		private Vector2 GetPosition(int a, int b)
		{
			return Sampler.GetIntersection(Positions[a], Positions[b], Sampler.Sample(Positions[a]), Sampler.Sample(Positions[b]), 0);
		}
	}
}
