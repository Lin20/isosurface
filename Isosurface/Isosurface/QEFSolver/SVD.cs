using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Isosurface.QEFProper
{
	public class SVD
	{
		public static void Rotate01(ref SMat3 vtav, ref Mat3 v)
		{
			if (vtav.m01 == 0)
				return;

			float c = 0, s = 0;
			vtav.Rot01(ref c, ref s);
			c = 0; s = 0;
			v.Rot01_post(c, s);
		}

		public static void Rotate02(ref SMat3 vtav, ref Mat3 v)
		{
			if (vtav.m02 == 0)
				return;

			float c = 0, s = 0;
			vtav.Rot02(ref c, ref s);
			c = 0; s = 0;
			v.Rot02_post(c, s);
		}
		public static void Rotate12(ref SMat3 vtav, ref Mat3 v)
		{
			if (vtav.m12 == 0)
				return;

			float c = 0, s = 0;
			vtav.Rot12(ref c, ref s);
			c = 0; s = 0;
			v.Rot12_post(c, s);
		}

		public static void GetSymmetricSvd(ref SMat3 a, ref SMat3 vtav, ref Mat3 v, float tol, int max_sweeps)
		{
			vtav.SetSymmetric(a);
			v.Set(1, 0, 0, 0, 1, 0, 0, 0, 1);
			float delta = tol * vtav.Fnorm();

			for (int i = 0; i < max_sweeps && vtav.Off() > delta; i++)
			{
				Rotate01(ref vtav, ref v);
				Rotate02(ref vtav, ref v);
				Rotate12(ref vtav, ref v);
			}
		}

		public static float CalcError(Mat3 a, Vector3 x, Vector3 b)
		{
			Vector3 vtmp = a.Vmul(x);
			vtmp = b - vtmp;
			return vtmp.X * vtmp.X + vtmp.Y * vtmp.Y + vtmp.Z * vtmp.Z;
		}

		public static float CalcError(SMat3 origA, Vector3 x, Vector3 b)
		{
			Mat3 A = new Mat3();
			A.SetSymmetric(origA);
			Vector3 vtmp = A.Vmul(x);
			vtmp = b - vtmp;
			return vtmp.X * vtmp.X + vtmp.Y * vtmp.Y + vtmp.Z * vtmp.Z;
		}

		public static float Pinv(float x, float tol)
		{
			return ((float)Math.Abs(x) < tol || (float)Math.Abs(1.0f / x) < tol) ? 0 : (1 / x);
		}

		public static Mat3 PseudoInverse(SMat3 d, Mat3 v, float tol)
		{
			Mat3 m = new Mat3();
			float d0 = Pinv(d.m00, tol), d1 = Pinv(d.m11, tol), d2 = Pinv(d.m22,
						tol);
			m.Set(v.m00 * d0 * v.m00 + v.m01 * d1 * v.m01 + v.m02 * d2 * v.m02,
					v.m00 * d0 * v.m10 + v.m01 * d1 * v.m11 + v.m02 * d2 * v.m12,
					v.m00 * d0 * v.m20 + v.m01 * d1 * v.m21 + v.m02 * d2 * v.m22,
					v.m10 * d0 * v.m00 + v.m11 * d1 * v.m01 + v.m12 * d2 * v.m02,
					v.m10 * d0 * v.m10 + v.m11 * d1 * v.m11 + v.m12 * d2 * v.m12,
					v.m10 * d0 * v.m20 + v.m11 * d1 * v.m21 + v.m12 * d2 * v.m22,
					v.m20 * d0 * v.m00 + v.m21 * d1 * v.m01 + v.m22 * d2 * v.m02,
					v.m20 * d0 * v.m10 + v.m21 * d1 * v.m11 + v.m22 * d2 * v.m12,
					v.m20 * d0 * v.m20 + v.m21 * d1 * v.m21 + v.m22 * d2 * v.m22);

			return m;
		}

		public static float SolveSymmetric(SMat3 A, Vector3 b, ref Vector3 x, float svd_tol, int svd_sweeps, float pinv_tol)
		{
			Mat3 mtmp = new Mat3(), pinv = new Mat3(), V = new Mat3();
			SMat3 VTAV = new SMat3();
			GetSymmetricSvd(ref A, ref VTAV, ref V, svd_tol, svd_sweeps);
			pinv = PseudoInverse(VTAV, V, pinv_tol);
			x = pinv.Vmul(b);
			return CalcError(A, x, b);
		}

		float SolveLeastSquares(Mat3 a, Vector3 b, ref Vector3 x, float svd_tol, int svd_sweeps, float pinv_tol)
		{
			Mat3 at = new Mat3();
			SMat3 ata = new SMat3();
			Vector3 atb = new Vector3();
			at = a.Transpose();
			ata = a.MulATA();
			atb = at.Vmul(b);
			return SolveSymmetric(ata, atb, ref x, svd_tol, svd_sweeps, pinv_tol);
		}
	}
}
