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

namespace Isosurface.AdaptiveDualContouring2D
{
	public class ADC : ISurfaceAlgorithm
	{
		public override string Name { get { return "AdaptiveDualContouring2D"; } }
		float[,] map;
		int[,] vertex_indexes;
		Random rnd = new Random();

		QuadtreeNode tree;

		int[,] edges = new int[,] { { 0, 2 }, { 1, 3 }, { 0, 1 }, { 2, 3 } };
		Vector2[] deltas = new Vector2[] { new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };

		public ADC(GraphicsDevice device, int resolution, int size)
			: base(device, resolution, size, false)
		{
			map = new float[resolution, resolution];
			vertex_indexes = new int[resolution, resolution];

			tree = new QuadtreeNode();
		}

		public override long Contour(float quality)
		{
			Stopwatch watch = new Stopwatch();

			List<VertexPositionColorNormal> vertices = new List<VertexPositionColorNormal>();
			watch.Start();

			VertexCount = tree.Build(Vector2.Zero, Resolution, 0.01f, vertices, this.Size);
			VertexBuffer.SetData<VertexPositionColorNormal>(vertices.ToArray());
			ConstructTreeGrid(tree);
			CalculateIndexes();

			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public void ConstructTreeGrid(QuadtreeNode node)
		{
			if (node == null)
				return;
			VertexPositionColor[] vs = new VertexPositionColor[16];
			int x = (int)node.position.X * this.Size;
			int y = (int)node.position.Y * this.Size;
			Color c = Color.LightSteelBlue;
			Color v = Color.Red;

			float size = node.size * this.Size;
			vs[0] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, 0), c);
			vs[1] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, 0), c);
			vs[2] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, 0), c);
			vs[3] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, 0), c);
			vs[4] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, 0), c);
			vs[5] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, 0), c);
			vs[6] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, 0), c);
			vs[7] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, 0), c);

			if (node.type != QuadtreeNodeType.Leaf || node.draw_info.index == -1)
			{
				OutlineBuffer.SetData<VertexPositionColor>(OutlineLocation * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 8, VertexPositionColor.VertexDeclaration.VertexStride);
				OutlineLocation += 8;
			}
			else
			{
				x += (int)(node.draw_info.position.X * (float)this.Size);
				y += (int)(node.draw_info.position.Y * (float)this.Size);
				float r = 2;
				vs[8] = new VertexPositionColor(new Vector3(x - r, y - r, 0), v);
				vs[9] = new VertexPositionColor(new Vector3(x + r, y - r, 0), v);
				vs[10] = new VertexPositionColor(new Vector3(x + r, y - r, 0), v);
				vs[11] = new VertexPositionColor(new Vector3(x + r, y + r, 0), v);
				vs[12] = new VertexPositionColor(new Vector3(x + r, y + r, 0), v);
				vs[13] = new VertexPositionColor(new Vector3(x - r, y + r, 0), v);
				vs[14] = new VertexPositionColor(new Vector3(x - r, y + r, 0), v);
				vs[15] = new VertexPositionColor(new Vector3(x - r, y - r, 0), v);
				OutlineBuffer.SetData<VertexPositionColor>(OutlineLocation * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 16, VertexPositionColor.VertexDeclaration.VertexStride);
				OutlineLocation += 16;
			}

			if (node.type != QuadtreeNodeType.Leaf)
			{
				for (int i = 0; i < 4; i++)
				{
					ConstructTreeGrid(node.children[i]);
				}
			}
		}

		public void CalculateIndexes()
		{
			List<int> indexes = new List<int>();

			tree.ProcessFace(indexes);
			IndexCount = indexes.Count;
			if (indexes.Count == 0)
				return;

			IndexBuffer.SetData<int>(indexes.ToArray());
		}
	}
}
