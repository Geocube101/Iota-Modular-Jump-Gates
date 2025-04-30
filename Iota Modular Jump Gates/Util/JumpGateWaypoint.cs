using ProtoBuf;
using System;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.Util
{
    public enum MyWaypointType : byte { NONE = 0, JUMP_GATE = 1, JUMPGATE = 1, GATE = 1, GPS = 2, BEACON = 3, SERVER = 4 };

    /// <summary>
    /// Serializable wrapper for GPSs
    /// </summary>
    [ProtoContract]
	public class MyGpsWrapper : IEquatable<MyGpsWrapper>
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
		public static bool operator ==(MyGpsWrapper a, MyGpsWrapper b)
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
		public static bool operator !=(MyGpsWrapper a, MyGpsWrapper b)
        {
            return !(a == b);
        }
		#endregion

		#region Constructors
        /// <summary>
        /// Dummy default constructor for ProtoBuf
        /// </summary>
		public MyGpsWrapper() { }

		/// <summary>
		/// Creates a new MyGpsWrapper from a GPS
		/// </summary>
		/// <param name="gps">The source GPS</param>
		public MyGpsWrapper(IMyGps gps)
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
            return this.Equals(obj as MyGpsWrapper);
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
		public bool Equals(MyGpsWrapper other)
		{
			if (object.ReferenceEquals(other, null)) return false;
			else if (object.ReferenceEquals(this, other)) return true;
			return this.Coords == other.Coords && this.GPSColor == other.GPSColor && this.Name == other.Name && this.Description == other.Description;
		}
		#endregion
	}

	[ProtoContract]
    public class MyServerJumpGate : IEquatable<MyServerJumpGate>
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
        public JumpGateUUID JumpGate;
		#endregion

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first MyServerJumpGate operand</param>
		/// <param name="b">The second MyServerJumpGate operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(MyServerJumpGate a, MyServerJumpGate b)
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
		public static bool operator !=(MyServerJumpGate a, MyServerJumpGate b)
		{
			return !(a == b);
		}
		#endregion

		#region Constructors
        /// <summary>
        /// Dummy default constructor for ProtoBuf
        /// </summary>
		public MyServerJumpGate() { }

        /// <summary>
        /// Creates a new MyServerJumpGate
        /// </summary>
        /// <param name="server_address">The target server address</param>
        /// <param name="jump_gate">The target jump gate</param>
        /// <param name="server_password">The server password if applicable or null</param>
		public MyServerJumpGate(string server_address, JumpGateUUID jump_gate, string server_password = null)
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
			return this.Equals(obj as MyServerJumpGate);
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
		public bool Equals(MyServerJumpGate other)
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
    public class MyJumpGateWaypoint : IEquatable<MyJumpGateWaypoint>
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
        public MyGpsWrapper GPS { get; private set; } = null;

		/// <summary>
		/// The target beacon
		/// </summary>
		[ProtoMember(3)]
		public MyBeaconLinkWrapper Beacon { get; private set; } = null;

        /// <summary>
        /// The target server jump gate
        /// </summary>
        [ProtoMember(4)]
        public MyServerJumpGate ServerJumpGate { get; private set; } = null;

        /// <summary>
        /// The type of waypoint this is
        /// </summary>
        [ProtoMember(5)]
        public MyWaypointType WaypointType { get; private set; } = MyWaypointType.NONE;
		#endregion

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first MyJumpGateWaypoint operand</param>
		/// <param name="b">The second MyJumpGateWaypoint operand</param>
		/// <returns>Equality</returns>
		public static bool operator== (MyJumpGateWaypoint a, MyJumpGateWaypoint b)
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
		public static bool operator!= (MyJumpGateWaypoint a, MyJumpGateWaypoint b)
        {
            return !(a == b);
        }
		#endregion

		#region Constructors
        /// <summary>
        /// Dummy default constructor for Protobuf
        /// </summary>
		MyJumpGateWaypoint() { }

        /// <summary>
        /// Creates a new waypoint targeting the specified jump gate
        /// </summary>
        /// <param name="jump_gate">The non-null jump gate</param>
        internal MyJumpGateWaypoint(MyJumpGate jump_gate)
        {
            this.JumpGate = JumpGateUUID.FromJumpGate(jump_gate).ToGuid();
            this.WaypointType = MyWaypointType.JUMP_GATE;
		}

		/// <summary>
		/// Creates a new waypoint targeting the specified GPS
		/// </summary>
		/// <param name="gps">The non-null GPS</param>
		public MyJumpGateWaypoint(IMyGps gps)
        {
            this.GPS = new MyGpsWrapper(gps);
            this.WaypointType = MyWaypointType.GPS;
        }

		/// <summary>
		/// Creates a new waypoint targeting the specified beacon
		/// </summary>
		/// <param name="gps">The non-null beacon</param>
		public MyJumpGateWaypoint(MyBeaconLinkWrapper beacon)
		{
			this.Beacon = beacon;
			this.WaypointType = MyWaypointType.BEACON;
		}

		/// <summary>
		/// Creates a new waypoint targeting the specified server jump gate
		/// </summary>
		/// <param name="server_gate">The non-null server jump gate</param>
		public MyJumpGateWaypoint(MyServerJumpGate server_gate)
        {
            this.ServerJumpGate = server_gate;
            this.WaypointType = MyWaypointType.SERVER;
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
			return this.Equals(obj as MyJumpGateWaypoint);
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

		#region Internal Methods
		/// <summary>
		/// Gets the endpoint of this waypoint in world coordinates
		/// </summary>
		/// <param name="target_jump_gate">The targeted jump gate or null if target is not a jump gate</param>
		/// <returns>The target's world coordinates<br />null if this waypoint is None<br />Vector3D.Zero if this waypoint targets a server</returns>
		/// <exception cref="InvalidOperationException"></exception>
		internal Vector3D? GetEndpoint(out MyJumpGate target_jump_gate)
		{
			target_jump_gate = null;

			switch (this.WaypointType)
			{
				case MyWaypointType.NONE:
					return null;
				case MyWaypointType.JUMP_GATE:
					target_jump_gate = MyJumpGateModSession.Instance.GetJumpGate(JumpGateUUID.FromGuid(this.JumpGate));
					if (target_jump_gate == null || (!MyNetworkInterface.IsStandaloneMultiplayerClient && !target_jump_gate.IsValid())) return null;
					return target_jump_gate.WorldJumpNode;
				case MyWaypointType.GPS:
					return this.GPS?.Coords;
				case MyWaypointType.BEACON:
					return this.Beacon?.BeaconPosition;
				case MyWaypointType.SERVER:
					if (this.ServerJumpGate == null) return null;
					return Vector3D.Zero;
				default:
					throw new InvalidOperationException("Waypoint is invalid");
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Whether or this waypoint has a valid target
		/// </summary>
		/// <returns>MyJumpGateWaypoint::WaypointType != MyWaypointType.None</returns>
		public bool HasValue()
        {
            return this.WaypointType != MyWaypointType.NONE;
        }

		/// <summary>
		/// Checks if this MyJumpGateWaypoint equals another
		/// </summary>
		/// <param name="other">The MyJumpGateWaypoint to check</param>
		/// <returns>Equality</returns>
		public bool Equals(MyJumpGateWaypoint other)
        {
			if (object.ReferenceEquals(other, null)) return false;
			else if (object.ReferenceEquals(this, other)) return true;
			else if (this.WaypointType != other.WaypointType) return false;
			else if (this.WaypointType == MyWaypointType.JUMP_GATE) return this.JumpGate == other.JumpGate;
			else if (this.WaypointType == MyWaypointType.GPS) return this.GPS == other.GPS;
			else if (this.WaypointType == MyWaypointType.BEACON) return this.Beacon == other.Beacon;
			else if (this.WaypointType == MyWaypointType.SERVER) return this.ServerJumpGate == other.ServerJumpGate;
			else if (this.WaypointType == MyWaypointType.NONE) return true;
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
				case MyWaypointType.NONE:
					return null;
				case MyWaypointType.JUMP_GATE:
				{
					MyJumpGate jump_gate = MyJumpGateModSession.Instance.GetJumpGate(JumpGateUUID.FromGuid(this.JumpGate));
					if (jump_gate == null || (!MyNetworkInterface.IsStandaloneMultiplayerClient && !jump_gate.IsValid())) return null;
					return jump_gate.WorldJumpNode;
				}
				case MyWaypointType.GPS:
					return this.GPS?.Coords;
				case MyWaypointType.BEACON:
					return this.Beacon?.BeaconPosition;
				case MyWaypointType.SERVER:
					if (this.ServerJumpGate == null) return null;
					return Vector3D.Zero;
				default:
					throw new InvalidOperationException("Waypoint is invalid");
			}
        }

		public void GetNameAndTooltip(ref Vector3D this_pos, out string name, out string tooltip)
		{
			name = null;
			tooltip = null;
			int cutoff = 15;
			MyJumpGate destination_jump_gate;
			Vector3D? endpoint = this.GetEndpoint(out destination_jump_gate);
			if (endpoint == null) return;
			double distance = Vector3D.Distance(this_pos, endpoint.Value);

			if (this.WaypointType == MyWaypointType.JUMP_GATE && destination_jump_gate != null && (MyNetworkInterface.IsStandaloneMultiplayerClient || destination_jump_gate.IsComplete()))
			{
				string grid_name = destination_jump_gate.JumpGateGrid?.PrimaryCubeGridCustomName ?? "N/A";
				string grid_name_cut = (grid_name.Length > cutoff) ? $"{grid_name.Substring(0, cutoff - 3)}..." : grid_name;
				string gate_name = destination_jump_gate.GetPrintableName();
				string gate_name_cut = (gate_name.Length > cutoff) ? $"{gate_name.Substring(0, cutoff - 3)}..." : gate_name;
				tooltip = $"{grid_name} - {gate_name}: (Jump Gate {MyJumpGateModSession.AutoconvertMetricUnits(distance, "m", 2)} away)";
				name = $"{grid_name_cut} - {gate_name_cut}: (Jump Gate {MyJumpGateModSession.AutoconvertMetricUnits(distance, "m", 2)} away)";
			}
			else if (this.WaypointType == MyWaypointType.GPS)
			{
				MyGpsWrapper gps = this.GPS;
				name = tooltip = $"{gps.Name} (GPS {MyJumpGateModSession.AutoconvertMetricUnits(distance, "m", 2)} away)";
			}
			else if (this.WaypointType == MyWaypointType.BEACON)
			{
				name = tooltip = $"{this.Beacon.BroadcastName} (Beacon {MyJumpGateModSession.AutoconvertMetricUnits(distance, "m", 2)} away)";
			}
		}
		#endregion
	}
}
