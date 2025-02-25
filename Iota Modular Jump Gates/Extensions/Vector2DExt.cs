using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IOTA.ModularJumpGates.Extensions
{
	internal static partial class Extensions
	{
		public static double AbsMax(this Vector2D vector)
		{
			return Math.Max(Math.Abs(vector.X), Math.Abs(vector.Y));
		}
	}
}
