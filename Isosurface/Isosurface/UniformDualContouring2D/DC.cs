/* Uniform 2D Dual Contouring
 * Messy, but it works
 * This was my first implementation, so it operates on a pre-defined grid and might have some messy/useless stuff
 * Like the adaptive implementation, it suffers from connectivity issues
 * TODO: Fix connectivity issues
 */
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

namespace Isosurface.UniformDualContouring2D
{
	public class DC : ISurfaceAlgorithm
	{
		public override string Name { get { return "Uniform Dual Contouring 2D"; } }
		float[,] map;
		Vector2[,] vertices;
		int[,] vertex_indexes;
		Random rnd = new Random();

		int[,] edges = new int[,] { { 0, 2 }, { 1, 3 }, { 0, 1 }, { 2, 3 } };
		Vector2[] deltas = new Vector2[] { new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };

		public DC(GraphicsDevice device, int resolution, int Size)
			: base(device, resolution, Size, false)
		{
			map = new float[resolution, resolution];
			vertices = new Vector2[resolution, resolution];
			vertex_indexes = new int[resolution, resolution];

			InitData();
		}

		private void InitData()
		{
			for (int x = 0; x < Resolution; x++)
			{
				for (int y = 0; y < Resolution; y++)
				{
					map[x, y] = Sampler.Sample(new Vector2(x, y));
					vertex_indexes[x, y] = -1;
				}
			}
		}

		public override long Contour(float threshold)
		{
			Stopwatch watch = new Stopwatch();

			VertexCount = 0;
			IndexCount = 0;
			OutlineLocation = 0;

			watch.Start();
			for (int x = 1; x < Resolution - 1; x++)
			{
				for (int y = 1; y < Resolution - 1; y++)
				{
					GenerateAt(x, y);
				}
			}

			for (int x = 1; x < Resolution - 1; x++)
			{
				for (int y = 1; y < Resolution - 1; y++)
				{
					GenerateIndexAt(x, y);
				}
			}

			watch.Stop();


			if (Vertices.Count > 0)
				VertexBuffer.SetData<VertexPositionColorNormal>(Vertices.ToArray());
			if (Indices.Count > 0)
				IndexBuffer.SetData<int>(Indices.ToArray());

			return watch.ElapsedMilliseconds;
		}

		public void GenerateAt(int x, int y)
		{
			int corners = 0;
			for (int i = 0; i < 4; i++)
			{
				if (map[x + i / 2, y + i % 2] < 0)
					corners |= 1 << i;
			}

			if (corners == 0 || corners == 15)
				return;

			VertexPositionColor[] vs = new VertexPositionColor[10];
			Color c = Color.LightSteelBlue;
			vs[0] = new VertexPositionColor(new Vector3((x + 0) * Size, (y + 0) * Size, 0), c);
			vs[1] = new VertexPositionColor(new Vector3((x + 1) * Size, (y + 0) * Size, 0), c);
			vs[2] = new VertexPositionColor(new Vector3((x + 1) * Size, (y + 0) * Size, 0), c);
			vs[3] = new VertexPositionColor(new Vector3((x + 1) * Size, (y + 1) * Size, 0), c);
			vs[4] = new VertexPositionColor(new Vector3((x + 1) * Size, (y + 1) * Size, 0), c);
			vs[5] = new VertexPositionColor(new Vector3((x + 0) * Size, (y + 1) * Size, 0), c);
			vs[6] = new VertexPositionColor(new Vector3((x + 0) * Size, (y + 1) * Size, 0), c);
			vs[7] = new VertexPositionColor(new Vector3((x + 0) * Size, (y + 0) * Size, 0), c);
			OutlineBuffer.SetData<VertexPositionColor>(OutlineLocation * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 8, VertexPositionColor.VertexDeclaration.VertexStride);
			OutlineLocation += 8;

			QEF qef = new QEF();
			Vector3 average_normal = new Vector3();
			for (int i = 0; i < 4; i++)
			{
				int c1 = edges[i, 0];
				int c2 = edges[i, 1];

				int m1 = (corners >> c1) & 1;
				int m2 = (corners >> c2) & 1;
				if (m1 == m2)
					continue;

				float d1 = map[x + c1 / 2, y + c1 % 2];
				float d2 = map[x + c2 / 2, y + c2 % 2];

				Vector2 p1 = new Vector2((float)((c1 / 2)), (float)((c1 % 2)));
				Vector2 p2 = new Vector2((float)((c2 / 2)), (float)((c2 % 2)));

				Vector2 intersection = Sampler.GetIntersection(p1, p2, d1, d2);
				Vector2 normal = Sampler.GetNormal(intersection + new Vector2(x, y));//GetNormal(x, y);
				average_normal += new Vector3(normal, 0);

				qef.Add(intersection, normal);
			}

			average_normal /= (float)qef.Intersections.Count;
			average_normal.Normalize();

			vertices[x, y] = qef.Solve2(0, 16, 0);

			Vector2 p = vertices[x, y];
			Color n_c = new Color(average_normal * 0.5f + Vector3.One * 0.5f);
			VertexPositionColorNormal v = new VertexPositionColorNormal(new Vector3((p.X + x) * Size, (p.Y + y) * Size, 0), n_c, average_normal);
			Vertices.Add(v);
			vertex_indexes[x, y] = VertexCount;
			VertexCount++;
		}

		public void GenerateIndexAt(int x, int y)
		{
			for (int i = 0; i < 4; i++)
			{
				int c1 = edges[i, 0];
				int c2 = edges[i, 1];

				int v1 = vertex_indexes[x + c1 % 2, y + c1 / 2];
				int v2 = vertex_indexes[x + c2 % 2, y + c2 / 2];
				if (v1 == -1 || v2 == -1)
					continue;

				Indices.Add(v1);
				Indices.Add(v2);
				IndexCount += 2;
			}
		}
	}
}
