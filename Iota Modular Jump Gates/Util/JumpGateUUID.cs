using IOTA.ModularJumpGates.CubeBlock;
using ProtoBuf;
using System;

namespace IOTA.ModularJumpGates.Util
{
	/// <summary>
	/// UUID class for jump gates, jump gate grids, and game logic components
	/// </summary>
	[ProtoContract]
	internal struct JumpGateUUID : IEquatable<JumpGateUUID>
    {
		#region Public Static Variables
		/// <summary>
		/// An empty UUID
		/// </summary>
		public static readonly JumpGateUUID Empty = new JumpGateUUID(0, 0);
		#endregion

		#region Private Variables
		/// <summary>
		/// The grid construct ID component of the UUID or 0 if storing only a grid construct
		/// </summary>
		[ProtoMember(1)]
		private readonly long GridID;

		/// <summary>
		/// The entity ID component of the UUID
		/// </summary>
		[ProtoMember(2)]
        private readonly long EntityID;
		#endregion

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first JumpGateUUID operand</param>
		/// <param name="b">The second JumpGateUUID operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(JumpGateUUID a, JumpGateUUID b)
		{
			if (object.ReferenceEquals(a, b)) return true;
			else if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
			else if (object.ReferenceEquals(b, null)) return object.ReferenceEquals(a, null);
			else return a.Equals(b);
		}

		/// <summary>
		/// Overloads inequality operator "!=" to check inequality
		/// </summary>
		/// <param name="a">The first JumpGateUUID operand</param>
		/// <param name="b">The second JumpGateUUID operand</param>
		/// <returns>Inequality</returns>
		public static bool operator !=(JumpGateUUID a, JumpGateUUID b)
		{
			return !(a == b);
		}
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Creates a JumpGateUUID from a CubeBlockBase logic component
		/// </summary>
		/// <param name="jump_gate">The non-null logic component</param>
		/// <returns>The associated UUID</returns>
		public static JumpGateUUID FromBlock(MyCubeBlockBase block)
		{
			if (block == null) throw new ArgumentNullException("block was null");
			long grid_id = block.JumpGateGrid?.CubeGridID ?? -1;
			return new JumpGateUUID(grid_id, block.TerminalBlock.EntityId);
		}

		/// <summary>
		/// Creates a JumpGateUUID from a MyJumpGateConstruct
		/// </summary>
		/// <param name="cube_grid">The non-null grid construct</param>
		/// <returns>The associated UUID</returns>
		public static JumpGateUUID FromJumpGateGrid(MyJumpGateConstruct cube_grid)
		{
			if (cube_grid == null) throw new ArgumentNullException("cube_grid was null");
			return new JumpGateUUID(0, cube_grid.CubeGridID);
		}

		/// <summary>
		/// Creates a JumpGateUUID from a MyJumpGate
		/// </summary>
		/// <param name="jump_gate">The non-null jump gate</param>
		/// <returns>The associated UUID</returns>
		public static JumpGateUUID FromJumpGate(MyJumpGate jump_gate)
		{
			if (jump_gate == null) throw new ArgumentNullException("jump_gate was null");
			return new JumpGateUUID(jump_gate.JumpGateGrid.CubeGridID, -jump_gate.JumpGateID);
		}
		#endregion

		#region Constructors
		/// <summary>
		/// - Constructor -<br />
		/// Creates a new JumpGateUUID
		/// </summary>
		/// <param name="jump_gate_grid_id">The ID of the MyJumpGateConstruct construct</param>
		/// <param name="entity_id">The ID of the child entity</param>
		private JumpGateUUID(long jump_gate_grid_id, long entity_id)
        {
            this.GridID = jump_gate_grid_id;
            this.EntityID = entity_id;
        }

		public JumpGateUUID(long[] uuid)
		{
			this.GridID = (uuid == null) ? 0 : uuid[0];
			this.EntityID = (uuid == null) ? 0 : uuid[1];
		}
		#endregion

		#region "object" Methods
		/// <summary>
		/// Checks if this Guuid equals another
		/// </summary>
		/// <param name="other">The object to check</param>
		/// <returns>Equality</returns>
		public override bool Equals(object other)
		{
			return other != null && other is JumpGateUUID && this.Equals((JumpGateUUID) other);
		}

		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this Guid</returns>
		public override int GetHashCode()
		{
			byte[] buffer = new byte[16];
			BitConverter.GetBytes(this.GridID).CopyTo(buffer, 0);
			BitConverter.GetBytes(this.EntityID).CopyTo(buffer, sizeof(long));
			return new Guid(buffer).GetHashCode();
		}

		public override string ToString()
		{
			return $"{this.GridID}-{this.EntityID}";
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Checks if this UUID equals another
		/// </summary>
		/// <param name="other">The JumpGateUUID to check</param>
		/// <returns>Equality</returns>
		public bool Equals(JumpGateUUID other)
		{
			return object.ReferenceEquals(this, other) || (this.GridID == other.GridID && this.EntityID == other.EntityID);
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether this UUID represents a MyJumpGateConstruct object</returns>
		public bool IsJumpGateGrid()
        {
            return this.GridID == 0;
        }

		/// <summary>
		/// </summary>
		/// <returns>Whether this UUID represents a MyJumpGate object</returns>
		public bool IsJumpGate()
        {
            return this.GridID != 0 && this.EntityID <= 0;
        }

		/// <summary>
		/// </summary>
		/// <returns>Whether this UUID represents a CubeBlockBase object</returns>
		public bool IsBlock()
        {
            return this.GridID != 0 && this.EntityID > 0;
        }

		/// <summary>
		/// Gets the MyJumpGateConstruct ID component from this UUID
		/// </summary>
		/// <returns>MyJumpGateConstruct ID</returns>
		public long GetJumpGateGrid()
        {
            return this.IsJumpGateGrid() ? this.EntityID : this.GridID;
        }

		/// <summary>
		/// Gets the MyJumpGate ID component from this UUID
		/// </summary>
		/// <returns>MyJumpGate ID</returns>
		public long GetJumpGate()
        {
            return -this.EntityID;
        }

		/// <summary>
		/// Gets the CubeBlockBase ID component from this UUID
		/// </summary>
		/// <returns>CubeBlockBase ID</returns>
		public long GetBlock()
        {
            return this.EntityID;
        }

		public long[] Packed()
		{
			return new long[2] {this.GridID, this.EntityID};
		}
		#endregion
	}
}
