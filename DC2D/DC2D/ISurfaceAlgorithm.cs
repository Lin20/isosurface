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

namespace DC2D
{
	public abstract class ISurfaceAlgorithm<VertexType>
	{
		public GraphicsDevice Device { get; private set; }
		public int Resolution { get; set; }
		public int Size { get; set; }

		public virtual bool Is3D { get; protected set; }
		public virtual bool IsIndexed { get; protected set; }

		public List<VertexType> Vertices { get; protected set; }
		public List<int> Indices { get; protected set; }

		public DynamicVertexBuffer VertexBuffer { get; set; }
		public DynamicVertexBuffer OutlineBuffer { get; set; }
		public DynamicIndexBuffer IndexBuffer { get; set; }

		public ISurfaceAlgorithm(GraphicsDevice device, int resolution, int size, bool _3d, bool indexed = true, int vertex_size = 262144, int index_size = 4000000)
		{
			Device = device;
			Resolution = resolution;
			Size = size;

			Is3D = _3d;
			IsIndexed = indexed;

			VertexBuffer = new DynamicVertexBuffer(device, VertexPositionColorNormal.VertexDeclaration, vertex_size, BufferUsage.None);
			OutlineBuffer = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, index_size, BufferUsage.None);
			if (indexed)
				IndexBuffer = new DynamicIndexBuffer(device, IndexElementSize.ThirtyTwoBits, index_size, BufferUsage.None);
		}

		public abstract long Contour(float threshold);
		public virtual void Draw(BasicEffect effect, bool enable_lighting = false)
		{
			effect.LightingEnabled = false;
			if (OutlineBuffer.VertexCount > 0)
			{
				effect.CurrentTechnique.Passes[0].Apply();
				Device.SetVertexBuffer(OutlineBuffer);
				Device.DrawPrimitives(PrimitiveType.LineList, 0, OutlineBuffer.VertexCount / 2);
			}

			if ((IsIndexed && Indices.Count == 0) || (!IsIndexed && Vertices.Count == 0))
				return;

			if (enable_lighting)
			{
				effect.LightingEnabled = true;
				effect.PreferPerPixelLighting = true;
				effect.SpecularPower = 64;
				effect.SpecularColor = Color.Black.ToVector3();
				effect.CurrentTechnique.Passes[0].Apply();
				effect.AmbientLightColor = Color.Gray.ToVector3();
			}

			Device.SetVertexBuffer(VertexBuffer);
			if (IsIndexed)
			{
				Device.Indices = IndexBuffer;
				if (Is3D)
					Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Vertices.Count, 0, Indices.Count / 3);
				else
					Device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, Vertices.Count, 0, Indices.Count / 2);
				Device.Indices = null;
			}
			else
			{
				if (Is3D)
					Device.DrawPrimitives(PrimitiveType.TriangleList, 0, Vertices.Count / 3);
				else
					Device.DrawPrimitives(PrimitiveType.LineList, 0, Vertices.Count / 2);
			}
			Device.SetVertexBuffer(null);
		}
	}
}
