using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Isosurface.QEFProper
{
	public class Mat3
	{
		public float m00, m01, m02, m10, m11, m12, m20, m21, m22;

		public Mat3()
		{
			Clear();
		}

		public Mat3(float m00, float m01, float m02,
			  float m10, float m11, float m12,
			  float m20, float m21, float m22)
		{
			Set(m00, m01, m02, m10, m11, m12, m20, m21, m22);
		}

		public void Clear()
		{
			Set(0, 0, 0, 0, 0, 0, 0, 0, 0);
		}

		public void Set(float m00, float m01, float m02,
			  float m10, float m11, float m12,
			  float m20, float m21, float m22)
		{
			this.m00 = m00;
			this.m01 = m01;
			this.m02 = m02;
			this.m10 = m10;
			this.m11 = m11;
			this.m12 = m12;
			this.m20 = m20;
			this.m21 = m21;
			this.m22 = m22;
		}

		public void Set(Mat3 rhs)
		{
			Set(rhs.m00, rhs.m01, rhs.m02, rhs.m10, rhs.m11, rhs.m12, rhs.m20, rhs.m21, rhs.m22);
		}

		public void SetSymmetric(float a00, float a01, float a02, float a11, float a12, float a22)
		{
			Set(a00, a01, a02, a01, a11, a12, a02, a12, a22);
		}

		public void SetSymmetric(SMat3 rhs)
		{
			SetSymmetric(rhs.m00, rhs.m01, rhs.m02, rhs.m11, rhs.m12, rhs.m22);
		}

		public float Fnorm()
		{
			return (float)Math.Sqrt((m00 * m00) + (m01 * m01) + (m02 * m02)
					+ (m10 * m10) + (m11 * m11) + (m12 * m12)
					+ (m20 * m20) + (m21 * m21) + (m22 * m22));
		}

		public float Off()
		{
			return (float)Math.Sqrt((m01 * m01) + (m02 * m02) + (m10 * m10)
					+ (m12 * m12) + (m20 * m20) + (m21 * m21));
		}

		public static Mat3 operator *(Mat3 a, Mat3 b)
		{
			Mat3 m = new Mat3();
			m.Set(a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20,
				a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21,
				a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22,
				a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20,
				a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21,
				a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22,
				a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20,
				a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21,
				a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22);
			return m;
		}

		public SMat3 MulATA()
		{
			SMat3 m = new SMat3();
			m.SetSymmetric(m00 * m00 + m10 * m10 + m20 * m20,
						 m00 * m01 + m10 * m11 + m20 * m21,
						 m00 * m02 + m10 * m12 + m20 * m22,
						 m01 * m01 + m11 * m11 + m21 * m21,
						 m01 * m02 + m11 * m12 + m21 * m22,
						 m02 * m02 + m12 * m12 + m22 * m22);
			return m;
		}

		public Mat3 Transpose()
		{
			Mat3 m = new Mat3();
			m.Set(m00, m10, m20, m01, m11, m21, m02, m12, m22);
			return m;
		}

		public Vector3 Vmul(Vector3 v)
		{
			Vector3 o = new Vector3();
			o.X = (m00 * v.X) + (m01 * v.Y) + (m02 * v.Z);
			o.Y = (m10 * v.X) + (m11 * v.Y) + (m12 * v.Z);
			o.Z = (m20 * v.X) + (m21 * v.Y) + (m22 * v.Z);
			return o;
		}

		public void Rot01_post(float c, float s)
		{
			float m00 = this.m00, m01 = this.m01, m10 = this.m10, m11 = this.m11, m20 = this.m20,
					m21 = this.m21;
			Set(c * m00 - s * m01, s * m00 + c * m01, this.m02, c * m10 - s * m11,
				  s * m10 + c * m11, this.m12, c * m20 - s * m21, s * m20 + c * m21, this.m22);
		}

		public void Rot02_post(float c, float s)
		{
			float m00 = this.m00, m02 = this.m02, m10 = this.m10, m12 = this.m12,
					m20 = this.m20, m22 = this.m22;
			Set(c * m00 - s * m02, this.m01, s * m00 + c * m02, c * m10 - s * m12, this.m11,
				  s * m10 + c * m12, c * m20 - s * m22, this.m21, s * m20 + c * m22);
		}

		public void Rot12_post(float c, float s)
		{
			float m01 = this.m01, m02 = this.m02, m11 = this.m11, m12 = this.m12,
					m21 = this.m21, m22 = this.m22;
			Set(this.m00, c * m01 - s * m02, s * m01 + c * m02, this.m10, c * m11 - s * m12,
				  s * m11 + c * m12, this.m20, c * m21 - s * m22, s * m21 + c * m22);
		}

		public static void CalcSymmetricGivensCoefficients(float a_pp, float a_pq, float a_qq, out float c, out float s)
		{
			if (a_pq == 0)
			{
				c = 1;
				s = 0;
				return;
			}

			float tau = (a_qq - a_pp) / (2.0f * a_pq);
			float stt = (float)Math.Sqrt(1.0f + tau * tau);
			float tan = 1.0f / ((tau >= 0) ? (tau + stt) : (tau - stt));
			c = 1.0f / (float)Math.Sqrt(1.0f + tan * tan);
			s = tan * c;
		}
	}
}
