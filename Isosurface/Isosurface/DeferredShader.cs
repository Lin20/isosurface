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
	public class DeferredShader
	{
		private Effect ClearShader { get; set; }
		private Effect RenderToShader { get; set; }
		private Effect FinalShader { get; set; }

		public SpriteBatch SpriteBatch { get; set; }

		public GraphicsDevice Device { get; set; }

		private RenderTarget2D color_target;
		private RenderTarget2D normal_target;
		private RenderTarget2D depth_target;
		private RenderTargetBinding[] bindings;

		private Quad quad;
		private RasterizerState r_state;

		public DeferredShader(GraphicsDevice device, ContentManager content, SpriteBatch batch)
		{
			Device = device;
			SpriteBatch = batch;

			ClearShader = content.Load<Effect>("deferred_clear");
			RenderToShader = content.Load<Effect>("deferred_to");
			FinalShader = content.Load<Effect>("deferred_final");

			int width = device.PresentationParameters.BackBufferWidth;
			int height = device.PresentationParameters.BackBufferHeight;
			color_target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
			normal_target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
			depth_target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Single, DepthFormat.None);

			FinalShader.Parameters["half_pixel"].SetValue(new Vector2(0.5f / (float)width, 0.5f / (float)height));
			FinalShader.Parameters["noise_map"].SetValue(content.Load<Texture2D>("noise_norm"));

			quad = new Quad(device);
			r_state = new RasterizerState();
			r_state.CullMode = CullMode.None;
			
		}

		public void Draw(ISurfaceAlgorithm a, Camera c)
		{
			/* Set buffers */
			Device.SetRenderTargets(color_target, normal_target, depth_target);

			Clear();
			DrawScene(c, a);
			Device.SetRenderTarget(null);

			DrawFinal(c);
		}

		private void Clear()
		{
			ClearShader.CurrentTechnique.Passes[0].Apply();
			quad.Render(Device);
		}

		private void DrawScene(Camera c, ISurfaceAlgorithm a)
		{
			Device.RasterizerState = r_state;
			RenderToShader.Parameters["World"].SetValue(Matrix.CreateTranslation(new Vector3(-Game1.Resolution / 2, -Game1.Resolution / 2, -Game1.Resolution / 2)));
			RenderToShader.Parameters["View"].SetValue(c.View);
			RenderToShader.Parameters["Projection"].SetValue(c.Projection);

			RenderToShader.CurrentTechnique.Passes[0].Apply();
			a.Draw(null);
		}

		private void DrawFinal(Camera c)
		{
			FinalShader.Parameters["color_map"].SetValue(color_target);
			FinalShader.Parameters["normal_map"].SetValue(normal_target);
			FinalShader.Parameters["depth_map"].SetValue(depth_target);
			FinalShader.Parameters["InvertViewProjection"].SetValue(Matrix.Invert(c.View * c.Projection));

			FinalShader.CurrentTechnique.Passes[0].Apply();
			quad.Render(Device);
		}
	}
}
