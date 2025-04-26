using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace IOTA.ModularJumpGates.API.ModAPI
{
	public class Util
	{
		public enum MyAPISessionStatusEnum : byte { OFFLINE, LOADING, RUNNING, UNLOADING };
		public enum MyAPIGridInvalidationReason : byte { NONE, CLOSED, NULL_GRID, INSUFFICIENT_GRIDS, NULL_PHYSICS };
		public enum MyAPIJumpGateStatus : byte { NONE, SWITCHING, IDLE, OUTBOUND, INBOUND, CANCELLED, INVALID = 0xFF };
		public enum MyAPIJumpGatePhase : byte { NONE, IDLE, CHARGING, JUMPING, RESETING, INVALID = 0xFF };
		public enum MyAPIGateInvalidationReason : byte { NONE, CLOSED, INSUFFICIENT_DRIVES, NULL_GRID, NULL_STATUS, NULL_PHASE, INVALID_ID, INSUFFICIENT_NODES };
		public enum MyAPIJumpFailReason : byte {
			NONE, SUCCESS, IN_PROGRESS,
			SRC_INVALID, CONTROLLER_NOT_CONNECTED, SRC_DISABLED, SRC_NOT_CONFIGURED, SRC_BUSY, SRC_ROUTING_DISABLED, SRC_INBOUND_ONLY, SRC_ROUTING_CHANGED, SRC_CLOSED,
			NULL_DESTINATION, DESTINATION_UNAVAILABLE, NULL_ANIMATION, SUBSPACE_BUSY, RADIO_LINK_FAILED, BEACON_LINK_FAILED, JUMP_SPACE_TRANSPOSED, CANCELLED, UNKNOWN_ERROR, NO_ENTITIES, NO_ENTITIES_JUMPED, INSUFFICIENT_POWER, CROSS_SERVER_JUMP, BEACON_BLOCKED,
			DST_UNAVAILABLE, DST_ROUTING_DISABLED, DST_OUTBOUND_ONLY, DST_FORBIDDEN, DST_DISABLED, DST_NOT_CONFIGURED, DST_BUSY, DST_RADIO_CONNECTION_INTERRUPTED, DST_BEACON_CONNECTION_INTERRUPTED, DST_ROUTING_CHANGED, DST_VOIDED,
			INVALID = 0xFF
		}
		public enum MyAPIGateColliderStatus : byte { NONE, ATTACHED, CLOSED };
		public enum MyAPIJumpTypeEnum : byte { STANDARD, INBOUND_VOID, OUTBOUND_VOID }
		public enum MyAPIWaypointType : byte { NONE, JUMP_GATE, GPS, BEACON, SERVER };
		public enum MyAPIFactionDisplayType : byte { UNOWNED = 1, ENEMY = 2, NEUTRAL = 4, FRIENDLY = 8, OWNED = 16 }

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

			#region Private Static Methods
			private static void DrawTransparentLine(Vector3D start, Vector3D end, MyStringId? material, ref Vector4 color, float thickness, MyBillboard.BlendTypeEnum blendtype = MyBillboard.BlendTypeEnum.Standard)
			{
				Vector3D dir = end - start;
				float len = (float) dir.Length();
				MyTransparentGeometry.AddLineBillboard(material ?? MyStringId.GetOrCompute("GizmoDrawLine"), color, start, dir.Normalized(), len, thickness, blendtype);
			}

			/// <summary>
			/// Converts a world position vector to a local position vector
			/// </summary>
			/// <param name="world_matrix">The world matrix</param>
			/// <param name="world_pos">The world vector to convert</param>
			/// <returns>The local vector</returns>
			private static Vector3D WorldVectorToLocalVectorP(MatrixD world_matrix, Vector3D world_pos)
			{
				return Vector3D.TransformNormal(world_pos - world_matrix.Translation, MatrixD.Transpose(world_matrix));
			}

			/// <summary>
			/// Converts a world position vector to a local position vector
			/// </summary>
			/// <param name="world_matrix">The world matrix</param>
			/// <param name="world_pos">The world vector to convert</param>
			/// <returns>The local vector</returns>
			private static Vector3D WorldVectorToLocalVectorP(ref MatrixD world_matrix, Vector3D world_pos)
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
			private static Vector3D LocalVectorToWorldVectorP(MatrixD world_matrix, Vector3D local_pos)
			{
				return Vector3D.Transform(local_pos, ref world_matrix);
			}

			/// <summary>
			/// Converts a local position vector to a world position vector
			/// </summary>
			/// <param name="world_matrix">The world matrix</param>
			/// <param name="world_pos">The local vector to convert</param>
			/// <returns>The world vector</returns>
			private static Vector3D LocalVectorToWorldVectorP(ref MatrixD world_matrix, Vector3D local_pos)
			{
				return Vector3D.Transform(local_pos, ref world_matrix);
			}

			/// <summary>
			/// Converts a world direction vector to a local direction vector
			/// </summary>
			/// <param name="world_matrix">The world matrix</param>
			/// <param name="world_pos">The world vector to convert</param>
			/// <returns>The local vector</returns>
			private static Vector3D WorldVectorToLocalVectorD(MatrixD world_matrix, Vector3D world_direction)
			{
				return Vector3D.TransformNormal(world_direction, MatrixD.Transpose(world_matrix));
			}

			/// <summary>
			/// Converts a world direction vector to a local direction vector
			/// </summary>
			/// <param name="world_matrix">The world matrix</param>
			/// <param name="world_pos">The world vector to convert</param>
			/// <returns>The local vector</returns>
			private static Vector3D WorldVectorToLocalVectorD(ref MatrixD world_matrix, Vector3D world_direction)
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
			private static Vector3D LocalVectorToWorldVectorD(MatrixD world_matrix, Vector3D local_direction)
			{
				return Vector3D.TransformNormal(local_direction, ref world_matrix);
			}

			/// <summary>
			/// Converts a local direction vector to a world direction vector
			/// </summary>
			/// <param name="world_matrix">The world matrix</param>
			/// <param name="world_pos">The local vector to convert</param>
			/// <returns>The world vector</returns>
			private static Vector3D LocalVectorToWorldVectorD(ref MatrixD world_matrix, Vector3D local_direction)
			{
				return Vector3D.TransformNormal(local_direction, ref world_matrix);
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
				BoundingEllipsoidD ellipsoid;
				ellipsoid.Radii = new Vector3D(doubles[0], doubles[1], doubles[2]);
				Vector3D forward = new Vector3D(doubles[3], doubles[4], doubles[5]);
				Vector3D up = new Vector3D(doubles[6], doubles[7], doubles[8]);
				Vector3D translation = new Vector3D(doubles[9], doubles[10], doubles[11]);
				MatrixD.CreateWorld(ref translation, ref forward, ref up, out ellipsoid.WorldMatrix);
				return ellipsoid;
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
			/// <param name="radius">The ellipsoid's spherical radius</param>
			/// <param name="world_matrix">The ellipsoid's world matrix</param>
			public BoundingEllipsoidD(double radius, MatrixD world_matrix)
			{
				this.Radii = new Vector3D(Math.Abs(radius));
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

			/// <summary>
			/// Creates a new BoundingEllipsoidD
			/// </summary>
			/// <param name="radius">The ellipsoid's spherical radius</param>
			/// <param name="world_matrix">The ellipsoid's world matrix</param>
			public BoundingEllipsoidD(double radius, ref MatrixD world_matrix)
			{
				this.Radii = new Vector3D(Math.Abs(radius));
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

					Vector3D dim11 = BoundingEllipsoidD.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x1, y1, 0));
					Vector3D dim12 = BoundingEllipsoidD.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x2, y2, 0));

					Vector3D dim21 = BoundingEllipsoidD.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x1, 0, z1));
					Vector3D dim22 = BoundingEllipsoidD.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x2, 0, z2));

					y1 = this.Radii.Y * Math.Cos(i);
					y2 = this.Radii.Y * Math.Cos(i + angle_steps);

					Vector3D dim31 = BoundingEllipsoidD.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(0, y1, z1));
					Vector3D dim32 = BoundingEllipsoidD.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(0, y2, z2));

					BoundingEllipsoidD.DrawTransparentLine(dim11, dim12, material, ref color_v4, thickness);
					BoundingEllipsoidD.DrawTransparentLine(dim21, dim22, material, ref color_v4, thickness);
					BoundingEllipsoidD.DrawTransparentLine(dim31, dim32, material, ref color_v4, thickness);
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

						double x = this.Radii.X * Math.Sin(phi) * Math.Cos(theta);
						double y = this.Radii.Y * Math.Sin(phi) * Math.Sin(theta);
						double z = this.Radii.Z * Math.Cos(phi);

						vertices[i, j] = BoundingEllipsoidD.LocalVectorToWorldVectorP(ref this.WorldMatrix, new Vector3D(x, y, z));
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
						BoundingEllipsoidD.DrawTransparentLine(current, vertices[i, next_j], MyStringId.GetOrCompute("WeaponLaser"), ref color_v, thickness);

						// Draw vertical line (next latitude)
						if (i + 1 < phi_count) BoundingEllipsoidD.DrawTransparentLine(current, vertices[i + 1, j], MyStringId.GetOrCompute("WeaponLaser"), ref color_v, thickness);
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
				return this.IsLocalPointInEllipse(BoundingEllipsoidD.WorldVectorToLocalVectorP(ref this.WorldMatrix, world_coord));
			}

			/// <summary>
			/// Checks if the specified point is within this ellipsoid
			/// </summary>
			/// <param name="world_coord">The world coordinate</param>
			/// <returns>true if point is inside</returns>
			public bool IsPointInEllipse(Vector3D world_coord)
			{
				return this.IsLocalPointInEllipse(BoundingEllipsoidD.WorldVectorToLocalVectorP(ref this.WorldMatrix, world_coord));
			}

			/// <summary>
			/// Checks if the specified point is within this ellipsoid
			/// </summary>
			/// <param name="local_coord">The ellipsoid local coordinate</param>
			/// <returns>True if point is inside</returns>
			public bool IsLocalPointInEllipse(ref Vector3D local_coord)
			{
				double frac_x = local_coord.X / this.Radii.X;
				double frac_y = local_coord.Y / this.Radii.Y;
				double frac_z = local_coord.Z / this.Radii.Z;
				return (frac_x * frac_x) + (frac_y * frac_y) + (frac_z * frac_z) <= 1;
			}

			/// <summary>
			/// Checks if the specified point is within this ellipsoid
			/// </summary>
			/// <param name="local_coord">The ellipsoid local coordinate</param>
			/// <returns>True if point is inside</returns>
			public bool IsLocalPointInEllipse(Vector3D local_coord)
			{
				double frac_x = local_coord.X / this.Radii.X;
				double frac_y = local_coord.Y / this.Radii.Y;
				double frac_z = local_coord.Z / this.Radii.Z;
				return (frac_x * frac_x) + (frac_y * frac_y) + (frac_z * frac_z) <= 1;
			}

			/// <summary>
			/// Checks if this ellipsoid intersects another<br />
			/// Due to inaccurasies in ellipsoid angles, overlap is checked with a O(log(n)) algorithim instead of O(1) math
			/// </summary>
			/// <param name="ellipse">The other ellipsoid</param>
			/// <returns>Whether this and ellipse intersect</returns>
			public bool Intersects(ref BoundingEllipsoidD ellipse)
			{
				Vector3D end = BoundingEllipsoidD.WorldVectorToLocalVectorP(ref this.WorldMatrix, ellipse.WorldMatrix.Translation);
				Vector3D start = Vector3D.Zero;
				Vector3D midpoint = end / 2d;

				while ((start - end).Length() > 1d)
				{
					if (this.IsLocalPointInEllipse(ref midpoint)) start = midpoint;
					else end = midpoint;
					midpoint = (start + end) / 2d;
				}

				return ellipse.IsPointInEllipse(BoundingEllipsoidD.LocalVectorToWorldVectorP(ref this.WorldMatrix, midpoint));
			}

			/// <summary>
			/// Checks if this ellipsoid intersects another<br />
			/// Due to inaccurasies in ellipsoid angles, overlap is checked with a O(log(n)) algorithim instead of O(1) math
			/// </summary>
			/// <param name="ellipse">The other ellipsoid</param>
			/// <returns>Whether this and ellipse intersect</returns>
			public bool Intersects(BoundingEllipsoidD ellipse)
			{
				Vector3D end = BoundingEllipsoidD.WorldVectorToLocalVectorP(ref this.WorldMatrix, ellipse.WorldMatrix.Translation);
				Vector3D start = Vector3D.Zero;
				Vector3D midpoint = end / 2d;

				while ((start - end).Length() > 1d)
				{
					if (this.IsLocalPointInEllipse(ref midpoint)) start = midpoint;
					else end = midpoint;
					midpoint = (start + end) / 2d;
				}

				return ellipse.IsPointInEllipse(BoundingEllipsoidD.LocalVectorToWorldVectorP(ref this.WorldMatrix, midpoint));
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
				Vector3D forward, up, translation;
				forward = BoundingEllipsoidD.LocalVectorToWorldVectorD(ref world_matrix, this.WorldMatrix.Forward);
				up = BoundingEllipsoidD.LocalVectorToWorldVectorD(ref world_matrix, this.WorldMatrix.Up);
				translation = BoundingEllipsoidD.LocalVectorToWorldVectorP(ref world_matrix, this.WorldMatrix.Translation);
				return new BoundingEllipsoidD(this.Radii * world_matrix.Scale, MatrixD.CreateWorld(translation, forward, up));
			}

			/// <summary>
			/// Converts this ellipse from world space to local space
			/// </summary>
			/// <param name="world_matrix">The world matrix</param>
			/// <returns>A local-aligned ellipse</returns>
			public BoundingEllipsoidD ToLocalSpace(ref MatrixD world_matrix)
			{
				Vector3D forward, up, translation;
				forward = BoundingEllipsoidD.WorldVectorToLocalVectorD(ref world_matrix, this.WorldMatrix.Forward);
				up = BoundingEllipsoidD.WorldVectorToLocalVectorD(ref world_matrix, this.WorldMatrix.Up);
				translation = BoundingEllipsoidD.WorldVectorToLocalVectorP(ref world_matrix, this.WorldMatrix.Translation);
				return new BoundingEllipsoidD(this.Radii * world_matrix.Scale, MatrixD.CreateWorld(translation, forward, up));
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

		public struct MyAPIPrefabInfo
		{
			public readonly string PrefabName;
			public readonly Vector3D Position;
			public readonly Vector3D Forward;
			public readonly Vector3D Up;
			public readonly Vector3D InitialLinearVelocity;
			public readonly Vector3D InitialAngularVelocity;
			public readonly string BeaconName;
			public readonly SpawningOptions SpawningOptions;
			public readonly bool UpdateSync;
			public readonly Action Callback;

			public MyAPIPrefabInfo(Dictionary<string, object> info)
			{
				this.PrefabName = (string) info["PrefabName"];
				this.Position = (Vector3D) info["Position"];
				this.Forward = (Vector3D) info["Forward"];
				this.Up = (Vector3D) info["Up"];
				this.InitialLinearVelocity = (Vector3D) info["InitialLinearVelocity"];
				this.InitialAngularVelocity = (Vector3D) info["InitialAngularVelocity"];
				this.BeaconName = (string) info["BeaconName"];
				this.SpawningOptions = (SpawningOptions) info["SpawningOptions"];
				this.UpdateSync = (bool) info["UpdateSync"];
				this.Callback = (Action) info["Callback"];
			}
			public MyAPIPrefabInfo(string prefab_name, Vector3D position, Vector3D forward, Vector3D up, Vector3D initial_linear_velocity = default(Vector3D), Vector3D initial_angular_velocity = default(Vector3D), string beacon_name = null, SpawningOptions spawning_options = SpawningOptions.None, bool update_sync = false, Action callback = null)
			{
				this.PrefabName = prefab_name;
				this.Position = position;
				this.Forward = forward;
				this.Up = up;
				this.InitialLinearVelocity = initial_linear_velocity;
				this.InitialAngularVelocity = initial_angular_velocity;
				this.BeaconName = beacon_name;
				this.SpawningOptions = spawning_options;
				this.UpdateSync = update_sync;
				this.Callback = callback;
			}

			public void Spawn(List<IMyCubeGrid> spawned_grids)
			{
				MyAPIGateway.PrefabManager.SpawnPrefab(spawned_grids, this.PrefabName, this.Position, this.Forward, this.Up, this.InitialLinearVelocity, this.InitialAngularVelocity, this.BeaconName, this.SpawningOptions, this.UpdateSync, this.Callback);
			}

			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["PrefabName"] = this.PrefabName,
					["Position"] = this.Position,
					["Forward"] = this.Forward,
					["Up"] = this.Up,
					["InitialLinearVelocity"] = this.InitialLinearVelocity,
					["InitialAngularVelocity"] = this.InitialAngularVelocity,
					["BeaconName"] = this.BeaconName,
					["SpawningOptions"] = this.SpawningOptions,
					["UpdateSync"] = this.UpdateSync,
					["Callback"] = this.Callback,
				};
			}
		}

		[ProtoContract(UseProtoMembersOnly = true)]
		public class MyAPIBeaconLinkWrapper : IEquatable<MyAPIBeaconLinkWrapper>
		{
			#region Private Variables
			[ProtoMember(1, IsRequired = true, Name = "F_BeaconPosition")]
			private Vector3D _BeaconPosition;
			[ProtoMember(2, IsRequired = true, Name = "F_BroadcastName")]
			private string _BroadcastName;
			#endregion

			#region Public Variables
			[ProtoMember(3, IsRequired = true)]
			public long BeaconID { get; private set; }

			[ProtoIgnore]
			public IMyBeacon Beacon
			{
				get
				{
					return (IMyBeacon) MyAPIGateway.Entities.GetEntityById(this.BeaconID);
				}
			}

			[ProtoIgnore]
			public Vector3D BeaconPosition
			{
				get
				{
					if (this.Beacon != null) this._BeaconPosition = this.Beacon.WorldMatrix.Translation;
					return this._BeaconPosition;
				}
			}

			[ProtoIgnore]
			public string BroadcastName
			{
				get
				{
					if (this.Beacon != null) this._BroadcastName = (this.Beacon.HudText == null || this.Beacon.HudText.Length == 0) ? this.Beacon.CustomName : this.Beacon.HudText;
					return this._BroadcastName;
				}
			}
			#endregion

			#region Public Static Operators
			/// <summary>
			/// Overloads equality operator "==" to check equality
			/// </summary>
			/// <param name="a">The first BeaconLinkWrapper operand</param>
			/// <param name="b">The second BeaconLinkWrapper operand</param>
			/// <returns>Equality</returns>
			public static bool operator ==(MyAPIBeaconLinkWrapper a, MyAPIBeaconLinkWrapper b)
			{
				if (object.ReferenceEquals(a, b)) return true;
				else if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
				else if (object.ReferenceEquals(b, null)) return object.ReferenceEquals(a, null);
				else return a.Equals(b);
			}

			/// <summary>
			/// Overloads inequality operator "!=" to check inequality
			/// </summary>
			/// <param name="a">The first BeaconLinkWrapper operand</param>
			/// <param name="b">The second BeaconLinkWrapper operand</param>
			/// <returns>Inequality</returns>
			public static bool operator !=(MyAPIBeaconLinkWrapper a, MyAPIBeaconLinkWrapper b)
			{
				return !(a == b);
			}
			#endregion

			#region Constructors
			private MyAPIBeaconLinkWrapper() { }

			public MyAPIBeaconLinkWrapper(IMyBeacon beacon)
			{
				this._BeaconPosition = beacon.WorldMatrix.Translation;
				this._BroadcastName = (beacon.HudText == null || beacon.HudText.Length == 0) ? beacon.CustomName : beacon.HudText;
				this.BeaconID = beacon.EntityId;
			}
			#endregion

			#region Public Methods
			/// <summary>
			/// Checks if this BeaconLinkWrapper equals another
			/// </summary>
			/// <param name="obj">The object to check</param>
			/// <returns>Equality</returns>
			public override bool Equals(object obj)
			{
				return this.Equals(obj as MyAPIBeaconLinkWrapper);
			}

			/// <summary>
			/// The hashcode for this object
			/// </summary>
			/// <returns>The hashcode of this object</returns>
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}

			/// <summary>
			/// Checks if this BeaconLinkWrapper equals another
			/// </summary>
			/// <param name="other">The BeaconLinkWrapper to check</param>
			/// <returns>Equality</returns>
			public bool Equals(MyAPIBeaconLinkWrapper other)
			{
				if (object.ReferenceEquals(other, null)) return false;
				else if (object.ReferenceEquals(this, other)) return true;
				else return this.BeaconID == other.BeaconID;
			}
			#endregion
		}

		/// <summary>
		/// Serializable wrapper for GPSs
		/// </summary>
		[ProtoContract]
		public class MyAPIGpsWrapper : IEquatable<MyAPIGpsWrapper>
		{
			#region Public Variables
			/// <summary>
			/// The GPS's coordinates
			/// </summary>
			[ProtoMember(1)]
			public Vector3D Coords;

			/// <summary>
			/// The GPS's color
			/// </summary>
			[ProtoMember(2)]
			public Color GPSColor;

			/// <summary>
			/// The GPS's name
			/// </summary>
			[ProtoMember(3)]
			public string Name;

			/// <summary>
			/// The GPS's description
			/// </summary>
			[ProtoMember(4)]
			public string Description;
			#endregion

			#region Public Static Operators
			/// <summary>
			/// Overloads equality operator "==" to check equality
			/// </summary>
			/// <param name="a">The first MyGpsWrapper operand</param>
			/// <param name="b">The second MyGpsWrapper operand</param>
			/// <returns>Equality</returns>
			public static bool operator ==(MyAPIGpsWrapper a, MyAPIGpsWrapper b)
			{
				if (object.ReferenceEquals(a, b)) return true;
				else if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) return false;
				else return a.Equals(b);
			}

			/// <summary>
			/// Overloads inequality operator "!=" to check inequality
			/// </summary>
			/// <param name="a">The first MyGpsWrapper operand</param>
			/// <param name="b">The second MyGpsWrapper operand</param>
			/// <returns>Inequality</returns>
			public static bool operator !=(MyAPIGpsWrapper a, MyAPIGpsWrapper b)
			{
				return !(a == b);
			}
			#endregion

			#region Constructors
			/// <summary>
			/// Dummy default constructor for ProtoBuf
			/// </summary>
			public MyAPIGpsWrapper() { }

			/// <summary>
			/// Creates a new MyGpsWrapper from a GPS
			/// </summary>
			/// <param name="gps">The source GPS</param>
			public MyAPIGpsWrapper(IMyGps gps)
			{
				if (gps != null)
				{
					this.Coords = gps.Coords;
					this.Name = gps.Name;
					this.Description = gps.Description;
					this.GPSColor = gps.GPSColor;
				}
			}
			#endregion

			#region "object" Methods
			/// <summary>
			/// Checks if this MyGpsWrapper equals another
			/// </summary>
			/// <param name="obj">The object to check</param>
			/// <returns>Equality</returns>
			public override bool Equals(object obj)
			{
				return this.Equals(obj as MyAPIGpsWrapper);
			}

			/// <summary>
			/// The hashcode for this object
			/// </summary>
			/// <returns>The hashcode of this object</returns>
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
			#endregion

			#region Public Methods
			/// <summary>
			/// Checks if this MyGpsWrapper equals another
			/// </summary>
			/// <param name="other">The MyGpsWrapper to check</param>
			/// <returns>Equality</returns>
			public bool Equals(MyAPIGpsWrapper other)
			{
				if (object.ReferenceEquals(other, null)) return false;
				else if (object.ReferenceEquals(this, other)) return true;
				return this.Coords == other.Coords && this.GPSColor == other.GPSColor && this.Name == other.Name && this.Description == other.Description;
			}
			#endregion
		}

		[ProtoContract]
		public class MyAPIServerJumpGate : IEquatable<MyAPIServerJumpGate>
		{
			#region Public Variables
			/// <summary>
			/// The target server's address
			/// </summary>
			[ProtoMember(1)]
			public string ServerAddress;

			/// <summary>
			/// The target server's password or null
			/// </summary>
			[ProtoMember(2)]
			public string ServerPassword;

			/// <summary>
			/// The UUID of the target jump gate
			/// </summary>
			[ProtoMember(3)]
			public Guid JumpGate;
			#endregion

			#region Public Static Operators
			/// <summary>
			/// Overloads equality operator "==" to check equality
			/// </summary>
			/// <param name="a">The first MyServerJumpGate operand</param>
			/// <param name="b">The second MyServerJumpGate operand</param>
			/// <returns>Equality</returns>
			public static bool operator ==(MyAPIServerJumpGate a, MyAPIServerJumpGate b)
			{
				if (object.ReferenceEquals(a, b)) return true;
				else if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) return false;
				else return a.Equals(b);
			}

			/// <summary>
			/// Overloads inequality operator "!=" to check inequality
			/// </summary>
			/// <param name="a">The first MyServerJumpGate operand</param>
			/// <param name="b">The second MyServerJumpGate operand</param>
			/// <returns>Inequality</returns>
			public static bool operator !=(MyAPIServerJumpGate a, MyAPIServerJumpGate b)
			{
				return !(a == b);
			}
			#endregion

			#region Constructors
			/// <summary>
			/// Dummy default constructor for ProtoBuf
			/// </summary>
			public MyAPIServerJumpGate() { }

			/// <summary>
			/// Creates a new MyServerJumpGate
			/// </summary>
			/// <param name="server_address">The target server address</param>
			/// <param name="jump_gate">The target jump gate</param>
			/// <param name="server_password">The server password if applicable or null</param>
			public MyAPIServerJumpGate(string server_address, Guid jump_gate, string server_password = null)
			{
				this.ServerAddress = server_address;
				this.ServerPassword = server_password;
				this.JumpGate = jump_gate;
			}
			#endregion

			#region "object" Methods
			/// <summary>
			/// Checks if this MyServerJumpGate equals another
			/// </summary>
			/// <param name="obj">The object to check</param>
			/// <returns>Equality</returns>
			public override bool Equals(object obj)
			{
				return this.Equals(obj as MyAPIServerJumpGate);
			}

			/// <summary>
			/// The hashcode for this object
			/// </summary>
			/// <returns>The hashcode of this object</returns>
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
			#endregion

			#region Public Methods
			/// <summary>
			/// Checks if this MyGpsWrapper equals another
			/// </summary>
			/// <param name="other">The MyServerJumpGate to check</param>
			/// <returns>Equality</returns>
			public bool Equals(MyAPIServerJumpGate other)
			{
				if (object.ReferenceEquals(other, null)) return false;
				else if (object.ReferenceEquals(this, other)) return true;
				return this.ServerAddress == other.ServerAddress && this.JumpGate == other.JumpGate;
			}
			#endregion
		}

		/// <summary>
		/// Class representing a destination for a jump gate
		/// </summary>
		[ProtoContract]
		public class MyAPIJumpGateWaypoint : IEquatable<MyAPIJumpGateWaypoint>
		{
			#region Public Variables
			/// <summary>
			/// The target jump gate
			/// </summary>
			[ProtoMember(1)]
			public Guid JumpGate { get; private set; } = Guid.Empty;

			/// <summary>
			/// The target GPS
			/// </summary>
			[ProtoMember(2)]
			public MyAPIGpsWrapper GPS { get; private set; } = null;

			/// <summary>
			/// The target beacon
			/// </summary>
			[ProtoMember(3)]
			public MyAPIBeaconLinkWrapper Beacon { get; private set; } = null;

			/// <summary>
			/// The target server jump gate
			/// </summary>
			[ProtoMember(4)]
			public MyAPIServerJumpGate ServerJumpGate { get; private set; } = null;

			/// <summary>
			/// The type of waypoint this is
			/// </summary>
			[ProtoMember(5)]
			public MyAPIWaypointType WaypointType { get; private set; } = MyAPIWaypointType.NONE;
			#endregion

			#region Public Static Operators
			/// <summary>
			/// Overloads equality operator "==" to check equality
			/// </summary>
			/// <param name="a">The first MyJumpGateWaypoint operand</param>
			/// <param name="b">The second MyJumpGateWaypoint operand</param>
			/// <returns>Equality</returns>
			public static bool operator ==(MyAPIJumpGateWaypoint a, MyAPIJumpGateWaypoint b)
			{
				if (object.ReferenceEquals(a, b)) return true;
				else if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
				else if (object.ReferenceEquals(b, null)) return object.ReferenceEquals(a, null);
				else return a.Equals(b);
			}

			/// <summary>
			/// Overloads inequality operator "!=" to check inequality
			/// </summary>
			/// <param name="a">The first MyJumpGateWaypoint operand</param>
			/// <param name="b">The second MyJumpGateWaypoint operand</param>
			/// <returns>Inequality</returns>
			public static bool operator !=(MyAPIJumpGateWaypoint a, MyAPIJumpGateWaypoint b)
			{
				return !(a == b);
			}
			#endregion

			#region Constructors
			/// <summary>
			/// Dummy default constructor for Protobuf
			/// </summary>
			MyAPIJumpGateWaypoint() { }

			/// <summary>
			/// Creates a new waypoint targeting the specified jump gate
			/// </summary>
			/// <param name="jump_gate">The non-null jump gate</param>
			internal MyAPIJumpGateWaypoint(MyAPIJumpGate jump_gate)
			{
				this.JumpGate = jump_gate.Guid;
				this.WaypointType = MyAPIWaypointType.JUMP_GATE;
			}

			/// <summary>
			/// Creates a new waypoint targeting the specified GPS
			/// </summary>
			/// <param name="gps">The non-null GPS</param>
			public MyAPIJumpGateWaypoint(IMyGps gps)
			{
				this.GPS = new MyAPIGpsWrapper(gps);
				this.WaypointType = MyAPIWaypointType.GPS;
			}

			/// <summary>
			/// Creates a new waypoint targeting the specified beacon
			/// </summary>
			/// <param name="gps">The non-null beacon</param>
			public MyAPIJumpGateWaypoint(IMyBeacon beacon)
			{
				this.Beacon = new MyAPIBeaconLinkWrapper(beacon);
				this.WaypointType = MyAPIWaypointType.BEACON;
			}

			/// <summary>
			/// Creates a new waypoint targeting the specified beacon
			/// </summary>
			/// <param name="gps">The non-null beacon</param>
			public MyAPIJumpGateWaypoint(MyAPIBeaconLinkWrapper beacon)
			{
				this.Beacon = beacon;
				this.WaypointType = MyAPIWaypointType.BEACON;
			}

			/// <summary>
			/// Creates a new waypoint targeting the specified server jump gate
			/// </summary>
			/// <param name="server_gate">The non-null server jump gate</param>
			public MyAPIJumpGateWaypoint(MyAPIServerJumpGate server_gate)
			{
				this.ServerJumpGate = server_gate;
				this.WaypointType = MyAPIWaypointType.SERVER;
			}
			#endregion

			#region "object" Methods
			/// <summary>
			/// Checks if this MyJumpGateWaypoint equals another
			/// </summary>
			/// <param name="obj">The object to check</param>
			/// <returns>Equality</returns>
			public override bool Equals(object obj)
			{
				return this.Equals(obj as MyAPIJumpGateWaypoint);
			}

			/// <summary>
			/// The hashcode for this object
			/// </summary>
			/// <returns>The hashcode of this object</returns>
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
			#endregion

			#region Public Methods
			/// <summary>
			/// Gets the endpoint of this waypoint in world coordinates
			/// </summary>
			/// <param name="target_jump_gate">The targeted jump gate or null if target is not a jump gate</param>
			/// <returns>The target's world coordinates<br />null if this waypoint is None<br />Vector3D.Zero if this waypoint targets a server</returns>
			/// <exception cref="InvalidOperationException"></exception>
			public Vector3D? GetEndpoint(out MyAPIJumpGate target_jump_gate)
			{
				target_jump_gate = null;

				switch (this.WaypointType)
				{
					case MyAPIWaypointType.NONE:
						return null;
					case MyAPIWaypointType.JUMP_GATE:
						target_jump_gate = MyAPISession.Instance.GetJumpGate(this.JumpGate);
						if (target_jump_gate == null || !target_jump_gate.IsValid()) return null;
						return target_jump_gate.WorldJumpNode;
					case MyAPIWaypointType.GPS:
						return this.GPS?.Coords;
					case MyAPIWaypointType.BEACON:
						return this.Beacon?.BeaconPosition;
					case MyAPIWaypointType.SERVER:
						if (this.ServerJumpGate == null) return null;
						return Vector3D.Zero;
					default:
						throw new InvalidOperationException("Waypoint is invalid");
				}
			}

			/// <summary>
			/// Whether or this waypoint has a valid target
			/// </summary>
			/// <returns>MyJumpGateWaypoint::WaypointType != MyAPIWaypointType.None</returns>
			public bool HasValue()
			{
				return this.WaypointType != MyAPIWaypointType.NONE;
			}

			/// <summary>
			/// Checks if this MyJumpGateWaypoint equals another
			/// </summary>
			/// <param name="other">The MyJumpGateWaypoint to check</param>
			/// <returns>Equality</returns>
			public bool Equals(MyAPIJumpGateWaypoint other)
			{
				if (object.ReferenceEquals(other, null)) return false;
				else if (object.ReferenceEquals(this, other)) return true;
				else if (this.WaypointType != other.WaypointType) return false;
				else if (this.WaypointType == MyAPIWaypointType.JUMP_GATE) return this.JumpGate == other.JumpGate;
				else if (this.WaypointType == MyAPIWaypointType.GPS) return this.GPS == other.GPS;
				else if (this.WaypointType == MyAPIWaypointType.BEACON) return this.Beacon == other.Beacon;
				else if (this.WaypointType == MyAPIWaypointType.SERVER) return this.ServerJumpGate == other.ServerJumpGate;
				else if (this.WaypointType == MyAPIWaypointType.NONE) return true;
				return false;
			}

			/// <summary>
			/// Gets the endpoint of this waypoint in world coordinates
			/// </summary>
			/// <returns>The target's world coordinates<br />null if this waypoint is None<br />Vector3D.Zero if this waypoint targets a server</returns>
			/// <exception cref="InvalidOperationException">If MyJumpGateWaypoint::WaypointType is invalid</exception>
			public Vector3D? GetEndpoint()
			{
				switch (this.WaypointType)
				{
					case MyAPIWaypointType.NONE:
						return null;
					case MyAPIWaypointType.JUMP_GATE:
					{
						MyAPIJumpGate jump_gate = MyAPISession.Instance.GetJumpGate(this.JumpGate);
						if (jump_gate == null || !jump_gate.IsValid()) return null;
						return jump_gate.WorldJumpNode;
					}
					case MyAPIWaypointType.GPS:
						return this.GPS?.Coords;
					case MyAPIWaypointType.BEACON:
						return this.Beacon?.BeaconPosition;
					case MyAPIWaypointType.SERVER:
						if (this.ServerJumpGate == null) return null;
						return Vector3D.Zero;
					default:
						throw new InvalidOperationException("Waypoint is invalid");
				}
			}
			#endregion
		}
	}
}
