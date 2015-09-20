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

namespace Isosurface.UniformDualContouring
{
	public class DC3D : ISurfaceAlgorithm
	{
		public override string Name { get { return "Uniform Dual Contouring"; } }

		float[, ,] map;
		int[, ,] vertex_indexes;
		Random rnd = new Random();

		int[,] edges =
		{
			{0,4},{1,5},{2,6},{3,7},	// x-axis 
			{0,2},{1,3},{4,6},{5,7},	// y-axis
			{0,1},{2,3},{4,5},{6,7}		// z-axis
		};

		int[,] dirs = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

		public DC3D(GraphicsDevice device, int Resolution, int size) : base(device, Resolution, size, true)
		{
			map = new float[Resolution, Resolution, Resolution];

			InitData();
		}

		private void InitData()
		{
			vertex_indexes = new int[Resolution, Resolution, Resolution];
			for (int x = 0; x < Resolution; x++)
			{
				for (int y = 0; y < Resolution; y++)
				{
					for (int z = 0; z < Resolution; z++)
					{
						//map[x, y] = Circle(new Vector3(x, y));
						map[x, y, z] = Sampler.Sample(new Vector3(x, y, z));
						//map[x, y] = Math.Max(Cuboid(new Vector3(x - 1, y - 1)), Cuboid(new Vector3(x + 12, y + 12)));
						//map[x, y] = Noise(new Vector3(x, y));
					}
				}
			}
		}

		public override long Contour(float threshold)
		{
			Stopwatch watch = new Stopwatch();

			VertexCount = 1;
			IndexCount = 0;
			OutlineLocation = 0;

			watch.Start();
			for (int x = 1; x < Resolution - 1; x++)
			{
				for (int y = 1; y < Resolution - 1; y++)
				{
					for (int z = 1; z < Resolution - 1; z++)
					{
						GenerateAt(x, y, z);
					}
				}
			}

			for (int x = 1; x < Resolution - 1; x++)
			{
				for (int y = 1; y < Resolution - 1; y++)
				{
					for (int z = 1; z < Resolution - 1; z++)
					{
						GenerateIndexAt(x, y, z);
					}
				}
			}

			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public void GenerateAt(int x, int y, int z)
		{
			int corners = 0;
			for (int i = 0; i < 8; i++)
			{
				if (map[x + i / 4, y + i % 4 / 2, z + i % 2] < 0)
					corners |= 1 << i;
			}

			if (corners == 0 || corners == 255)
				return;

			VertexPositionColor[] vs = new VertexPositionColor[24];
			Color c = Color.LightSteelBlue;
			vs[0] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 0)), c);
			vs[1] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 0)), c);
			vs[2] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 0)), c);
			vs[3] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 0)), c);
			vs[4] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 0)), c);
			vs[5] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 0)), c);
			vs[6] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 0)), c);
			vs[7] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 0)), c);

			vs[8] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 1)), c);
			vs[9] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 1)), c);
			vs[10] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 1)), c);
			vs[11] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 1)), c);
			vs[12] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 1)), c);
			vs[13] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 1)), c);
			vs[14] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 1)), c);
			vs[15] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 1)), c);

			vs[16] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 0)), c);
			vs[17] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 1)), c);
			vs[18] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 0)), c);
			vs[19] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 1)), c);

			vs[20] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 0)), c);
			vs[21] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 1)), c);
			vs[22] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 0)), c);
			vs[23] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 1)), c);

			OutlineBuffer.SetData<VertexPositionColor>(OutlineLocation * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 24, VertexPositionColor.VertexDeclaration.VertexStride);
			OutlineLocation += 24;

			QEF3D qef = new QEF3D();
			Vector3 average_normal = new Vector3();
			for (int i = 0; i < 12; i++)
			{
				int c1 = edges[i, 0];
				int c2 = edges[i, 1];

				int m1 = (corners >> c1) & 1;
				int m2 = (corners >> c2) & 1;
				if (m1 == m2)
					continue;

				float d1 = map[x + c1 / 4, y + c1 % 4 / 2, z + c1 % 2];
				float d2 = map[x + c2 / 4, y + c2 % 4 / 2, z + c2 % 2];

				Vector3 p1 = new Vector3((float)((c1 / 4)), (float)((c1 % 4 / 2)), (float)((c1 % 2)));
				Vector3 p2 = new Vector3((float)((c2 / 4)), (float)((c2 % 4 / 2)), (float)((c2 % 2)));

				Vector3 intersection = Sampler.GetIntersection(p1, p2, d1, d2);
				Vector3 normal = Sampler.GetNormal(intersection + new Vector3(x, y, z));//GetNormal(x, y);
				average_normal += normal;

				qef.Add(intersection, normal);
			}

			Vector3 p = qef.Solve2(0, 16, 0);

			Vector3 n = average_normal / (float)qef.Intersections.Count;
			VertexPositionColorNormal[] v2 = new VertexPositionColorNormal[1];
			Color color = new Color(n * 0.5f + Vector3.One * 0.5f);
			v2[0] = new VertexPositionColorNormal(new Vector3((p.X + x), (p.Y + y), (p.Z + z)), color, n);
			VertexBuffer.SetData<VertexPositionColorNormal>(VertexCount * VertexPositionColorNormal.VertexDeclaration.VertexStride, v2, 0, 1, VertexPositionColorNormal.VertexDeclaration.VertexStride);
			vertex_indexes[x, y, z] = VertexCount;
			VertexCount++;
			/*vs[0] = new VertexPositionColor(new Vector3((x + 0) , (y + 0) , 0), Color.Black);
			vs[1] = new VertexPositionColor(new Vector3((x + 1) , (y + 0) , 0), Color.Black);
			vs[2] = new VertexPositionColor(new Vector3((x + 1) , (y + 0) , 0), Color.Black);
			vs[3] = new VertexPositionColor(new Vector3((x + 1) , (y + 1) , 0), Color.Black);
			vs[4] = new VertexPositionColor(new Vector3((x + 1) , (y + 1) , 0), Color.Black);
			vs[5] = new VertexPositionColor(new Vector3((x + 0) , (y + 1) , 0), Color.Black);
			vs[6] = new VertexPositionColor(new Vector3((x + 0) , (y + 1) , 0), Color.Black);
			vs[7] = new VertexPositionColor(new Vector3((x + 0) , (y + 0) , 0), Color.Black);

			vs[8] = new VertexPositionColor(new Vector3((p.X + x) , (p.Y + y) , 0), Color.Black);
			vs[9] = new VertexPositionColor(new Vector3((p.X + x + .1f) , (p.Y + y + .1f) , 0), Color.Black);
			index = 10;

			VertexBuffer.SetData<VertexPositionColor>(VertexCount * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, index, VertexPositionColor.VertexDeclaration.VertexStride);
			VertexCount += index;*/
		}

		public void GenerateIndexAt(int x, int y, int z)
		{
			//int corners = 0;

			int v1 = vertex_indexes[x, y, z];
			if (v1 == 0)
				return;

			int[] indices = new int[256];

			int index = 0;

			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < i; j++)
				{
					int v2 = vertex_indexes[x + dirs[i, 0], y + dirs[i, 1], z + dirs[i, 2]];
					int v3 = vertex_indexes[x + dirs[j, 0], y + dirs[j, 1], z + dirs[j, 2]];
					int v4 = vertex_indexes[x + dirs[i, 0] + dirs[j, 0], y + dirs[i, 1] + dirs[j, 1], z + dirs[i, 2] + dirs[j, 2]];
					if (v2 == 0 || v3 == 0 || v4 == 0)
						continue;

					indices[index++] = v1;
					indices[index++] = v2;
					indices[index++] = v3;

					indices[index++] = v4;
					indices[index++] = v3;
					indices[index++] = v2;
				}
			}

			/*for (int i = 0; i < 12; i++)
			{
				for (int k = 0; k < 2; k++)
				{
					int c1 = edgeProcEdgeMask[i / 4, k, 0];
					int c2 = edgeProcEdgeMask[i / 4, k, 1];
					int c3 = edgeProcEdgeMask[i / 4, k, 2];
					int c4 = edgeProcEdgeMask[i / 4, k, 3];


					int v1 = vertex_indexes[x + c1 / 4, y + c1 % 4 / 2, z + c1 % 2];
					int v2 = vertex_indexes[x + c2 / 4, y + c2 % 4 / 2, z + c2 % 2];
					int v3 = vertex_indexes[x + c3 / 4, y + c3 % 4 / 2, z + c3 % 2];
					int v4 = vertex_indexes[x + c4 / 4, y + c4 % 4 / 2, z + c4 % 2];
					if (v1 == 0 || v2 == 0 || v3 == 0 || v4 == 0)
						continue;

					indices[index++] = v1;
					indices[index++] = v2;
					indices[index++] = v3;

					indices[index++] = v3;
					indices[index++] = v4;
					indices[index++] = v1;
				}
			}*/

			if (index > 0)
				IndexBuffer.SetData<int>(IndexCount * 4, indices, 0, index);
			IndexCount += index;
		}
	}
}
