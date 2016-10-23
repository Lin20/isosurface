/* Main class for Isosurface project
 * Most the code written by Lin
 * Other pieces of code borrowed from existing implementations
 * https://github.com/aewallin/dualcontouring
 * https://code.google.com/p/simplexnoise/
 * http://www.volume-gfx.com/
 * All of this code is meant for experimenting purposes only!
 * Do not use any of it as a guide for how every algorithm works specifically
 * NONE of the algorithms are implemented to completion, or to the exact specification in the original papers
 * For example, the QEF solvers for Dual Contouring use a brute-force method of calculating the best point
 * In the 3D DC implementations, QEF solving is disabled altogether
 * The Dual Marching Squares implementation substitutes an error-reducing function with a separate, faster one
 * Some implementations might exhibit bugs, like improper connectivity in the 2D DC implementations
 * These should all be fixed in time though
 * The goal of this code is to provide the simplest, basic implementations of each algorithm for people looking to get better than Marching Cubes results
 * All of the implemented algorithms have their own namespace in their own folder, which means they don't depend on anything else
 * With the exception of the QEF solvers and Sampler class, and of course the abstract class ISurfaceAlgorithm
 * You can find all of the papers by using Google
 * Good luck!
 * https://github.com/Lin20/isosurface
 */

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

	public struct RawModel
	{
		public string Filename;
		public int Width;
		public int Height;
		public int Length;
		public float IsoLevel;
		public bool Flip;
		public int Bytes;
		public bool Mrc;

		public RawModel(string filename, int width, int height, int length, float isolevel, bool flip = true, int bytes = 1)
		{
			Filename = filename;
			Width = width;
			Height = height;
			Length = length;
			IsoLevel = isolevel;
			Flip = flip;
			Bytes = bytes;
			Mrc = filename.EndsWith(".mrc");
		}
	}

	public class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		Effect dn_effect;
		Effect reg_effect;
		Effect wire_effect;
		
		KeyboardState last_state;

		public int QualityIndex { get; set; }
		public int AlgorithmIndex { get; set; }
		public int ModelIndex { get; set; }

		public float[] Qualities = { 0.0f, 0.001f, 0.01f, 0.05f, 0.1f, 0.2f, 0.4f, 0.5f, 0.8f, 1.0f, 1.5f, 2.0f, 5.0f, 10.0f, 25.0f, 50.0f, 100.0f, 250.0f, 500.0f, 1000.0f, 2500.0f, 5000.0f, 10000.0f, 25000.0f, 50000.0f, 100000.0f };

		public RawModel[] Models = 
		{
			new RawModel("BostonTeapot", 178, 256, 256, 0.1f),
			new RawModel("engine", 128, 256, 256, 0.2f),
			new RawModel("bonsai", 256, 256, 256, 0.15f, false),
			new RawModel("lobster", 56, 324, 301, 0.18f),
			new RawModel("horse.mrc", 256,256,256, 1, false),
			new RawModel("dragon.mrc", 256,256,256, 1, false),
			new RawModel("dragon2.mrc", 256,256,256, 1, false),
			new RawModel("star.mrc", 256,256,256, 1, false),
			new RawModel("table.mrc", 256,256,256, 1, false),
			new RawModel("piano.mrc", 256,256,256, 1, false),
			new RawModel("statue.mrc", 256,256,256, 1, false)
		};

		/* Add new algorithms here to see them by pressing Tab */
		public Type[] AlgorithmTypes = { typeof(ManifoldDC.MDC3D) /*,typeof(DMCNeilson.DMCN)*//*, typeof(DualMarchingSquaresNeilson.DMSNeilson), typeof(DualMarchingSquares.DMS), typeof(UniformDualContouring2D.DC), typeof(AdaptiveDualContouring2D.ADC), typeof(UniformDualContouring.DC3D)*/, typeof(AdaptiveDualContouring.ADC3D) };

		public ISurfaceAlgorithm SelectedAlgorithm { get; set; }
		private Camera Camera { get; set; }

		public const int TileSize = 14;
		public const int Resolution = 64;

		public DrawModes DrawMode { get; set; }
		public RasterizerState RState { get; set; }
		public WireframeModes WireframeMode { get; set; }
		public DeferredShader DeferredRenderer { get; set; }

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			AdvancingFrontVIS2006.AdvancingFrontVIS2006.GetIdealEdgeLength(0, (Resolution / 2 - 2), 0);

			//DualMarchingSquaresNeilson.MarchingSquaresTableGenerator.PrintCaseTable();

			ModelIndex = -1;
			if (ModelIndex > -1)
				Sampler.ReadData(Models[ModelIndex], Resolution);

			float n = SimplexNoise.Noise(0, 0);
			RState = new RasterizerState();
			RState.CullMode = (Sampler.ImageData != null ? CullMode.CullCounterClockwiseFace : CullMode.CullClockwiseFace);
			GraphicsDevice.RasterizerState = RState;
			graphics.PreferredBackBufferWidth = 1600;
			graphics.PreferredBackBufferHeight = 900;
			graphics.PreferMultiSampling = true;
			graphics.ApplyChanges();

			IsMouseVisible = true;

			//effect = new BasicEffect(GraphicsDevice);
			reg_effect = Content.Load<Effect>("ShaderRegular");
			reg_effect.Parameters["ColorEnabled"].SetValue(true);
			dn_effect = Content.Load<Effect>("ShaderDN");
			dn_effect.Parameters["ColorEnabled"].SetValue(true);
			wire_effect = Content.Load<Effect>("WireShader");


			QualityIndex = 0;
			NextAlgorithm();

			//effect.VertexColorEnabled = true;

			Camera = new Camera(GraphicsDevice, new Vector3(-Resolution, Resolution, -Resolution), 1f);
			Camera.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), (float)graphics.PreferredBackBufferWidth / (float)graphics.PreferredBackBufferHeight, 0.1f, 1000.0f);
			if (SelectedAlgorithm.Is3D)
			{
				Camera.Update(true);
				//effect.View = Camera.View;
				reg_effect.Parameters["View"].SetValue(Camera.View);
				reg_effect.Parameters["Projection"].SetValue(Camera.Projection);
				dn_effect.Parameters["View"].SetValue(Camera.View);
				dn_effect.Parameters["Projection"].SetValue(Camera.Projection);
			}
			last_state = Keyboard.GetState();

			DrawMode = Isosurface.DrawModes.Mesh;
			WireframeMode = WireframeModes.Fill;

			base.Initialize();
		}

		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

			DeferredRenderer = new DeferredShader(GraphicsDevice, Content, spriteBatch);
		}

		public void NextAlgorithm()
		{
			SetAlgorithm(AlgorithmTypes[AlgorithmIndex]);
			AlgorithmIndex = (AlgorithmIndex + 1) % AlgorithmTypes.Length;
		}

		public void SetAlgorithm(Type t)
		{
			SelectedAlgorithm = (ISurfaceAlgorithm)Activator.CreateInstance(t, GraphicsDevice, Resolution, TileSize);
			UpdateQuality();

			if (SelectedAlgorithm.Is3D)
			{
				/*effect.View = Matrix.CreateLookAt(new Vector3(-1, 1, 1) * (float)Resolution, Vector3.Zero, Vector3.Up);
				effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), (float)graphics.PreferredBackBufferWidth / (float)graphics.PreferredBackBufferHeight, 1.0f, 1000.0f);
				effect.EnableDefaultLighting();*/
				Effect e = (SelectedAlgorithm.SpecialShader ? dn_effect : reg_effect);
				e.Parameters["View"].SetValue(Matrix.CreateLookAt(new Vector3(-1, 1, 1) * (float)Resolution, Vector3.Zero, Vector3.Up));
				if (Camera != null)
					e.Parameters["Projection"].SetValue(Camera.Projection);
			}
			else
			{
				/*effect.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, 1);
				effect.View = Matrix.Identity;*/
			}
		}

		private void UpdateQuality()
		{
			long time = SelectedAlgorithm.Contour(Qualities[QualityIndex]);
			System.Text.StringBuilder text = new System.Text.StringBuilder();

			text.Append(SelectedAlgorithm.Name).Append(" - ");

			string topology_type = (SelectedAlgorithm.Is3D ? "Triangles" : "Lines");
			if (SelectedAlgorithm.IsIndexed)
				text.Append((SelectedAlgorithm.IndexCount / (SelectedAlgorithm.Is3D ? 3 : 2)) + " " + topology_type + ", " + SelectedAlgorithm.VertexCount + " Vertices");
			else
				text.Append((SelectedAlgorithm.VertexCount / (SelectedAlgorithm.Is3D ? 3 : 2)) + " " + topology_type);

			if (SelectedAlgorithm.ExtraInformation != "")
				text.Append(", " + SelectedAlgorithm.ExtraInformation);

			text.Append(" (" + time + " ms)");

			text.Append(" - Quality " + Qualities[QualityIndex]);

			Window.Title = text.ToString();
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
				QualityIndex = (QualityIndex + 1) % Qualities.Length;
				UpdateQuality();
			}
			if (!last_state.IsKeyDown(Keys.Tab) && Keyboard.GetState().IsKeyDown(Keys.Tab))
			{
				NextAlgorithm();
			}

			if (!last_state.IsKeyDown(Keys.F) && Keyboard.GetState().IsKeyDown(Keys.F))
			{
				SelectedAlgorithm = (ISurfaceAlgorithm)Activator.CreateInstance(SelectedAlgorithm.GetType(), GraphicsDevice, Resolution, TileSize);
				ModelIndex = (ModelIndex + 1) % Models.Length;
				Sampler.ReadData(Models[ModelIndex], Resolution);
				UpdateQuality();
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

				/*if (WireframeMode != (WireframeModes.Fill | WireframeModes.Wireframe))
				{
					RState = new RasterizerState();
					RState.CullMode = CullMode.None;
					RState.FillMode = (WireframeMode == WireframeModes.Fill ? FillMode.Solid : FillMode.WireFrame);
					GraphicsDevice.RasterizerState = RState;
				}*/
			}

			if (!last_state.IsKeyDown(Keys.C) && Keyboard.GetState().IsKeyDown(Keys.C))
			{
				Camera.MouseLocked = !Camera.MouseLocked;
			}

			if (!last_state.IsKeyDown(Keys.M) && Keyboard.GetState().IsKeyDown(Keys.M))
			{
				if (SelectedAlgorithm.GetType() == typeof(ManifoldDC.MDC3D))
				{
					((ManifoldDC.MDC3D)SelectedAlgorithm).EnforceManifold = !((ManifoldDC.MDC3D)SelectedAlgorithm).EnforceManifold;
					UpdateQuality();
				}
			}

			if (SelectedAlgorithm.Is3D)
			{
				Camera.Update(true);
				//effect.View = Camera.View;
				(SelectedAlgorithm.SpecialShader ? dn_effect : reg_effect).Parameters["View"].SetValue(Camera.View);
			}

			last_state = Keyboard.GetState();

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			if (SelectedAlgorithm.SupportsDeferred)
			{
				DeferredRenderer.Draw(SelectedAlgorithm, Camera);
				return;
			}

			if (SelectedAlgorithm.Is3D)
				GraphicsDevice.Clear(Color.DimGray);
			else
				GraphicsDevice.Clear(Color.WhiteSmoke);

			Effect e = (SelectedAlgorithm.SpecialShader ? dn_effect : reg_effect);

			if (SelectedAlgorithm.Is3D)
				e.Parameters["World"].SetValue(Matrix.CreateTranslation(new Vector3(-Resolution / 2, -Resolution / 2, -Resolution / 2)));
			else
				e.Parameters["World"].SetValue(Matrix.Identity);

			if (SelectedAlgorithm.Is3D && (int)(WireframeMode & WireframeModes.Fill) != 0)
			{
				RasterizerState rs = new RasterizerState();
				rs.CullMode = (Sampler.ImageData != null ? CullMode.CullCounterClockwiseFace : CullMode.CullClockwiseFace);
				rs.FillMode = FillMode.Solid;
				rs.DepthBias = 0;
				GraphicsDevice.RasterizerState = rs;
			}

			if (!SelectedAlgorithm.Is3D || WireframeMode != WireframeModes.Wireframe)
				SelectedAlgorithm.Draw(e, false, DrawMode);

			if (SelectedAlgorithm.Is3D && (int)(WireframeMode & WireframeModes.Wireframe) != 0 && !SelectedAlgorithm.SupportsDeferred)
			{
				if (!SelectedAlgorithm.CustomWireframe)
				{
					RasterizerState rs = new RasterizerState();
					rs.CullMode = (Sampler.ImageData != null ? CullMode.CullCounterClockwiseFace : CullMode.CullClockwiseFace);
					rs.FillMode = FillMode.WireFrame;
					//rs.DepthBias = -0.0001f;
					GraphicsDevice.RasterizerState = rs;
					e.Parameters["ColorEnabled"].SetValue(false);
					SelectedAlgorithm.Draw(e, false, DrawMode);
					e.Parameters["ColorEnabled"].SetValue(true);
				}
				else
				{
					//effect.Parameters["ColorEnabled"].SetValue(false);
					SelectedAlgorithm.DrawWireframe(Camera, wire_effect, Matrix.CreateTranslation(new Vector3(-Resolution / 2, -Resolution / 2, -Resolution / 2)));
					//effect.Parameters["ColorEnabled"].SetValue(true);
				}
			}

			base.Draw(gameTime);
		}
	}
}
