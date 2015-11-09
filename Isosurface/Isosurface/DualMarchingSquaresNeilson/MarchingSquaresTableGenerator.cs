using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Isosurface.DualMarchingSquaresNeilson
{
	public class MarchingSquaresTableGenerator
	{
		public class FaceEdges
		{
			public int Count { get; set; }
			public Pair<int, int>[] Edges { get; set; }

			public FaceEdges()
			{
				Count = 0;
				Edges = new Pair<int, int>[2];
				Edges[0] = new Pair<int, int>();
				Edges[1] = new Pair<int, int>();
				Edges[0].First = -1;
				Edges[0].Second = -1;
				Edges[1].First = -1;
				Edges[1].Second = -1;
			}
		}

		public static FaceEdges[] CaseTable { get; set; }

		public static void SetCaseTable()
		{
			CaseTable = new FaceEdges[1 << 4];
			for (int idx = 0; idx < 1 << 4; idx++)
			{
				int c1 = 0, c2 = 0;
				CaseTable[idx] = new FaceEdges();

				for (int i = 0; i < 4; i++)
				{
					OrientedEdgeCorners(i, ref c1, ref c2);
					if ((idx & (1 << c1)) == 0 && (idx & (1 << c2)) != 0)
						CaseTable[idx].Edges[CaseTable[idx].Count++].First = i;
				}

				CaseTable[idx].Count = 0;
				for (int i = 0; i < 4; i++)
				{
					OrientedEdgeCorners(i, ref c1, ref c2);
					if ((idx & (1 << c1)) != 0 && (idx & (1 << c2)) == 0)
						CaseTable[idx].Edges[CaseTable[idx].Count++].Second = i;
				}
			}
		}

		public static void PrintCaseTable()
		{
			SetCaseTable();
			for (int idx = 0; idx < 1 << 4; idx++)
			{
				FaceEdges f = CaseTable[idx];
				StringBuilder s = new StringBuilder();
				s.Append(idx.ToString() + ": ");
				s.Append("Count=" + f.Count);
				s.Append("  Edges=");
				for (int i = 0; i < 2; i++)
				{
					s.Append(f.Edges[i].First);
					s.Append(", " + f.Edges[i].Second + (i == 0 ? ", " : ""));
				}
				Console.WriteLine(s.ToString());
			}
		}

		private static int CornerIndex(int x, int y)
		{
			return (y << 1) | x;
		}

		private static void FactorCornerIndex(int idx, ref int x, ref int y)
		{
			x = (idx >> 0) % 2;
			y = (idx >> 1) % 2;
		}

		private static int EdgeIndex(int orientation, int i)
		{
			switch (orientation)
			{
				case 0: //x
					if (i == 0)
						return 0;
					else
						return 2;
				case 1: //y
					if (i == 0)
						return 3;
					else
						return 1;
			}

			return -1;
		}

		private static void FactorEdgeIndex(int idx, ref int orientation, ref int i)
		{
			switch (idx)
			{
				case 0:
				case 2:
					orientation = 0;
					i = idx / 2;
					return;

				case 1:
				case 3:
					orientation = 1;
					i = ((idx / 2) + 1) % 2;
					return;
			}
		}

		private static void EdgeCorners(int idx, ref int c1, ref int c2)
		{
			int orientation = 0, i = 0;
			FactorEdgeIndex(idx, ref orientation, ref i);

			switch (orientation)
			{
				case 0:
					c1 = CornerIndex(0, i);
					c2 = CornerIndex(1, i);
					break;
				case 1:
					c1 = CornerIndex(i, 0);
					c2 = CornerIndex(i, 1);
					break;
			}
		}

		private static void OrientedEdgeCorners(int idx, ref int c1, ref int c2)
		{
			int orientation = 0, i = 0;
			FactorEdgeIndex(idx, ref orientation, ref i);

			switch (orientation)
			{
				case 0:
					c1 = CornerIndex(i & 1, i);
					c2 = CornerIndex((i + 1) & 1, i);
					break;
				case 1:
					c1 = CornerIndex(i, (i + 1) & 1);
					c2 = CornerIndex(i, (i) & 1);
					break;
			}
		}

		private static int ReflectEdgeIndex(int idx, int edge_index)
		{
			int orientation = edge_index % 2;
			int o = 0, i = 0;
			FactorEdgeIndex(idx, ref o, ref i);

			if (o != orientation)
				return idx;
			return EdgeIndex(o, (i + 1) % 2);
		}

		private static int ReflectCornerIndex(int idx, int edge_index)
		{
			int orientation = edge_index % 2;
			int x = 0, y = 0;
			FactorCornerIndex(idx, ref x, ref y);

			if (orientation == 0)
				return CornerIndex((x + 1) % 2, y);
			else if (orientation == 1)
				return CornerIndex(x, (y + 1) % 2);
			else
				return -1;
		}
	}
}
