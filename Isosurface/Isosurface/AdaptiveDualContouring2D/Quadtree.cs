/* This is a 2D Adaptive Dual Contouring implementation
 * Dual vertices are placed where they have the smallest error
 * The current QEF solver uses a bruteforce solution, which is slow and doesn't result in the best-looking meshes
 * But for the most part, it preserves sharp features and smooths round ones
 * There is currently a connectivity issue that needs to be fixed
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

namespace Isosurface.AdaptiveDualContouring2D
{
	public enum QuadtreeNodeType
	{
		None,
		Internal,
		Pseudo,
		Leaf
	}

	public class QuadtreeDrawInfo
	{
		public QuadtreeDrawInfo()
		{
			index = -1;
			corners = 0;
		}

		public int index;
		public int corners;
		public Vector2 position;
		public Vector3 averageNormal;
		QEF qef;
	}

	public class QuadtreeNode
	{
		public QuadtreeNode()
		{
			type = QuadtreeNodeType.None;
			position = Vector2.Zero;
			size = 0;
			children = new QuadtreeNode[4];
			draw_info = new QuadtreeDrawInfo();
		}

		public QuadtreeNodeType type;
		public Vector2 position;
		public int size;
		public QuadtreeNode[] children; //Z order
		public QuadtreeDrawInfo draw_info;

		/* These tables have an error in them somewhere that leads to cells connecting that shouldn't
		 * Or rather, cells that don't actually exhibit sign changes but get stored as such
		 * TODO: Fix
		 */
		private static int[,] edges = new int[,] { { 0, 2 }, { 1, 3 }, { 0, 1 }, { 2, 3 } };
		private static int[, ,] edge_mask = new int[,,] { { { 2, 0 }, { 3, 1 } }, { { 1, 0 }, { 3, 2 } } };
		private static int[,] process_edge_mask = new int[,] { { 0, 2 }, { 1, 3 } };

		public int Build(Vector2 min, int size, float threshold, List<VertexPositionColorNormal> vertices, int grid_size)
		{
			this.position = min;
			this.size = size;
			this.type = QuadtreeNodeType.Internal;
			int v_index = 0;
			ConstructNodes(ref v_index, vertices, grid_size);
			return v_index;
		}

		public bool ConstructNodes(ref int v_index, List<VertexPositionColorNormal> vertices, int grid_size)
		{
			if (size == 1)
			{
				return ConstructLeaf(ref v_index, vertices, grid_size);
			}

			int child_size = size / 2;
			bool has_children = false;
			for (int i = 0; i < 4; i++)
			{
				Vector2 offset = new Vector2(i / 2, i % 2);
				QuadtreeNode child = new QuadtreeNode();
				child.size = child_size;
				child.position = position + offset * (float)child_size;
				child.type = QuadtreeNodeType.Internal;

				if (child.ConstructNodes(ref v_index, vertices, grid_size))
					has_children = true;
				children[i] = child;
			}

			if (!has_children)
			{
				type = QuadtreeNodeType.Leaf;
				return false;
			}

			type = QuadtreeNodeType.Internal;
			return true;
		}

		public bool ConstructLeaf(ref int v_index, List<VertexPositionColorNormal> vertices, int grid_size)
		{
			int corners = 0;
			float[,] samples = new float[2, 2];
			for (int i = 0; i < 4; i++)
			{
				if ((samples[i / 2, i % 2] = Sampler.Sample(position + new Vector2(i / 2, i % 2))) < 0)
					corners |= 1 << i;
			}

			if (corners == 0 || corners == 15)
				return false;

			QEF qef = new QEF();
			Vector3 average_normal = new Vector3();
			for (int i = 0; i < 4; i++)
			{
				int c1 = Sampler.Edges[i, 0];
				int c2 = Sampler.Edges[i, 1];

				int m1 = (corners >> c1) & 1;
				int m2 = (corners >> c2) & 1;
				if (m1 == m2)
					continue;

				float d1 = samples[c1 / 2, c1 % 2];
				float d2 = samples[c2 / 2, c2 % 2];

				Vector2 p1 = new Vector2((float)((c1 / 2)), (float)((c1 % 2)));
				Vector2 p2 = new Vector2((float)((c2 / 2)), (float)((c2 % 2)));

				Vector2 intersection = Sampler.GetIntersection(p1, p2, d1, d2);
				Vector2 normal = Sampler.GetNormal(intersection + position);//GetNormal(x, y);
				average_normal += new Vector3(normal, 0);

				qef.Add(intersection, normal);
			}

			average_normal /= (float)qef.Intersections.Count;
			average_normal.Normalize();

			draw_info = new QuadtreeDrawInfo();
			draw_info.position = qef.Solve2(0, 0, 0);
			draw_info.averageNormal = average_normal;
			draw_info.corners = corners;
			draw_info.index = v_index++;
			Color n_c = new Color(average_normal * 0.5f + Vector3.One * 0.5f);
			vertices.Add(new VertexPositionColorNormal(new Vector3(position * grid_size + draw_info.position * size * grid_size, 0), n_c, average_normal));

			type = QuadtreeNodeType.Leaf;
			return true;
		}

		/* The code for generating the contour
		 * It has connectivity issues due to table errors
		 * TODO: Fix
		 */
		public void ProcessFace(List<int> indexes)
		{
			if (type == QuadtreeNodeType.Internal)
			{
				for (int i = 0; i < 4; i++)
				{
					if (children[i] != null)
						children[i].ProcessFace(indexes);
				}

				for (int i = 0; i < 4; i++)
				{
					QuadtreeNode c1 = children[edges[i, 0]];
					QuadtreeNode c2 = children[edges[i, 1]];

					if (c1 == null || c2 == null)
						continue;

					ProcessEdge(c1, c2, i / 2, indexes);
				}
			}
		}

		public static void ProcessEdge(QuadtreeNode node1, QuadtreeNode node2, int direction, List<int> indexes)
		{
			if (node1 == null || node2 == null)
				return;
			if (node1.size != 1 || node2.size != 1 || node1.draw_info.index == -1 || node2.draw_info.index == -1)
			{
				QuadtreeNode leaf1;
				QuadtreeNode leaf2;

				for (int i = 0; i < 2; i++)
				{
					if (node1.size == 1 && node1.draw_info.index != -1)
						leaf1 = node1;
					else
					{
						int c = edge_mask[direction, i, 0];
						leaf1 = node1.children[c];
					}

					if (node2.size == 1 && node2.draw_info.index != -1)
						leaf2 = node2;
					else
					{
						int c = edge_mask[direction, i, 1];
						leaf2 = node2.children[c];
					}

					ProcessEdge(leaf1, leaf2, direction, indexes);
				}
			}
			else
			{
				if (node1.draw_info.index == -1 || node2.draw_info.index == -1)
				{
					return;
				}

				int min_size = 100000;
				bool sign_change = false;
				QuadtreeNode[] nodes = new QuadtreeNode[] { node1, node2 };

				for (int i = 0; i < 2; i++)
				{
					int edge = process_edge_mask[direction, i];
					int c1 = edges[edge, 0];
					int c2 = edges[edge, 1];

					int m1 = (nodes[i].draw_info.corners >> c1) & 1;
					int m2 = (nodes[i].draw_info.corners >> c2) & 1;
					
				/* We have connectivity issues, so sign change is improperly set to favor connectivity
				 * As a result, cells that shouldn't connect do
				 * TODO: Fix
				 */
					//if (nodes[i].size <= min_size)
					{
						min_size = nodes[i].size;
						if (!sign_change)
							sign_change = m1 != m2;
					}
				}

				if (sign_change)
				{
					indexes.Add(node1.draw_info.index);
					indexes.Add(node2.draw_info.index);
				}
			}
		}
	}
}
