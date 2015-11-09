/* Uniform Dual Contouring
 * Messy, but it works
 * This was an earlier implementation so it still operates on pre-calculated values rather than the function directly
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

namespace Isosurface.DMCNeilson
{
	public class Vertex
	{
		public int Index { get; set; }
		public List<int> Edges { get; set; }
		public Vector3 Position { get; set; }

		public Vertex()
		{
			Edges = new List<int>();
		}
	}

	public class Edge
	{
		public int Index { get; set; }
		public Vector3 A { get; set; }
		public Vector3 B { get; set; }
		public float ValueA { get; set; }
		public float ValueB { get; set; }
		public List<Vertex> Vertices { get; set; }
		public bool Flipped { get; set; }

		public Edge()
		{
			Vertices = new List<Vertex>();
		}

		public Vector3 GetIntersection()
		{

			return Sampler.GetIntersection(A, B, ValueA, ValueB);
		}
	}

	public class Cell
	{
		public Vertex[] Vertices { get; set; }
		public Edge[] Edges { get; set; }

		public Cell()
		{
			Edges = new Edge[12];
			for (int i = 0; i < 12; i++)
			{
				Edges[i] = new Edge();
				Edges[i].Index = i;
			}
		}
	}

	public enum VertexModes
	{
		Edges = 0,
		Block = 1,
		QEF = 2,
		QEM = 3
	}

	public class DMCN : ISurfaceAlgorithm
	{
		public Cell[, ,] Cells { get; set; }
		public List<VertexPositionColor> CalculatedVertices { get; set; }
		private bool UseFlatShading { get; set; }
		public const bool Quads = true;
		public VertexModes VertexMode { get; set; }

		private Vector3[] CornerDeltas =
		{
			new Vector3(0,0,0),
			new Vector3(1,0,0),
			new Vector3(1,0,1),
			new Vector3(0,0,1),
			
			new Vector3(0,1,0),
			new Vector3(1,1,0),
			new Vector3(1,1,1),
			new Vector3(0,1,1)
		};

		private int[,] EdgePairs = 
		{
			{ 0, 1 },
			{ 1, 2 },
			{ 3, 2 },
			{ 0, 3 },
			
			{ 4, 5 },
			{ 5, 6 },
			{ 7, 6 },
			{ 4, 7 },

			{ 4, 0 },
			{ 1, 5 },
			{ 2, 6 },
			{ 3, 7 }
		};


		public override string Name { get { return "Dual Marching Cubes (Neilson)"; } }
		public DMCN(GraphicsDevice device, int resolution, int size)
			: base(device, resolution, size, true, false, 0x100000)
		{
			VertexMode = VertexModes.QEF;
			UseFlatShading = true;
			if (UseFlatShading)
				CalculatedVertices = new List<VertexPositionColor>();
		}

		public override long Contour(float threshold)
		{
			Stopwatch watch = new Stopwatch();

			watch.Start();
			Cells = new Cell[Resolution, Resolution, Resolution];
			for (int x = 0; x < Resolution; x++)
			{
				for (int y = 0; y < Resolution; y++)
				{
					for (int z = 0; z < Resolution; z++)
					{
						GenerateCells(x, y, z);
					}
				}
			}

			for (int x = 0; x < Resolution - 1; x++)
			{
				for (int y = 0; y < Resolution - 1; y++)
				{
					for (int z = 0; z < Resolution - 1; z++)
					{
						GenerateEdges(x, y, z);
					}
				}
			}

			//for (int x = Resolution - 2; x >= 0; x--)
			for (int x = 0; x < Resolution - 1; x++)
			{
				//for (int y = Resolution - 2; y >= 0; y-- )
				for (int y = 0; y < Resolution - 1; y++)
				{
					//for (int z = Resolution - 2; z >= 0; z--)
					for (int z = 0; z < Resolution - 1; z++)
					{
						Polygonize(x, y, z);
					}
				}
			}

			for (int x = 0; x < Resolution; x++)
			{
				for (int y = 0; y < Resolution; y++)
				{
					for (int z = 0; z < Resolution; z++)
					{
						GenerateIndexes(x, y, z);
					}
				}
			}

			VertexCount = Vertices.Count;
			if (Indices != null)
				IndexCount = Indices.Count;

			if (Vertices.Count > 0)
				VertexBuffer.SetData<VertexPositionColorNormal>(0, Vertices.ToArray(), 0, VertexCount, VertexPositionColorNormal.VertexDeclaration.VertexStride);
			if (!UseFlatShading && Indices.Count > 0)
				IndexBuffer.SetData<int>(Indices.ToArray());

			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		private void GenerateCells(int x, int y, int z)
		{
			Cells[x, y, z] = new Cell();
			Cells[x, y, z].Edges[0] = GenerateEdge(x, y, z, 0);
			Cells[x, y, z].Edges[3] = GenerateEdge(x, y, z, 3);
			Cells[x, y, z].Edges[8] = GenerateEdge(x, y, z, 8);
		}

		private Edge GenerateEdge(int x, int y, int z, int i)
		{
			Edge e = new Edge();
			e.Index = i;
			e.A = new Vector3(x, y, z) + CornerDeltas[EdgePairs[i, 0]];
			e.B = new Vector3(x, y, z) + CornerDeltas[EdgePairs[i, 1]];

			e.ValueA = Sampler.Sample(e.A);
			e.ValueB = Sampler.Sample(e.B);

			return e;
		}

		private void GenerateEdges(int x, int y, int z)
		{
			Cells[x, y, z].Edges[1] = Cells[x + 1, y, z].Edges[3];
			Cells[x, y, z].Edges[2] = Cells[x, y, z + 1].Edges[0];

			Cells[x, y, z].Edges[4] = Cells[x, y + 1, z].Edges[0];
			Cells[x, y, z].Edges[5] = Cells[x + 1, y + 1, z].Edges[3];
			Cells[x, y, z].Edges[6] = Cells[x, y + 1, z + 1].Edges[0];
			Cells[x, y, z].Edges[7] = Cells[x, y + 1, z].Edges[3];

			Cells[x, y, z].Edges[9] = Cells[x + 1, y, z].Edges[8];
			Cells[x, y, z].Edges[10] = Cells[x + 1, y, z + 1].Edges[8];
			Cells[x, y, z].Edges[11] = Cells[x, y, z + 1].Edges[8];
		}

		private void Polygonize(int x, int y, int z)
		{
			if (Cells[x, y, z] == null)
				return;

			int cube_index = 0;
			for (int i = 0; i < 8; i++)
			{
				if (Sampler.Sample(new Vector3(x, y, z) + CornerDeltas[i]) < 0)
					cube_index |= 1 << i;
			}

			if (cube_index == 0 || cube_index == 255)
				return;

			Cells[x, y, z].Vertices = new Vertex[VerticesNumberTable[cube_index]];
			/*for (int i = 0; i < 12; i++)
			{
				Cells[x, y, z].Edges[i].A = new Vector3(x, y, z) + CornerDeltas[EdgePairs[i, 0]];
				Cells[x, y, z].Edges[i].B = new Vector3(x, y, z) + CornerDeltas[EdgePairs[i, 1]];

				Cells[x, y, z].Edges[i].ValueA = Sampler.Sample(Cells[x, y, z].Edges[i].A);
				Cells[x, y, z].Edges[i].ValueB = Sampler.Sample(Cells[x, y, z].Edges[i].B);
			}*/

			int v_index = 0;
			Cells[x, y, z].Vertices[0] = new Vertex();
			for (int e = 0; e < EdgesTable.GetLength(1); e++)
			{
				if (EdgesTable[cube_index, e] == -2)
					break;
				if (EdgesTable[cube_index, e] == -1)
				{
					v_index++;
					if (v_index < Cells[x, y, z].Vertices.Length)
						Cells[x, y, z].Vertices[v_index] = new Vertex();
					continue;
				}

				//Cells[x, y, z].Vertices[v_index].Index = v_index;
				Cells[x, y, z].Vertices[v_index].Edges.Add(EdgesTable[cube_index, e]);
				Cells[x, y, z].Edges[EdgesTable[cube_index, e]].Vertices.Add(Cells[x, y, z].Vertices[v_index]);
				//Cells[x, y, z].Edges[EdgesTable[cube_index, e]].Flipped = Cells[x, y, z].Edges[EdgesTable[cube_index, e]].ValueA < 0;
			}

			foreach (Vertex v in Cells[x, y, z].Vertices)
			{
				Vertex tx = v;
				if (v == null) //for edges 241/243, which were originally marked as having 2 vertices...?
					continue;
				Vector3 point = new Vector3();

				if (VertexMode != VertexModes.Block)
				{
					//QEF3D qef = new QEF3D();
					QEFProper.QEFSolver qef = new QEFProper.QEFSolver();
					VertexPlacement qem = new VertexPlacement();
					for (int e_i = 0; e_i < tx.Edges.Count; e_i++)
					{
						Edge e = Cells[x, y, z].Edges[tx.Edges[e_i]];
						if (VertexMode == VertexModes.Edges)
							point += e.GetIntersection();
						else if (VertexMode == VertexModes.QEF)
							qef.Add(e.GetIntersection() - new Vector3(x, y, z), Sampler.GetNormal(e.GetIntersection()));
						else
							qem.AddPlane(e.GetIntersection() - new Vector3(x, y, z), Sampler.GetNormal(e.GetIntersection()));
					}

					if (VertexMode == VertexModes.Edges)
						point /= (float)tx.Edges.Count;
					else if (VertexMode == VertexModes.QEF)
						point = qef.Solve(1e-6f, 4, 1e-6f) + new Vector3(x, y, z);
					else
						point = qem.Solve() + new Vector3(x, y, z);
				}
				else
					point = new Vector3(x, y, z) + Vector3.One * 0.5f;
				//point = Vector3.Clamp(point, new Vector3(x, y, z), new Vector3(x + 1, y + 1, z + 1));

				tx.Position = point;
				Vector3 norm = Sampler.GetNormal(point);
				Vector3 c_v = norm * 0.5f + Vector3.One * 0.5f;
				c_v.Normalize();
				Color clr = new Color(c_v);
				if (!UseFlatShading)
				{
					tx.Index = Vertices.Count;
					VertexPositionColorNormal pv = new VertexPositionColorNormal(tx.Position, clr, norm);
					Vertices.Add(pv);
				}
				else
				{
					tx.Index = CalculatedVertices.Count;
					VertexPositionColor pv = new VertexPositionColor(tx.Position, clr);
					CalculatedVertices.Add(pv);
				}

				VertexPositionColor[] vs = new VertexPositionColor[24];
				Color c = Color.Red;
				float vx = tx.Position.X;
				float vy = tx.Position.Y;
				float vz = tx.Position.Z;
				float r = 0.25f;
				vs[0] = new VertexPositionColor(new Vector3((vx + 0), (vy + 0), (vz + 0)), c);
				vs[1] = new VertexPositionColor(new Vector3((vx + r), (vy + 0), (vz + 0)), c);
				vs[2] = new VertexPositionColor(new Vector3((vx + r), (vy + 0), (vz + 0)), c);
				vs[3] = new VertexPositionColor(new Vector3((vx + r), (vy + r), (vz + 0)), c);
				vs[4] = new VertexPositionColor(new Vector3((vx + r), (vy + r), (vz + 0)), c);
				vs[5] = new VertexPositionColor(new Vector3((vx + 0), (vy + r), (vz + 0)), c);
				vs[6] = new VertexPositionColor(new Vector3((vx + 0), (vy + r), (vz + 0)), c);
				vs[7] = new VertexPositionColor(new Vector3((vx + 0), (vy + 0), (vz + 0)), c);

				vs[8] = new VertexPositionColor(new Vector3((vx + 0), (vy + 0), (vz + r)), c);
				vs[9] = new VertexPositionColor(new Vector3((vx + r), (vy + 0), (vz + r)), c);
				vs[10] = new VertexPositionColor(new Vector3((vx + r), (vy + 0), (vz + r)), c);
				vs[11] = new VertexPositionColor(new Vector3((vx + r), (vy + r), (vz + r)), c);
				vs[12] = new VertexPositionColor(new Vector3((vx + r), (vy + r), (vz + r)), c);
				vs[13] = new VertexPositionColor(new Vector3((vx + 0), (vy + r), (vz + r)), c);
				vs[14] = new VertexPositionColor(new Vector3((vx + 0), (vy + r), (vz + r)), c);
				vs[15] = new VertexPositionColor(new Vector3((vx + 0), (vy + 0), (vz + r)), c);

				vs[16] = new VertexPositionColor(new Vector3((vx + 0), (vy + 0), (vz + 0)), c);
				vs[17] = new VertexPositionColor(new Vector3((vx + 0), (vy + 0), (vz + r)), c);
				vs[18] = new VertexPositionColor(new Vector3((vx + 0), (vy + r), (vz + 0)), c);
				vs[19] = new VertexPositionColor(new Vector3((vx + 0), (vy + r), (vz + r)), c);

				vs[20] = new VertexPositionColor(new Vector3((vx + r), (vy + 0), (vz + 0)), c);
				vs[21] = new VertexPositionColor(new Vector3((vx + r), (vy + 0), (vz + r)), c);
				vs[22] = new VertexPositionColor(new Vector3((vx + r), (vy + r), (vz + 0)), c);
				vs[23] = new VertexPositionColor(new Vector3((vx + r), (vy + r), (vz + r)), c);

				OutlineBuffer.SetData<VertexPositionColor>(OutlineLocation * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 24, VertexPositionColor.VertexDeclaration.VertexStride);
				OutlineLocation += 24;
			}


		}

		private void GenerateIndexes(int x, int y, int z)
		{
			if (Cells[x, y, z] == null)
				return;

			int[] edge_indexes = { 0, 3, 8 };
			for (int i = 0; i < 3; i++)
			{
				Edge e = Cells[x, y, z].Edges[edge_indexes[i]];
				bool flipped = e.ValueA < 0;
				if (e.Vertices.Count > 3)
				{
					if (!UseFlatShading)
					{
						Indices.Add(e.Vertices[2].Index);
						Indices.Add(e.Vertices[0].Index);
						Indices.Add(e.Vertices[1].Index);

						Indices.Add(e.Vertices[1].Index);
						Indices.Add(e.Vertices[3].Index);
						Indices.Add(e.Vertices[2].Index);
					}
					else
					{
						AddFlatTriangle(flipped, e.Vertices[0].Index, e.Vertices[1].Index, e.Vertices[2].Index, e.Vertices[3].Index);
					}
				}
				else if (e.Vertices.Count == 3)
				{
					if (!UseFlatShading)
					{
						Indices.Add(e.Vertices[0].Index);
						Indices.Add(e.Vertices[1].Index);
						Indices.Add(e.Vertices[2].Index);
					}
					else
					{
						AddFlatTriangle(flipped, e.Vertices[0].Index, e.Vertices[1].Index, e.Vertices[2].Index);
					}
				}
			}
		}

		private void AddFlatTriangle(bool flipped, params int[] indexes)
		{
			VertexPositionColorNormal[] verts = new VertexPositionColorNormal[indexes.Length];
			for (int i = 0; i < verts.Length; i++)
				verts[i] = new VertexPositionColorNormal(CalculatedVertices[indexes[i]].Position, CalculatedVertices[indexes[i]].Color, Vector3.Zero);

			if (indexes.Length > 3)
			{
				if (!flipped)
				{
					Vector3 n = GetNormalQ(ref verts, 2, 0, 1, 1, 3, 2);
					Vector3 c_v = n * 0.5f + Vector3.One * 0.5f;
					c_v.Normalize();
					Color clr = new Color(c_v);

					Vector3 d = new Vector3(-.1f, -1f, -.1f);
					d.Normalize();
					float g = (Vector3.Dot(n, d) + 1.0f) * 0.5f;
					//clr = new Color(0, g, 0);

					for (int i = 0; i < 4; i++)
					{
						verts[i].Normal = n;
						verts[i].Color = clr;
					}

					//GetNormal(ref verts, 2, 0, 1);
					Vertices.Add(verts[2]);
					Vertices.Add(verts[0]);
					Vertices.Add(verts[1]);

					//GetNormal(ref verts, 1, 3, 2);
					Vertices.Add(verts[1]);
					Vertices.Add(verts[3]);
					Vertices.Add(verts[2]);
				}
				else
				{
					Vector3 n = GetNormalQ(ref verts, 2, 1, 0, 1, 2, 3);
					Vector3 c_v = n * 0.5f + Vector3.One * 0.5f;
					c_v.Normalize();
					Color clr = new Color(c_v);

					Vector3 d = new Vector3(-.1f, -1f, -.1f);
					d.Normalize();
					float g = (Vector3.Dot(n, d) + 1.0f) * 0.5f;
					//clr = new Color(0, g, 0);

					for (int i = 0; i < 4; i++)
					{
						verts[i].Normal = n;
						verts[i].Color = clr;
					}

					//GetNormal(ref verts, 2, 1, 0);
					Vertices.Add(verts[2]);
					Vertices.Add(verts[1]);
					Vertices.Add(verts[0]);

					//GetNormal(ref verts, 1, 2, 3);
					Vertices.Add(verts[1]);
					Vertices.Add(verts[2]);
					Vertices.Add(verts[3]);
				}
			}
			else if (indexes.Length == 3)
			{
				/*GetNormal(ref verts, 0, 1, 2);
				Vertices.Add(verts[0]);
				Vertices.Add(verts[1]);
				Vertices.Add(verts[2]);*/
			}
		}

		private Vector3 GetNormalQ(ref VertexPositionColorNormal[] verts, params int[] indexes)
		{
			Vector3 a = verts[indexes[2]].Position - verts[indexes[1]].Position;
			Vector3 b = verts[indexes[2]].Position - verts[indexes[0]].Position;
			Vector3 c = Vector3.Cross(a, b);

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
			c.Normalize();

			return -c;
		}

		private Vector3 GetNormalNA(ref VertexPositionColorNormal[] verts, params int[] indexes)
		{
			Vector3 product = new Vector3();

			Vector3 a = verts[indexes[0]].Position - verts[indexes[2]].Position;
			Vector3 b = verts[indexes[1]].Position - verts[indexes[2]].Position;
			Vector3 c = Vector3.Cross(a, b);
			//c.Normalize();
			product += c;

			a = verts[indexes[2]].Position - verts[indexes[1]].Position;
			b = verts[indexes[3]].Position - verts[indexes[1]].Position;
			c = Vector3.Cross(a, b);
			//c.Normalize();
			product += c;
			//product *= 0.5f;
			product.Normalize();

			return product;
		}

		private Vector3 GetNormal(ref VertexPositionColorNormal[] verts, params int[] indexes)
		{
			/*Vector3 product = new Vector3();

			Vector3 a = verts[0].Position - verts[2].Position;
			Vector3 b = verts[1].Position - verts[2].Position;
			Vector3 c = Vector3.Cross(a, b);
			//c = new Vector3(Math.Abs(c.X), Math.Abs(c.Y), Math.Abs(c.Z));
			c.Normalize();
			product += c;

			a = verts[2].Position - verts[1].Position;
			b = verts[3].Position - verts[1].Position;
			c = Vector3.Cross(a, b);
			//c = new Vector3(Math.Abs(c.X), Math.Abs(c.Y), Math.Abs(c.Z));
			c.Normalize();
			product += c;
			product /= 2.0f;
			product.Normalize();*/

			Vector3 product = new Vector3();
			if (!Quads)
			{
				Vector3 n0 = Sampler.GetNormal(verts[indexes[0]].Position);
				Vector3 n1 = Sampler.GetNormal(verts[indexes[1]].Position);
				Vector3 n2 = Sampler.GetNormal(verts[indexes[2]].Position);
				product = (n0 + n1 + n2) / 3.0f;
				product = Sampler.GetNormal((verts[indexes[0]].Position + verts[indexes[1]].Position + verts[indexes[2]].Position) / 3.0f);
				product.Normalize();
			}
			else
			{
				Vector3 n0 = Sampler.GetNormal(verts[0].Position);
				Vector3 n1 = Sampler.GetNormal(verts[1].Position);
				Vector3 n2 = Sampler.GetNormal(verts[2].Position);
				Vector3 n3 = Sampler.GetNormal(verts[3].Position);
				product = (n0 + n1 + n2 + n3);
				//product = Sampler.GetNormal((verts[0].Position + verts[1].Position + verts[2].Position + verts[3].Position) * 0.25f);
				product.Normalize();
			}

			Vector3 c_v = product * 0.5f + Vector3.One * 0.5f;
			c_v.Normalize();
			Color clr = new Color(c_v);
			Vector3 d = new Vector3(-.1f, 1f, -.1f);
			d.Normalize();
			float g = (Vector3.Dot(product, d) + 1.0f) * 0.5f;
			clr = new Color(0, g, 0);
			//clr = Color.Green;

			verts[indexes[0]].Normal = product;
			verts[indexes[1]].Normal = product;
			verts[indexes[2]].Normal = product;
			verts[indexes[0]].Color = clr;
			verts[indexes[1]].Color = clr;
			verts[indexes[2]].Color = clr;
			return product;
		}


		/* These tables courtesy of http://stackoverflow.com/questions/16638711/dual-marching-cubes-table */
		#region EdgesTable

		public static int[,] EdgesTable = new int[256, 16]
		{
            {-2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 8, 3, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {0, 1, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {1, 3, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {1, 2, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, 
            {0, 8, 3, -1, 1, 2, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1},    
            {9, 0, 2, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {10, 2, 3, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {3, 11, 2, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, 
            {0, 8, 11, 2, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {1, 9, 0, -1, 2, 3, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},    
            {1, 2, 8, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {1, 3, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, 
            {0, 1, 8, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {0, 3, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {8, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, 
            {4, 7, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {0, 3, 4, 7, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {0, 1, 9, -1, 8, 4, 7, -2, -1, -1, -1, -1, -1, -1, -1, -1},     
            {1, 3, 4, 7, 9, -2, -1, -1, 1, -1, -1, -1, -1, -1, -1, -1},              
            {1, 2, 10, -1, 8, 4, 7, -2, -1, -1, -1, -1, -1, -1, -1, -1},    
            {1, 2, 10, -1, 0, 3, 7, 4, -2, -1, -1, -1, -1, -1, -1, -1},     
            {0, 2, 10, 9, -1, 8, 7, 4, -2, -1, -1, -1, -1, -1, -1, -1},     
            {2, 3, 4, 7, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},    
            {8, 4, 7, -1, 3, 11, 2, -2, -1, -1, -1, -1, -1, -1, -1, -1},    
            {0, 2, 4, 7, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {9, 0, 1, -1, 8, 4, 7, -1, 2, 3, 11, -2, -1, -1, -1, -1},       
            {1, 2, 4, 7, 9, 11, -2, -1, -1, -1, -1, -1-1, -1, -1, -1, -1},              
            {3, 11, 10, 1, -1, 8, 7, 4, -2, -1, -1, -1, -1, -1, -1, -1},    
            {0, 1, 4, 7, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {4, 7, 8, -1, 0, 3, 11, 10, 9, -2, -1, -1, -1, -1, -1, -1},     
            {4, 7, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {9, 5, 4, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {9, 5, 4, -1, 0, 8, 3, -2, -1, -1, -1, -1, -1, -1, -1, -1},     
            {0, 5, 4, 1, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {1, 3, 4, 5, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},    
            {1, 2, 10, -1, 9, 5, 4, -2, -1, -1, -1, -1, -1, -1, -1, -1},    
            {3, 0, 8, -1, 1, 2, 10, -1, 4, 9, 5, -2, -1, -1, -1, -1},       
            {0, 2, 4, 5, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {2, 3, 4, 5, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},    
            {9, 5, 4, -1, 2, 3, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},    
            {9, 4, 5, -1, 0, 2, 11, 8, -2, -1, -1, -1, -1, -1, -1, -1},     
            {3, 11, 2, -1, 0, 4, 5, 1, -2, -1, -1, -1, -1, -1, -1, -1},     
            {1, 2, 4, 5, 8, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},    
            {3, 11, 10, 1, -1, 9, 4, 5, -2, -1, -1, -1, -1, -1, -1, -1},    
            {4, 9, 5, -1, 0, 1, 8, 10, 11, -2, -1, -1, -1, -1, -1, -1},     
            {0, 3, 4, 5, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {5, 4, 8, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  
            {5, 7, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {0, 3, 5, 7, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},    
            {0, 1, 5, 7, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},    
            {1, 5, 3, 7, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {1, 2, 10, -1, 8, 7, 5, 9, -2, -1, -1, -1, -1, -1, -1, -1},     
            {1, 2, 10, -1, 0, 3, 7, 5, 9, -2, -1, -1, -1, -1, -1, -1},      
            {0, 2, 5, 7, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},    
            {2, 3, 5, 7, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {2, 3, 11, -1, 5, 7, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1},            
            {0, 2, 5, 7, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},    
            {2, 3, 11, -1, 0, 1, 5, 7, 8, -2, -1, -1, -1, -1, -1, -1},      
            {1, 2, 5, 7, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},   
            {3, 11, 10, 1, -1, 8, 7, 5, 9, -2, -1, -1, -1, -1, -1, -1},     
            {0, 1, 5, 7, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},    
            {0, 3, 5, 7, 8, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},    
            {5, 7, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {5, 6, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 8, -1, 10, 5, 6, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 9, -1, 10, 5, 6, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {10, 5, 6, -1, 3, 8, 9, 1, -2, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 5, 6, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 8, -1, 1, 2, 6, 5, -2, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 5, 6, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 5, 6, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {3, 11, 2, -1, 10, 6, 5, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {10, 5, 6, -1, 0, 8, 2, 11, -2, -1, -1, -1, -1, -1, -1, -1},
            {3, 11, 2, -1, 0, 1, 9, -1, 10, 5, 6, -2, -1, -1, -1, -1},
            {10, 5, 6, -1, 11, 2, 8, 9, 1, -2, -1, -1, -1, -1, -1, -1},
            {1, 3, 5, 6, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 5, 6, 8, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 5, 6, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {5, 6, 8, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {8, 7, 4, -1, 5, 6, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {5, 6, 10, -1, 0, 3, 4, 7, -2, -1, -1, -1, -1, -1, -1, -1},
            {10, 5, 6, -1, 1, 9, 0, -1, 8, 7, 4, -2, -1, -1, -1, -1},
            {10, 5, 6, -1, 7, 4, 9, 1, 3, -2, -1, -1, -1, -1, -1, -1},
            {8, 7, 4, -1, 1, 2, 6, 5, -2, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 7, 4, -1, 1, 2, 6, 5, -2, -1, -1, -1, -1, -1, -1},
            {8, 7, 4, -1, 0, 9, 2, 5, 6, -2, -1, -1, -1, -1, -1, -1},
            {2, 3, 4, 5, 6, 7, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {3, 11, 2, -1, 5, 6, 10, -1, 8, 7, 4, -2, -1, -1, -1, -1},
            {10, 5, 6, -1, 0, 2, 11, 7, 4, -2, -1, -1, -1, -1, -1, -1},
            {3, 11, 2, -1, 0, 1, 9, -1, 10, 5, 6, -1, 8, 7, 4, -2},
            {10, 5, 6, -1, 7, 4, 11, 2, 1, 9, -2, -1, -1, -1, -1, -1},
            {8, 7, 4, -1, 3, 11, 6, 5, 1, -2, -1, -1, -1, -1, -1, -1},
            {0, 1, 4, 5, 6, 7, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {8, 7, 4, -1, 6, 5, 9, 0, 11, 3, -2, -1, -1, -1, -1, -1},
            {4, 5, 6, 7, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},             
            {4, 6, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 8, -1, 9, 10, 6, 4, -2, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 4, 6, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 4, 6, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 4, 6, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 8, -1, 1, 2, 4, 6, 9, -2, -1, -1, -1, -1, -1, -1},
            {0, 2, 4, 6, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 4, 6, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {11, 2, 3, -1, 9, 4, 10, 6, -2, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 11, 8, -1, 9, 4, 6, 10, -2, -1, -1, -1, -1, -1, -1},
            {2, 3, 11, -1, 0, 1, 4, 6, 10, -2, -1, -1, -1, -1, -1, -1},
            {1, 2, 4, 6, 8, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 4, 6, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 4, 6, 8, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 4, 6, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {4, 6, 8, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {6, 7, 8, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 6, 7, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 6, 7, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 6, 7, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 6, 7, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 2, 3, 6, 7, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 6, 7, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 6, 7, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {3, 11, 2, -1, 10, 6, 9, 7, 8, -2, -1, -1, -1, -1, -1, -1},
            {0, 2, 6, 7, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {3, 11, 2, -1, 8, 7, 0, 1, 10, 6, -2, -1, -1, -1, -1, -1},
            {1, 2, 6, 7, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 6, 7, 8, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 6, 7, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 6, 7, 8, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {6, 7, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -1, 0, 3, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -1, 0, 9, 1, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -1, 1, 3, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -1, 1, 2, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -1, 1, 2, 10, -1, 0, 3, 8, -2, -1, -1, -1, -1},
            {11, 7, 6, -1, 0, 9, 10, 2, -2, -1, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -1, 2, 3, 8, 9, 10, -2, -1, -1, -1, -1, -1, -1},
            {2, 3, 6, 7, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 6, 7, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 9, -1, 3, 2, 6, 7, -2, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 6, 7, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 6, 7, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 6, 7, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 6, 7, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {6, 7, 8, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {4, 6, 8, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 4, 6, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 9, -1, 8, 4, 11, 6, -2, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 4, 6, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 10, -1, 8, 4, 6, 11, -2, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 10, -1, 0, 3, 11, 6, 4, -2, -1, -1, -1, -1, -1, -1},
            {0, 9, 10, 2, -1, 8, 4, 11, 6, -2, -1, -1, -1, -1, -1, -1},
            {2, 3, 4, 6, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 4, 6, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 4, 6, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 9, -1, 2, 3, 8, 4, 6, -2, -1, -1, -1, -1, -1, -1},
            {1, 2, 4, 6, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 4, 6, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 4, 6, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 4, 6, 8, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {4, 6, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {6, 7, 11, -1, 4, 5, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {6, 7, 11, -1, 4, 5, 9, -1, 0, 3, 8, -2, -1, -1, -1, -1},
            {11, 7, 6, -1, 1, 0, 5, 4, -2, -1, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -1, 8, 3, 1, 5, 4, -2, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -1, 4, 5, 9, -1, 1, 2, 10, -2, -1, -1, -1, -1},
            {11, 7, 6, -1, 4, 5, 9, -1, 1, 2, 10, -1, 0, 3, 8, -2},
            {11, 7, 6, -1, 0, 2, 10, 5, 4, -2, -1, -1, -1, -1, -1, -1},
            {11, 7, 6, -1, 8, 3, 2, 10, 5, 4, -2, -1, -1, -1, -1, -1},
            {4, 5, 9, -1, 3, 2, 6, 7, -2, -1, -1, -1, -1, -1, -1, -1},
            {4, 5, 9, -1, 2, 0, 8, 7, 6, -2, -1, -1, -1, -1, -1, -1},
            {3, 2, 6, 7, -1, 0, 1, 5, 4, -2, -1, -1, -1, -1, -1, -1},
            {1, 2, 4, 5, 6, 7, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {9, 4, 5, -1, 1, 10, 6, 7, 3, -2, -1, -1, -1, -1, -1, -1},
            {9, 4, 5, -1, 6, 10, 1, 0, 8, 7, -2, -1, -1, -1, -1, -1},
            {0, 3, 4, 5, 6, 7, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {4, 5, 6, 7, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {5, 6, 8, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 5, 6, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 5, 6, 8, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 5, 6, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 10, -1, 9, 5, 6, 11, 8, -2, -1, -1, -1, -1, -1, -1},
            {1, 2, 10, -1, 9, 0, 3, 11, 6, 5, -2, -1, -1, -1, -1, -1},
            {0, 2, 5, 6, 8, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 5, 6, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 5, 6, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 5, 6, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 2, 3, 5, 6, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 5, 6, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 5, 6, 8, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 5, 6, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 5, 6, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {5, 6, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {5, 7, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {5, 7, 10, 11, -1, 0, 3, 8, -2, -1, -1, -1, -1, -1, -1, -1},
            {5, 7, 10, 11, -1, 0, 1, 9, -2, -1, -1, -1, -1, -1, -1, -1},
            {5, 7, 10, 11, -1, 3, 8, 9, 1, -2, -1, -1, -1, -1, -1, -1},
            {1, 2, 5, 7, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 5, 7, 11, -1, 0, 3, 8, -2, -1, -1, -1, -1, -1, -1},
            {0, 2, 5, 7, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 5, 7, 8, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 5, 7, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 5, 7, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 9, -1, 7, 3, 2, 10, 5, -2, -1, -1, -1, -1, -1, -1},
            {1, 2, 5, 7, 8, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 5, 7, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 5, 7, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 5, 7, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {5, 7, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {4, 5, 8, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 4, 5, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 9, -1, 8, 11, 10, 4, 5, -2, -1, -1, -1, -1, -1, -1},
            {1, 3, 4, 5, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 4, 5, 8, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 2, 3, 4, 5, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 4, 5, 8, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 4, 5, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 4, 5, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 4, 5, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 9, -1, 8, 3, 2, 10, 5, 4, -2, -1, -1, -1, -1, -1},
            {1, 2, 4, 5, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 4, 5, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 4, 5, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 4, 5, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {4, 5, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {4, 7, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 8, -1, 10, 9, 4, 7, 11, -2, -1, -1, -1, -1, -1, -1},
            {0, 1, 4, 7, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 4, 7, 8, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 4, 7, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 4, 7, 9, 11, -1, 0, 3, 8, -2, -1, -1, -1, -1, -1},
            {0, 2, 4, 7, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 4, 7, 8, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 4, 7, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 4, 7, 8, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 2, 3, 4, 7, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 4, 7, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 4, 7, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 4, 7, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 4, 7, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {4, 7, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {8, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 9, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, //241 - originally marked as having 2 vertices
            {0, 1, 8, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 10, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, //243 - originally marked as having 2 vertices
            {1, 2, 8, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 2, 3, 9, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 8, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 11, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {2, 3, 8, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 2, 9, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 2, 3, 8, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 2, 10, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {1, 3, 8, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 1, 9, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {0, 3, 8, -2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {-2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
		};

		#endregion

		public static int[] VerticesNumberTable = new int[256]
		{
			0, 1, 1, 1, 1, 2, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1,
			1, 1, 2, 1, 2, 2, 2, 1, 2, 1, 3, 1, 2, 1, 2, 1,
			1, 2, 1, 1, 2, 3, 1, 1, 2, 2, 2, 1, 2, 2, 1, 1, 
			1, 1, 1, 1, 2, 2, 1, 1, 2, 1, 2, 1, 2, 1, 1, 1,
			1, 2, 2, 2, 1, 2, 1, 1, 2, 2, 3, 2, 1, 1, 1, 1,
			2, 2, 3, 2, 2, 2, 2, 1, 3, 2, 4, 2, 2, 1, 2, 1,
			1, 2, 1, 1, 1, 2, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1,
			1, 2, 2, 2, 2, 3, 2, 2, 1, 1, 2, 1, 1, 1, 1, 1,
			1, 1, 2, 1, 2, 2, 2, 1, 1, 1, 2, 1, 1, 1, 2, 1,
			2, 3, 2, 2, 3, 4, 2, 2, 2, 2, 2, 1, 2, 2, 1, 1,
			1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 2, 2, 2, 1, 2, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1,
			1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1,
			1, 2, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 
			1, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0
		};
	}
}