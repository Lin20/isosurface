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
		public const bool FlatShading = true;

		private List<VertexPositionColorNormalNormal> VerticesDN { get; set; }

		OctreeNode tree;

		private bool enforce_manifold;
		public bool EnforceManifold
		{
			get { return enforce_manifold; }
			set { enforce_manifold = value; OctreeNode.EnforceManifold = value; }
		}

		public override string ExtraInformation
		{
			get { return "Manifold: " + enforce_manifold.ToString(); }
		}

		public MDC3D(GraphicsDevice device, int resolution, int size)
			: base(device, resolution, size, true, !FlatShading, 2097152)
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

			EnforceManifold = true;
			OctreeNode.EnforceManifold = EnforceManifold;
			CustomWireframe = FlatShading;
			SpecialShader = true;
			SupportsDeferred = false;

			VerticesDN = new List<VertexPositionColorNormalNormal>();
			VertexBuffer = new DynamicVertexBuffer(device, VertexPositionColorNormalNormal.VertexDeclaration, 735468, BufferUsage.None);

			WireframeBuffer = new VertexBuffer(device, VertexPositionColor.VertexDeclaration, 524288, BufferUsage.None);
			WireframeIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, 1048576, BufferUsage.None);

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

			watch.Start();
			if (tree == null)
			{
				VerticesDN.Clear();
				tree = new OctreeNode();
				List<VertexPositionColorNormalNormal> vs = new List<VertexPositionColorNormalNormal>();

				tree.ConstructBase(Resolution, 0, ref vs);
				tree.ClusterCellBase(0);
				//VerticesDN = vs.ToList();

				tree.GenerateVertexBuffer(VerticesDN);

				if (VerticesDN.Count > 0)
					VertexBuffer.SetData<VertexPositionColorNormalNormal>(VerticesDN.ToArray());
				VertexCount = VerticesDN.Count;
			}

			OutlineLocation = 0;
			//ConstructTreeGrid(tree);
			CalculateIndexes(threshold);
			watch.Stop();

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

			if (node.type == NodeType.Internal && node.vertices.Length == 0)
			{
				for (int i = 0; i < 8; i++)
				{
					ConstructTreeGrid(node.children[i]);
				}
			}
		}

		public void CalculateIndexes(float threshold)
		{
			if (!FlatShading)
				Indices.Clear();
			else
				Indices = new List<int>();
			List<int> tri_count = new List<int>();

			tree.ProcessCell(Indices, tri_count, threshold);
			if (!FlatShading)
			{
				IndexCount = Indices.Count;
				if (Indices.Count == 0)
					return;

				for (int i = 0; i < Indices.Count; i++)
					Indices[i] = Indices[i] & 0xFFFFFFF;
				IndexBuffer.SetData<int>(Indices.ToArray());
			}
			else
			{
				List<VertexPositionColorNormalNormal> new_vertices = new List<VertexPositionColorNormalNormal>();
				List<VertexPositionColor> wire_verts = new List<VertexPositionColor>();
				List<int> wire_indexes = new List<int>();

				int t_index = 0;
				for (int i = 0; i < Indices.Count; i += 3)
				{
					int count = tri_count[t_index++];
					Vector3 n = Vector3.Zero;
					if (count == 1)
						n = GetNormalQ(VerticesDN, Indices[i + 2] & 0xFFFFFFF, Indices[i + 0] & 0xFFFFFFF, Indices[i + 1] & 0xFFFFFFF);
					else
						n = GetNormalQ(VerticesDN, Indices[i + 2] & 0xFFFFFFF, Indices[i + 0] & 0xFFFFFFF, Indices[i + 1] & 0xFFFFFFF, Indices[i + 5] & 0xFFFFFFF, Indices[i + 3] & 0xFFFFFFF, Indices[i + 4] & 0xFFFFFFF);
					Vector3 nc = n * 0.5f + Vector3.One * 0.5f;
					nc.Normalize();
					Color c = new Color(nc);

					VertexPositionColorNormalNormal v0 = new VertexPositionColorNormalNormal(VerticesDN[Indices[i + 0] & 0xFFFFFFF].Position, c, n, VerticesDN[Indices[i + 0] & 0xFFFFFFF].Normal);
					VertexPositionColorNormalNormal v1 = new VertexPositionColorNormalNormal(VerticesDN[Indices[i + 1] & 0xFFFFFFF].Position, c, n, VerticesDN[Indices[i + 1] & 0xFFFFFFF].Normal);
					VertexPositionColorNormalNormal v2 = new VertexPositionColorNormalNormal(VerticesDN[Indices[i + 2] & 0xFFFFFFF].Position, c, n, VerticesDN[Indices[i + 2] & 0xFFFFFFF].Normal);

					new_vertices.Add(v0);
					new_vertices.Add(v1);
					new_vertices.Add(v2);

					int start = wire_verts.Count;
					if (count > 1)
					{
						VertexPositionColorNormalNormal v3 = new VertexPositionColorNormalNormal(VerticesDN[Indices[i + 3] & 0xFFFFFFF].Position, c, n, VerticesDN[Indices[i + 3] & 0xFFFFFFF].Normal);
						VertexPositionColorNormalNormal v4 = new VertexPositionColorNormalNormal(VerticesDN[Indices[i + 4] & 0xFFFFFFF].Position, c, n, VerticesDN[Indices[i + 4] & 0xFFFFFFF].Normal);
						VertexPositionColorNormalNormal v5 = new VertexPositionColorNormalNormal(VerticesDN[Indices[i + 5] & 0xFFFFFFF].Position, c, n, VerticesDN[Indices[i + 5] & 0xFFFFFFF].Normal);

						new_vertices.Add(v3);
						new_vertices.Add(v4);
						new_vertices.Add(v5);

						if (Indices[i] >= 0x10000000)
						{
							//0-2, 2-1, 1-4, 4-0
							wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 0] & 0xFFFFFFF].Position, c));
							wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 1] & 0xFFFFFFF].Position, c));
							wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 2] & 0xFFFFFFF].Position, c));
							wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 4] & 0xFFFFFFF].Position, c));
							wire_indexes.Add(start + 0);
							wire_indexes.Add(start + 2);
							wire_indexes.Add(start + 2);
							wire_indexes.Add(start + 1);
							wire_indexes.Add(start + 1);
							wire_indexes.Add(start + 3);
							wire_indexes.Add(start + 3);
							wire_indexes.Add(start + 0);
						}
						else
						{
							//0-1, 1-2, 2-5, 5-0
							wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 0] & 0xFFFFFFF].Position, c));
							wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 1] & 0xFFFFFFF].Position, c));
							wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 2] & 0xFFFFFFF].Position, c));
							wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 5] & 0xFFFFFFF].Position, c));
							wire_indexes.Add(start + 0);
							wire_indexes.Add(start + 1);
							wire_indexes.Add(start + 1);
							wire_indexes.Add(start + 2);
							wire_indexes.Add(start + 2);
							wire_indexes.Add(start + 3);
							wire_indexes.Add(start + 3);
							wire_indexes.Add(start + 0);
						}

						i += 3;
					}
					else
					{
						wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 0] & 0xFFFFFFF].Position, c));
						wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 1] & 0xFFFFFFF].Position, c));
						wire_verts.Add(new VertexPositionColor(VerticesDN[Indices[i + 2] & 0xFFFFFFF].Position, c));
						wire_indexes.Add(start + 0);
						wire_indexes.Add(start + 1);
						wire_indexes.Add(start + 1);
						wire_indexes.Add(start + 2);
						wire_indexes.Add(start + 2);
						wire_indexes.Add(start + 0);
					}
				}

				if (new_vertices.Count > 0)
					VertexBuffer.SetData<VertexPositionColorNormalNormal>(new_vertices.ToArray());
				if (wire_verts.Count > 0)
				{
					WireframeBuffer.SetData<VertexPositionColor>(wire_verts.ToArray());
					WireframeIndexBuffer.SetData<int>(wire_indexes.ToArray());
				}
				VertexCount = new_vertices.Count;
				WireframeCount = wire_indexes.Count;
				WireframeVertexCount = wire_verts.Count;
			}
		}

		private Vector3 GetNormalQ(List<VertexPositionColorNormalNormal> verts, params int[] indexes)
		{
			Vector3 a = verts[indexes[2]].Position - verts[indexes[1]].Position;
			Vector3 b = verts[indexes[2]].Position - verts[indexes[0]].Position;
			Vector3 c = Vector3.Cross(a, b);

			if (indexes.Length == 6)
			{
				a = verts[indexes[5]].Position - verts[indexes[4]].Position;
				b = verts[indexes[5]].Position - verts[indexes[3]].Position;
				Vector3 d = Vector3.Cross(a, b);

				//c.Normalize();
				if (float.IsNaN(c.X))
					c = Vector3.Zero;
				if (float.IsNaN(d.X))
					d = Vector3.Zero;

				c += d;
				c /= 2.0f;
			}

			c.Normalize();

			return -c;
		}
	}
}
