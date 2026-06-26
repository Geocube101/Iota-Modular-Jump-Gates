using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Public Static Methods
		public static void DrawTransparentLine(Vector3D start, Vector3D end, MyStringId? material, ref Vector4 color, float thickness, MyBillboard.BlendTypeEnum blendtype = MyBillboard.BlendTypeEnum.Standard)
		{
			Vector3D dir = end - start;
			float len = (float) dir.Length();
			MyTransparentGeometry.AddLineBillboard(material ?? MyJumpGateModSession.Instance.Materials.GizmoDrawLine, color, start, dir.Normalized(), len, thickness, blendtype);
		}

		/// <summary>
		/// Executes a callback on all elements of an enumerable in parallel<br />
		/// Blocks until all threads are complete
		/// </summary>
		/// <typeparam name="T">The collection type</typeparam>
		/// <param name="enumerable">The collection enumerate</param>
		/// <param name="action">The callback to execute</param>
		public static void ParallelFor<T>(IEnumerable<T> enumerable, Action<T> action)
		{
			if (enumerable == null) return;
			int count = 0;
			object mutex = new object();

			foreach (T element in enumerable)
			{
				lock (mutex) ++count;
				MyAPIGateway.Parallel.Start(() => {
					try
					{
						action(element);
					}
					finally
					{
						lock (mutex) --count;
					}
				});
			}

			while (count > 0) MyAPIGateway.Parallel.Sleep(new TimeSpan(10));
		}

		/// <summary>
		/// Converts a world position vector to a local position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorP(MatrixD world_matrix, Vector3D world_pos)
		{
			return Vector3D.TransformNormal(world_pos - world_matrix.Translation, MatrixD.Transpose(world_matrix));
		}

		/// <summary>
		/// Converts a world position vector to a local position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorP(ref MatrixD world_matrix, Vector3D world_pos)
		{
			MatrixD transposed;
			MatrixD.Transpose(ref world_matrix, out transposed);
			return Vector3D.TransformNormal(world_pos - world_matrix.Translation, ref transposed);
		}

		/// <summary>
		/// Converts a local position vector to a world position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorP(MatrixD world_matrix, Vector3D local_pos)
		{
			return Vector3D.Transform(local_pos, ref world_matrix);
		}

		/// <summary>
		/// Converts a local position vector to a world position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorP(ref MatrixD world_matrix, Vector3D local_pos)
		{
			return Vector3D.Transform(local_pos, ref world_matrix);
		}

		/// <summary>
		/// Converts a world direction vector to a local direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorD(MatrixD world_matrix, Vector3D world_direction)
		{
			return Vector3D.TransformNormal(world_direction, MatrixD.Transpose(world_matrix));
		}

		/// <summary>
		/// Converts a world direction vector to a local direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorD(ref MatrixD world_matrix, Vector3D world_direction)
		{
			MatrixD transposed;
			MatrixD.Transpose(ref world_matrix, out transposed);
			return Vector3D.TransformNormal(world_direction, ref transposed);
		}

		/// <summary>
		/// Converts a local direction vector to a world direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorD(MatrixD world_matrix, Vector3D local_direction)
		{
			return Vector3D.TransformNormal(local_direction, ref world_matrix);
		}

		/// <summary>
		/// Converts a local direction vector to a world direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorD(ref MatrixD world_matrix, Vector3D local_direction)
		{
			return Vector3D.TransformNormal(local_direction, ref world_matrix);
		}

		/// <summary>
		/// Gets a player by player ID
		/// </summary>
		/// <param name="player_id">The player ID</param>
		/// <returns>The specified player or null</returns>
		public static IMyPlayer GetPlayerByID(long player_id)
		{
			List<IMyPlayer> players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players, (player) => player.IdentityId == player_id);
			return players.FirstOrDefault();
		}

		/// <summary>
		/// Converts a value to its simplist metric unit<br />
		/// Example: 1000 -> 1 K<br />
		/// Example: 1000000 -> 1 M<br />
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <param name="unit">The base unit</param>
		/// <param name="places">The number of places to round to</param>
		/// <returns>The resulting value</returns>
		public static string AutoconvertMetricUnits(double value, string unit, int places)
		{
			if (double.IsPositiveInfinity(value)) return "INFINITE";
			else if (double.IsNegativeInfinity(value)) return "-INFINITE";
			else if (double.IsNaN(value)) return "NaN";
			else if (value == 0) return $"0 {unit}";
			string[] prefixes_up = { "", "K", "M", "G", "T", "P", "E", "Z", "Y", "R", "Q" };
			string[] prefixes_down = { "", "m", "μ", "n", "p", "f", "a", "z", "y", "r", "q" };
			int index = (int) Math.Log(value, 1000);
			index = MathHelper.Clamp(index, -10, 10);
			return $"{Math.Round(value / Math.Pow(1000, index), places)} {((index > 0) ? prefixes_up[index] : prefixes_down[-index])}{unit}";
		}

		/// <summary>
		/// Converts a value to base 10 scientific notation
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <param name="places">The number of places to round to</param>
		/// <param name="e">The value used in place of 'E'</param>
		/// <returns>The resulting value in scientific notation</returns>
		public static string AutoconvertSciNotUnits(double value, int places, string e = " E ")
		{
			if (double.IsPositiveInfinity(value)) return "INFINITE";
			else if (double.IsNegativeInfinity(value)) return "-INFINITE";
			else if (double.IsNaN(value)) return "NaN";
			else if (value == 0) return "0";
			int l10 = (int) Math.Floor(Math.Log10(Math.Abs(value)));
			string sign = (value < 0) ? "-" : "";
			value /= Math.Pow(10, l10);
			return $"{sign}{Math.Round(value, places)}{e}{l10}";
		}

		/// <summary>
		/// Converts a value to time units
		/// </summary>
		/// <param name="seconds">The seconds to convert</param>
		/// <param name="places">The number of places to round to</param>
		/// <returns>The resulting value in time notation</returns>
		public static string AutoconvertTimeUnits(double seconds, int places)
		{
			if (double.IsInfinity(seconds)) return "INF";
			else if (double.IsNaN(seconds)) return "NaN";
			seconds = Math.Round(seconds, places + 2);
			if (seconds == 1) return $"1 second";
			else if (seconds < 60) return $"{Math.Round(seconds, places)} seconds";
			else if (seconds == 60) return $"1 minute";
			else if (seconds < 3600) return $"{Math.Round(seconds / 60, places)} minutes";
			else if (seconds == 3600) return $"1 hour";
			else if (seconds < 86400) return $"{Math.Round(seconds / 3600, places)} hours";
			else if (seconds == 86400) return $"1 day";
			else if (seconds < 604800) return $"{Math.Round(seconds / 86400, places)} days";
			else if (seconds == 604800) return $"1 week";
			else return $"{Math.Round(seconds / 604800, places)} weeks";
		}

		/// <summary>
		/// Converts a value to HH:MM:SS format
		/// </summary>
		/// <param name="total_seconds">The total seconds to convert</param>
		/// <returns>The time string</returns>
		public static string AutoconvertTimeHHMMSS(double total_seconds)
		{
			if (double.IsInfinity(total_seconds)) return "--:--:--";
			else if (double.IsNaN(total_seconds) || total_seconds <= 0) return "00:00:00";
			uint hours = (uint) (total_seconds / 3600);
			uint minutes = (uint) (total_seconds % 3600 / 60d);
			uint seconds = (uint) (total_seconds % 60d);
			return $"{hours:00}:{minutes:00}:{seconds:00}";
		}
		#endregion
	}
}
