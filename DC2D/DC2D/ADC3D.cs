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

namespace DC2D
{
	public class ADC3D
	{
		GraphicsDevice device;
		DynamicVertexBuffer buffer;
		DynamicVertexBuffer outline_buffer;
		DynamicIndexBuffer index_buffer;
		float[, ,] map;
		int resolution;
		int size;
		public int VertexCount { get; set; }
		int outline_location;
		public int IndexCount { get; set; }
		Vector3[, ,] vertices;
		int[, ,] vertex_indexes;
		Random rnd = new Random();

		OctreeNode tree;

		int[,] edges =
		{
			{0,4},{1,5},{2,6},{3,7},	// x-axis 
			{0,2},{1,3},{4,6},{5,7},	// y-axis
			{0,1},{2,3},{4,5},{6,7}		// z-axis
		};

		int[,] dirs = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

		public ADC3D(GraphicsDevice device, int resolution, int size)
		{
			this.device = device;
			this.resolution = resolution;
			this.size = size;
			//map = new float[resolution, resolution, resolution];
			//vertices = new Vector3[resolution, resolution, resolution];
			//vertex_indexes = new int[resolution, resolution, resolution];

			buffer = new DynamicVertexBuffer(device, VertexPositionColorNormal.VertexDeclaration, 262144, BufferUsage.None);
			outline_buffer = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, 4000000, BufferUsage.None);
			index_buffer = new DynamicIndexBuffer(device, IndexElementSize.ThirtyTwoBits, 4000000, BufferUsage.None);
			//InitData();

		}

		public long Contour(float threshold)
		{
			Stopwatch watch = new Stopwatch();

			List<VertexPositionColorNormal> vertices = new List<VertexPositionColorNormal>();
			tree = new OctreeNode();

			watch.Start();
			tree.Build(Vector3.Zero, resolution, threshold, vertices, this.size);
			watch.Stop();

			tree.GenerateVertexBuffer(vertices);
			if (vertices.Count > 0)
				buffer.SetData<VertexPositionColorNormal>(vertices.ToArray());
			VertexCount = vertices.Count;
			//ConstructTreeGrid(tree);
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

			outline_buffer.SetData<VertexPositionColor>(outline_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 24, VertexPositionColor.VertexDeclaration.VertexStride);
			outline_location += 24;

			/*if (node.type != OctreeNodeType.Leaf || node.draw_info.index == -1 || true)
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
			List<int> indexes = new List<int>();

			tree.ProcessCell(indexes);
			IndexCount = indexes.Count;
			if (indexes.Count == 0)
				return;

			index_buffer.SetData<int>(indexes.ToArray());

		}


		public void Draw(BasicEffect effect)
		{
			effect.LightingEnabled = false;
			if (outline_location > 0)
			{
				effect.CurrentTechnique.Passes[0].Apply();
				device.SetVertexBuffer(outline_buffer);
				device.DrawPrimitives(PrimitiveType.LineList, 0, outline_location / 2);
			}
			//return;
			if (IndexCount == 0)                               
				return;
			//effect.LightingEnabled = true;
			effect.PreferPerPixelLighting = true;
			effect.SpecularPower = 64;
			effect.SpecularColor = Color.Black.ToVector3();
			effect.CurrentTechnique.Passes[0].Apply();
			effect.AmbientLightColor = Color.Gray.ToVector3();
			device.SetVertexBuffer(buffer);
			device.Indices = index_buffer;
			//device.DrawPrimitives(PrimitiveType.LineList, 0, vertex_location / 2);
			device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexCount, 0, IndexCount / 3);
			device.Indices = null;
			device.SetVertexBuffer(null);
		}
	}
}
