using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Isosurface
{
	public struct Pair<T1, T2>
	{
		public T1 First { get; set; }
		public T2 Second { get; set; }
	}
}
