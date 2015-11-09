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

namespace Isosurface.DualMarchingSquaresNeilson
{
	public struct Cell
	{
		public int VertexCount { get; set; }
		public int[] Vertices { get; set; }
		public Vector2[] VertexPositions { get; set; }
	}

	public class DMSNeilson : ISurfaceAlgorithm
	{
		public override string Name { get { return "Dual Marching Squares (Neilson)"; } }

		Random rnd = new Random();
		Cell[,] cells;

		public DMSNeilson(GraphicsDevice device, int resolution, int Size)
			: base(device, resolution, Size, false)
		{
			InitData();
		}

		private void InitData()
		{
			cells = new Cell[Resolution, Resolution];
			for (int x = 0; x < Resolution; x++)
			{
				for (int y = 0; y < Resolution; y++)
				{
					cells[x, y] = new Cell();
					cells[x, y].Vertices = new int[2];
					cells[x, y].VertexPositions = new Vector2[2];
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
			for (int x = 0; x < Resolution - 1; x++)
			{
				for (int y = 0; y < Resolution - 1; y++)
				{
					GenerateAt(x, y);
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
				if (Sampler.Sample(new Vector2(x + i % 2, y + i / 2)) < 0)
					corners |= 1 << i;
			}

			if (corners == 0 || corners == 15)
				return;


			int v_count = MarchingSquaresTableGenerator.CaseTable[corners].Count;
			for (int i = 0; i < v_count; i++)
			{
				int e1 = MarchingSquaresTableGenerator.CaseTable[corners].Edges[i].First;
				int e2 = MarchingSquaresTableGenerator.CaseTable[corners].Edges[i].Second;
				Vector2 e1p = new Vector2((x + e1 / 2) * Size, (y + e1 % 2) * Size);
				Vector2 e2p = new Vector2((x + e2 / 2) * Size, (y + e2 % 2) * Size);

				Vector2 v1p = Sampler.GetIntersection(e1p, e2p, Sampler.Sample(e1p), Sampler.Sample(e2p));
			}

			cells[x, y].VertexCount = v_count;

			GenerateGrid(x, y);
		}

		private void GenerateGrid(int x, int y)
		{
			VertexPositionColor[] vs = new VertexPositionColor[24];
			Color c = Color.LightSteelBlue;
			vs[0] = new VertexPositionColor(new Vector3((x + 0) * Size, (y + 0) * Size, 0), c);
			vs[1] = new VertexPositionColor(new Vector3((x + 1) * Size, (y + 0) * Size, 0), c);
			vs[2] = new VertexPositionColor(new Vector3((x + 1) * Size, (y + 0) * Size, 0), c);
			vs[3] = new VertexPositionColor(new Vector3((x + 1) * Size, (y + 1) * Size, 0), c);
			vs[4] = new VertexPositionColor(new Vector3((x + 1) * Size, (y + 1) * Size, 0), c);
			vs[5] = new VertexPositionColor(new Vector3((x + 0) * Size, (y + 1) * Size, 0), c);
			vs[6] = new VertexPositionColor(new Vector3((x + 0) * Size, (y + 1) * Size, 0), c);
			vs[7] = new VertexPositionColor(new Vector3((x + 0) * Size, (y + 0) * Size, 0), c);

			for (int i = 0; i < cells[x, y].VertexCount; i++)
			{
				float vx = cells[x, y].VertexPositions[i].X;
				float vy = cells[x, y].VertexPositions[i].Y;
				float r = 2;
				vs[8 + i * 8] = new VertexPositionColor(new Vector3(vx - r, vy - r, 0), Color.Red);
				vs[9 + i * 8] = new VertexPositionColor(new Vector3(vx + r, vy - r, 0), Color.Red);
				vs[10 + i * 8] = new VertexPositionColor(new Vector3(vx + r, vy - r, 0), Color.Red);
				vs[11 + i * 8] = new VertexPositionColor(new Vector3(vx + r, vy + r, 0), Color.Red);
				vs[12 + i * 8] = new VertexPositionColor(new Vector3(vx + r, vy + r, 0), Color.Red);
				vs[13 + i * 8] = new VertexPositionColor(new Vector3(vx - r, vy + r, 0), Color.Red);
				vs[14 + i * 8] = new VertexPositionColor(new Vector3(vx - r, vy + r, 0), Color.Red);
				vs[15 + i * 8] = new VertexPositionColor(new Vector3(vx - r, vy - r, 0), Color.Red);
			}
			OutlineBuffer.SetData<VertexPositionColor>(OutlineLocation * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 8 + cells[x, y].VertexCount * 8, VertexPositionColor.VertexDeclaration.VertexStride);
			OutlineLocation += 8 + cells[x, y].VertexCount * 8;
		}

		public void GenerateIndexAt(int x, int y)
		{

		}
	}
}