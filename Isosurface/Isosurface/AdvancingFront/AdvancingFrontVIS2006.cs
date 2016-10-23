/* Uniform Dual Contouring
 * Messy, but it works
 * This was an earlier implementation so it still operates on pre-calculated values rather than the function directly
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

namespace Isosurface.AdvancingFrontVIS2006
{
	public class AdvancingFrontVIS2006 : ISurfaceAlgorithm
	{
		public List<VertexPositionColor> CalculatedVertices { get; set; }
		private bool UseFlatShading { get; set; }
		public const bool Quads = true;

		const double rho = 0.5; //ρ
		const double eta = 1.2; //η

		private double[,] GuidanceField;

		public override string Name { get { return "Advancing Front (vis2006)"; } }
		public AdvancingFrontVIS2006(GraphicsDevice device, int resolution, int size)
			: base(device, resolution, size, true, false, 0x100000)
		{
			UseFlatShading = true;
			if (UseFlatShading)
				CalculatedVertices = new List<VertexPositionColor>();
		}

		public override long Contour(float threshold)
		{
			Stopwatch watch = new Stopwatch();

			watch.Start();

			GuidanceField = new double[Game1.Resolution, Game1.Resolution];


			VertexCount = Vertices.Count;
			if (Indices != null)
				IndexCount = Indices.Count;

			if (Vertices.Count > 0)
				VertexBuffer.SetData<VertexPositionColorNormal>(0, Vertices.ToArray(), 0, VertexCount, VertexPositionColorNormal.VertexDeclaration.VertexStride);
			if (!UseFlatShading && Indices.Count > 0)
				IndexBuffer.SetData<int>(Indices.ToArray());
			return watch.ElapsedMilliseconds;
		}



		public static double GetIdealEdgeLength(float x, float y, float z)
		{
			double kmax = GetKMax(x, y, z);
			double length = 2.0 * Math.Sin(rho / 2.0) / kmax;
			return length;
		}

		public static double GetKMax(float vx, float vy, float vz)
		{
			// this stuff is fairly straightforward
			// just some simple matrix maths
			Vector3 n = GetGradient(new Vector3(vx, vy, vz));
			float n_length = n.Length();
			n /= n_length;

			double[,] ident = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

			double[,] n_mat =
				{
					{ n.X, n.Y, n.Z },
					{ 0, 1, 0 },
					{ 0, 0, 1 }
				};

			double[,] n_matT =
				{
					{ n.X, n.Y, n.Z },
					{ 0, 1, 0 },
					{ 0, 0, 1 }
				};

			// multiply n by nT (the 1 after n_matT denotes Transposed, which is why n_matT isn't actually transposed)
			alglib.rmatrixgemm(3, 3, 3, 1.0, n_mat, 0, 0, 0, n_matT, 0, 0, 1, 0, ref ident, 0, 0);
			double[,] P =
			{
				{ 1 - n_mat[0, 0], 0 - n_mat[0, 1], 0 - n_mat[0, 2] },
				{ 0 - n_mat[1, 0], 1 - n_mat[1, 1], 0 - n_mat[1, 2]},
				{ 0 - n_mat[2, 0], 0 - n_mat[2, 1], 1 - n_mat[2, 2] }
			};

			// compute H
			double[,] H =
			{
				{ Derivative(vx,vy,vz,1,0,0), Derivative(vx,vy,vz,1,1,0), Derivative(vx,vy,vz,1,0,1) },
				{ Derivative(vx,vy,vz,1,1,0), Derivative(vx,vy,vz,0,1,0), Derivative(vx,vy,vz,0,1,1) },
				{ Derivative(vx,vy,vz,1,0,1), Derivative(vx,vy,vz,0,1,1), Derivative(vx,vy,vz,0,0,1) }
			};

			// compute G
			double[,] G = P.Clone() as double[,]; //G = P
			alglib.rmatrixgemm(3, 3, 3, 1.0, G, 0, 0, 0, H, 0, 0, 0, 0, ref ident, 0, 0); // G = PH
			alglib.rmatrixgemm(3, 3, 3, 1.0 / n_length, G, 0, 0, 0, P, 0, 0, 0, 0, ref ident, 0, 0); // G = PHP/n_length

			// compute eigenvalues
			double[] out_r, out_i;
			double[,] out_vl;
			double[,] out_vr;
			alglib.rmatrixevd(G, 3, 0, out out_r, out out_i, out out_vl, out out_vr);

			//compute spectral radius, aka k_max
			double s_r = Math.Max(Math.Max(out_r[0], out_r[1]), out_r[2]);

			return s_r;
		}

		public static Vector3 GetGradient(Vector3 v)
		{
			float h = 1.0f;
			float dxp = Sampler.Sphere(v.X + h, v.Y, v.Z);
			float dxm = Sampler.Sphere(v.X - h, v.Y, v.Z);
			float dyp = Sampler.Sphere(v.X, v.Y + h, v.Z);
			float dym = Sampler.Sphere(v.X, v.Y - h, v.Z);
			float dzp = Sampler.Sphere(v.X, v.Y, v.Z + h);
			float dzm = Sampler.Sphere(v.X, v.Y, v.Z - h);
			Vector3 gradient = new Vector3(dxp - dxm, dyp - dym, dzp - dzm);
			return gradient;
		}

		// compute 2nd order partial derivative
		private static float Derivative(float x, float y, float z, float dx, float dy, float dz)
		{
			// f''(x) = [f(x+h)-2f(x)+f(x-h)]/h^2
			const float h = 0.001f;
			float a = Sampler.Sphere(x + dx * h, y + dy * h, z + dz * h);
			float b = 2.0f * Sampler.Sphere(x, y, z);
			float c = Sampler.Sphere(x - dx * h, y - dy * h, z - dz * h);
			return (a - b + c) / (h * h);
		}

		private Vector3 GetNormalQ(ref VertexPositionColorNormal[] verts, params int[] indexes)
		{
			Vector3 a = verts[indexes[2]].Position - verts[indexes[1]].Position;
			Vector3 b = verts[indexes[2]].Position - verts[indexes[0]].Position;
			Vector3 c = Vector3.Cross(a, b);

			a = verts[indexes[5]].Position - verts[indexes[4]].Position;
			b = verts[indexes[5]].Position - verts[indexes[3]].Position;
			Vector3 d = Vector3.Cross(a, b);

			//c.Normalize();
			if (float.IsNaN(c.X))
				c = Vector3.Zero;
			if (float.IsNaN(d.X))
				d = Vector3.Zero;

			c += d;
			c /= 2.0f;
			c.Normalize();

			return -c;
		}

		private Vector3 GetNormalNA(ref VertexPositionColorNormal[] verts, params int[] indexes)
		{
			Vector3 product = new Vector3();

			Vector3 a = verts[indexes[0]].Position - verts[indexes[2]].Position;
			Vector3 b = verts[indexes[1]].Position - verts[indexes[2]].Position;
			Vector3 c = Vector3.Cross(a, b);
			//c.Normalize();
			product += c;

			a = verts[indexes[2]].Position - verts[indexes[1]].Position;
			b = verts[indexes[3]].Position - verts[indexes[1]].Position;
			c = Vector3.Cross(a, b);
			//c.Normalize();
			product += c;
			//product *= 0.5f;
			product.Normalize();

			return product;
		}

		private Vector3 GetNormal(ref VertexPositionColorNormal[] verts, params int[] indexes)
		{
			/*Vector3 product = new Vector3();

			Vector3 a = verts[0].Position - verts[2].Position;
			Vector3 b = verts[1].Position - verts[2].Position;
			Vector3 c = Vector3.Cross(a, b);
			//c = new Vector3(Math.Abs(c.X), Math.Abs(c.Y), Math.Abs(c.Z));
			c.Normalize();
			product += c;

			a = verts[2].Position - verts[1].Position;
			b = verts[3].Position - verts[1].Position;
			c = Vector3.Cross(a, b);
			//c = new Vector3(Math.Abs(c.X), Math.Abs(c.Y), Math.Abs(c.Z));
			c.Normalize();
			product += c;
			product /= 2.0f;
			product.Normalize();*/

			Vector3 product = new Vector3();
			if (!Quads)
			{
				Vector3 n0 = Sampler.GetNormal(verts[indexes[0]].Position);
				Vector3 n1 = Sampler.GetNormal(verts[indexes[1]].Position);
				Vector3 n2 = Sampler.GetNormal(verts[indexes[2]].Position);
				product = (n0 + n1 + n2) / 3.0f;
				product = Sampler.GetNormal((verts[indexes[0]].Position + verts[indexes[1]].Position + verts[indexes[2]].Position) / 3.0f);
				product.Normalize();
			}
			else
			{
				Vector3 n0 = Sampler.GetNormal(verts[0].Position);
				Vector3 n1 = Sampler.GetNormal(verts[1].Position);
				Vector3 n2 = Sampler.GetNormal(verts[2].Position);
				Vector3 n3 = Sampler.GetNormal(verts[3].Position);
				product = (n0 + n1 + n2 + n3);
				//product = Sampler.GetNormal((verts[0].Position + verts[1].Position + verts[2].Position + verts[3].Position) * 0.25f);
				product.Normalize();
			}

			Vector3 c_v = product * 0.5f + Vector3.One * 0.5f;
			c_v.Normalize();
			Color clr = new Color(c_v);
			Vector3 d = new Vector3(-.1f, 1f, -.1f);
			d.Normalize();
			float g = (Vector3.Dot(product, d) + 1.0f) * 0.5f;
			clr = new Color(0, g, 0);
			//clr = Color.Green;

			verts[indexes[0]].Normal = product;
			verts[indexes[1]].Normal = product;
			verts[indexes[2]].Normal = product;
			verts[indexes[0]].Color = clr;
			verts[indexes[1]].Color = clr;
			verts[indexes[2]].Color = clr;
			return product;
		}
	}
}