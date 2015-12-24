/* This is a 3D Manifold Dual Contouring implementation
 * It's a big mess with debug and testing code everywhere, along with comments that don't match.
 * But it's the only published implementation out there at this time, so it's better than nothing.
 * As of now, everything is working. Full vertex clustering with and without manifold criterion!
 * 
 * Current issues:
 * - NONE! :D
 *   
 * TODO is cleanup and comment
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Isosurface.ManifoldDC
{
	public enum NodeType
	{
		None,
		Internal,
		Leaf,
		Collapsed
	}

	public class Vertex
	{
		public Vertex parent;
		public int index;
		public bool collapsible;
		public QEFProper.QEFSolver qef;
		public Vector3 normal;
		public int surface_index;
		public Vector3 Position { get { if (qef != null) return qef.Solve(1e-6f, 4, 1e-6f); return Vector3.Zero; } }
		public float error;
		public int euler;
		public int[] eis;
		public int in_cell;
		public bool face_prop2;

		public Vertex()
		{
			parent = null;
			index = -1;
			collapsible = true;
			qef = null;
			normal = Vector3.Zero;
			surface_index = -1;
			eis = null;
		}

		public override string ToString()
		{
			return "Surface = " + surface_index + ", parent = " + (parent == null ? "false" : "true");
			//return base.ToString();
		}
	}

	public class OctreeNode
	{
		public int index = 0;
		public Vector3 position;
		public int size;
		public OctreeNode[] children;
		public NodeType type;
		public Vertex[] vertices;
		public byte corners;
		public int child_index;

		public static bool EnforceManifold { get; set; }

		public OctreeNode()
		{
		}

		public override string ToString()
		{
			return "Index = " + index + ", size = " + size;
			//return base.ToString();
		}

		public OctreeNode(Vector3 position, int size, NodeType type)
		{
			this.position = position;
			this.size = size;
			this.type = type;
			this.children = new OctreeNode[8];
			this.vertices = new Vertex[0];
		}

		public void ConstructBase(int size, float error, ref List<VertexPositionColorNormal> vertices)
		{
			this.index = 0;
			this.position = Vector3.Zero;
			this.size = size;
			this.type = NodeType.Internal;
			this.children = new OctreeNode[8];
			this.vertices = new Vertex[0];
			this.child_index = 0;
			int n_index = 1;
			ConstructNodes(vertices, ref n_index, 1);
		}

		public void GenerateVertexBuffer(List<VertexPositionColorNormal> vertices)
		{
			if (type != NodeType.Leaf)
			{
				for (int i = 0; i < 8; i++)
				{
					if (children[i] != null)
						children[i].GenerateVertexBuffer(vertices);
				}
			}

			//if (type != NodeType.Internal)
			{
				if (vertices == null || this.vertices.Length == 0)
					return;

				for (int i = 0; i < this.vertices.Length; i++)
				{
					if (this.vertices[i] == null)
						continue;
					this.vertices[i].index = vertices.Count;
					Vector3 nc = this.vertices[i].normal * 0.5f + Vector3.One * 0.5f;
					nc.Normalize();
					Color c = new Color(nc);
					vertices.Add(new VertexPositionColorNormal(this.vertices[i].qef.Solve(1e-6f, 4, 1e-6f), c, this.vertices[i].normal));

				}
			}
		}

		public bool ConstructNodes(List<VertexPositionColorNormal> vertices, ref int n_index, int threaded = 0)
		{
			if (size == 1)
				return ConstructLeaf(ref vertices, ref n_index);

			type = NodeType.Internal;
			int child_size = size / 2;
			bool has_children = false;

			Task[] threads = new Task[8];
			bool[] return_values = new bool[8];

			for (int i = 0; i < 8; i++)
			{
				this.index = n_index++;
				Vector3 child_pos = Utilities.TCornerDeltas[i];
				children[i] = new OctreeNode(position + child_pos * (float)child_size, child_size, NodeType.Internal);
				children[i].child_index = i;

				int index = i;
				if (threaded > 0 && size > 2)
				{
					threads[index] = Task.Factory.StartNew(
						() =>
						{
							int temp = 0;
							return_values[index] = children[index].ConstructNodes(vertices, ref temp, threaded - 1);
							if (!return_values[index])
								children[index] = null;
						}, TaskCreationOptions.AttachedToParent);
					//threads[index].Start();
				}
				else
				{
					if (children[i].ConstructNodes(vertices, ref n_index, 0))
						has_children = true;
					else
						children[i] = null;
				}
			}

			if (threaded > 0 && size > 2)
			{
				for (int i = 0; i < 8; i++)
				{
					threads[i].Wait();
					if (return_values[i])
						has_children = true;
				}
			}

			return has_children;
		}

		public bool ConstructLeaf(ref List<VertexPositionColorNormal> vertices, ref int index)
		{
			if (size != 1)
				return false;

			this.index = index++;
			type = NodeType.Leaf;
			int corners = 0;
			float[] samples = new float[8];
			for (int i = 0; i < 8; i++)
			{
				if ((samples[i] = Sampler.Sample(position + Utilities.TCornerDeltas[i])) < 0)
					corners |= 1 << i;
			}
			this.corners = (byte)corners;

			if (corners == 0 || corners == 255)
				return false;

			int[][] v_edges = new int[Utilities.TransformedVerticesNumberTable[corners]][];
			this.vertices = new Vertex[Utilities.TransformedVerticesNumberTable[corners]];

			int v_index = 0;
			int e_index = 0;
			v_edges[0] = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
			for (int e = 0; e < 16; e++)
			{
				int code = Utilities.TransformedEdgesTable[corners, e];
				if (code == -2)
				{
					v_index++;
					break;
				}
				if (code == -1)
				{
					v_index++;
					e_index = 0;
					v_edges[v_index] = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
					continue;
				}

				v_edges[v_index][e_index++] = code;
			}

			if (v_index > 1)
			{
			}

			for (int i = 0; i < v_index; i++)
			{
				int k = 0;
				this.vertices[i] = new Vertex();
				this.vertices[i].qef = new QEFProper.QEFSolver();
				Vector3 normal = Vector3.Zero;
				int[] ei = new int[12];
				while (v_edges[i][k] != -1)
				{
					ei[v_edges[i][k]] = 1;
					Vector3 a = position + Utilities.TCornerDeltas[Utilities.TEdgePairs[v_edges[i][k], 0]] * size;
					Vector3 b = position + Utilities.TCornerDeltas[Utilities.TEdgePairs[v_edges[i][k], 1]] * size;
					Vector3 intersection = Sampler.GetIntersection(a, b, samples[Utilities.TEdgePairs[v_edges[i][k], 0]], samples[Utilities.TEdgePairs[v_edges[i][k], 1]]);
					Vector3 n = Sampler.GetNormal(intersection);
					normal += n;
					this.vertices[i].qef.Add(intersection, n);
					k++;
				}

				normal /= (float)k;
				normal.Normalize();
				this.vertices[i].index = vertices.Count;
				this.vertices[i].parent = null;
				this.vertices[i].collapsible = true;
				this.vertices[i].normal = normal;
				this.vertices[i].euler = 1;
				this.vertices[i].eis = ei;
				this.vertices[i].in_cell = this.child_index;
				this.vertices[i].face_prop2 = true;
				//VertexPositionColorNormal vert = new VertexPositionColorNormal();
				//vert.Position = this.vertices[i].qef.Solve(1e-6f, 4, 1e-6f) + position;
				//vert.Normal = Sampler.GetNormal(vert.Position);
				//vert.Color = new Color(vert.Normal * 0.5f + Vector3.One * 0.5f);
				//vertices.Add(vert);
			}

			for (int i = 0; i < this.vertices.Length; i++)
			{
				if (this.vertices[i] == null)
				{
				}
			}

			return true;
		}

		public void ProcessCell(List<int> indexes, List<int> tri_count, float threshold)
		{
			if (type == NodeType.Internal)
			{
				for (int i = 0; i < 8; i++)
				{
					if (children[i] != null)
						children[i].ProcessCell(indexes, tri_count, threshold);
				}

				if (index == 31681)
				{
				}
				for (int i = 0; i < 12; i++)
				{
					OctreeNode[] face_nodes = new OctreeNode[2];

					int c1 = Utilities.TEdgePairs[i, 0];
					int c2 = Utilities.TEdgePairs[i, 1];

					face_nodes[0] = children[c1];
					face_nodes[1] = children[c2];

					ProcessFace(face_nodes, Utilities.TEdgePairs[i, 2], indexes, tri_count, threshold);
				}

				for (int i = 0; i < 6; i++)
				{
					OctreeNode[] edge_nodes = 
					{
						children[Utilities.TCellProcEdgeMask[i, 0]],
						children[Utilities.TCellProcEdgeMask[i, 1]],
						children[Utilities.TCellProcEdgeMask[i, 2]],
						children[Utilities.TCellProcEdgeMask[i, 3]]
					};

					ProcessEdge(edge_nodes, Utilities.TCellProcEdgeMask[i, 4], indexes, tri_count, threshold);
				}
			}
		}

		public static void ProcessFace(OctreeNode[] nodes, int direction, List<int> indexes, List<int> tri_count, float threshold)
		{
			if (nodes[0] == null || nodes[1] == null)
				return;

			if (nodes[0].type != NodeType.Leaf || nodes[1].type != NodeType.Leaf)
			{
				for (int i = 0; i < 4; i++)
				{
					OctreeNode[] face_nodes = new OctreeNode[2];

					for (int j = 0; j < 2; j++)
					{
						if (nodes[j].type == NodeType.Leaf)
							face_nodes[j] = nodes[j];
						else
							face_nodes[j] = nodes[j].children[Utilities.TFaceProcFaceMask[direction, i, j]];
					}

					ProcessFace(face_nodes, Utilities.TFaceProcFaceMask[direction, i, 2], indexes, tri_count, threshold);
				}

				int[,] orders =
				{
					{ 0, 0, 1, 1 },
					{ 0, 1, 0, 1 },
				};

				for (int i = 0; i < 4; i++)
				{
					OctreeNode[] edge_nodes = new OctreeNode[4];

					for (int j = 0; j < 4; j++)
					{
						if (nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]].type == NodeType.Leaf)
							edge_nodes[j] = nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]];
						else
							edge_nodes[j] = nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]].children[Utilities.TFaceProcEdgeMask[direction, i, 1 + j]];
					}

					ProcessEdge(edge_nodes, Utilities.TFaceProcEdgeMask[direction, i, 5], indexes, tri_count, threshold);
				}
			}
		}

		public static void ProcessEdge(OctreeNode[] nodes, int direction, List<int> indexes, List<int> tri_count, float threshold)
		{
			if (nodes[0] == null || nodes[1] == null || nodes[2] == null || nodes[3] == null)
				return;

			if (nodes[0].type == NodeType.Leaf && nodes[1].type == NodeType.Leaf && nodes[2].type == NodeType.Leaf && nodes[3].type == NodeType.Leaf)
			{
				ProcessIndexes(nodes, direction, indexes, tri_count, threshold);
			}
			else
			{
				for (int i = 0; i < 2; i++)
				{
					OctreeNode[] edge_nodes = new OctreeNode[4];

					for (int j = 0; j < 4; j++)
					{
						if (nodes[j].type == NodeType.Leaf)
							edge_nodes[j] = nodes[j];
						else
							edge_nodes[j] = nodes[j].children[Utilities.TEdgeProcEdgeMask[direction, i, j]];
					}

					ProcessEdge(edge_nodes, Utilities.TEdgeProcEdgeMask[direction, i, 4], indexes, tri_count, threshold);
				}
			}
		}

		public static void ProcessIndexes(OctreeNode[] nodes, int direction, List<int> indexes, List<int> tri_count, float threshold)
		{
			int min_size = 10000000;
			int min_index = 0;
			int[] indices = { -1, -1, -1, -1 };
			bool flip = false;
			bool sign_changed = false;
			int v_count = 0;
			//return;

			for (int i = 0; i < 4; i++)
			{
				int edge = Utilities.TProcessEdgeMask[direction, i];
				int c1 = Utilities.TEdgePairs[edge, 0];
				int c2 = Utilities.TEdgePairs[edge, 1];

				int m1 = (nodes[i].corners >> c1) & 1;
				int m2 = (nodes[i].corners >> c2) & 1;

				if (nodes[i].size < min_size)
				{
					min_size = nodes[i].size;
					min_index = i;
					flip = m1 == 1;
					sign_changed = ((m1 == 0 && m2 != 0) || (m1 != 0 && m2 == 0));
				}

				//if (!((m1 == 0 && m2 != 0) || (m1 != 0 && m2 == 0)))
				//	continue;

				//find the vertex index
				int index = 0;
				bool skip = false;
				if (nodes[i].corners == 179)
				{
				}
				for (int k = 0; k < 16; k++)
				{
					int e = Utilities.TransformedEdgesTable[nodes[i].corners, k];
					if (e == -1)
					{
						index++;
						continue;
					}
					if (e == -2)
					{
						skip = true;
						break;
					}
					if (e == edge)
						break;
				}

				if (skip)
					continue;
				if (nodes[i].index == 30733)
				{
				}

				v_count++;
				if (index >= nodes[i].vertices.Length)
					return;
				Vertex v = nodes[i].vertices[index];
				Vertex highest = v;
				while (highest.parent != null)
				{
					if (highest.parent.collapsible || (highest.parent.error <= threshold && (!EnforceManifold || (highest.parent.euler == 1 && highest.parent.face_prop2))))
						highest = v = highest.parent;
					else
						highest = highest.parent;
				}

				//Vector3 p = v.qef.Solve(1e-6f, 4, 1e-6f);
				indices[i] = v.index;
				if (v.index == -1)
				{
				}

				//sign_changed = true;
			}

			if (v_count > 0 && v_count < 4)
			{
			}

			/*
			 * Next generate the triangles.
			 * Because we're generating from the finest levels that were collapsed, many triangles will collapse to edges or vertices.
			 * That's why we check if the indices are different and discard the triangle, as mentioned in the paper.
			 */
			if (sign_changed)
			{
				int count = 0;
				if (!flip)
				{
					if (indices[0] != -1 && indices[1] != -1 && indices[2] != -1 && indices[0] != indices[1] && indices[1] != indices[3])
					{
						indexes.Add(indices[0]);
						indexes.Add(indices[1]);
						indexes.Add(indices[3]);
						count++;
					}

					if (indices[0] != -1 && indices[2] != -1 && indices[3] != -1 && indices[0] != indices[2] && indices[2] != indices[3])
					{
						indexes.Add(indices[0]);
						indexes.Add(indices[3]);
						indexes.Add(indices[2]);
						count++;
					}
				}
				else
				{
					if (indices[0] != -1 && indices[3] != -1 && indices[1] != -1 && indices[0] != indices[1] && indices[1] != indices[3])
					{
						indexes.Add(indices[0]);
						indexes.Add(indices[3]);
						indexes.Add(indices[1]);
						count++;
					}

					if (indices[0] != -1 && indices[2] != -1 && indices[3] != -1 && indices[0] != indices[2] && indices[2] != indices[3])
					{
						indexes.Add(indices[0]);
						indexes.Add(indices[2]);
						indexes.Add(indices[3]);
						count++;
					}
				}

				if (count > 0)
					tri_count.Add(count);
			}
		}

		public void ClusterCellBase(float error)
		{
			if (type != NodeType.Internal)
				return;

			for (int i = 0; i < 8; i++)
			{
				if (children[i] == null)
					continue;

				children[i].ClusterCell(error);
			}
		}

		public static Random rnd = new Random();

		/*
		 * Cell stage
		 */
		public void ClusterCell(float error)
		{
			if (type != NodeType.Internal)
				return;

			/*
			 * First cluster all the children nodes
			 */

			int[] signs = { -1, -1, -1, -1, -1, -1, -1, -1 };
			int mid_sign = -1;

			bool is_collapsible = true;
			for (int i = 0; i < 8; i++)
			{
				if (children[i] == null)
					continue;

				children[i].ClusterCell(error);
				if (children[i].type == NodeType.Internal) //Can't cluster if the child has children
					is_collapsible = false;
				else
				{
					mid_sign = (children[i].corners >> (7 - i)) & 1;
					signs[i] = (children[i].corners >> i) & 1;
				}
			}

			corners = 0;
			for (int i = 0; i < 8; i++)
			{
				if (signs[i] == -1)
					corners |= (byte)(mid_sign << i);
				else
					corners |= (byte)(signs[i] << i);
			}

			//if (!is_collapsible)
			//	return;

			int surface_index = 0;
			List<Vertex> collected_vertices = new List<Vertex>();
			List<Vertex> new_vertices = new List<Vertex>();



			if (index == 31681)
			{
			}
			if (index == 61440)
			{
			}
			if (index == 7715)
			{
			}

			/*
			 * Find all the surfaces inside the children that cross the 6 Euclidean edges and the vertices that connect to them
			 */
			for (int i = 0; i < 12; i++)
			{
				OctreeNode[] face_nodes = new OctreeNode[2];

				int c1 = Utilities.TEdgePairs[i, 0];
				int c2 = Utilities.TEdgePairs[i, 1];

				face_nodes[0] = children[c1];
				face_nodes[1] = children[c2];

				ClusterFace(face_nodes, Utilities.TEdgePairs[i, 2], ref surface_index, collected_vertices);
			}


			for (int i = 0; i < 6; i++)
			{
				OctreeNode[] edge_nodes = 
					{
						children[Utilities.TCellProcEdgeMask[i, 0]],
						children[Utilities.TCellProcEdgeMask[i, 1]],
						children[Utilities.TCellProcEdgeMask[i, 2]],
						children[Utilities.TCellProcEdgeMask[i, 3]]
					};
				if (size == 4 && i == 1)
				{
				}
				ClusterEdge(edge_nodes, Utilities.TCellProcEdgeMask[i, 4], ref surface_index, collected_vertices);
			}

			if (size == 16 && position.X == 0 && position.Y == 16 && position.Z == 16)
			{
			}
			if (index == 61440)
			{
			}

			int highest_index = surface_index;

			if (highest_index == -1)
				highest_index = 0;
			/*
			 * Gather the stray vertices
			 */
			foreach (OctreeNode n in children)
			{
				if (n == null)
					continue;
				foreach (Vertex v in n.vertices)
				{
					if (v == null)
						continue;
					if (v.surface_index == -1)
					{
						v.surface_index = highest_index++;
						collected_vertices.Add(v);
					}
				}
			}
			//GatherVertices(this, collected_vertices, ref highest_index);

			//if (surface_index == 0 && highest_index > 1)
			//	return;

			if (highest_index == 7)
			{
			}

			int clustered_count = 0;
			if (collected_vertices.Count > 0)
			{

				for (int i = 0; i <= highest_index; i++)
				{
					QEFProper.QEFSolver qef = new QEFProper.QEFSolver();
					Vector3 normal = Vector3.Zero;
					int count = 0;
					int[] edges = new int[12];
					int euler = 0;
					int e = 0;
					foreach (Vertex v in collected_vertices)
					{
						if (v.surface_index == i)
						{
							/*if (!v.qef.hasSolution)
								v.qef.Solve(1e-6f, 4, 1e-6f);
							if (v.qef.GetError() > error)
							{
								count = 0;
								break;
							}*/

							/* Calculate ei(Sv) */
							for (int k = 0; k < 3; k++)
							{
								int edge = Utilities.TExternalEdges[v.in_cell, k];
								edges[edge] += v.eis[edge];
							}
							/* Calculate e(Svk) */
							for (int k = 0; k < 9; k++)
							{
								int edge = Utilities.TInternalEdges[v.in_cell, k];
								e += v.eis[edge];
							}

							euler += v.euler;
							qef.Add(ref v.qef.data);
							normal += v.normal;
							count++;
						}
					}

					/*
					 * One vertex might have an error greater than the threshold, preventing simplification.
					 * When it's just one, we can ignore the error and proceed.
					 */
					if (count == 0)
					{
						continue;
					}

					bool face_prop2 = true;
					for (int f = 0; f < 6 && face_prop2; f++)
					{
						int intersections = 0;
						for (int ei = 0; ei < 4; ei++)
						{
							intersections += edges[Utilities.TFaces[f, ei]];
						}
						if (!(intersections == 0 || intersections == 2))
							face_prop2 = false;
					}

					Vertex new_vertex = new Vertex();
					normal /= (float)count;
					normal.Normalize();
					new_vertex.normal = normal;
					new_vertex.qef = qef;
					new_vertex.eis = edges;
					new_vertex.euler = euler - e / 4;
					if (new_vertex.euler != 1)
					{
					}
					new_vertex.in_cell = this.child_index;
					new_vertex.face_prop2 = face_prop2;
					if (face_prop2)
					{
					}
					if (new_vertex.euler != 1)
					{
					}
					new_vertices.Add(new_vertex);
					//new_vertex.index = rnd.Next();

					qef.Solve(1e-6f, 4, 1e-6f);
					float err = qef.GetError();
					new_vertex.collapsible = err <= error/* && new_vertex.euler == 1 && face_prop2*/;
					new_vertex.error = err;
					clustered_count++;

					if (count > 4)
					{
					}

					foreach (Vertex v in collected_vertices)
					{
						if (v.surface_index == i)
						{
							Vertex p = v;
							//p.surface_index = -1;
							/*while (p.parent != null)
							{
								p = p.parent;
								//p.surface_index = -1;
								if (p == p.parent)
								{
									p.parent = null;
									break;
								}
							}*/
							if (p != new_vertex)
								p.parent = new_vertex;
							else
								p.parent = null;
						}
					}
				}
			}
			else
			{
				return;
			}

			if (new_vertices.Count >= collected_vertices.Count)
			{
			}

			//if (clustered_count <= 0)
			{
				foreach (Vertex v2 in collected_vertices)
				{
					v2.surface_index = -1;
				}
			}

			//this.type = NodeType.Collapsed;
			//for (int i = 0; i < 8; i++)
			//	children[i] = null;
			this.vertices = new_vertices.ToArray();
		}

		public static void GatherVertices(OctreeNode n, List<Vertex> dest, ref int surface_index)
		{
			if (n == null)
				return;
			if (n.size > 1)
			{
				for (int i = 0; i < 8; i++)
					GatherVertices(n.children[i], dest, ref surface_index);
			}
			else
			{
				foreach (Vertex v in n.vertices)
				{
					if (v.surface_index == -1)
					{
						v.surface_index = surface_index++;
						dest.Add(v);
					}
				}
			}
		}

		public static void ClusterFace(OctreeNode[] nodes, int direction, ref int surface_index, List<Vertex> collected_vertices)
		{
			if (nodes[0] == null || nodes[1] == null)
				return;

			if (nodes[0].type != NodeType.Leaf || nodes[1].type != NodeType.Leaf)
			{
				for (int i = 0; i < 4; i++)
				{
					OctreeNode[] face_nodes = new OctreeNode[2];

					for (int j = 0; j < 2; j++)
					{
						if (nodes[j] == null)
							continue;
						if (nodes[j].type != NodeType.Internal)
							face_nodes[j] = nodes[j];
						else
							face_nodes[j] = nodes[j].children[Utilities.TFaceProcFaceMask[direction, i, j]];
					}

					ClusterFace(face_nodes, Utilities.TFaceProcFaceMask[direction, i, 2], ref surface_index, collected_vertices);
				}
			}

			int[,] orders =
				{
					{ 0, 0, 1, 1 },
					{ 0, 1, 0, 1 },
				};

			for (int i = 0; i < 4; i++)
			{
				OctreeNode[] edge_nodes = new OctreeNode[4];

				for (int j = 0; j < 4; j++)
				{
					if (nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]] == null)
						continue;
					if (nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]].type != NodeType.Internal)
						edge_nodes[j] = nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]];
					else
						edge_nodes[j] = nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]].children[Utilities.TFaceProcEdgeMask[direction, i, 1 + j]];
				}

				ClusterEdge(edge_nodes, Utilities.TFaceProcEdgeMask[direction, i, 5], ref surface_index, collected_vertices);
			}
		}

		public static void ClusterEdge(OctreeNode[] nodes, int direction, ref int surface_index, List<Vertex> collected_vertices)
		{
			if ((nodes[0] == null || nodes[0].type != NodeType.Internal) && (nodes[1] == null || nodes[1].type != NodeType.Internal) && (nodes[2] == null || nodes[2].type != NodeType.Internal) && (nodes[3] == null || nodes[3].type != NodeType.Internal))
			{
				ClusterIndexes(nodes, direction, ref surface_index, collected_vertices);
			}
			else
			{
				for (int i = 0; i < 2; i++)
				{
					OctreeNode[] edge_nodes = new OctreeNode[4];

					for (int j = 0; j < 4; j++)
					{
						if (nodes[j] == null)
							continue;
						if (nodes[j].type == NodeType.Leaf)
							edge_nodes[j] = nodes[j];
						else
							edge_nodes[j] = nodes[j].children[Utilities.TEdgeProcEdgeMask[direction, i, j]];
					}

					ClusterEdge(edge_nodes, Utilities.TEdgeProcEdgeMask[direction, i, 4], ref surface_index, collected_vertices);
				}
			}
		}

		public static void ClusterIndexes(OctreeNode[] nodes, int direction, ref int max_surface_index, List<Vertex> collected_vertices)
		{
			if (nodes[0] == null && nodes[1] == null && nodes[2] == null && nodes[3] == null)
				return;

			Vertex[] vertices = new Vertex[4];
			int v_count = 0;
			int node_count = 0;

			for (int i = 0; i < 4; i++)
			{
				if (nodes[i] == null)
					continue;
				if (nodes[i].size > 1)
				{
				}
				node_count++;
				if (nodes[i].vertices.Length > 1)
				{
				}

				int edge = Utilities.TProcessEdgeMask[direction, i];
				int c1 = Utilities.TEdgePairs[edge, 0];
				int c2 = Utilities.TEdgePairs[edge, 1];

				int m1 = (nodes[i].corners >> c1) & 1;
				int m2 = (nodes[i].corners >> c2) & 1;

				//if (!((m1 == 0 && m2 != 0) || (m1 != 0 && m2 == 0)))
				//	continue;

				//find the vertex index
				int index = 0;
				bool skip = false;
				for (int k = 0; k < 16; k++)
				{
					int e = Utilities.TransformedEdgesTable[nodes[i].corners, k];
					if (e == -1)
					{
						index++;
						continue;
					}
					if (e == -2)
					{
						if (!((m1 == 0 && m2 != 0) || (m1 != 0 && m2 == 0)))
							skip = true;
						break;
					}
					if (e == edge)
						break;
				}

				if (!skip && index < nodes[i].vertices.Length)
				{
					if (nodes[i].index == 30733)
					{
					}
					if (nodes[i].index == 12)
					{
					}
					vertices[i] = nodes[i].vertices[index];
					while (vertices[i].parent != null)
						vertices[i] = vertices[i].parent;
					if (i > v_count)
					{
					}
					v_count++;
				}
			}

			if (v_count == 0)
				return;
			if (node_count != v_count)
			{
			}

			if (!(vertices[0] != vertices[1] && vertices[1] != vertices[2] && vertices[2] != vertices[3]))
			{
				//return;
			}

			int surface_index = -1;

			for (int i = 0; i < 4; i++)
			{
				Vertex v = vertices[i];
				if (v == null)
					continue;
				//while (v != null)
				//{
				if (v.surface_index != -1)
				{
					if (surface_index != -1 && surface_index != v.surface_index)
					{
						AssignSurface(collected_vertices, v.surface_index, surface_index);
					}
					else if (surface_index == -1)
						surface_index = v.surface_index;
					//break;
				}

				//break;
				//v = v.parent;
				//}
			}

			if (surface_index == -1)
				surface_index = max_surface_index++;

			for (int i = 0; i < 4; i++)
			{
				Vertex v = vertices[i];
				if (v == null)
					continue;
				//while (v.parent != null)
				//	v = v.parent;
				//while (v != null)
				//{
				if (v.surface_index == -1)
				{
					collected_vertices.Add(v);
				}
				v.surface_index = surface_index;
				//v = v.parent;
				//}
			}
		}

		private static void AssignSurface(List<Vertex> vertices, int from, int to)
		{
			foreach (Vertex v in vertices)
			{
				if (v != null && v.surface_index == from)
					v.surface_index = to;
			}
		}
	}
}
