using System;
using System.Collections.Generic;
using VRageMath;

namespace IOTA.ModularJumpGates.Extensions
{
	internal static partial class Extensions
	{
		public static Vector4D ToVector4D(this Color color)
		{
			return new Vector4D(color.R / 255d, color.G / 255d, color.B / 255d, color.A / 255d);
		}

		public static bool AtLeast<T>(this IEnumerable<T> source, int count)
		{
			if (source == null) throw new ArgumentNullException("source");
			int found = 0;
			foreach (T item in source) if (++found == count) return true;
			return false;
		}
	}
}
