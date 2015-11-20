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

namespace Isosurface.ManifoldDC
{
	public class MDC3D : ISurfaceAlgorithm
	{
		public override string Name { get { return "Manifold Dual Contouring"; } }

		OctreeNode tree;

		public MDC3D(GraphicsDevice device, int resolution, int size)
			: base(device, resolution, size, true)
		{
			for (int i = 0; i < 256; i++)
			{
				bool[] found = new bool[16];
				for (int k = 0; k < 16; k++)
				{
					if (Utilities.TransformedEdgesTable[i, k] < 0)
						continue;
					if (found[Utilities.TransformedEdgesTable[i, k]])
					{
					}
					found[Utilities.TransformedEdgesTable[i, k]] = true;
				}
			}
			/*int[,] associated =
			{
				{ 0, 0 },
				{ 1, 4 },
				{ 2, 5 },
				{ 3, 1 },
				{ 4, 2 },
				{ 5, 6 },
				{ 6, 7 },
				{ 7, 3 }
			};
			int[] edges = { 0, 10, 1, 8, 2, 11, 3, 9, 4, 6, 7, 5 };
			int[,] new_table = new int[256, 16];
			int[] new_vtable = new int[256];
			string s = "";
			System.IO.StreamWriter sw = System.IO.File.CreateText("C:\\table.txt");
			for (int i = 0; i < 256; i++)
			{
				int old_code = i;
				int new_code = 0;
				for (int k = 0; k < 8; k++)
				{
					if ((old_code & (1 << k)) != 0)
						new_code |= 1 << associated[k, 1];
				}
				for (int k = 0; k < 16; k++)
				{
					new_table[new_code, k] = (Utilities.EdgesTable[i, k] >= 0 ? edges[Utilities.EdgesTable[i, k]] : Utilities.EdgesTable[i, k]);
				}
			}

			for (int i = 0; i < 256; i++)
			{
				s = "{ ";
				for (int k = 0; k < 16; k++)
				{
					s += new_table[i, k].ToString();
					if (k < 15)
						s += ", ";
				}
				if (i < 255)
					s += " },";
				else
					s += " }";
				sw.WriteLine(s);
			}

			sw.Close();

			
			for (int i = 0; i < 256; i++)
			{
				int old_code = i;
				int new_code = 0;
				for (int k = 0; k < 8; k++)
				{
					if ((old_code & (1 << k)) != 0)
						new_code |= 1 << associated[k, 1];
				}
				new_vtable[new_code] = Utilities.VerticesNumberTable[i];
			}

			sw = System.IO.File.CreateText("C:\\table2.txt");
			s = "{\r\n";
			for (int i = 0; i < 256; i++)
			{
				s += new_vtable[i].ToString();
				if (i == 255)
					s += "}";
				else if (i % 16 == 15)
					s += ",\r\n";
				else
					s += ", ";
			}
			sw.WriteLine(s);
			sw.Close();*/
		}

		public override long Contour(float threshold)
		{
			Stopwatch watch = new Stopwatch();

			Vertices.Clear();
			tree = new OctreeNode();

			watch.Start();
			List<VertexPositionColorNormal> vs = new List<VertexPositionColorNormal>();
			tree.ConstructBase(Resolution, threshold, ref vs);
			tree.ClusterCellBase(threshold);
			watch.Stop();
			//Vertices = vs.ToList();

			tree.GenerateVertexBuffer(Vertices);

			if (Vertices.Count > 0)
				VertexBuffer.SetData<VertexPositionColorNormal>(Vertices.ToArray());
			VertexCount = Vertices.Count;
			OutlineLocation = 0;
			ConstructTreeGrid(tree);
			CalculateIndexes();

			return watch.ElapsedMilliseconds;
		}

		public void ConstructTreeGrid(OctreeNode node)
		{
			if (node == null || node.type == NodeType.Leaf)
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

			if (node.type == NodeType.Internal)
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
