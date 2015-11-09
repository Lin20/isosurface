using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace Isosurface.DMCNeilson
{
	public class VertexPlacement
	{
		public List<Vector<float>> Intersections { get; set; }
		public List<Vector<float>> Normals { get; set; }
		public List<Vector<float>> PlaneNs { get; set; }
		public const float SupportingFactor = 0.5f;

		private QEF3D qef = new QEF3D();

		public VertexPlacement()
		{
			Intersections = new List<Vector<float>>();
			Normals = new List<Vector<float>>();
			PlaneNs = new List<Vector<float>>();
		}

		public void AddPlane(Microsoft.Xna.Framework.Vector3 intersection, Microsoft.Xna.Framework.Vector3 normal)
		{
			Vector<float> p = Vector<float>.Build.Dense(new float[] { intersection.X, intersection.Y, intersection.Z });
			Vector<float> n = Vector<float>.Build.Dense(new float[] { normal.X, normal.Y, normal.Z });

			Intersections.Add(p);
			Normals.Add(n);
			qef.Add(intersection, normal);
		}

		private Vector<float> GetMassPoint()
		{
			Vector<float> p_mass = Vector<float>.Build.Dense(3);
			foreach (Vector<float> p in Intersections)
				p_mass += p;

			return p_mass / (float)Intersections.Count;
		}

		private void ComputePlane(int index)
		{
			Vector<float> p_mass = GetMassPoint();

			Vector<float> n_mass = Vector<float>.Build.Dense(3);
			foreach (Vector<float> n in Normals)
				n_mass += n;
			n_mass = (n_mass / (float)Normals.Count).Normalize(1);

			Vector<float> ppt = p_mass - Intersections[index];
			Vector<float> e = ppt / ppt.AbsoluteMaximum();

			Vector<float> nnt = n_mass + Normals[index];
			Vector<float> n_e = nnt / nnt.AbsoluteMaximum();

			Vector<float> net = Cross(n_e, e);
			Vector<float> n_s = net / net.AbsoluteMaximum();

			PlaneNs.Add(n_s);
		}

		public float ComputePlaneError(int index, Vector<float> v)
		{
			Matrix<float> n_sT = PlaneNs[index].ToRowMatrix(); //transposed n_s [1x3]

			Matrix<float> m = n_sT * v.ToColumnMatrix();

			float q0 = n_sT.Multiply(v.ToColumnMatrix())[0, 0];
			float q1 = n_sT.Multiply(GetMassPoint().ToColumnMatrix())[0, 0];

			float q = q0 + q1;
			return q0 + q1; //ideally this is 0
		}

		public float ComputeTotalPlaneError(Vector<float> v)
		{
			for (int i = 0; i < Intersections.Count; i++)
				ComputePlane(i);

			float q_s = 0;
			for (int i = 0; i < Intersections.Count; i++)
				q_s += (ComputePlaneError(i, v));

			if (Math.Abs(q_s) > 0.0f)
			{
			}
			return q_s;
		}

		public Vector<float> ComputePlaneVertex(int i)
		{
			Matrix<float> n_sT = PlaneNs[i].ToRowMatrix();

			float q1 = (n_sT.Multiply(GetMassPoint().ToColumnMatrix())[0, 0]);

			Matrix<float> inv = n_sT.Transpose().QR(MathNet.Numerics.LinearAlgebra.Factorization.QRMethod.Full).Solve(Matrix<float>.Build.DenseIdentity(n_sT.ColumnCount));

			Matrix<float> res = q1 * inv.Transpose();

			return res.Column(0);
		}

		public Vector<float> ComputeQEMVertex(int i)
		{
			float d = Normals[i].DotProduct(Intersections[i]);
			Matrix<float> A = Normals[i].ToColumnMatrix() * Normals[i].ToRowMatrix();
			Vector<float> b = Normals[i] * d;
			float c = d * d;

			Matrix<float> vv = -A.Inverse() * b.ToColumnMatrix();
			return vv.Column(0);
		}

		public Microsoft.Xna.Framework.Vector3 Solve()
		{
			Microsoft.Xna.Framework.Vector3 q = qef.Solve2(0, 0, 0);
			Vector<float> v = Vector<float>.Build.Dense(new float[] { q.X, q.Y, q.Z });

			float error = ComputeTotalPlaneError(v);

			Vector<float> result = Vector<float>.Build.Dense(3);
			for (int i = 0; i < PlaneNs.Count; i++)
			{
				//result += ComputePlaneVertex(i);
				result += ComputeQEMVertex(i);
			}
			result /= (float)PlaneNs.Count;
			result += GetMassPoint();
			error = ComputeTotalPlaneError(result);
			if (error != 0)
			{
			}

			return new Microsoft.Xna.Framework.Vector3(result[0], result[1], result[2]);
		}

		public static Vector<float> Cross(Vector<float> left, Vector<float> right)
		{
			if ((left.Count != 3 || right.Count != 3))
			{
				string message = "Vectors must have a length of 3.";
				throw new Exception(message);
			}
			Vector<float> result = Vector<float>.Build.Dense(3);
			result[0] = left[1] * right[2] - left[2] * right[1];
			result[1] = -left[0] * right[2] + left[2] * right[0];
			result[2] = left[0] * right[1] - left[1] * right[0];

			return result;
		}
	}
}
