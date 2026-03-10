using IOTA.ModularJumpGates.Extensions;
using System;
using System.Collections.Generic;
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
		public static BoundingEllipsoidD Zero => new BoundingEllipsoidD(ref Vector3D.Zero, ref MatrixD.Identity);
		#endregion

		#region Public Variables
		/// <summary>
		/// The world matrix of this ellipse
		/// </summary>
		public MatrixD WorldMatrix;

		/// <summary>
		/// The inverted world matrix of this ellipse
		/// </summary>
		public MatrixD WorldMatrixInv;

		/// <summary>
		/// The ellipsoid radii
		/// </summary>
		public Vector3D Radii
		{
			get
			{
				return this.WorldMatrix.Scale;
			}
			set
			{
				this.WorldMatrix = MatrixD.CreateScale(value) * MatrixD.Normalize(this.WorldMatrix);
				MatrixD.Invert(ref this.WorldMatrix, out this.WorldMatrixInv);
			}
		}
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
			return new BoundingEllipsoidD(src.WorldMatrix.Scale + extends, ref src.WorldMatrix);
		}

		/// <summary>
		/// Extends the radii of this ellipse
		/// </summary>
		/// <param name="src">The ellipse to extend</param>
		/// <param name="extends">The radii values to exend by</param>
		/// <returns>The extended ellipse</returns>
		public static BoundingEllipsoidD operator *(BoundingEllipsoidD src, double extends)
		{
			return new BoundingEllipsoidD(src.WorldMatrix.Scale * extends, ref src.WorldMatrix);
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
			double[] doubles = new double[16];
			for (int i = 0; i < doubles.Length; ++i) doubles[i] = BitConverter.ToDouble(bytes, offset + i * sizeof(double));
			MatrixD world_matrix = new MatrixD(
				doubles[00], doubles[01], doubles[02], doubles[03],
				doubles[04], doubles[05], doubles[06], doubles[07],
				doubles[08], doubles[09], doubles[10], doubles[11],
				doubles[12], doubles[13], doubles[14], doubles[15]
			);
			return new BoundingEllipsoidD(ref world_matrix);
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="world_matrix">The ellipsoid's scaled world matrix</param>
		public BoundingEllipsoidD(ref MatrixD world_matrix)
		{
			this.WorldMatrix = world_matrix;
			MatrixD.Invert(ref world_matrix, out this.WorldMatrixInv);
		}

		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radii">The ellipsoid's radii</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(Vector3D radii, MatrixD world_matrix)
		{
			this.WorldMatrix = MatrixD.CreateScale(radii) * MatrixD.Normalize(world_matrix);
			MatrixD.Invert(ref this.WorldMatrix, out this.WorldMatrixInv);
		}

		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radii">The ellipsoid's radii</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(ref Vector3D radii, MatrixD world_matrix)
		{
			this.WorldMatrix = MatrixD.CreateScale(radii) * MatrixD.Normalize(world_matrix);
			MatrixD.Invert(ref this.WorldMatrix, out this.WorldMatrixInv);
		}

		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radius">The ellipsoid's spherical radius</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(double radius, MatrixD world_matrix)
		{
			this.WorldMatrix = MatrixD.CreateScale(radius) * MatrixD.Normalize(world_matrix);
			MatrixD.Invert(ref this.WorldMatrix, out this.WorldMatrixInv);
		}

		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radii">The ellipsoid's radii</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(Vector3D radii, ref MatrixD world_matrix)
		{
			this.WorldMatrix = MatrixD.CreateScale(radii) * MatrixD.Normalize(world_matrix);
			MatrixD.Invert(ref this.WorldMatrix, out this.WorldMatrixInv);
		}

		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radii">The ellipsoid's radii</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(ref Vector3D radii, ref MatrixD world_matrix)
		{
			MatrixD.CreateScale(ref radii, out this.WorldMatrix);
			this.WorldMatrix *= MatrixD.Normalize(world_matrix);
			MatrixD.Invert(ref this.WorldMatrix, out this.WorldMatrixInv);
		}

		/// <summary>
		/// Creates a new BoundingEllipsoidD
		/// </summary>
		/// <param name="radius">The ellipsoid's spherical radius</param>
		/// <param name="world_matrix">The ellipsoid's world matrix</param>
		public BoundingEllipsoidD(double radius, ref MatrixD world_matrix)
		{
			MatrixD.CreateScale(radius, out this.WorldMatrix);
			this.WorldMatrix *= MatrixD.Normalize(world_matrix);
			MatrixD.Invert(ref this.WorldMatrix, out this.WorldMatrixInv);
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
			return this.WorldMatrix.GetHashCode();
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
				double x1 = Math.Cos(i);
				double y1 = Math.Sin(i);
				double z1 = Math.Sin(i);

				double x2 = Math.Cos(i + angle_steps);
				double y2 = Math.Sin(i + angle_steps);
				double z2 = Math.Sin(i + angle_steps);

				Vector3D dim11 = Vector3D.Transform(new Vector3D(x1, y1, 0), ref this.WorldMatrix);
				Vector3D dim12 = Vector3D.Transform(new Vector3D(x2, y2, 0), ref this.WorldMatrix);

				Vector3D dim21 = Vector3D.Transform(new Vector3D(x1, 0, z1), ref this.WorldMatrix);
				Vector3D dim22 = Vector3D.Transform(new Vector3D(x2, 0, z2), ref this.WorldMatrix);

				y1 = Math.Cos(i);
				y2 = Math.Cos(i + angle_steps);

				Vector3D dim31 = Vector3D.Transform(new Vector3D(0, y1, z1), ref this.WorldMatrix);
				Vector3D dim32 = Vector3D.Transform(new Vector3D(0, y2, z2), ref this.WorldMatrix);

				MyJumpGateModSession.DrawTransparentLine(dim11, dim12, material, ref color_v4, thickness);
				MyJumpGateModSession.DrawTransparentLine(dim21, dim22, material, ref color_v4, thickness);
				MyJumpGateModSession.DrawTransparentLine(dim31, dim32, material, ref color_v4, thickness);
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
			this.Draw2(color, wire_divide_ratio, wire_divide_ratio, thickness, material, intensity);
		}

		/// <summary>
		/// Draws the full ellipsoid (without faces)
		/// </summary>
		/// <param name="color">The line color</param>
		/// <param name="theta_divide_ratio">The number of lateral angle divisions<br />Higher values are more accurate but have higher time complexity</param>
		/// <param name="phi_divide_ratio">The number of vertical angle divisions<br />Higher values are more accurate but have higher time complexity</param>
		/// <param name="thickness">The line thickness (in meters?)</param>
		/// <param name="material">The line material</param>
		/// <param name="intensity">The line color intensity</param>
		public void Draw2(Color color, int theta_divide_ratio, int phi_divide_ratio, float thickness, MyStringId? material = null, float intensity = 1)
		{
			double theta_step = 2 * Math.PI / theta_divide_ratio;
			double phi_step = 2 * Math.PI / phi_divide_ratio;
			int theta_count = (int) Math.Ceiling(2f * Math.PI / theta_step) + 1;
			int phi_count = (int) Math.Ceiling(Math.PI / phi_step) + 1;
			Vector4 color_v = color.ToVector4() * new Vector4(intensity, intensity, intensity, 1);
			Vector3D[,] vertices = new Vector3D[phi_count, theta_count];

			// Generate vertices
			for (int i = 0; i < phi_count; i++)
			{
				double phi = i * phi_step;

				for (int j = 0; j < theta_count; j++)
				{
					double theta = j * theta_step;

					double x = Math.Sin(phi) * Math.Cos(theta);
					double y = Math.Sin(phi) * Math.Sin(theta);
					double z = Math.Cos(phi);

					vertices[i, j] = Vector3D.Transform(new Vector3D(x, y, z), ref this.WorldMatrix);
				}
			}

			// Draw lines
			for (int i = 0; i < phi_count; i++)
			{
				for (int j = 0; j < theta_count; j++)
				{
					Vector3D current = vertices[i, j];
					int next_j = (j + 1) % theta_count;

					// Draw horizontal line (same latitude)
					MyJumpGateModSession.DrawTransparentLine(current, vertices[i, next_j], material, ref color_v, thickness);

					// Draw vertical line (next latitude)
					if (i + 1 < phi_count) MyJumpGateModSession.DrawTransparentLine(current, vertices[i + 1, j], material, ref color_v, thickness);
				}
			}
		}

		/// <summary>
		/// Checks if the specified point is within this ellipsoid
		/// </summary>
		/// <param name="world_coord">The world coordinate</param>
		/// <returns>true if point is inside</returns>
		public bool IsPointInEllipse(ref Vector3D world_coord)
		{
			Vector3D local_point;
			Vector3D.Transform(ref world_coord, ref this.WorldMatrixInv, out local_point);
			return this.IsLocalPointInEllipse(ref local_point);
		}

		/// <summary>
		/// Checks if the specified point is within this ellipsoid
		/// </summary>
		/// <param name="world_coord">The world coordinate</param>
		/// <returns>true if point is inside</returns>
		public bool IsPointInEllipse(Vector3D world_coord)
		{
			return this.IsPointInEllipse(ref world_coord);
		}

		/// <summary>
		/// Checks if the specified point is within this ellipsoid
		/// </summary>
		/// <param name="local_coord">The ellipsoid local coordinate</param>
		/// <returns>True if point is inside</returns>
		public bool IsLocalPointInEllipse(ref Vector3D local_coord)
		{
			return (local_coord * local_coord).Sum <= 1;
		}

		/// <summary>
		/// Checks if the specified point is within this ellipsoid
		/// </summary>
		/// <param name="local_coord">The ellipsoid local coordinate</param>
		/// <returns>True if point is inside</returns>
		public bool IsLocalPointInEllipse(Vector3D local_coord)
		{
			return this.IsLocalPointInEllipse(ref local_coord);
		}

		/// <summary>
		/// Checks if this ellipsoid intersects another
		/// </summary>
		/// <param name="ellipse">The other ellipsoid</param>
		/// <returns>Whether this and ellipse intersect</returns>
		public bool Intersects(ref BoundingEllipsoidD ellipse)
		{
			// Check this ellipsoid
			{
				Vector3D direction = ellipse.WorldMatrix.Translation - this.WorldMatrix.Translation;
				Vector3D local_direction;
				Vector3D.TransformNormal(ref direction, ref this.WorldMatrixInv, out local_direction);
				direction /= Math.Sqrt(local_direction.Dot(local_direction));
				direction += this.WorldMatrix.Translation;
				if (ellipse.IsPointInEllipse(ref direction)) return true;
			}

			// Check other ellipsoid
			{
				Vector3D direction = this.WorldMatrix.Translation - ellipse.WorldMatrix.Translation;
				Vector3D local_direction;
				Vector3D.TransformNormal(ref direction, ref ellipse.WorldMatrixInv, out local_direction);
				direction /= Math.Sqrt(local_direction.Dot(local_direction));
				direction += ellipse.WorldMatrix.Translation;
				if (this.IsPointInEllipse(ref direction)) return true;
			}

			return false;
		}

		/// <summary>
		/// Checks if this ellipsoid intersects another
		/// </summary>
		/// <param name="ellipse">The other ellipsoid</param>
		/// <returns>Whether this and ellipse intersect</returns>
		public bool Intersects(BoundingEllipsoidD ellipse)
		{
			return this.Intersects(ref ellipse);
		}

		/// <summary>
		/// Checks if this ellipsoid intersects a bounding sphere
		/// </summary>
		/// <param name="sphere">The bounding sphere</param>
		/// <returns>Whether this and sphere intersect</returns>
		public bool Intersects(ref BoundingSphereD sphere)
		{
			MatrixD world_matrix = MatrixD.Identity;
			world_matrix.Translation = sphere.Center;
			return this.Intersects(new BoundingEllipsoidD(sphere.Radius, ref world_matrix));
		}

		/// <summary>
		/// Checks if this ellipsoid intersects a bounding sphere
		/// </summary>
		/// <param name="sphere">The bounding sphere</param>
		/// <returns>Whether this and sphere intersect</returns>
		public bool Intersects(BoundingSphereD sphere)
		{
			return this.Intersects(ref sphere);
		}

		/// <summary>
		/// Checks if this BoundingEllipsoidD equals another
		/// </summary>
		/// <param name="other">The BoundingEllipsoidD to check</param>
		/// <returns>Equality</returns>
		public bool Equals(ref BoundingEllipsoidD other)
		{
			return this.WorldMatrix.EqualsFast(ref other.WorldMatrix);
		}

		/// <summary>
		/// Checks if this BoundingEllipsoidD equals another
		/// </summary>
		/// <param name="other">The BoundingEllipsoidD to check</param>
		/// <returns>Equality</returns>
		public bool Equals(BoundingEllipsoidD other)
		{
			return this.Equals(ref other);
		}

		/// <summary>
		/// Transforms this ellipsoid by the specified matrix in-place
		/// </summary>
		/// <param name="world_matrix">The matrix</param>
		public void Transform(ref MatrixD matrix)
		{
			this.WorldMatrix *= matrix;
		}

		/// <summary>
		/// Transforms this ellipsoid by the specified matrix
		/// </summary>
		/// <param name="matrix">The matrix</param>
		/// <param name="ellipsoid">The resulting ellipse</param>
		public void Transform(ref MatrixD matrix, out BoundingEllipsoidD ellipsoid)
		{
			ellipsoid.WorldMatrix = this.WorldMatrix * matrix;
			MatrixD.Invert(ref ellipsoid.WorldMatrix, out ellipsoid.WorldMatrixInv);
		}

		/// <summary>
		/// Transforms this ellipsoid by the specified matrix
		/// </summary>
		/// <param name="world_matrix">The matrix</param>
		/// <returns>The transformed ellipse</returns>
		public BoundingEllipsoidD Transformed(ref MatrixD matrix)
		{
			MatrixD ellipsoid_matrix = this.WorldMatrix * matrix;
			return new BoundingEllipsoidD(ref ellipsoid_matrix);
		}

		/// <summary>
		/// Serializes a BoundingEllipsoidD to a byte array<br />
		/// <i>As for some reason MatrixD is not ProtoBuf compatible</i>
		/// </summary>
		/// <returns>The serialized bounding ellipsoid</returns>
		public byte[] ToSerialized()
		{
			byte[] bytes = new byte[sizeof(double) * 16];
			double[] doubles = new double[16] {
				this.WorldMatrix.M11, this.WorldMatrix.M12, this.WorldMatrix.M13, this.WorldMatrix.M14,
				this.WorldMatrix.M21, this.WorldMatrix.M22, this.WorldMatrix.M23, this.WorldMatrix.M24,
				this.WorldMatrix.M31, this.WorldMatrix.M32, this.WorldMatrix.M33, this.WorldMatrix.M34,
				this.WorldMatrix.M41, this.WorldMatrix.M42, this.WorldMatrix.M43, this.WorldMatrix.M44,
			};
			for (int i = 0; i < doubles.Length; ++i) BitConverter.GetBytes(doubles[i]).CopyTo(bytes, i * sizeof(double));
			return bytes;
		}

		/// <summary>
		/// Generates and returns all vertices of this ellipsoid
		/// </summary>
		/// <param name="theta_divide_ratio">The number of lateral angle divisions<br />Higher values are more accurate but have higher time complexity</param>
		/// <param name="phi_divide_ratio">The number of vertical angle divisions<br />Higher values are more accurate but have higher time complexity</param>
		/// <returns>An enumerator to iterate all ellipsoid verices</returns>
		public IEnumerable<Vector3D> GetVertices(int theta_divide_ratio, int phi_divide_ratio)
		{
			double theta_step = 2 * Math.PI / theta_divide_ratio;
			double phi_step = 2 * Math.PI / phi_divide_ratio;
			int theta_count = (int) Math.Ceiling(2f * Math.PI / theta_step) + 1;
			int phi_count = (int) Math.Ceiling(Math.PI / phi_step) + 1;

			// Generate vertices
			for (int i = 0; i < phi_count; i++)
			{
				double phi = i * phi_step;

				for (int j = 0; j < theta_count; j++)
				{
					double theta = j * theta_step;

					double x = Math.Sin(phi) * Math.Cos(theta);
					double y = Math.Sin(phi) * Math.Sin(theta);
					double z = Math.Cos(phi);

					yield return Vector3D.Transform(new Vector3D(x, z, y), ref this.WorldMatrix);
				}
			}
		}
		#endregion
	}
}
