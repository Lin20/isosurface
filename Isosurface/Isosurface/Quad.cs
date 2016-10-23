using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Reflection;

namespace Isosurface
{
	public class Quad
	{
		private VertexPositionTexture[] vertices;
		private short[] indexes;

		public Quad(GraphicsDevice device)
		{
			vertices = new VertexPositionTexture[]
			{
				new VertexPositionTexture(new Vector3(0,0,1), new Vector2(1,1)),
				new VertexPositionTexture(new Vector3(0,0,1), new Vector2(0,1)),
				new VertexPositionTexture(new Vector3(0,0,1), new Vector2(0,0)),
				new VertexPositionTexture(new Vector3(0,0,1), new Vector2(1,0))
			};
			Vector2 v1 = -Vector2.One;
			Vector2 v2 = Vector2.One;

			vertices[0].Position.X = v2.X;
			vertices[0].Position.Y = v1.Y;

			vertices[1].Position.X = v1.X;
			vertices[1].Position.Y = v1.Y;

			vertices[2].Position.X = v1.X;
			vertices[2].Position.Y = v2.Y;

			vertices[3].Position.X = v2.X;
			vertices[3].Position.Y = v2.Y;

			indexes = new short[] { 0, 1, 2, 2, 3, 0 };
		}

		public void Render(GraphicsDevice device)
		{
			device.DrawUserIndexedPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, vertices, 0, 4, indexes, 0, 2);
		}
	}
}
