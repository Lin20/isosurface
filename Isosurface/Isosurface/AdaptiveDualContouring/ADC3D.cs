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

namespace Isosurface.AdaptiveDualContouring
{
	public class ADC3D : ISurfaceAlgorithm
	{
		public override string Name { get { return "Adaptive Dual Contouring"; } }

		OctreeNode tree;

		public ADC3D(GraphicsDevice device, int resolution, int size) : base(device, resolution, size, true)
		{
		}

		public override long Contour(float threshold)
		{
			Stopwatch watch = new Stopwatch();

			Vertices.Clear();
			tree = new OctreeNode();

			watch.Start();
			tree.Build(Vector3.Zero, Resolution, threshold, Vertices, Size);
			watch.Stop();

			tree.GenerateVertexBuffer(Vertices);
			if (Vertices.Count > 0)
				VertexBuffer.SetData<VertexPositionColorNormal>(Vertices.ToArray());
			VertexCount = Vertices.Count;
			ConstructTreeGrid(tree);
			CalculateIndexes();

			return watch.ElapsedMilliseconds;
		}

		public void ConstructTreeGrid(OctreeNode node)
		{
			if (node == null)
				return;
			VertexPositionColor[] vs = new VertexPositionColor[24];
			int x = (int)node.position.X;
			int y = (int)node.position.Y;
			int z = (int)node.position.Z;
			Color c = Color.LightSteelBlue;
			Color v = Color.Red;

			float size = node.size;

			vs[0] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, z + 0 * size), c);
			vs[1] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, z + 0 * size), c);
			vs[2] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, z + 0 * size), c);
			vs[3] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, z + 0 * size), c);
			vs[4] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, z + 0 * size), c);
			vs[5] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, z + 0 * size), c);
			vs[6] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, z + 0 * size), c);
			vs[7] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, z + 0 * size), c);

			vs[8] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, z + 1 * size), c);
			vs[9] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, z + 1 * size), c);
			vs[10] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, z + 1 * size), c);
			vs[11] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, z + 1 * size), c);
			vs[12] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, z + 1 * size), c);
			vs[13] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, z + 1 * size), c);
			vs[14] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, z + 1 * size), c);
			vs[15] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, z + 1 * size), c);

			vs[16] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, z + 0 * size), c);
			vs[17] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, z + 1 * size), c);
			vs[18] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, z + 0 * size), c);
			vs[19] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, z + 1 * size), c);

			vs[20] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, z + 0 * size), c);
			vs[21] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, z + 1 * size), c);
			vs[22] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, z + 0 * size), c);
			vs[23] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, z + 1 * size), c);

			OutlineBuffer.SetData<VertexPositionColor>(OutlineLocation * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 24, VertexPositionColor.VertexDeclaration.VertexStride);
			OutlineLocation += 24;

			/*if (node.type != OctreeNodeType.Leaf || node.draw_info.index == -1 || true)
			{
				OutlineBuffer.SetData<VertexPositionColor>(outline_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 8, VertexPositionColor.VertexDeclaration.VertexStride);
				outline_location += 8;
			}
			else
			{
				x += (int)(node.draw_info.position.X * (float)this.size);
				y += (int)(node.draw_info.position.Y * (float)this.size);
				float r = 2;
				vs[8] = new VertexPositionColor(new Vector3(x - r, y - r, 0), v);
				vs[9] = new VertexPositionColor(new Vector3(x + r, y - r, 0), v);
				vs[10] = new VertexPositionColor(new Vector3(x + r, y - r, 0), v);
				vs[11] = new VertexPositionColor(new Vector3(x + r, y + r, 0), v);
				vs[12] = new VertexPositionColor(new Vector3(x + r, y + r, 0), v);
				vs[13] = new VertexPositionColor(new Vector3(x - r, y + r, 0), v);
				vs[14] = new VertexPositionColor(new Vector3(x - r, y + r, 0), v);
				vs[15] = new VertexPositionColor(new Vector3(x - r, y - r, 0), v);
				OutlineBuffer.SetData<VertexPositionColor>(outline_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 16, VertexPositionColor.VertexDeclaration.VertexStride);
				outline_location += 16;
			}*/

			if (node.type != OctreeNodeType.Leaf)
			{
				for (int i = 0; i < 8; i++)
				{
					ConstructTreeGrid(node.children[i]);
				}
			}
		}

		public void CalculateIndexes()
		{
			Indices.Clear();

			tree.ProcessCell(Indices);
			IndexCount = Indices.Count;
			if (Indices.Count == 0)
				return;

			IndexBuffer.SetData<int>(Indices.ToArray());

		}
	}
}
