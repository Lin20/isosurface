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

		//TODO: Create simple interface to allow switching dual contouring method on-the-fly
		ADC3D dc;

		const int tile_size = 14;
		const int resolution = 64;

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
			//rs.FillMode = FillMode.WireFrame;
			GraphicsDevice.RasterizerState = rs;
			graphics.PreferredBackBufferWidth = tile_size * resolution;
			graphics.PreferredBackBufferHeight = tile_size * resolution;
			graphics.ApplyChanges();

			IsMouseVisible = true;

			effect = new BasicEffect(GraphicsDevice);

			//Ugly method of switching between 2D and 3D; fix later
			if (true)
			{
				effect.View = Matrix.CreateLookAt(new Vector3(-1, 1, 1) * 64.0f, Vector3.Zero, Vector3.Up);
				effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), 1.0f, 1.0f, 1000.0f);
				effect.EnableDefaultLighting();
			}
			else
				effect.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, 1);
			effect.VertexColorEnabled = true;

			//VertexPositionColor[] vertices = { new VertexPositionColor(new Vector3(10, 10, 0), Color.Red), new VertexPositionColor(new Vector3(10, 60, 0), Color.Blue) };
			//buffer.SetData<VertexPositionColor>(vertices, 0, 2);

			Sampler.Resolution = resolution;
			dc = new ADC3D(GraphicsDevice, 64, tile_size);
			dc.Contour();

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

			// TODO: use this.Content to load your game content here
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
		protected override void Update(GameTime gameTime)
		{
			// Allows the game to exit
			if (Keyboard.GetState().IsKeyDown(Keys.Escape))
				this.Exit();

			mx = Mouse.GetState().X / tile_size;
			my = Mouse.GetState().Y / tile_size;
			rx = (float)Mouse.GetState().X / (float)resolution * MathHelper.TwoPi * 0.25f;
			ry = (float)Mouse.GetState().Y / (float)resolution * MathHelper.TwoPi * 0.25f;
			if (Mouse.GetState().LeftButton == ButtonState.Pressed && mx > 0 && my > 0 && mx < resolution - 1 && my < resolution - 1)
			{
				//dc.GenerateAt(mx, my);
			}

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
			effect.World = m *Matrix.CreateFromYawPitchRoll(rx, ry, 0);
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
