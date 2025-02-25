using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.Util
{
	/// <summary>
	/// Class representing a 3D ellipsoid
	/// </summary>
	public struct BoundingEllipsoidD : IEquatable<BoundingEllipsoidD>
	{
		#region Public Static Variables
		/// <summary>
		/// A zero ellipse<br />
		/// An ellipse at world center with no radii
		/// </summary>
		public static readonly BoundingEllipsoidD Zero = new BoundingEllipsoidD(ref Vector3D.Zero, ref MatrixD.Zero);
		#endregion

		#region Public Variables
		/// <summary>
		/// The radii of this ellipse
		/// </summary>
		public Vector3D Radii;

		/// <summary>
		/// The world matrix of this ellipse
		/// </summary>
		public MatrixD WorldMatrix;
		#endregion

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first BoundingEllipsoidD operand</param>
		/// <param name="b">The second BoundingEllipsoidD operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(BoundingEllipsoidD a, BoundingEllipsoidD b)
		{
			if (ReferenceEquals(a, b)) return true;
			return a.Equals(b);
		}

		/// <summary>
		/// Overloads inequality operator "!=" to check inequality
		/// </summary>
		/// <param name="a">The first BoundingEllipsoidD operand</param>
		/// <param name="b">The second BoundingEllipsoidD operand</param>
		/// <returns>Inequality</returns>
		public static bool operator !=(BoundingEllipsoidD a, BoundingEllipsoidD b)
		{
			return !(a == b);
		}

		/// <summary>
		/// Extends the radii of this ellipse
		/// </summary>
		/// <param name="src">The ellipse to extend</param>
		/// <param name="extends">The radii values to exend by</param>
		/// <returns>The extended ellipse</returns>
		public static BoundingEllipsoidD operator +(BoundingEllipsoidD src, double extends)
		{
			return new BoundingEllipsoidD(src.Radii + extends, ref src.WorldMatrix);
		}

		/// <summary>
		/// Extends the radii of this ellipse
		/// </summary>
		/// <param name="src">The ellipse to extend</param>
		/// <param name="extends">The radii values to exend by</param>
		/// <returns>The extended ellipse</returns>
		public static BoundingEllipsoidD operator *(BoundingEllipsoidD src, double extends)
		{
			return new BoundingEllipsoidD(src.Radii * extends, ref src.WorldMatrix);
		}
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Deserializes a BoundingEllipsoidD from a byte array<br />
		/// <i>As for some reason MatrixD is not ProtoBuf compatible</i>
		/// </summary>
		/// <param name="bytes">The byte array to read from</param>
		/// <param name="offset">The start index to begin deserialization</param>
		/// <returns></returns>
		public static BoundingEllipsoidD FromSerialized(byte[] bytes, int offset)
		{
			double[] doubles = new double[12];
			for (int i = 0; i < doubles.Length; ++i) doubles[i] = BitConverter.ToDouble(bytes, offset + i * sizeof(double));
			Vector3D radii = new Vector3D(doubles[0], doubles[1], doubles[2]);
			Vector3D forward = new Vector3D(doubles[3], doubles[4], doubles[5]);
			Vector3D up = new Vector3D(doubles[6], doubles[7], doubles[8]);
			Vector3D translation = new Vector3D(doubles[9], doubles[10], doubles[11]);
			MatrixD matrix;
			MatrixD.CreateWorld(ref translation, ref forward, ref up, out matrix);
			return new BoundingEllipsoidD(ref radii, ref matrix);
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radii">The ellipsoid's radii</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(Vector3D radii, MatrixD world_matrix)
		{
			this.Radii = Vector3D.Abs(radii);
			this.WorldMatrix = world_matrix;
		}

		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radii">The ellipsoid's radii</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(ref Vector3D radii, MatrixD world_matrix)
		{
			this.Radii = Vector3D.Abs(radii);
			this.WorldMatrix = world_matrix;
		}

		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radii">The ellipsoid's radii</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(Vector3D radii, ref MatrixD world_matrix)
		{
			this.Radii = Vector3D.Abs(radii);
			this.WorldMatrix = world_matrix;
		}

		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radii">The ellipsoid's radii</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(ref Vector3D radii, ref MatrixD world_matrix)
		{
			this.Radii = Vector3D.Abs(radii);
			this.WorldMatrix = world_matrix;
		}
		#endregion

		#region "object" Methods
		/// <summary>
		/// Checks if this BoundingEllipsoidD equals another
		/// </summary>
		/// <param name="obj">The object to check</param>
		/// <returns>Equality</returns>
		public override bool Equals(object obj)
		{
			if (!(obj is BoundingEllipsoidD)) return false;
			return this.Equals((BoundingEllipsoidD) obj);
		}

		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>This ellipsoid's radii xor this ellipsoid's world matrix</returns>
		public override int GetHashCode()
		{
			return this.Radii.GetHashCode() ^ this.WorldMatrix.GetHashCode();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Draws the axial lines representing this bounding ellipsoid
		/// </summary>
		/// <param name="color">The line color</param>
		/// <param name="wire_divide_ratio">The number of angle divisions<br />Higher values are more accurate but have higher time complexity</param>
		/// <param name="thickness">The line thickness (in meters?)</param>
		/// <param name="material">The line material</param>
		/// <param name="intensity">The line color intensity</param>
		public void Draw(Color color, int wire_divide_ratio, float thickness, MyStringId? material = null, float intensity = 1)
		{
			Vector4 color_v4 = color.ToVector4();
			color_v4 *= intensity;
			double angle_steps = 2 * Math.PI / wire_divide_ratio;

			for (double i = 0; i < 2 * Math.PI; i += angle_steps)
			{
				double x1 = this.Radii.X * Math.Cos(i);
				double y1 = this.Radii.Y * Math.Sin(i);
				double z1 = this.Radii.Z * Math.Sin(i);

				double x2 = this.Radii.X * Math.Cos(i + angle_steps);
				double y2 = this.Radii.Y * Math.Sin(i + angle_steps);
				double z2 = this.Radii.Z * Math.Sin(i + angle_steps);

				Vector3D dim11 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x1, y1, 0));
				Vector3D dim12 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x2, y2, 0));

				Vector3D dim21 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x1, 0, z1));
				Vector3D dim22 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x2, 0, z2));

				y1 = this.Radii.Y * Math.Cos(i);
				y2 = this.Radii.Y * Math.Cos(i + angle_steps);

				Vector3D dim31 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(0, y1, z1));
				Vector3D dim32 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(0, y2, z2));

				MySimpleObjectDraw.DrawLine(dim11, dim12, material, ref color_v4, thickness);
				MySimpleObjectDraw.DrawLine(dim21, dim22, material, ref color_v4, thickness);
				MySimpleObjectDraw.DrawLine(dim31, dim32, material, ref color_v4, thickness);
			}
		}

		/// <summary>
		/// Draws the full ellipsoid (without faces)
		/// </summary>
		/// <param name="color">The line color</param>
		/// <param name="wire_divide_ratio">The number of angle divisions<br />Higher values are more accurate but have higher time complexity</param>
		/// <param name="thickness">The line thickness (in meters?)</param>
		/// <param name="material">The line material</param>
		/// <param name="intensity">The line color intensity</param>
		public void Draw2(Color color, int wire_divide_ratio, float thickness, MyStringId? material = null, float intensity = 1)
		{
			Vector4 color_v4 = color.ToVector4() * intensity;
			double angle_steps = 2 * Math.PI / wire_divide_ratio;
			List<List<Vector3D>> points = new List<List<Vector3D>>();

			for (double i = 0; i < 2 * Math.PI; i += angle_steps)
			{
				List<Vector3D> _points = new List<Vector3D>();

				for (double j = 0; j < 2 * Math.PI; j += angle_steps)
				{
					double x1 = this.Radii.X * Math.Sin(j) * Math.Cos(i);
					double y1 = this.Radii.Y * Math.Sin(j) * Math.Sin(i);
					double z1 = this.Radii.Z * Math.Cos(j);

					double _j = j + angle_steps;

					double x2 = this.Radii.X * Math.Sin(_j) * Math.Cos(i);
					double y2 = this.Radii.Y * Math.Sin(_j) * Math.Sin(i);
					double z2 = this.Radii.Z * Math.Cos(_j);

					Vector3D pos1 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x1, y1, z1));
					Vector3D pos2 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x2, y2, z2));
					_points.Add(pos1);
					MySimpleObjectDraw.DrawLine(pos1, pos2, material, ref color_v4, thickness);
				}

				points.Add(_points);
			}

			if (points.Count == 0) return;

			for (int i = 0; i < points[0].Count; ++i)
			{
				List<Vector3D> subpoints = points.Select((_points) => _points[i]).ToList();
				int count = subpoints.Count - 1;
				for (int a = 0; a < count; ++a) MySimpleObjectDraw.DrawLine(subpoints[a], subpoints[a + 1], material, ref color_v4, thickness);
				MySimpleObjectDraw.DrawLine(subpoints[0], subpoints[count], material, ref color_v4, thickness);
			}
		}

		/// <summary>
		/// Checks if the specified point is within this ellipsoid
		/// </summary>
		/// <param name="world_coord">The world coordinate</param>
		/// <returns>true if point is inside</returns>
		public bool IsPointInEllipse(ref Vector3D world_coord)
		{
			return this.IsLocalPointInEllipse(MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.WorldMatrix, world_coord));
		}

		/// <summary>
		/// Checks if the specified point is within this ellipsoid
		/// </summary>
		/// <param name="world_coord">The world coordinate</param>
		/// <returns>true if point is inside</returns>
		public bool IsPointInEllipse(Vector3D world_coord)
		{
			return this.IsLocalPointInEllipse(MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.WorldMatrix, world_coord));
		}

		/// <summary>
		/// Checks if the specified point is within this ellipsoid
		/// </summary>
		/// <param name="local_coord">The ellipsoid local coordinate</param>
		/// <returns>True if point is inside</returns>
		public bool IsLocalPointInEllipse(ref Vector3D local_coord)
		{
			double xc2 = Math.Pow(local_coord.X, 2);
			double yc2 = Math.Pow(local_coord.Y, 2);
			double zc2 = Math.Pow(local_coord.Z, 2);
			double xr2 = Math.Pow(this.Radii.X, 2);
			double yr2 = Math.Pow(this.Radii.Y, 2);
			double zr2 = Math.Pow(this.Radii.Z, 2);

			return xc2 / xr2 + yc2 / yr2 + zc2 / zr2 <= 1;
		}

		/// <summary>
		/// Checks if the specified point is within this ellipsoid
		/// </summary>
		/// <param name="local_coord">The ellipsoid local coordinate</param>
		/// <returns>True if point is inside</returns>
		public bool IsLocalPointInEllipse(Vector3D local_coord)
		{
			double xc2 = Math.Pow(local_coord.X, 2);
			double yc2 = Math.Pow(local_coord.Y, 2);
			double zc2 = Math.Pow(local_coord.Z, 2);
			double xr2 = Math.Pow(this.Radii.X, 2);
			double yr2 = Math.Pow(this.Radii.Y, 2);
			double zr2 = Math.Pow(this.Radii.Z, 2);

			return xc2 / xr2 + yc2 / yr2 + zc2 / zr2 <= 1;
		}

		/// <summary>
		/// Checks if this ellipsoid intersects another<br />
		/// Due to inaccurasies in ellipsoid angles, overlap is checked with a O(log(n)) algorithim instead of O(1) math
		/// </summary>
		/// <param name="ellipse">The other ellipsoid</param>
		/// <returns>Whether this and ellipse intersect</returns>
		public bool Intersects(ref BoundingEllipsoidD ellipse)
		{
			Vector3D end = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.WorldMatrix, ellipse.WorldMatrix.Translation);
			Vector3D start = Vector3D.Zero;
			Vector3D midpoint = end / 2d;

			while ((start - end).Length() > 1d)
			{
				if (this.IsLocalPointInEllipse(ref midpoint)) start = midpoint;
				else end = midpoint;
				midpoint = (start + end) / 2d;
			}

			return ellipse.IsPointInEllipse(MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, midpoint));
		}

		/// <summary>
		/// Checks if this ellipsoid intersects another<br />
		/// Due to inaccurasies in ellipsoid angles, overlap is checked with a O(log(n)) algorithim instead of O(1) math
		/// </summary>
		/// <param name="ellipse">The other ellipsoid</param>
		/// <returns>Whether this and ellipse intersect</returns>
		public bool Intersects(BoundingEllipsoidD ellipse)
		{
			Vector3D end = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.WorldMatrix, ellipse.WorldMatrix.Translation);
			Vector3D start = Vector3D.Zero;
			Vector3D midpoint = end / 2d;

			while ((start - end).Length() > 1d)
			{
				if (this.IsLocalPointInEllipse(ref midpoint)) start = midpoint;
				else end = midpoint;
				midpoint = (start + end) / 2d;
			}

			return ellipse.IsPointInEllipse(MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.WorldMatrix, midpoint));
		}

		/// <summary>
		/// Checks if this BoundingEllipsoidD equals another
		/// </summary>
		/// <param name="other">The BoundingEllipsoidD to check</param>
		/// <returns>Equality</returns>
		public bool Equals(ref BoundingEllipsoidD other)
		{
			return this.Radii == other.Radii && this.WorldMatrix == other.WorldMatrix;
		}

		/// <summary>
		/// Checks if this BoundingEllipsoidD equals another
		/// </summary>
		/// <param name="other">The BoundingEllipsoidD to check</param>
		/// <returns>Equality</returns>
		public bool Equals(BoundingEllipsoidD other)
		{
			return this.Radii == other.Radii && this.WorldMatrix == other.WorldMatrix;
		}

		/// <summary>
		/// Converts this ellipse from local space to world space
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <returns>A world-aligned ellipse</returns>
		public BoundingEllipsoidD ToWorldSpace(ref MatrixD world_matrix)
		{
			BoundingEllipsoidD ellipse = new BoundingEllipsoidD(this.Radii * world_matrix.Scale, ref world_matrix);
			ellipse.WorldMatrix.Translation = MyJumpGateModSession.LocalVectorToWorldVectorP(ref world_matrix, this.WorldMatrix.Translation);
			return ellipse;
		}

		/// <summary>
		/// Converts this ellipse from world space to local space
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <returns>A local-aligned ellipse</returns>
		public BoundingEllipsoidD ToLocalSpace(ref MatrixD world_matrix)
		{
			BoundingEllipsoidD ellipse = new BoundingEllipsoidD(this.Radii * world_matrix.Scale, ref world_matrix);
			ellipse.WorldMatrix.Translation = MyJumpGateModSession.WorldVectorToLocalVectorP(ref world_matrix, this.WorldMatrix.Translation);
			return ellipse;
		}

		/// <summary>
		/// Serializes a BoundingEllipsoidD to a byte array<br />
		/// <i>As for some reason MatrixD is not ProtoBuf compatible</i>
		/// </summary>
		/// <returns>The serialized bounding ellipsoid</returns>
		public byte[] ToSerialized()
		{
			byte[] bytes = new byte[sizeof(double) * 12];
			double[] doubles = new double[12];
			doubles[0] = this.Radii.X;
			doubles[1] = this.Radii.Y;
			doubles[2] = this.Radii.Z;
			doubles[3] = this.WorldMatrix.Forward.X;
			doubles[4] = this.WorldMatrix.Forward.Y;
			doubles[5] = this.WorldMatrix.Forward.Z;
			doubles[6] = this.WorldMatrix.Up.X;
			doubles[7] = this.WorldMatrix.Up.Y;
			doubles[8] = this.WorldMatrix.Up.Z;
			doubles[9] = this.WorldMatrix.Translation.X;
			doubles[10] = this.WorldMatrix.Translation.Y;
			doubles[11] = this.WorldMatrix.Translation.Z;
			for (int i = 0; i < doubles.Length; ++i) BitConverter.GetBytes(doubles[i]).CopyTo(bytes, i * sizeof(double));
			return bytes;
		}
		#endregion
	}
}
