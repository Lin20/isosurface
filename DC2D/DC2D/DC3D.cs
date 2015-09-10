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
	public class DC3D
	{
		GraphicsDevice device;
		DynamicVertexBuffer buffer;
		DynamicVertexBuffer outline_buffer;
		DynamicIndexBuffer indexes;
		float[, ,] map;
		int resolution;
		int size;
		int vertex_location;
		int outline_location;
		int index_location;
		Vector3[, ,] vertices;
		int[, ,] vertex_indexes;
		Random rnd = new Random();

		int[,] edges =
		{
			{0,4},{1,5},{2,6},{3,7},	// x-axis 
			{0,2},{1,3},{4,6},{5,7},	// y-axis
			{0,1},{2,3},{4,5},{6,7}		// z-axis
		};

		int[,] dirs = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

		public DC3D(GraphicsDevice device, int resolution, int size)
		{
			this.device = device;
			this.resolution = resolution;
			this.size = size;
			map = new float[resolution, resolution, resolution];
			vertices = new Vector3[resolution, resolution, resolution];
			vertex_indexes = new int[resolution, resolution, resolution];

			buffer = new DynamicVertexBuffer(device, VertexPositionColorNormal.VertexDeclaration, 65536, BufferUsage.None);
			outline_buffer = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, 1000000, BufferUsage.None);
			indexes = new DynamicIndexBuffer(device, IndexElementSize.ThirtyTwoBits, 1000000, BufferUsage.None);
			InitData();
		}

		private void InitData()
		{
			for (int x = 0; x < resolution; x++)
			{
				for (int y = 0; y < resolution; y++)
				{
					for (int z = 0; z < resolution; z++)
					{
						//map[x, y] = Circle(new Vector3(x, y));
						map[x, y, z] = Sample(new Vector3(x, y, z));
						//map[x, y] = Math.Max(Cuboid(new Vector3(x - 1, y - 1)), Cuboid(new Vector3(x + 12, y + 12)));
						//map[x, y] = Noise(new Vector3(x, y));
					}
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
					for (int z = 1; z < resolution - 1; z++)
					{
						GenerateAt(x, y, z);
					}
				}
			}

			for (int x = 1; x < resolution - 1; x++)
			{
				for (int y = 1; y < resolution - 1; y++)
				{
					for (int z = 1; z < resolution - 1; z++)
					{
						GenerateIndexAt(x, y, z);
					}
				}
			}
		}

		public void GenerateAt(int x, int y, int z)
		{
			int corners = 0;
			for (int i = 0; i < 8; i++)
			{
				if (map[x + i / 4, y + i % 4 / 2, z + i % 2] < 0)
					corners |= 1 << i;
			}

			if (corners == 0 || corners == 255)
				return;

			VertexPositionColor[] vs = new VertexPositionColor[24];
			Color c = Color.LightSteelBlue;
			vs[0] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 0)), c);
			vs[1] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 0)), c);
			vs[2] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 0)), c);
			vs[3] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 0)), c);
			vs[4] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 0)), c);
			vs[5] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 0)), c);
			vs[6] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 0)), c);
			vs[7] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 0)), c);

			vs[8] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 1)), c);
			vs[9] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 1)), c);
			vs[10] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 1)), c);
			vs[11] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 1)), c);
			vs[12] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 1)), c);
			vs[13] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 1)), c);
			vs[14] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 1)), c);
			vs[15] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 1)), c);

			vs[16] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 0)), c);
			vs[17] = new VertexPositionColor(new Vector3((x + 0), (y + 0), (z + 1)), c);
			vs[18] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 0)), c);
			vs[19] = new VertexPositionColor(new Vector3((x + 0), (y + 1), (z + 1)), c);

			vs[20] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 0)), c);
			vs[21] = new VertexPositionColor(new Vector3((x + 1), (y + 0), (z + 1)), c);
			vs[22] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 0)), c);
			vs[23] = new VertexPositionColor(new Vector3((x + 1), (y + 1), (z + 1)), c);

			outline_buffer.SetData<VertexPositionColor>(outline_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 24, VertexPositionColor.VertexDeclaration.VertexStride);
			outline_location += 24;

			QEF3D qef = new QEF3D();
			Vector3 average_normal = new Vector3();
			for (int i = 0; i < 12; i++)
			{
				int c1 = edges[i, 0];
				int c2 = edges[i, 1];

				int m1 = (corners >> c1) & 1;
				int m2 = (corners >> c2) & 1;
				if (m1 == m2)
					continue;

				float d1 = map[x + c1 / 4, y + c1 % 4 / 2, z + c1 % 2];
				float d2 = map[x + c2 / 4, y + c2 % 4 / 2, z + c2 % 2];

				Vector3 p1 = new Vector3((float)((c1 / 4)), (float)((c1 % 4 / 2)), (float)((c1 % 2)));
				Vector3 p2 = new Vector3((float)((c2 / 4)), (float)((c2 % 4 / 2)), (float)((c2 % 2)));

				Vector3 intersection = GetIntersection(p1, p2, d1, d2);
				Vector3 normal = GetNormal(intersection + new Vector3(x, y, z));//GetNormal(x, y);
				average_normal += normal;

				qef.Add(intersection, normal);
			}

			vertices[x, y, z] = qef.Solve2(0, 16, 0);

			Vector3 n = average_normal / (float)qef.Intersections.Count;
			Vector3 p = vertices[x, y, z];
			VertexPositionColorNormal[] v2 = new VertexPositionColorNormal[1];
			v2[0] = new VertexPositionColorNormal(new Vector3((p.X + x), (p.Y + y), (p.Z + z)), Color.LightGreen, n);
			buffer.SetData<VertexPositionColorNormal>(vertex_location * VertexPositionColorNormal.VertexDeclaration.VertexStride, v2, 0, 1, VertexPositionColorNormal.VertexDeclaration.VertexStride);
			vertex_indexes[x, y, z] = vertex_location;
			vertex_location++;
			/*vs[0] = new VertexPositionColor(new Vector3((x + 0) , (y + 0) , 0), Color.Black);
			vs[1] = new VertexPositionColor(new Vector3((x + 1) , (y + 0) , 0), Color.Black);
			vs[2] = new VertexPositionColor(new Vector3((x + 1) , (y + 0) , 0), Color.Black);
			vs[3] = new VertexPositionColor(new Vector3((x + 1) , (y + 1) , 0), Color.Black);
			vs[4] = new VertexPositionColor(new Vector3((x + 1) , (y + 1) , 0), Color.Black);
			vs[5] = new VertexPositionColor(new Vector3((x + 0) , (y + 1) , 0), Color.Black);
			vs[6] = new VertexPositionColor(new Vector3((x + 0) , (y + 1) , 0), Color.Black);
			vs[7] = new VertexPositionColor(new Vector3((x + 0) , (y + 0) , 0), Color.Black);

			vs[8] = new VertexPositionColor(new Vector3((p.X + x) , (p.Y + y) , 0), Color.Black);
			vs[9] = new VertexPositionColor(new Vector3((p.X + x + .1f) , (p.Y + y + .1f) , 0), Color.Black);
			index = 10;

			buffer.SetData<VertexPositionColor>(vertex_location * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, index, VertexPositionColor.VertexDeclaration.VertexStride);
			vertex_location += index;*/
		}

		public void GenerateIndexAt(int x, int y, int z)
		{
			//int corners = 0;

			int v1 = vertex_indexes[x, y, z];
			if (v1 == 0)
				return;

			int[] indices = new int[256];

			int index = 0;

			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < i; j++)
				{
					int v2 = vertex_indexes[x + dirs[i, 0], y + dirs[i, 1], z + dirs[i, 2]];
					int v3 = vertex_indexes[x + dirs[j, 0], y + dirs[j, 1], z + dirs[j, 2]];
					int v4 = vertex_indexes[x + dirs[i, 0] + dirs[j, 0], y + dirs[i, 1] + dirs[j, 1], z + dirs[i, 2] + dirs[j, 2]];
					if (v2 == 0 || v3 == 0 || v4 == 0)
						continue;

					indices[index++] = v1;
					indices[index++] = v2;
					indices[index++] = v3;

					indices[index++] = v4;
					indices[index++] = v3;
					indices[index++] = v2;
				}
			}

			/*for (int i = 0; i < 12; i++)
			{
				for (int k = 0; k < 2; k++)
				{
					int c1 = edgeProcEdgeMask[i / 4, k, 0];
					int c2 = edgeProcEdgeMask[i / 4, k, 1];
					int c3 = edgeProcEdgeMask[i / 4, k, 2];
					int c4 = edgeProcEdgeMask[i / 4, k, 3];


					int v1 = vertex_indexes[x + c1 / 4, y + c1 % 4 / 2, z + c1 % 2];
					int v2 = vertex_indexes[x + c2 / 4, y + c2 % 4 / 2, z + c2 % 2];
					int v3 = vertex_indexes[x + c3 / 4, y + c3 % 4 / 2, z + c3 % 2];
					int v4 = vertex_indexes[x + c4 / 4, y + c4 % 4 / 2, z + c4 % 2];
					if (v1 == 0 || v2 == 0 || v3 == 0 || v4 == 0)
						continue;

					indices[index++] = v1;
					indices[index++] = v2;
					indices[index++] = v3;

					indices[index++] = v3;
					indices[index++] = v4;
					indices[index++] = v1;
				}
			}*/

			if (index > 0)
				indexes.SetData<int>(index_location * 4, indices, 0, index);
			index_location += index;
		}

		private Vector3 GetIntersection(Vector3 p1, Vector3 p2, float d1, float d2)
		{
			//do a simple linear interpolation
			return p1 + (-d1) * (p2 - p1) / (d2 - d1);
		}

		private float Sphere(Vector3 pos)
		{
			float radius = (float)resolution / 4.0f;
			Vector3 origin = new Vector3(resolution / 2, resolution / 2, resolution / 2);
			return (pos - origin).Length() - radius;
		}

		float Cuboid(Vector3 pos)
		{
			float radius = (float)resolution / 8.0f;
			Vector3 local = pos - new Vector3(resolution / 2, resolution / 2, resolution / 2);
			Vector3 d = new Vector3(Math.Abs(local.X), Math.Abs(local.Y), Math.Abs(local.Z)) - new Vector3(radius, radius, radius);
			float m = Math.Max(d.X, Math.Max(d.Y, d.Z));
			Vector3 max = Vector3.Max(d, Vector3.Zero);
			return Math.Min(m, max.Length());
		}

		float Sample(Vector3 pos)
		{
			return Math.Min(Sphere(pos), Cuboid(pos - new Vector3(12, 12, 12)));
			//return Sphere(pos);
			return Cuboid(pos);
		}

		float Noise(Vector3 pos)
		{
			double d = pos.Y - Math.Sin((pos.X * 0.34172f + pos.X * 0.23111 + pos.X * pos.X) * 0.01f) * 16.0f - 32;
			return (float)d;
		}

		float sdTorus(Vector3 pos)
		{
			Vector2 t = new Vector2(resolution / 8, resolution / 8);
			Vector2 q = new Vector2(new Vector2(pos.X, pos.Z).Length() - t.X, pos.Y);
			return q.Length() - t.Y;
		}

		private Vector3 GetNormal(int x, int y, int z)
		{
			//can't compute gradient
			if (x == 0 || y == 0 || x == resolution - 1 || y == resolution - 1)
				return Vector3.Zero;

			Vector3 gradient = new Vector3(map[x + 1, y, z] - map[x - 1, y, z], map[x, y + 1, z] - map[x, y - 1, z], map[x, y, z + 1] - map[x, y, z - 1]);
			gradient.Normalize();
			return gradient;
		}

		private Vector3 GetNormal(Vector3 v)
		{
			//can't compute gradient
			float h = 0.001f;
			float dxp = Sample(new Vector3(v.X + h, v.Y, v.Z));
			float dxm = Sample(new Vector3(v.X - h, v.Y, v.Z));
			float dyp = Sample(new Vector3(v.X, v.Y + h, v.Z));
			float dym = Sample(new Vector3(v.X, v.Y - h, v.Z));
			float dzp = Sample(new Vector3(v.X, v.Y, v.Z + h));
			float dzm = Sample(new Vector3(v.X, v.Y, v.Z - h));
			//Vector3 gradient = new Vector3(map[x + 1, y] - map[x - 1, y], map[x, y + 1] - map[x, y - 1]);
			Vector3 gradient = new Vector3(dxp - dxm, dyp - dym, dzp - dzm);
			gradient.Normalize();
			return gradient;
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
			if (index_location == 0)
				return;
			effect.LightingEnabled = true;
			effect.PreferPerPixelLighting = true;
			effect.SpecularPower = 64;
			effect.SpecularColor = Color.Black.ToVector3();
			effect.CurrentTechnique.Passes[0].Apply();
			effect.AmbientLightColor = Color.Gray.ToVector3();
			device.SetVertexBuffer(buffer);
			device.Indices = indexes;
			//device.DrawPrimitives(PrimitiveType.LineList, 0, vertex_location / 2);
			device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertex_location, 0, index_location / 3);
		}
	}
}
