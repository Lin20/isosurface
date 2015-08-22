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
		DC dc;

		const int tile_size = 16;
		const int resolution = 60;

		Texture2D pixel;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			GraphicsDevice.RasterizerState = RasterizerState.CullNone;
			graphics.PreferredBackBufferWidth = tile_size * resolution;
			graphics.PreferredBackBufferHeight = tile_size * resolution;
			graphics.ApplyChanges();

			IsMouseVisible = true;

			effect = new BasicEffect(GraphicsDevice);
			effect.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, 1);
			effect.VertexColorEnabled = true;

			//VertexPositionColor[] vertices = { new VertexPositionColor(new Vector3(10, 10, 0), Color.Red), new VertexPositionColor(new Vector3(10, 60, 0), Color.Blue) };
			//buffer.SetData<VertexPositionColor>(vertices, 0, 2);

			dc = new DC(GraphicsDevice, 64, tile_size);
			dc.Contour();

			pixel = new Texture2D(GraphicsDevice, tile_size, tile_size);
			Color[] pixels = new Color[256];
			for (int i = 0; i < 256; i++)
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
		protected override void Update(GameTime gameTime)
		{
			// Allows the game to exit
			if(Keyboard.GetState().IsKeyDown(Keys.Escape))
				this.Exit();

			mx = Mouse.GetState().X / tile_size;
			my = Mouse.GetState().Y / tile_size ;
			if (Mouse.GetState().LeftButton == ButtonState.Pressed && mx > 0 && my > 0 && mx < resolution - 1 && my < resolution - 1)
			{
				dc.GenerateAt(mx, my);
			}

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.WhiteSmoke);

			effect.CurrentTechnique.Passes[0].Apply();
			dc.Draw();

			spriteBatch.Begin();
			spriteBatch.Draw(pixel, new Vector2(mx * tile_size, my * tile_size), Color.White);
			spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}
