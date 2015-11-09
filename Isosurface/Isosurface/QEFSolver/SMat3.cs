using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Isosurface.QEFProper
{
	public class SMat3
	{
		public float m00, m01, m02, m11, m12, m22;

		public SMat3()
		{
			Clear();
		}

		public SMat3(float m00, float m01, float m02,
					float m11, float m12, float m22)
		{
			SetSymmetric(m00, m01, m02, m11, m12, m22);
		}

		public void Clear()
		{
			SetSymmetric(0, 0, 0, 0, 0, 0);
		}

		public void SetSymmetric(float a00, float a01, float a02, float a11, float a12, float a22)
		{
			this.m00 = a00;
			this.m01 = a01;
			this.m02 = a02;
			this.m11 = a11;
			this.m12 = a12;
			this.m22 = a22;
		}

		public void SetSymmetric(SMat3 rhs)
		{
			SetSymmetric(rhs.m00, rhs.m01, rhs.m02, rhs.m11, rhs.m12, rhs.m22);
		}

		public float Fnorm()
		{
			return (float)Math.Sqrt((m00 * m00) + (m01 * m01) + (m02 * m02)
					+ (m01 * m01) + (m11 * m11) + (m12 * m12)
					+ (m02 * m02) + (m12 * m12) + (m22 * m22));
		}

		public float Off()
		{
			return (float)Math.Sqrt(2 * ((m01 * m01) + (m02 * m02) + (m12 * m12)));
		}

		public SMat3 Mul_ata(Mat3 a)
		{
			SMat3 m = new SMat3();
			m.SetSymmetric(a.m00 * a.m00 + a.m10 * a.m10 + a.m20 * a.m20,
							 a.m00 * a.m01 + a.m10 * a.m11 + a.m20 * a.m21,
							 a.m00 * a.m02 + a.m10 * a.m12 + a.m20 * a.m22,
							 a.m01 * a.m01 + a.m11 * a.m11 + a.m21 * a.m21,
							 a.m01 * a.m02 + a.m11 * a.m12 + a.m21 * a.m22,
							 a.m02 * a.m02 + a.m12 * a.m12 + a.m22 * a.m22);
			return m;
		}

		public Vector3 Vmul(Vector3 v)
		{
			Vector3 o = new Vector3();
			o.X = (m00 * v.X) + (m01 * v.Y) + (m02 * v.Z);
			o.Y = (m01 * v.X) + (m11 * v.Y) + (m12 * v.Z);
			o.Z = (m02 * v.X) + (m12 * v.Y) + (m22 * v.Z);
			return o;
		}

		public void Rot01(float c, float s)
		{
			Mat3.CalcSymmetricGivensCoefficients(m00, m01, m11, out c, out s);
			float cc = c * c;
			float ss = s * s;
			float mix = 2 * c * s * m01;
			SetSymmetric(cc * m00 - mix + ss * m11, 0, c * m02 - s * m12,
						   ss * m00 + mix + cc * m11, s * m02 + c * m12, m22);
		}

		public void Rot02(float c, float s)
		{
			Mat3.CalcSymmetricGivensCoefficients(m00, m02, m22, out c, out s);
			float cc = c * c;
			float ss = s * s;
			float mix = 2 * c * s * m02;
			SetSymmetric(cc * m00 - mix + ss * m22, c * m01 - s * m12, 0,
					   m11, s * m01 + c * m12, ss * m00 + mix + cc * m22);
		}

		public void Rot12(float c, float s)
		{
			Mat3.CalcSymmetricGivensCoefficients(m11, m12, m22, out c, out s);
			float cc = c * c;
			float ss = s * s;
			float mix = 2 * c * s * m12;
			SetSymmetric(m00, c * m01 - s * m02, s * m01 + c * m02,
					   cc * m11 - mix + ss * m22, 0, ss * m11 + mix + cc * m22);
		}


	}
}
