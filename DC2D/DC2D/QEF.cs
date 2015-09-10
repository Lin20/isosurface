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
using MathNet.Numerics.LinearAlgebra;

namespace DC2D
{
	public class QEF
	{
		public List<Vector2> Intersections { get; set; }
		public List<Vector2> Normals { get; set; }
		//Matrix ata;
		//Vector3 atb;
		Vector3 mass_point;
		//Vector3 x;
		//float btb;
		private static Random rnd = new Random();
		private static Vector2[] deltas;

		static QEF()
		{
			deltas = new Vector2[256];
			for (int x = 0; x < 16; x++)
			{
				float dx = (float)x / 8.0f - 1.0f;
				for (int y = 0; y < 16; y++)
				{
					float dy = (float)y / 8.0f - 1.0f;
					deltas[x * 16 + y] = new Vector2(dx, dy);
				}
			}
		}

		public QEF()
		{
			Intersections = new List<Vector2>();
			Normals = new List<Vector2>();
		}

		public void Add(Vector2 p, Vector2 n)
		{
			Intersections.Add(p);
			Normals.Add(n);
			/*ata.M11 += n.X * n.X;
			ata.M12 += n.X * n.Y;
			ata.M13 += 0; //x * z
			ata.M21 += n.Y * n.Y;
			ata.M22 += 0; //y * z
			ata.M23 += 0; //z * z

			float dot = n.X * p.X + n.Y * p.Y;
			atb.X += dot * n.X;
			atb.Y += dot * n.Y;

			btb += dot * dot;*/

			mass_point += new Vector3(p, 0);
		}

		/*public float Solve(out Vector2 p, float svd_tol, int sweeps, float pinv_tol)
		{
			mass_point /= (float)Intersections.Count;
			// this->setAta();
			// this->setAtb();
			//Vec3 tmpv;
			Vector3 tmpv = Vector3.Transform(mass_point, ata);
			//MatUtils::vmul_symmetric(tmpv, this->ata, this->massPoint);
			//  VecUtils::sub(this->atb, this->atb, tmpv);
			atb -= tmpv;
			// this->x.clear();
			x = Vector3.Zero;
			p = new Vector2(mass_point.X, mass_point.Y) / (float)Intersections.Count;
			return 0;
			// const float result = Svd::solveSymmetric(this->ata, this->atb,
			//                       this->x, svd_tol, svd_sweeps, pinv_tol);
			//  VecUtils::addScaled(this->x, 1, this->massPoint);
			// this->setAtb();
			// outx.set(x);
			// this->hasSolution = true;
			// return result;
		}*/

		private float GetDistanceSquared(Vector2 x)
		{
			float total = 0;
			for (int i = 0; i < Intersections.Count; i++)
			{
				Vector2 d = x - Intersections[i];
				float dot = Normals[i].X * d.X + Normals[i].Y * d.Y;
				total += dot * dot;
			}
			return total;
		}

		private float GetDistanceSquared(Vector2 x, Vector2 p, Vector2 n)
		{
			Vector2 d = x - p;
			//Vector2 n2 = n * n;
			float dot = n.X * d.X + n.Y * d.Y;
			return dot * dot;
		}

		public Vector2 Solve2(float svd_tol, int sweeps, float pinv_tol)
		{
			Vector2 x = new Vector2(mass_point.X, mass_point.Y) / (float)Intersections.Count;
			//return x;
			float error = GetDistanceSquared(x);

			if (Math.Abs(error) >= 0.0001f)
			{
				for (int i = 0; i < deltas.Length; i++)
				{
					Vector2 new_point = new Vector2(x.X + deltas[i].X, x.Y + deltas[i].Y);
					//new_point = Vector2.Clamp(new_point, Vector2.Zero, Vector2.One);
					float e = GetDistanceSquared(new_point);
					if (e <= error)
					{
						x = new_point;
						if (Math.Abs(e) < 0.0001f)
							break;
						error = e;
					}
				}
			}

			if (x.X < 0 || x.Y < 0 || x.X >= 1 || x.Y >= 1)
				x = new Vector2(mass_point.X, mass_point.Y) / (float)Intersections.Count;
			return Vector2.Clamp(x, Vector2.Zero, Vector2.One);
		}
	}
}
