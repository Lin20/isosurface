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
	public struct VertexPositionColorNormalNormal
	{
		public Vector3 Position;
		public Color Color;
		public Vector3 Normal;
		public Vector3 Normal2;

		public VertexPositionColorNormalNormal(Vector3 pos, Color color, Vector3 norm, Vector3 norm2)
		{
			Position = pos;
			Color = color;
			norm.Normalize();
			Normal = norm;
			norm2.Normalize();
			Normal2 = norm2;
		}

		public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
		(
			new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
			new VertexElement(sizeof(float) * 3, VertexElementFormat.Color, VertexElementUsage.Color, 0),
			new VertexElement(sizeof(float) * 3 + 4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
			new VertexElement(sizeof(float) * 6 + 4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 1)
		);
	}
}
