/* This class servers as the front-end to the "Dual Marching Squares" algorithm
 * See DualMarchingSquares.QuadTree for the actual algorithm contents
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

namespace Isosurface.DualMarchingSquares
{
	public class DMS : ISurfaceAlgorithm
	{
		public override string Name { get { return "Dual Marching Squares"; } }
		private QuadtreeNode Tree { get; set; }
		private List<Cell> Cells { get; set; }
		public VertexBuffer DualGridBuffer { get; set; }
		public int DualGridCount { get; set; }

		public DMS(GraphicsDevice device, int Resolution, int size)
			: base(device, Resolution, size, false, true)
		{
			Cells = new List<Cell>();
			DualGridBuffer = new DynamicVertexBuffer(device, VertexPositionColorNormal.VertexDeclaration, 262144, BufferUsage.None);
		}

		public override long Contour(float threshold)
		{
			Stopwatch watch = new Stopwatch();

			Vertices.Clear();
			Tree = new QuadtreeNode();
			OutlineLocation = 0;
			watch.Start();

			Tree.Build(Resolution, 1, threshold, this.Size, Vertices);
			DualGridCount = Vertices.Count;
			if (DualGridCount > 0)
				DualGridBuffer.SetData<VertexPositionColorNormal>(Vertices.ToArray());

			CalculateIndexes();

			watch.Stop();

			ConstructTreeGrid(Tree);

			return watch.ElapsedMilliseconds;
		}

		public void ConstructTreeGrid(QuadtreeNode node)
		{
			if (node == null)
				return;
			VertexPositionColor[] vs = new VertexPositionColor[16];
			int x = (int)node.position.X * this.Size;
			int y = (int)node.position.Y * this.Size;

			Color c = Color.LightSteelBlue;
			Color v = Color.LightSalmon;

			float size = node.size * this.Size;
			vs[0] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, 0), c);
			vs[1] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, 0), c);
			vs[2] = new VertexPositionColor(new Vector3(x + 1 * size, y + 0 * size, 0), c);
			vs[3] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, 0), c);
			vs[4] = new VertexPositionColor(new Vector3(x + 1 * size, y + 1 * size, 0), c);
			vs[5] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, 0), c);
			vs[6] = new VertexPositionColor(new Vector3(x + 0 * size, y + 1 * size, 0), c);
			vs[7] = new VertexPositionColor(new Vector3(x + 0 * size, y + 0 * size, 0), c);

			OutlineBuffer.SetData<VertexPositionColor>(OutlineLocation * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 8, VertexPositionColor.VertexDeclaration.VertexStride);
			OutlineLocation += 8;

			if (node.leaf && false)
			{
				x += (int)(node.dualgrid_pos.X * (float)this.Size);
				y += (int)(node.dualgrid_pos.Y * (float)this.Size);
				float r = 2;
				vs[8] = new VertexPositionColor(new Vector3(x - r, y - r, 0), v);
				vs[9] = new VertexPositionColor(new Vector3(x + r, y - r, 0), v);
				vs[10] = new VertexPositionColor(new Vector3(x + r, y - r, 0), v);
				vs[11] = new VertexPositionColor(new Vector3(x + r, y + r, 0), v);
				vs[12] = new VertexPositionColor(new Vector3(x + r, y + r, 0), v);
				vs[13] = new VertexPositionColor(new Vector3(x - r, y + r, 0), v);
				vs[14] = new VertexPositionColor(new Vector3(x - r, y + r, 0), v);
				vs[15] = new VertexPositionColor(new Vector3(x - r, y - r, 0), v);
				OutlineBuffer.SetData<VertexPositionColor>(OutlineLocation * VertexPositionColor.VertexDeclaration.VertexStride, vs, 0, 16, VertexPositionColor.VertexDeclaration.VertexStride);
				OutlineLocation += 16;
			}

			for (int i = 0; i < 4; i++)
			{
				ConstructTreeGrid(node.children[i]);
			}
		}

		public void CalculateIndexes()
		{
			List<int> indexes = new List<int>();

			Cells.Clear();
			QuadtreeNode.ProcessFace(Tree, indexes, Cells);
			IndexCount = indexes.Count;
			if (indexes.Count != 0)
				IndexBuffer.SetData<int>(indexes.ToArray());

			Vertices.Clear();
			foreach (Cell c in Cells)
				c.Polygonize(Vertices, Size);

			VertexCount = Vertices.Count;
			if (VertexCount > 0)
				VertexBuffer.SetData<VertexPositionColorNormal>(Vertices.ToArray());
		}

		public override void Draw(Effect effect, bool enable_lighting = false, DrawModes mode = DrawModes.Mesh | DrawModes.Outline)
		{
			//effect.LightingEnabled = false;
			if (OutlineLocation > 0 && (mode & DrawModes.Outline) != 0)
			{
				effect.CurrentTechnique.Passes[0].Apply();
				Device.SetVertexBuffer(OutlineBuffer);
				Device.DrawPrimitives(PrimitiveType.LineList, 0, OutlineLocation / 2);
				Device.SetVertexBuffer(null);
			}

			if (DualGridCount > 0 && IndexCount > 0 && (mode & DrawModes.Outline) != 0)
			{
				effect.CurrentTechnique.Passes[0].Apply();
				Device.SetVertexBuffer(DualGridBuffer);

				Device.Indices = IndexBuffer;
				Device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, DualGridCount, 0, IndexCount / 2);
				Device.Indices = null;
			}

			if (VertexCount == 0 || ((mode & DrawModes.Mesh) == 0))
				return;

			if (enable_lighting)
			{
				/*effect.LightingEnabled = true;
				effect.PreferPerPixelLighting = true;
				effect.SpecularPower = 64;
				effect.SpecularColor = Color.Black.ToVector3();
				effect.CurrentTechnique.Passes[0].Apply();
				effect.AmbientLightColor = Color.Gray.ToVector3();*/
			}

			effect.CurrentTechnique.Passes[0].Apply();
			Device.SetVertexBuffer(VertexBuffer);

			Device.DrawPrimitives(PrimitiveType.LineList, 0, VertexCount / 2);

			Device.SetVertexBuffer(null);
		}
	}
}
