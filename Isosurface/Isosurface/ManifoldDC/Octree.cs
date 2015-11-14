using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

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
	}

	public class OctreeNode
	{
		public Vector3 position;
		public int size;
		public OctreeNode[] children;
		public NodeType type;
		public Vertex[] vertices;
		public byte corners;

		public OctreeNode()
		{
		}

		public OctreeNode(Vector3 position, int size, NodeType type)
		{
			this.position = position;
			this.size = size;
			this.type = type;
			this.children = new OctreeNode[8];
		}

		public void ConstructBase(int size, float error, ref List<VertexPositionColorNormal> vertices)
		{
			this.position = Vector3.Zero;
			this.size = size;
			this.type = NodeType.Internal;
			this.children = new OctreeNode[8];
			ConstructNodes(error, ref vertices);
		}

		public bool ConstructNodes(float error, ref List<VertexPositionColorNormal> vertices)
		{
			if (size == 1)
				return ConstructLeaf(error, ref vertices);

			type = NodeType.Internal;
			int child_size = size / 2;
			bool has_children = false;

			for (int i = 0; i < 8; i++)
			{
				Vector3 child_pos = Utilities.TCornerDeltas[i];
				children[i] = new OctreeNode(position + child_pos * (float)child_size, child_size, NodeType.Internal);

				bool b = children[i].ConstructNodes(error, ref vertices);
				if (b)
					has_children = true;
				else
					children[i] = null;
			}

			return has_children;
		}

		public bool ConstructLeaf(float error, ref List<VertexPositionColorNormal> vertices)
		{
			if (size != 1)
				return false;

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

			for (int i = 0; i < v_index; i++)
			{
				int k = 0;
				this.vertices[i] = new Vertex();
				this.vertices[i].qef = new QEFProper.QEFSolver();
				while(v_edges[i][k] != -1)
				{
					Vector3 a = position + Utilities.TCornerDeltas[Utilities.TEdgePairs[v_edges[i][k], 0]] * size;
					Vector3 b = position + Utilities.TCornerDeltas[Utilities.TEdgePairs[v_edges[i][k], 1]] * size;
					Vector3 intersection = Sampler.GetIntersection(a, b, samples[Utilities.TEdgePairs[v_edges[i][k], 0]], samples[Utilities.TEdgePairs[v_edges[i][k], 1]]);
					this.vertices[i].qef.Add(intersection - position, Sampler.GetNormal(intersection));
					k++;
				}

				this.vertices[i].index = vertices.Count;
				this.vertices[i].parent = null;
				this.vertices[i].collapsible = true;
				VertexPositionColorNormal vert = new VertexPositionColorNormal();
				vert.Position = this.vertices[i].qef.Solve(1e-6f, 4, 1e-6f) + position;
				vert.Normal = Sampler.GetNormal(vert.Position);
				vert.Color = new Color(vert.Normal * 0.5f + Vector3.One * 0.5f);
				vertices.Add(vert);
			}

			return true;
		}

		public void ProcessCell(List<int> indexes)
		{
			if (type == NodeType.Internal)
			{
				for (int i = 0; i < 8; i++)
				{
					if (children[i] != null)
						children[i].ProcessCell(indexes);
				}

				for (int i = 0; i < 12; i++)
				{
					OctreeNode[] face_nodes = new OctreeNode[2];

					int c1 = Utilities.TEdgePairs[i, 0];
					int c2 = Utilities.TEdgePairs[i, 1];

					face_nodes[0] = children[c1];
					face_nodes[1] = children[c2];

					ProcessFace(face_nodes, Utilities.TEdgePairs[i, 2], indexes);
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

					ProcessEdge(edge_nodes, Utilities.TCellProcEdgeMask[i, 4], indexes);
				}
			}
		}

		public static void ProcessFace(OctreeNode[] nodes, int direction, List<int> indexes)
		{
			if (nodes[0] == null || nodes[1] == null)
				return;

			if (nodes[0].type == NodeType.Internal || nodes[1].type == NodeType.Internal)
			{
				for (int i = 0; i < 4; i++)
				{
					OctreeNode[] face_nodes = new OctreeNode[2];

					for (int j = 0; j < 2; j++)
					{
						if (nodes[j].type != NodeType.Internal)
							face_nodes[j] = nodes[j];
						else
							face_nodes[j] = nodes[j].children[Utilities.TFaceProcFaceMask[direction, i, j]];
					}

					ProcessFace(face_nodes, Utilities.TFaceProcFaceMask[direction, i, 2], indexes);
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
						if (nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]].type == NodeType.Leaf || nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]].type == NodeType.Collapsed)
							edge_nodes[j] = nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]];
						else
							edge_nodes[j] = nodes[orders[Utilities.TFaceProcEdgeMask[direction, i, 0], j]].children[Utilities.TFaceProcEdgeMask[direction, i, 1 + j]];
					}

					ProcessEdge(edge_nodes, Utilities.TFaceProcEdgeMask[direction, i, 5], indexes);
				}
			}
		}

		public static void ProcessEdge(OctreeNode[] nodes, int direction, List<int> indexes)
		{
			if (nodes[0] == null || nodes[1] == null || nodes[2] == null || nodes[3] == null)
				return;

			if (nodes[0].type != NodeType.Internal && nodes[1].type != NodeType.Internal && nodes[2].type != NodeType.Internal && nodes[3].type != NodeType.Internal)
			{
				ProcessIndexes(nodes, direction, indexes);
			}
			else
			{
				for (int i = 0; i < 2; i++)
				{
					OctreeNode[] edge_nodes = new OctreeNode[4];

					for (int j = 0; j < 4; j++)
					{
						if (nodes[j].type == NodeType.Leaf || nodes[j].type == NodeType.Collapsed)
							edge_nodes[j] = nodes[j];
						else
							edge_nodes[j] = nodes[j].children[Utilities.TEdgeProcEdgeMask[direction, i, j]];
					}

					ProcessEdge(edge_nodes, Utilities.TEdgeProcEdgeMask[direction, i, 4], indexes);
				}
			}
		}

		public static void ProcessIndexes(OctreeNode[] nodes, int direction, List<int> indexes)
		{
			int min_size = 10000000;
			int min_index = 0;
			int[] indices = { -1, -1, -1, -1 };
			bool flip = false;
			bool sign_changed = false;

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

				//find the vertex index
				int index = 0;
				for (int k = 0; k < 16; k++)
				{
					int e = Utilities.TransformedEdgesTable[nodes[i].corners, k];
					if (e == -1)
					{
						index++;
						continue;
					}
					if (e == -2)
						break;
					if (e == edge)
						break;
				}

				indices[i] = nodes[i].vertices[index].index;

				//sign_changed = true;
			}

			if (sign_changed)
			{
				if (!flip)
				{
					indexes.Add(indices[0]);
					indexes.Add(indices[1]);
					indexes.Add(indices[3]);

					indexes.Add(indices[0]);
					indexes.Add(indices[3]);
					indexes.Add(indices[2]);
				}
				else
				{
					indexes.Add(indices[0]);
					indexes.Add(indices[3]);
					indexes.Add(indices[1]);

					indexes.Add(indices[0]);
					indexes.Add(indices[2]);
					indexes.Add(indices[3]);
				}
			}
		}

		public void ClusterVertices(float error)
		{
			if (type != NodeType.Internal)
				return;

			for (int i = 0; i < 8; i++)
			{
				if (children[i] == null)
					continue;

			}
		}
	}
}
