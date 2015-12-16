/* A static sampling function class
 * It does not support CSG trees or have any real elegance to it
 * It's just meant as a way to test different functions
 * Also some don't even work...!
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

namespace Isosurface
{
	public class Sampler
	{
		public const int Resolution = Game1.Resolution;
		public static int[,] Edges = new int[,] { { 0, 2 }, { 1, 3 }, { 0, 1 }, { 2, 3 } };

		public static Vector2 GetIntersection(Vector2 p1, Vector2 p2, float d1, float d2, float isolevel = 0)
		{
			//do a simple linear interpolation
			float mu = (isolevel - d1) / (d2 - d1);
			return p1 + mu * (p2 - p1);
		}

		public static float Circle(Vector2 pos)
		{
			float radius = (float)Resolution / 4.0f;
			Vector2 origin = new Vector2(Resolution / 2, Resolution / 2);
			return (pos - origin).Length() - radius;
		}

		public static float Circle(Vector2 pos, float radius)
		{
			Vector2 origin = new Vector2(Resolution / 4, Resolution / 4);
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
			float scale = 0.5f;
			//return SimplexNoise.Noise(pos.X * scale, pos.Y * scale);
			//return pos.Y - SimplexNoise.Noise(0, pos.X * 0.1f) * 10.0f - 16.5f;
			//float d = Math.Min(-Circle(pos), Circle(pos - new Vector2(8, 8), Resolution / 4));
			//return Math.Min(-d, Square(pos + new Vector2(6, 6), Resolution / 6.0f));
			//return sdTorus88(pos);
			//return Math.Min(-Circle(pos), Circle(pos - new Vector2(8, 8), Resolution / 4));
			//return Circle(pos);
			return Cuboid(pos);
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

		public static Vector2 GetGradient(Vector2 v)
		{
			//can't compute gradient
			float h = 0.001f;
			float dxp = Sample(new Vector2(v.X + h, v.Y));
			float dxm = Sample(new Vector2(v.X - h, v.Y));
			float dyp = Sample(new Vector2(v.X, v.Y + h));
			float dym = Sample(new Vector2(v.X, v.Y - h));
			//Vector2 gradient = new Vector2(map[x + 1, y] - map[x - 1, y], map[x, y + 1] - map[x, y - 1]);
			Vector2 gradient = new Vector2(dxp - dxm, dyp - dym);
			return gradient;
		}

		public static Vector2 GetNormal(Vector2 v)
		{
			Vector2 grad = GetGradient(v);
			grad.Normalize();
			return grad;
		}

		public static Vector3 GetIntersection(Vector3 p1, Vector3 p2, float d1, float d2)
		{
			//do a simple linear interpolation
			return p1 + (-d1) * (p2 - p1) / (d2 - d1);
		}

		public static float Sphere(Vector3 pos)
		{
			const float radius = (float)Resolution / 2.0f - 2.0f;
			Vector3 origin = new Vector3((Resolution - 2.0f) * 0.5f);
			return (pos - origin).LengthSquared() - radius * radius;
		}

		public static float Sphere(Vector3 pos, float radius)
		{
			Vector3 origin = new Vector3((Resolution - 2.0f) * 0.5f);
			return (pos - origin).LengthSquared() - radius * radius;
		}

		public static float SphereR(Vector3 pos)
		{
			 float radius = (float)Resolution / 3.0f - 2.0f + Noise(pos) * 7.0f;
			Vector3 origin = new Vector3((Resolution - 2.0f) * 0.5f);
			return (pos - origin).LengthSquared() - radius * radius;
		}

		public static float Cuboid(Vector3 pos)
		{
			float radius = (float)Resolution / 8.0f;
			Vector3 local = pos - new Vector3(Resolution / 2, Resolution / 2, Resolution / 2);
			Vector3 d = new Vector3(Math.Abs(local.X), Math.Abs(local.Y), Math.Abs(local.Z)) - new Vector3(radius, radius, radius);
			float m = Math.Max(d.X, Math.Max(d.Y, d.Z));
			Vector3 max = d;
			return Math.Min(m, max.Length());
		}

		public static float Cuboid(Vector3 pos, float radius)
		{
			Vector3 local = pos - new Vector3(Resolution / 2, Resolution / 2, Resolution / 2);
			Vector3 d = new Vector3(Math.Abs(local.X), Math.Abs(local.Y), Math.Abs(local.Z)) - new Vector3(radius, radius, radius);
			float m = Math.Max(d.X, Math.Max(d.Y, d.Z));
			Vector3 max = d;
			return Math.Min(m, max.Length());
		}

		public static float Cuboid(Vector3 pos, Vector3 radius)
		{
			Vector3 local = pos - new Vector3(Resolution / 2, Resolution / 2, Resolution / 2);
			Vector3 d = new Vector3(Math.Abs(local.X), Math.Abs(local.Y), Math.Abs(local.Z)) - radius;
			float m = Math.Max(d.X, Math.Max(d.Y, d.Z));
			Vector3 max = d;
			return Math.Min(m, max.Length());
		}

		public static float Sample(Vector3 pos)
		{
			//if (pos.Y > 8)
			//	return -1;
			//return 1;
			//return Noise(pos);
			//return Sphere(pos);
			//return pos.Y - Noise(pos) * 8.0f -8;
			//return Math.Min(Cuboid(pos), pos.Y - Noise(pos) * 16.0f - 8);
			//return Math.Min(Sphere(pos, Resolution / 8.0f), Cuboid(pos - new Vector3(4, 4, 4)));
			//return Math.Min(Cuboid(pos + new Vector3(0, 8, 0), Resolution / 4.0f), Sphere(pos, Resolution / 8.0f));
			//return SphereR(pos);
			//return Math.Min(Cuboid(pos + new Vector3(0, 2, 0), new Vector3(16, 4, 16)), Sphere(pos - new Vector3(0,8,0), 4));
			//return Math.Min(Cuboid(pos), Cuboid(pos - new Vector3(4, 4, 4)));
			//return Math.Min(Sphere(pos), Math.Min(Sphere(pos + new Vector3(16, 16, 16)), Sphere(pos - new Vector3(16, 16, 16))));
			//return Cuboid(pos + new Vector3(3, 3, 3));
			//return Math.Min(Cuboid(pos + new Vector3(5, 5, 5), 2), Cuboid(pos - new Vector3(4, 4, 4), 4));
			//return Sphere(pos - new Vector3(1, 1, 1), 4);
			//return Math.Min(Sphere(pos + new Vector3(1, 1, 1), 4), Sphere(pos - new Vector3(4, 4, 4), 4));
			float sdx = sdTorusX(pos - new Vector3(Resolution / 2.0f, Resolution / 2.0f, Resolution / 2.75f), new Vector2(Resolution / 4, Resolution / 10));
			float sdy = sdTorusY(pos - new Vector3(Resolution / 2.0f, Resolution / 2.0f, Resolution / 1.75f), new Vector2(Resolution / 4, Resolution / 10));
			float sdz = sdTorusZ(pos - Vector3.One * Resolution / 2.0f, new Vector2(Resolution / 4, Resolution / 10));
			return Math.Min(sdx, sdy);
			return Math.Max(Cuboid(pos), -Sphere(pos + new Vector3(8, 8, 8)));
			return Cuboid(pos) * SphereR(pos);
			return Cuboid(pos);
		}

		public static float Noise(Vector3 pos)
		{
			float r = 0.1f;
			return SimplexNoise.Noise(pos.X * r, pos.Y * r, pos.Z * r);
		}

		public static float sdTorusX(Vector3 p, Vector2 t)
		{
			Vector2 q = new Vector2((float)Math.Abs(Math.Sqrt(p.Y * p.Y + p.Z * p.Z)) - t.X, p.X);
			return q.Length() - t.Y;
		}

		public static float sdTorusY(Vector3 p, Vector2 t)
		{
			Vector2 q = new Vector2((float)Math.Abs(Math.Sqrt(p.X * p.X + p.Z * p.Z)) - t.X, p.Y);
			return q.Length() - t.Y;
		}

		public static float sdTorusZ(Vector3 p, Vector2 t)
		{
			Vector2 q = new Vector2((float)Math.Abs(Math.Sqrt(p.X * p.X + p.Y * p.Y)) - t.X, p.Z);
			return q.Length() - t.Y;
		}

		public static float CappedCylinder(Vector3 p, Vector2 h)
		{
			Vector2 d = new Vector2((float)Math.Abs(Math.Sqrt(p.X * p.X + p.Z * p.Z)), Math.Abs(p.Y)) - h;
			d = new Vector2(Math.Max(d.X, 0), Math.Max(d.Y, 0));
			float f = Math.Min(Math.Max(d.X, d.Y), 0.0f) + d.Length();
			return f;
		}

		public static float Cylinder(Vector3 p, Vector3 h)
		{
			Vector2 a = new Vector2(p.X, p.Z);
			Vector2 b = new Vector2(h.X, h.Y);
			return (a - b).Length() - h.Z;
		}

		public static float Blend(float a, float b, float k)
		{
			a = (float)Math.Pow(a, k);
			b = (float)Math.Pow(b, k);
			return (float)Math.Pow((a * b) / (a + b), 1.0f / k);
		}

		public static Vector3 GetNormal(Vector3 v)
		{
			//v = new Vector3((int)Math.Round(v.X), (int)Math.Round(v.Y), (int)Math.Round(v.Z));
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
	}
}
