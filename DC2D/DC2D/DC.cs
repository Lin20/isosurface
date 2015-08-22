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
	public class DC
	{
		GraphicsDevice device;
		DynamicVertexBuffer buffer;
		DynamicVertexBuffer outline_buffer;
		DynamicIndexBuffer indexes;
		float[,] map;
		int resolution;
		int size;
		int vertex_location;
		int outline_location;
		int index_location;
		Vector2[,] vertices;
		int[,] vertex_indexes;
		Random rnd = new Random();

		int[,] edges = new int[,] { { 0, 2 }, { 1, 3 }, { 0, 1 }, { 2, 3 } };
		Vector2[] deltas = new Vector2[] { new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };

		public DC(GraphicsDevice device, int resolution, int size)
		{
			this.device = device;
			this.resolution = resolution;
			this.size = size;
			map = new float[resolution, resolution];
			vertices = new Vector2[resolution, resolution];
			vertex_indexes = new int[resolution, resolution];

			buffer = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, 65536, BufferUsage.None);
			outline_buffer = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, 65536, BufferUsage.None);
			indexes = new DynamicIndexBuffer(device, IndexElementSize.ThirtyTwoBits, 65536, BufferUsage.None);
			InitData();
		}

		private void InitData()
		{
			for (int x = 0; x < resolution; x++)
			{
				for (int y = 0; y < resolution; y++)
				{
					//map[x, y] = Circle(new Vector2(x, y));
					map[x, y] = Sample(new Vector2(x, y));
					//map[x, y] = Math.Max(Cuboid(new Vector2(x - 1, y - 1)), Cuboid(new Vector2(x + 12, y + 12)));
					//map[x, y] = Noise(new Vector2(x, y));
				}
			}
		}

		public void Contour()
		{
			vertex_location = 1;
			index_location = 0;
			outline_location = 0;
			for (int x = 1; x < resolution - 1; x++)
			{
				for (int y = 1; y < resolution - 1; y++)
				{
					GenerateAt(x, y);
				}
			}

			for (int x = 1; x < resolution - 1; x++)
			{
				for (int y = 1; y < resolution - 1; y++)
				{
					GenerateIndexAt(x, y);
				}
			}
		}

		public void GenerateAt(int x, int y)
		{
			int corners = 0;
			for (int i = 0; i < 4; i++)
			{
				if (map[x + i / 2, y + i % 2] < 0)
					corners |= 1 << i;
			}

			if (corners == 0 || corners == 15)
				return;

			VertexPositionColor[] vs = new VertexPositionColor[10];
			Color c = Color.LightSteelBlue;
			vs[0] = new VertexPositionColor(new Vector3((x + 0) * size, (y + 0) * size, 0), c);
			vs[1] = new VertexPositionColor(new Vector3((x + 1) * size, (y + 0) * size, 0), c);
			vs[2] = new VertexPositionColor(new Vector3((x + 1) * size, (y + 0) * size, 0), c);
			vs[3] = new VertexPositionColor(new Vector3((x + 1) * size, (y + 1) * size, 0), c);
			vs[4] = new VertexPositionColor(new Vector3((x + 1) * size, (y + 1) * size, 0), c);
			vs[5] = new VertexPositionColor(new Vector3((x + 0) * size, (y + 1) * size, 0), c);
			vs[6] = new VertexPositionColor(new Vector3((x + 0) * size, (y + 1) * size, 0), c);
			vs[7] = new VertexPositionColor(new Vector3((x + 0) * size, (y + 0) * size, 0), c);
			outline_buffer.SetData<VertexPositionColor>(outline_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 8, VertexPositionColor.VertexDeclaration.VertexStride);
			outline_location += 8;

			QEF qef = new QEF();
			for (int i = 0; i < 4; i++)
			{
				int c1 = edges[i, 0];
				int c2 = edges[i, 1];

				int m1 = (corners >> c1) & 1;
				int m2 = (corners >> c2) & 1;
				if (m1 == m2)
					continue;

				float d1 = map[x + c1 / 2, y + c1 % 2];
				float d2 = map[x + c2 / 2, y + c2 % 2];

				Vector2 p1 = new Vector2((float)((c1 / 2)), (float)((c1 % 2)));
				Vector2 p2 = new Vector2((float)((c2 / 2)), (float)((c2 % 2)));

				Vector2 intersection = GetIntersection(p1, p2, d1, d2);
				Vector2 normal = GetNormal(intersection + new Vector2(x, y));//GetNormal(x, y);

				qef.Add(intersection, normal);
			}

			vertices[x, y] = qef.Solve2(0, 16, 0);

			Vector2 p = vertices[x, y];
			vs[0] = new VertexPositionColor(new Vector3((p.X + x) * size, (p.Y + y) * size, 0), Color.Black);
			buffer.SetData<VertexPositionColor>(vertex_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 1, VertexPositionColor.VertexDeclaration.VertexStride);
			vertex_indexes[x, y] = vertex_location;
			vertex_location++;
			/*vs[0] = new VertexPositionColor(new Vector3((x + 0) * size, (y + 0) * size, 0), Color.Black);
			vs[1] = new VertexPositionColor(new Vector3((x + 1) * size, (y + 0) * size, 0), Color.Black);
			vs[2] = new VertexPositionColor(new Vector3((x + 1) * size, (y + 0) * size, 0), Color.Black);
			vs[3] = new VertexPositionColor(new Vector3((x + 1) * size, (y + 1) * size, 0), Color.Black);
			vs[4] = new VertexPositionColor(new Vector3((x + 1) * size, (y + 1) * size, 0), Color.Black);
			vs[5] = new VertexPositionColor(new Vector3((x + 0) * size, (y + 1) * size, 0), Color.Black);
			vs[6] = new VertexPositionColor(new Vector3((x + 0) * size, (y + 1) * size, 0), Color.Black);
			vs[7] = new VertexPositionColor(new Vector3((x + 0) * size, (y + 0) * size, 0), Color.Black);

			vs[8] = new VertexPositionColor(new Vector3((p.X + x) * size, (p.Y + y) * size, 0), Color.Black);
			vs[9] = new VertexPositionColor(new Vector3((p.X + x + .1f) * size, (p.Y + y + .1f) * size, 0), Color.Black);
			index = 10;

			buffer.SetData<VertexPositionColor>(vertex_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, index, VertexPositionColor.VertexDeclaration.VertexStride);
			vertex_location += index;*/
		}

		public void GenerateIndexAt(int x, int y)
		{
			//int corners = 0;

			int[] indices = new int[8];

			int index = 0;
			for (int i = 0; i < 4; i++)
			{
				int c1 = edges[i, 0];
				int c2 = edges[i, 1];

				int v1 = vertex_indexes[x + c1 / 2, y + c1 % 2];
				int v2 = vertex_indexes[x + c2 / 2, y + c2 % 2];
				if (v1 == 0 || v2 == 0)
					continue;

				indices[index++] = v1;
				indices[index++] = v2;
			}

			if (index > 0)
				indexes.SetData<int>(index_location * 4, indices, 0, index);
			index_location += index;
		}

		private Vector2 GetIntersection(Vector2 p1, Vector2 p2, float d1, float d2)
		{
			//do a simple linear interpolation
			return p1 + (-d1) * (p2 - p1) / (d2 - d1);
		}

		private float Circle(Vector2 pos)
		{
			float radius = (float)resolution / 4.0f;
			Vector2 origin = new Vector2(resolution / 2, resolution / 2);
			return (pos - origin).Length() - radius;
		}

		float Cuboid(Vector2 pos)
		{
			float radius = (float)resolution / 8.0f;
			Vector2 local = pos - new Vector2(resolution / 2, resolution / 2);
			Vector2 d = new Vector2(Math.Abs(local.X), Math.Abs(local.Y)) - new Vector2(radius, radius);
			float m = Math.Max(d.X, d.Y);
			Vector2 max = Vector2.Max(d, Vector2.Zero);
			return Math.Min(m, max.Length());
		}

		float Sample(Vector2 pos)
		{
			//return sdTorus88(pos);
			return Math.Min(Circle(pos), Cuboid(pos - new Vector2(12, 12)));
			//return Circle(pos);
			//return Cuboid(pos);
		}

		float Noise(Vector2 pos)
		{
			double d = pos.Y - Math.Sin((pos.X * 0.34172f + pos.X * 0.23111 + pos.X * pos.X) * 0.01f) * 16.0f - 32;
			return (float)d;
		}

		float sdTorus88(Vector2 pos)
		{
			Vector2 t = new Vector2(resolution / 4, resolution / 4);
			Vector2 q = new Vector2(pos.X - resolution / 2 - t.X, pos.Y - resolution / 2);
			return q.Length() - t.Y;
		}

		private Vector2 GetNormal(int x, int y)
		{
			//can't compute gradient
			if (x == 0 || y == 0 || x == resolution - 1 || y == resolution - 1)
				return Vector2.Zero;

			Vector2 gradient = new Vector2(map[x + 1, y] - map[x - 1, y], map[x, y + 1] - map[x, y - 1]);
			gradient.Normalize();
			return gradient;
		}

		private Vector2 GetNormal(Vector2 v)
		{
			//can't compute gradient
			float h = 0.001f;
			float dxp = Sample(new Vector2(v.X + h, v.Y));
			float dxm = Sample(new Vector2(v.X - h, v.Y));
			float dyp = Sample(new Vector2(v.X, v.Y + h));
			float dym = Sample(new Vector2(v.X,  v.Y - h));
			//Vector2 gradient = new Vector2(map[x + 1, y] - map[x - 1, y], map[x, y + 1] - map[x, y - 1]);
			Vector2 gradient = new Vector2(dxp - dxm, dyp - dym);
			gradient.Normalize();
			return gradient;
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
			device.Indices = indexes;
			//device.DrawPrimitives(PrimitiveType.LineList, 0, vertex_location / 2);
			device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, vertex_location, 0, index_location / 2);
		}
	}
}
