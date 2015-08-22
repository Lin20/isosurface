using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DC2D
{
	public enum QuadtreeNodeType
	{
		None,
		Internal,
		Pseudo,
		Leaf
	}

	public class QuadtreeDrawInfo
	{
		public QuadtreeDrawInfo()
		{
			index = -1;
			corners = 0;
		}

		public int index;
		public int corners;
		public Point position;
		public Point averageNormal;
		//QEF qef;
	}

	public class QuadtreeNode
	{
		public QuadtreeNode()
		{
			type = QuadtreeNodeType.None;
			min = new Point(0, 0);
			size = 0;
			children = new QuadtreeNode[4];
			draw_info = new QuadtreeDrawInfo();
		}

		public QuadtreeNodeType type;
		public Point min;
		public int size;
		public QuadtreeNode[] children;
		public QuadtreeDrawInfo draw_info;
	}

	public class Quadtree
	{
		
	}
}
