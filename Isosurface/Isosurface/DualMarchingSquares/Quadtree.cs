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

namespace Isosurface.DualMarchingSquares
{
	public class QuadtreeNode
	{
		public QuadtreeNode()
		{
			//type = QuadtreeNodeType.None;
			position = Vector2.Zero;
			size = 0;
			children = new QuadtreeNode[4];
			vertex_index = -1;
			//draw_info = new QuadtreeDrawInfo();
		}

		//public QuadtreeNodeType type;
		public Vector2 position;
		public int size;
		public QuadtreeNode[] children; //Z order
		public Vector2 dualgrid_pos;
		public float isovalue;
		public Vector2 normal;
		public bool leaf;
		public int vertex_index;
		public int index;
		//public QuadtreeDrawInfo draw_info;

		private static int[,] edges = new int[,] { { 0, 2 }, { 1, 3 }, { 0, 1 }, { 2, 3 } };
		private static int[, ,] edge_children = new int[,,]
		{
			{ { 2, 0 }, { 3, 1 } },
			{ { 1, 0 }, { 3, 2 } }
		};
		private static int[, ,] vertex_children = new int[,,]
		{
			{ { 2, 3 }, { 0, 1 } },
			{ { 1, 3 }, { 0, 2 } }
		};
		private static int[] middle_points = { 3, 2, 1, 0 };
		private static int[] connect_points = { 0, 2, 3, 1 };

		public void Build(int size, int min_size, float threshold, int grid_size, List<VertexPositionColorNormal> vertices)
		{
			this.size = size;
			position = Vector2.Zero;
			leaf = false;
			TrySplit(min_size, threshold, grid_size, vertices);
		}

		public void TrySplit(int min_size, float threshold, int grid_size, List<VertexPositionColorNormal> vertices)
		{
			float minSplitDistanceDiagonalFactor = 1000.0f;
			if (Sampler.Sample(position + Vector2.One * size * 0.5f) > size * (float)Math.Sqrt(2) * minSplitDistanceDiagonalFactor || size <= min_size || GetError(threshold) < threshold)
			{
				dualgrid_pos = 0.5f * size * Vector2.One;
				isovalue = Sampler.Sample(dualgrid_pos);
				normal = Sampler.GetNormal(dualgrid_pos);
				vertex_index = vertices.Count;
				leaf = true;

				Color n_c = new Color(210, 220, 210);
				vertices.Add(new VertexPositionColorNormal(new Vector3(position * grid_size + dualgrid_pos * grid_size, 0), n_c, new Vector3(normal, 0)));
				return;
			}

			for (int i = 0; i < 4; i++)
			{
				children[i] = new QuadtreeNode();
				children[i].index = i;
				children[i].position = position + (float)(size / 2) * new Vector2(i / 2, i % 2);
				children[i].size = size / 2;
				children[i].TrySplit(min_size, threshold, grid_size, vertices);
			}
		}

		public float GetError(float threshold)
		{
			float error = 0;

			float[] values = new float[4];
			Vector2[] positions = new Vector2[5];
			Vector2[] middle_positions = 
			{
				new Vector2(0.5f, 0.0f),
				new Vector2(0.0f, 0.5f),
				new Vector2(0.5f, 0.5f),
				new Vector2(1.0f, 0.5f),
				new Vector2(0.5f, 1.0f)
			};

			for (int i = 0; i < 5; i++)
			{
				positions[i] = position + size * middle_positions[i];
				if (i < 4)
					values[i] = Sampler.Sample(position + size * new Vector2(i / 2, i % 2));
			}

			for (int i = 0; i < 5; i++)
			{
				float center_value = Sampler.Sample(positions[i]);
				Vector2 gradient = Sampler.GetGradient(positions[i]);

				float interpolated = Interpolate(values[0], values[1], values[2], values[3], middle_positions[i]);
				float mag = Math.Max(1.0f, gradient.Length());

				error += Math.Abs(center_value - interpolated) / mag;
				if (error >= threshold)
					break;
			}

			return error;
		}

		private static float Interpolate(float f00, float f01, float f10, float f11, Vector2 position)
		{
			float m_x = 1.0f - position.X;
			float m_y = 1.0f - position.Y;

			return f00 * m_x * m_y +
				f01 * m_x * position.Y +
				f10 * position.X * m_y +
				f11 * position.X * position.Y;
		}

		public static void ProcessFace(QuadtreeNode q1, List<int> indices, List<Cell> cells)
		{
			if (q1 == null || q1.leaf)
				return;

			for (int i = 0; i < 4; i++)
			{
				ProcessFace(q1.children[i], indices, cells);
			}

			for (int i = 0; i < 4; i++)
			{
				QuadtreeNode c1 = q1.children[edges[i, 0]];
				QuadtreeNode c2 = q1.children[edges[i, 1]];

				ProcessEdge(c1, c2, i / 2, indices, cells);
			}

			ProcessVertices(q1.children[0], q1.children[1], q1.children[2], q1.children[3], indices, cells);
		}

		public static void ProcessEdge(QuadtreeNode q1, QuadtreeNode q2, int edge, List<int> indices, List<Cell> cells)
		{
			if (q1.leaf && q2.leaf)
				return;

			for (int i = 0; i < 2; i++)
			{
				QuadtreeNode n1 = (q1.leaf ? q1 : q1.children[edge_children[edge, i, 0]]);
				QuadtreeNode n2 = (q2.leaf ? q2 : q2.children[edge_children[edge, i, 1]]);
				ProcessEdge(n1, n2, edge, indices, cells);
			}

			if (edge == 0)
				ProcessVertices((q1.leaf ? q1 : q1.children[vertex_children[edge, 0, 0]]), (q1.leaf ? q1 : q1.children[vertex_children[edge, 0, 1]]), (q2.leaf ? q2 : q2.children[vertex_children[edge, 1, 0]]), (q2.leaf ? q2 : q2.children[vertex_children[edge, 1, 1]]), indices, cells);
			else
				ProcessVertices((q1.leaf ? q1 : q1.children[vertex_children[edge, 0, 0]]), (q2.leaf ? q2 : q2.children[vertex_children[edge, 1, 0]]), (q1.leaf ? q1 : q1.children[vertex_children[edge, 0, 1]]), (q2.leaf ? q2 : q2.children[vertex_children[edge, 1, 1]]), indices, cells);
		}

		public static void ProcessVertices(QuadtreeNode q1, QuadtreeNode q2, QuadtreeNode q3, QuadtreeNode q4, List<int> indices, List<Cell> cells)
		{
			if (q1 == null || q2 == null || q3 == null || q4 == null)
				return;

			QuadtreeNode[] children = { q1, q2, q3, q4 };
			QuadtreeNode[] leafs = new QuadtreeNode[4];

			if (!children[0].leaf || !children[1].leaf || !children[2].leaf || !children[3].leaf)
			{
				for (int i = 0; i < 4; i++)
				{
					if (children[i] == null) //shouldn't happen
						return;
					if (children[i].leaf)
						leafs[i] = children[i];
					else
						leafs[i] = children[i].children[middle_points[i]];
				}

				ProcessVertices(leafs[0], leafs[1], leafs[2], leafs[3], indices, cells);
			}
			else
			{
				Cell c = new Cell();
				for (int i = 0; i < 4; i++)
				{
					indices.Add(children[connect_points[i]].vertex_index);
					indices.Add(children[connect_points[(i + 1) % 4]].vertex_index);
					c.Positions[i] = children[i].dualgrid_pos + children[i].position;
					c.Values[i] = children[i].isovalue;
					c.Normals[i] = children[i].normal;
				}
				cells.Add(c);
			}
		}
	}
}
