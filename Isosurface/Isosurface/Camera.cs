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

namespace Isosurface
{
	public class Camera
	{
		public GraphicsDevice Device { get; private set; }
		public Vector3 Position { get; set; }

		public Matrix View { get; private set; }
		public Matrix Projection { get; private set; }
		public BoundingFrustum Frustrum { get; private set; }

		public Matrix Rotation { get; private set; }
		public float RotationX { get; set; }
		public float RotationY { get; set; }

		public float RotationSpeed { get; set; }
		private Vector2 _center;
		public MouseState OriginalMouseState { get; set; }

		public bool MouseLocked { get; set; }

		public float TargetDistance { get; set; }

		public Camera(GraphicsDevice d, Vector3 position, float rotSpeed)
		{
			Device = d;
			Position = position;
			RotationSpeed = rotSpeed;
			RotationX = -MathHelper.PiOver4 * 3.0f;
			RotationY = -MathHelper.Pi * 0.2f;
			MouseLocked = true;
			TargetDistance = 12f;

			_center = new Vector2(d.Viewport.Width / 2, d.Viewport.Height / 2);
			Mouse.SetPosition((int)_center.X, (int)_center.Y);
			OriginalMouseState = Mouse.GetState();

			Update();
			UpdateViewMatrix();
			Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, d.Viewport.AspectRatio, 0.01f, 1000f);
		}

		public void Update(bool forceView = false)
		{
			float speed = 0.05f;
			if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
				speed *= 10.0f;
			if (Keyboard.GetState().IsKeyDown(Keys.W))
				Position += Vector3.Transform(Vector3.Forward * speed, Rotation);
			else if (Keyboard.GetState().IsKeyDown(Keys.S))
				Position += Vector3.Transform(Vector3.Backward * speed, Rotation);
			if (Keyboard.GetState().IsKeyDown(Keys.D))
				Position += Vector3.Transform(Vector3.Right * speed, Rotation);
			else if (Keyboard.GetState().IsKeyDown(Keys.A))
				Position += Vector3.Transform(Vector3.Left * speed, Rotation);

			MouseState currentMouseState = Mouse.GetState();
			if (currentMouseState != OriginalMouseState)
			{
				float xDifference = currentMouseState.X - OriginalMouseState.X;
				float yDifference = currentMouseState.Y - OriginalMouseState.Y;
				RotationX -= RotationSpeed * xDifference * 0.01f;
				RotationY -= RotationSpeed * yDifference * 0.01f;
				if (MouseLocked)
					Mouse.SetPosition((int)_center.X, (int)_center.Y);
				OriginalMouseState = Mouse.GetState();
				UpdateViewMatrix();
			}
			else if (forceView)
				UpdateViewMatrix();
		}

		public void UpdateViewMatrix()
		{
			Rotation = Matrix.CreateRotationX(RotationY) * Matrix.CreateRotationY(RotationX);

			Vector3 cameraRotatedTarget = Vector3.Transform(Vector3.Forward, Rotation);
			Vector3 cameraFinalTarget = Position + cameraRotatedTarget;

			Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);
			Vector3 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, Rotation);

			View = Matrix.CreateLookAt(Position, cameraFinalTarget, cameraRotatedUpVector);
			Frustrum = new BoundingFrustum(View * Projection);
		}

		public Ray CastRay()
		{
			Vector3 near = Device.Viewport.Unproject(new Vector3(OriginalMouseState.X, OriginalMouseState.Y, 0), Projection, View, Matrix.Identity);
			Vector3 far = Device.Viewport.Unproject(new Vector3(OriginalMouseState.X, OriginalMouseState.Y, 1), Projection, View, Matrix.Identity);
			Vector3 direction = far - near;
			direction.Normalize();
			return new Ray(near, direction);
		}
	}
}