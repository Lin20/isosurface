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
	public enum WireframeModes
	{
		Fill = 1,
		Wireframe = 2
	}

	public class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		BasicEffect effect;
		KeyboardState last_state;

		public int QualityIndex { get; set; }
		public int AlgorithmIndex { get; set; }

		public float[] Qualities = { 0.0f, 0.001f, 0.01f, 0.05f, 0.1f, 0.2f, 0.4f, 0.5f, 0.8f, 1.0f, 1.5f, 2.0f, 5.0f, 10.0f, 25.0f, 50.0f };
		public Type[] AlgorithmTypes = { typeof(UniformDualContouring.DC3D), typeof(AdaptiveDualContouring.ADC3D), typeof(UniformDualContouring2D.DC), typeof(AdaptiveDualContouring2D.ADC) };

		public ISurfaceAlgorithm SelectedAlgorithm { get; set; }
		private Camera Camera { get; set; }

		public const int TileSize = 14;
		public const int Resolution = 64;

		public DrawModes DrawMode { get; set; }
		public RasterizerState RState { get; set; }
		public WireframeModes WireframeMode { get; set; }

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			RState = new RasterizerState();
			RState.CullMode = CullMode.None;
			GraphicsDevice.RasterizerState = RState;
			graphics.PreferredBackBufferWidth = 1600;
			graphics.PreferredBackBufferHeight = 900;
			graphics.ApplyChanges();

			IsMouseVisible = true;

			effect = new BasicEffect(GraphicsDevice);

			QualityIndex = 1;
			NextAlgorithm();

			effect.VertexColorEnabled = true;

			Camera = new Camera(GraphicsDevice, new Vector3(Resolution, Resolution, Resolution), 1f);
			Camera.Update(true);
			effect.View = Camera.View;
			last_state = Keyboard.GetState();

			DrawMode = Isosurface.DrawModes.Mesh;
			WireframeMode = WireframeModes.Fill;

			base.Initialize();
		}

		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);
		}

		public void NextAlgorithm()
		{
			SetAlgorithm(AlgorithmTypes[AlgorithmIndex]);
			AlgorithmIndex = (AlgorithmIndex + 1) % AlgorithmTypes.Length;
		}

		public void SetAlgorithm(Type t)
		{
			SelectedAlgorithm = (ISurfaceAlgorithm)Activator.CreateInstance(t, GraphicsDevice, Resolution, TileSize);
			QualityIndex--;
			NextQuality();

			if (SelectedAlgorithm.Is3D)
			{
				effect.View = Matrix.CreateLookAt(new Vector3(-1, 1, 1) * (float)Resolution, Vector3.Zero, Vector3.Up);
				effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), (float)graphics.PreferredBackBufferWidth / (float)graphics.PreferredBackBufferHeight, 1.0f, 1000.0f);
				effect.EnableDefaultLighting();
			}
			else
			{
				effect.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, 1);
				effect.View = Matrix.Identity;
			}
		}

		private void NextQuality()
		{
			long time = SelectedAlgorithm.Contour(Qualities[QualityIndex]);
			System.Text.StringBuilder text = new System.Text.StringBuilder();

			text.Append(SelectedAlgorithm.Name).Append(" - ");

			string topology_type = (SelectedAlgorithm.Is3D ? "Triangles" : "Lines");
			if (SelectedAlgorithm.IsIndexed)
				text.Append((SelectedAlgorithm.IndexCount / (SelectedAlgorithm.Is3D ? 3 : 2)) + " " + topology_type + ", " + SelectedAlgorithm.VertexCount + " Vertices");
			else
				text.Append((SelectedAlgorithm.VertexCount / (SelectedAlgorithm.Is3D ? 3 : 2)) + " " + topology_type);

			text.Append(" (" + time + " ms)");
			
			text.Append(" - Quality " + Qualities[QualityIndex]);

			Window.Title = text.ToString();
			QualityIndex = (QualityIndex + 1) % Qualities.Length;
		}

		protected override void UnloadContent()
		{
		}

		protected override void Update(GameTime gameTime)
		{
			// Allows the game to exit
			if (Keyboard.GetState().IsKeyDown(Keys.Escape))
				this.Exit();

			if (!last_state.IsKeyDown(Keys.Space) && Keyboard.GetState().IsKeyDown(Keys.Space))
			{
				NextQuality();
			}
			if (!last_state.IsKeyDown(Keys.Tab) && Keyboard.GetState().IsKeyDown(Keys.Tab))
			{
				NextAlgorithm();
			}

			if (!last_state.IsKeyDown(Keys.D1) && Keyboard.GetState().IsKeyDown(Keys.D1))
			{
				if (DrawMode != DrawModes.Mesh)
					DrawMode ^= DrawModes.Mesh;
			}
			if (!last_state.IsKeyDown(Keys.D2) && Keyboard.GetState().IsKeyDown(Keys.D2))
			{
				if (DrawMode != DrawModes.Outline)
					DrawMode ^= DrawModes.Outline;
			}

			if (!last_state.IsKeyDown(Keys.D3) && Keyboard.GetState().IsKeyDown(Keys.D3))
			{
				if (WireframeMode == WireframeModes.Fill)
					WireframeMode = WireframeModes.Fill | WireframeModes.Wireframe;
				else if (WireframeMode == (WireframeModes.Fill | WireframeModes.Wireframe))
					WireframeMode = WireframeModes.Wireframe;
				else
					WireframeMode = WireframeModes.Fill;

				if (WireframeMode != (WireframeModes.Fill | WireframeModes.Wireframe))
				{
					RState = new RasterizerState();
					RState.CullMode = CullMode.None;
					RState.FillMode = (WireframeMode == WireframeModes.Fill ? FillMode.Solid : FillMode.WireFrame);
					GraphicsDevice.RasterizerState = RState;
				}
			}

			if (!last_state.IsKeyDown(Keys.C) && Keyboard.GetState().IsKeyDown(Keys.C))
			{
				Camera.MouseLocked = !Camera.MouseLocked;
			}

			if (SelectedAlgorithm.Is3D)
			{
				Camera.Update(true);
				effect.View = Camera.View;
			}

			last_state = Keyboard.GetState();

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.DimGray);

			if (SelectedAlgorithm.Is3D)
				effect.World = Matrix.CreateTranslation(new Vector3(-Resolution / 2, -Resolution / 2, -Resolution / 2));
			else
				effect.World = Matrix.Identity;

			if (SelectedAlgorithm.Is3D && WireframeMode == (WireframeModes.Fill | WireframeModes.Wireframe))
			{
				RasterizerState rs = new RasterizerState();
				rs.CullMode = CullMode.None;
				rs.FillMode = FillMode.Solid;
				rs.DepthBias = 0;
				GraphicsDevice.RasterizerState = rs;
			}

			SelectedAlgorithm.Draw(effect, true, DrawMode);

			if (SelectedAlgorithm.Is3D && WireframeMode == (WireframeModes.Fill | WireframeModes.Wireframe))
			{
				RasterizerState rs = new RasterizerState();
				rs.CullMode = CullMode.None;
				rs.FillMode = FillMode.WireFrame;
				rs.DepthBias = -0.001f;
				GraphicsDevice.RasterizerState = rs;
				effect.VertexColorEnabled = false;
				SelectedAlgorithm.Draw(effect, true, DrawMode);
				effect.VertexColorEnabled = true;
			}

			base.Draw(gameTime);
		}
	}
}