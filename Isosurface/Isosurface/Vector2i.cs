using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DC2D
{
	public struct Vector2i
	{
		public int x;
		public int y;

		public Vector2i(int x = 0, int y = 0)
		{
			this.x = 0;
			this.y = 0;
		}

		public static Vector2i operator +(Vector2i a, Vector2i b)
		{
			return new Vector2i(a.x + b.x, a.y + b.y);
		}
	}
}
