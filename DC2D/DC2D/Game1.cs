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

namespace DC2D
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		BasicEffect effect;


		int quality_index = 0;
		float[] qualities = { 0.0f, 0.001f, 0.01f, 0.05f, 0.1f, 0.2f, 0.4f, 0.5f, 0.8f, 1.0f, 1.5f, 2.0f, 5.0f, 10.0f, 25.0f, 50.0f };

		//TODO: Create simple interface to allow switching dual contouring method on-the-fly
		ADC3D dc;
		Camera camera;

		const int tile_size = 14;
		public const int resolution = 128;

		Texture2D pixel;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			RasterizerState rs = new RasterizerState();
			rs.CullMode = CullMode.None;
			rs.FillMode = FillMode.WireFrame;
			GraphicsDevice.RasterizerState = rs;
			graphics.PreferredBackBufferWidth = 1600;
			graphics.PreferredBackBufferHeight = 900;
			graphics.ApplyChanges();

			IsMouseVisible = true;

			effect = new BasicEffect(GraphicsDevice);

			//Ugly method of switching between 2D and 3D; fix later
			if (true)
			{
				effect.View = Matrix.CreateLookAt(new Vector3(-1, 1, 1) * (float)resolution , Vector3.Zero, Vector3.Up);
				effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), (float)graphics.PreferredBackBufferWidth / (float)graphics.PreferredBackBufferHeight, 1.0f, 1000.0f);
				effect.EnableDefaultLighting();
			}
			else
				effect.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, 1);
			effect.VertexColorEnabled = true;

			camera = new Camera(GraphicsDevice, new Vector3(0, 50, -30), 1f);
			camera.Update(true);
			effect.View = camera.View;

			//VertexPositionColor[] vertices = { new VertexPositionColor(new Vector3(10, 10, 0), Color.Red), new VertexPositionColor(new Vector3(10, 60, 0), Color.Blue) };
			//buffer.SetData<VertexPositionColor>(vertices, 0, 2);


			pixel = new Texture2D(GraphicsDevice, tile_size, tile_size);
			Color[] pixels = new Color[tile_size * tile_size];
			for (int i = 0; i < tile_size * tile_size; i++)
				pixels[i] = Color.Black;
			pixel.SetData<Color>(pixels);

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

			dc = new ADC3D(GraphicsDevice, resolution, tile_size);
			NextQuality();
		}

		private void NextQuality()
		{
			long time = dc.Contour(qualities[quality_index]);
			Window.Title = "Dual Contouring - " + (dc.IndexCount / 3) + " Triangles, " + dc.VertexCount + " Vertices (" + time + " ms) - Quality " + qualities[quality_index];
			quality_index = (quality_index + 1) % qualities.Length;
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}

		int mx, my;
		float rx, ry;
		bool last_down = false;
		protected override void Update(GameTime gameTime)
		{
			// Allows the game to exit
			if (Keyboard.GetState().IsKeyDown(Keys.Escape))
				this.Exit();

			if (!last_down && Keyboard.GetState().IsKeyDown(Keys.Space))
			{
				last_down = true;
				NextQuality();
			}
			else if (!Keyboard.GetState().IsKeyDown(Keys.Space))
				last_down = false;

			mx = Mouse.GetState().X / tile_size;
			my = Mouse.GetState().Y / tile_size;
			rx = (float)Mouse.GetState().X / (float)resolution * MathHelper.TwoPi * 0.25f;
			ry = (float)Mouse.GetState().Y / (float)resolution * MathHelper.TwoPi * 0.25f;
			if (Mouse.GetState().LeftButton == ButtonState.Pressed && mx > 0 && my > 0 && mx < resolution - 1 && my < resolution - 1)
			{
				//dc.GenerateAt(mx, my);
			}

			int speed = 1;
			if (Keyboard.GetState().IsKeyDown(Keys.W))
				camera.Position += Vector3.Transform(Vector3.Forward * speed, camera.Rotation);
			else if (Keyboard.GetState().IsKeyDown(Keys.S))
				camera.Position += Vector3.Transform(Vector3.Backward * speed, camera.Rotation);
			if (Keyboard.GetState().IsKeyDown(Keys.D))
				camera.Position += Vector3.Transform(Vector3.Right * speed, camera.Rotation);
			else if (Keyboard.GetState().IsKeyDown(Keys.A))
				camera.Position += Vector3.Transform(Vector3.Left * speed, camera.Rotation);
			if (Keyboard.GetState().IsKeyDown(Keys.Space))
				camera.Position += Vector3.Transform(Vector3.Up * speed, camera.Rotation);

			camera.Update(true);
			effect.View = camera.View;

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			//GraphicsDevice.Clear(Color.WhiteSmoke);
			GraphicsDevice.Clear(Color.DimGray);

			Matrix m = Matrix.CreateTranslation(new Vector3(-resolution / 2, -resolution / 2, -resolution / 2));
			//effect.World = m *Matrix.CreateFromYawPitchRoll(rx, ry, 0);
			//effect.CurrentTechnique.Passes[0].Apply();
			//dc.Draw();
			dc.Draw(effect);

			//spriteBatch.Begin();
			//spriteBatch.Draw(pixel, new Vector2(mx * tile_size, my * tile_size), Color.White);
			//spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}
