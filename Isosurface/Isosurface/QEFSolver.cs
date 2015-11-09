/*
 * Borrowed from https://github.com/nickgildea/DualContouringSample/blob/master/DualContouringSample
 * Very, very helpful and saves a lot of time
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

namespace Isosurface
{
	public class QEFSolver
	{
		const float epsilon = 1.0E-20f;


		void givens_coeffs_sym(float a_pp, float a_pq, float a_qq, out float c, out float s)
		{
			if (a_pq == 0.0f)
			{
				c = 1.0f;
				s = 0.0f;
				return;
			}
			float tau = (a_qq - a_pp) / (2.0f * a_pq);
			float stt = (float)Math.Sqrt(1.0f + tau * tau);
			float tan = 1.0f / ((tau >= 0.0f) ? (tau + stt) : (tau - stt));
			c = 1.0f / (float)Math.Sqrt(1.0f + tan * tan);
			s = tan * c;
		}

		void svd_rotate_xy(ref float x, ref float y, float c, float s)
		{
			float u = x; float v = y;
			x = c * u - s * v;
			y = s * u + c * v;
		}

		void svd_rotateq_xy(ref float x, ref float y, ref float a, float c, float s)
		{
			float cc = c * c; float ss = s * s;
			float mx = 2.0f * c * s * a;
			float u = x; float v = y;
			x = cc * u - mx + ss * v;
			y = ss * u + mx + cc * v;
		}

		void svd_rotate(ref float[,] vtav, ref float[,] v, int a, int b)
		{
			if (vtav[a, b] == 0.0f) return;

			float c, s;
			givens_coeffs_sym(vtav[a, a], vtav[a, b], vtav[b, b], out c, out s);
			svd_rotateq_xy(ref vtav[a, a], ref vtav[b, b], ref vtav[a, b], c, s);
			svd_rotate_xy(ref vtav[0, 3 - b], ref vtav[1 - a, 2], c, s);
			vtav[a, b] = 0.0f;

			svd_rotate_xy(ref v[0, a], ref v[0, b], c, s);
			svd_rotate_xy(ref v[1, a], ref v[1, b], c, s);
			svd_rotate_xy(ref v[2, a], ref v[2, b], c, s);
		}

		void svd_solve_sym(float[,] a, out Vector3 sigma, ref float[,] v)
		{
			// assuming that A is symmetric: can optimize all operations for 
			// the upper right triagonal
			float[,] vtav = new float[3, 3];
			Array.Copy(a, vtav, a.Length);
			// assuming V is identity: you can also pass a matrix the rotations
			// should be applied to
			// U is not computed
			for (int i = 0; i < 5; ++i)
			{
				svd_rotate(ref vtav, ref  v, 0, 1);
				svd_rotate(ref vtav, ref v, 0, 2);
				svd_rotate(ref vtav, ref v, 1, 2);
			}
			sigma = new Vector3(vtav[0, 0], vtav[1, 1], vtav[2, 2]);
		}

		float svd_invdet(float x, float tol)
		{
			return (Math.Abs(x) < tol || Math.Abs(1.0f / x) < tol) ? 0.0f : (1.0f / x);
		}

		void svd_pseudoinverse(out float[,] o, Vector3 sigma, float[,] v)
		{
			float d0 = svd_invdet(sigma.X, epsilon);
			float d1 = svd_invdet(sigma.Y, epsilon);
			float d2 = svd_invdet(sigma.Z, epsilon);
			o = new float[,] 
			{
				{
					v[0,0] * d0 * v[0,0] + v[0,1] * d1 * v[0,1] + v[0,2] * d2 * v[0,2],
					v[0,0] * d0 * v[1,0] + v[0,1] * d1 * v[1,1] + v[0,2] * d2 * v[1,2],
					v[0,0] * d0 * v[2,0] + v[0,1] * d1 * v[2,1] + v[0,2] * d2 * v[2,2]
				},
				{
					v[1,0] * d0 * v[0,0] + v[1,1] * d1 * v[0,1] + v[1,2] * d2 * v[0,2],
					v[1,0] * d0 * v[1,0] + v[1,1] * d1 * v[1,1] + v[1,2] * d2 * v[1,2],
					v[1,0] * d0 * v[2,0] + v[1,1] * d1 * v[2,1] + v[1,2] * d2 * v[2,2]
				},
				{ 
					v[2,0] * d0 * v[0,0] + v[2,1] * d1 * v[0,1] + v[2,2] * d2 * v[0,2],
					v[2,0] * d0 * v[1,0] + v[2,1] * d1 * v[1,1] + v[2,2] * d2 * v[1,2],
					v[2,0] * d0 * v[2,0] + v[2,1] * d1 * v[2,1] + v[2,2] * d2 * v[2,2]
				}
			};
		}

		void svd_solve_ATA_ATb(float[,] ATA, Vector3 ATb, out Vector3 x
)
		{
			float[,] V = new float[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
			Vector3 sigma;

			svd_solve_sym(ATA, out sigma, ref V);

			// A = UEV^T; U = A / (E*V^T)
			float[,] Vinv;
			svd_pseudoinverse(out Vinv, sigma, V);
			x = Multiply(Vinv, ATb);
		}

		private Vector3 Multiply(float[,] a, Vector3 b)
		{
			Vector3 product = new Vector3();
			for (int row = 0; row < 3; row++)
			{
				for (int col = 0; col < 3; col++)
				{
					// Multiply the row of A by the column of B to get the row, column of product.
					for (int inner = 0; inner < 3; inner++)
					{
						//+= a[row, inner] * (col == 0 ? b.X : col == 1 ? b.Y : b.Z);
					}
				}
			}

			return product;
		}
	}
}
