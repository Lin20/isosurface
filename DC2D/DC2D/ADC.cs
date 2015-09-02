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

namespace DC2D
{
	public class ADC
	{
		GraphicsDevice device;
		DynamicVertexBuffer buffer;
		DynamicVertexBuffer outline_buffer;
		DynamicIndexBuffer index_buffer;
		float[,] map;
		int resolution;
		int size;
		int vertex_location;
		int outline_location;
		int index_location;
		Vector2[,] vertices;
		int[,] vertex_indexes;
		Random rnd = new Random();

		QuadtreeNode tree;

		int[,] edges = new int[,] { { 0, 2 }, { 1, 3 }, { 0, 1 }, { 2, 3 } };
		Vector2[] deltas = new Vector2[] { new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };

		public ADC(GraphicsDevice device, int resolution, int size)
		{
			this.device = device;
			this.resolution = resolution;
			this.size = size;
			map = new float[resolution, resolution];
			vertices = new Vector2[resolution, resolution];
			vertex_indexes = new int[resolution, resolution];

			buffer = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, 65536, BufferUsage.None);
			outline_buffer = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, 65536, BufferUsage.None);
			index_buffer = new DynamicIndexBuffer(device, IndexElementSize.ThirtyTwoBits, 65536, BufferUsage.None);
			//InitData();

			tree = new QuadtreeNode();
		}

		public void Contour()
		{
			List<VertexPositionColor> vertices = new List<VertexPositionColor>();
			vertex_location = tree.Build(Vector2.Zero, resolution, 0.01f, vertices, this.size);
			buffer.SetData<VertexPositionColor>(vertices.ToArray());
			ConstructTreeGrid(tree);
			CalculateIndexes();
		}

		public void ConstructTreeGrid(QuadtreeNode node)
		{
			if (node == null)
				return;
			VertexPositionColor[] vs = new VertexPositionColor[16];
			int x = (int)node.position.X * this.size;
			int y = (int)node.position.Y * this.size;
			Color c = Color.LightSteelBlue;
			Color v = Color.Red;

			float size = node.size * this.size;
			vs[0] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, 0), c);
			vs[1] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, 0), c);
			vs[2] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, 0), c);
			vs[3] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, 0), c);
			vs[4] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, 0), c);
			vs[5] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, 0), c);
			vs[6] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, 0), c);
			vs[7] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, 0), c);

			if (node.type != QuadtreeNodeType.Leaf || node.draw_info.index == -1 || true)
			{
				outline_buffer.SetData<VertexPositionColor>(outline_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 8, VertexPositionColor.VertexDeclaration.VertexStride);
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
				outline_buffer.SetData<VertexPositionColor>(outline_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 16, VertexPositionColor.VertexDeclaration.VertexStride);
				outline_location += 16;
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
			index_location = indexes.Count;
			if (indexes.Count == 0)
				return;

			index_buffer.SetData<int>(indexes.ToArray());
		}

		public void Draw()
		{
			if (outline_location > 0)
			{
				device.SetVertexBuffer(outline_buffer);
				device.DrawPrimitives(PrimitiveType.LineList, 0, outline_location / 2);
			}
			if (index_location == 0)
				return;
			device.SetVertexBuffer(buffer);
			device.Indices = index_buffer;
			//device.DrawPrimitives(PrimitiveType.LineList, 0, vertex_location / 2);
			device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, vertex_location, 0, index_location / 2);
		}
	}
}
