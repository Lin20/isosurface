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
	public class Sampler
	{
		public static float Resolution { get; set; }
		public static int[,] Edges = new int[,] { { 0, 2 }, { 1, 3 }, { 0, 1 }, { 2, 3 } };

		public static Vector2 GetIntersection(Vector2 p1, Vector2 p2, float d1, float d2)
		{
			//do a simple linear interpolation
			return p1 + (-d1) * (p2 - p1) / (d2 - d1);
		}

		public static  float Circle(Vector2 pos)
		{
			float radius = (float)Resolution / 4.0f;
			Vector2 origin = new Vector2(Resolution / 2, Resolution / 2);
			return (pos - origin).Length() - radius;
		}

		public static float Circle(Vector2 pos, float radius)
		{
			Vector2 origin = new Vector2(Resolution / 2, Resolution / 2);
			return (pos - origin).Length() - radius;
		}

		public static float Cuboid(Vector2 pos)
		{
			float radius = (float)Resolution / 8.0f;
			Vector2 local = pos - new Vector2(Resolution / 2, Resolution / 2);
			Vector2 d = new Vector2(Math.Abs(local.X), Math.Abs(local.Y)) - new Vector2(radius, radius);
			float m = Math.Max(d.X, d.Y);
			Vector2 max = Vector2.Max(d, Vector2.Zero);
			return Math.Min(m, max.Length());
		}

		public static float Square(Vector2 pos, float radius)
		{
			Vector2 local = pos - new Vector2(Resolution / 2, Resolution / 2);
			Vector2 d = new Vector2(Math.Abs(local.X), Math.Abs(local.Y)) - new Vector2(radius, radius);
			float m = Math.Max(d.X, d.Y);
			Vector2 max = Vector2.Max(d, Vector2.Zero);
			return Math.Min(m, max.Length());
		}

		public static float Sample(Vector2 pos)
		{
			return SimplexNoise.Noise(pos.X * 0.2f, pos.Y * 0.2f);
			return pos.Y - SimplexNoise.Noise(0, pos.X * 0.1f) * 10.0f - 16.5f;
			float d = Math.Min(-Circle(pos), Circle(pos - new Vector2(8, 8), Resolution / 4));
			return Math.Min(-d, Square(pos + new Vector2(6, 6), Resolution / 16.0f));
			//return sdTorus88(pos);
			//return Math.Min(-Circle(pos), Circle(pos - new Vector2(8,8), Resolution / 4));
			//return Circle(pos);
			//return Cuboid(pos);
		}

		public static float Noise(Vector2 pos)
		{
			double d = pos.Y - Math.Sin((pos.X * 0.34172f + pos.X * 0.23111 + pos.X * pos.X) * 0.01f) * 16.0f - 32;
			return (float)d;
		}

		public static float sdTorus88(Vector2 pos)
		{
			Vector2 t = new Vector2(Resolution / 4, Resolution / 4);
			Vector2 q = new Vector2(pos.X - Resolution / 2 - t.X, pos.Y - Resolution / 2);
			return q.Length() - t.Y;
		}

		public static Vector2 GetNormal(Vector2 v)
		{
			//can't compute gradient
			float h = 0.001f;
			float dxp = Sample(new Vector2(v.X + h, v.Y));
			float dxm = Sample(new Vector2(v.X - h, v.Y));
			float dyp = Sample(new Vector2(v.X, v.Y + h));
			float dym = Sample(new Vector2(v.X, v.Y - h));
			//Vector2 gradient = new Vector2(map[x + 1, y] - map[x - 1, y], map[x, y + 1] - map[x, y - 1]);
			Vector2 gradient = new Vector2(dxp - dxm, dyp - dym);
			gradient.Normalize();
			return gradient;
		}

		public static Vector3 GetIntersection(Vector3 p1, Vector3 p2, float d1, float d2)
		{
			//do a simple linear interpolation
			return p1 + (-d1) * (p2 - p1) / (d2 - d1);
		}

		public static float Sphere(Vector3 pos)
		{
			float radius = (float)Resolution / 4.0f;
			Vector3 origin = new Vector3(Resolution / 2, Resolution / 2, Resolution / 2);
			return (pos - origin).Length() - radius;
		}

		public static float Cuboid(Vector3 pos)
		{
			float radius = (float)Resolution / 8.0f;
			Vector3 local = pos - new Vector3(Resolution / 2, Resolution / 2, Resolution / 2);
			Vector3 d = new Vector3(Math.Abs(local.X), Math.Abs(local.Y), Math.Abs(local.Z)) - new Vector3(radius, radius, radius);
			float m = Math.Max(d.X, Math.Max(d.Y, d.Z));
			Vector3 max = Vector3.Max(d, Vector3.Zero);
			return Math.Min(m, max.Length());
		}

		public static float Sample(Vector3 pos)
		{
			return Noise(pos);
			return Math.Min(Sphere(pos), Cuboid(pos - new Vector3(12, 12, 12)));
			return Sphere(pos);
			return Cuboid(pos);
		}

		public static float Noise(Vector3 pos)
		{
			return SimplexNoise.Noise(pos.X * 0.04f, pos.Y * 0.04f, pos.Z * 0.04f);
		}

		public static float sdTorus(Vector3 pos)
		{
			Vector2 t = new Vector2(Resolution / 8, Resolution / 8);
			Vector2 q = new Vector2(new Vector2(pos.X, pos.Z).Length() - t.X, pos.Y);
			return q.Length() - t.Y;
		}

		public static Vector3 GetNormal(Vector3 v)
		{
			//can't compute gradient
			float h = 1.0f;
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
	}
}
