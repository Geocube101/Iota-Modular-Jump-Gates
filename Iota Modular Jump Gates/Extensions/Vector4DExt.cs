using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IOTA.ModularJumpGates.Extensions
{
	internal static class Vector4DExt
	{
		public static void MinMax(double a, double b, out double min, out double max)
		{
			if (a < b)
			{
				min = a;
				max = b;
			}
			else
			{
				min = b;
				max = a;
			}
		}

		public static void MinMax(ref Vector4D a, ref Vector4D b, out Vector4D min, out Vector4D max)
		{
			Vector4DExt.MinMax(a.X, b.X, out min.X, out max.X);
			Vector4DExt.MinMax(a.Y, b.Y, out min.Y, out max.Y);
			Vector4DExt.MinMax(a.Z, b.Z, out min.Z, out max.Z);
			Vector4DExt.MinMax(a.W, b.W, out min.W, out max.W);
		}

		public static Vector4 ToVector4(this Vector4D v)
		{
			return new Vector4(
				(float) v.X,
				(float) v.Y,
				(float) v.Z,
				(float) v.W
			);
		}
	}
}
