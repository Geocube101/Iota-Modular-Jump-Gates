using IOTA.ModularJumpGates.Session;
using System;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	public enum MyGridInvalidationReason : byte { NONE, CLOSED, NULL_GRID, INSUFFICIENT_GRIDS, NULL_PHYSICS };

	internal partial class MyJumpGateConstruct : IEquatable<MyJumpGateConstruct>
	{
		#region Constructors
		/// <summary>
		/// Creates a new construct from a CubeGrid
		/// </summary>
		/// <param name="source">The main cube grid</param>
		public MyJumpGateConstruct(IMyCubeGrid source, long fallback_id)
		{
			this.CubeGrid = source;
			this.PrimaryCubeGridCustomName = source?.CustomName;

			if (MyJumpGateModSession.Instance.Network.Registered)
			{
				MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.UPDATE_GRID, this.OnNetworkJumpGateGridUpdate);
				MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.STATICIFY_CONSTRUCT, this.OnStaticifyConstruct);
				MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
			}

			if (source == null) this.Suspend(fallback_id, false);
			else this.SetupConstruct();
		}

		/// <summary>
		/// Creates a new construct from a serialized grid
		/// </summary>
		/// <param name="serialized">The serialized grid data</param>
		public MyJumpGateConstruct(MySerializedJumpGateConstruct serialized)
		{
			this.FromSerialized(serialized);

			if (MyJumpGateModSession.Instance.Network.Registered)
			{
				MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.UPDATE_GRID, this.OnNetworkJumpGateGridUpdate);
				MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.STATICIFY_CONSTRUCT, this.OnStaticifyConstruct);
				MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
			}
		}
		#endregion

		#region "object" Methods
		public bool Equals(MyJumpGateConstruct other)
		{
			return other != null && (object.ReferenceEquals(this, other) || this.CubeGridID == other.CubeGridID);
		}

		public override bool Equals(object other)
		{
			return other is MyJumpGateConstruct && this.Equals((MyJumpGateConstruct) other);
		}

		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this construct's main cube grid</returns>
		public override int GetHashCode()
		{
			return this.CubeGrid.GetHashCode();
		}
		#endregion
	}
}
