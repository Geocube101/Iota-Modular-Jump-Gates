using ProtoBuf;

namespace IOTA.ModularJumpGates.CubeBlock
{
	/// <summary>
	/// Game logic for jump gate server antennas
	/// </summary>
	internal class MyJumpGateServerAntenna : MyCubeBlockBase
	{
		#region Constructors
		public MyJumpGateServerAntenna() : base() { }

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		public MyJumpGateServerAntenna(MySerializedJumpGateServerAntenna serialized, MyJumpGateConstruct parent = null) : base(serialized, parent)
		{
			this.FromSerialized(serialized, parent);
		}
		#endregion

		public bool FromSerialized(MySerializedJumpGateServerAntenna antenna, MyJumpGateConstruct parent = null)
		{
			if (!base.FromSerialized(antenna, parent)) return false;
			return true;
		}

		public new MySerializedJumpGateServerAntenna ToSerialized(bool as_client_request)
		{
			if (as_client_request) return base.ToSerialized<MySerializedJumpGateServerAntenna>(true);
			MySerializedJumpGateServerAntenna serialized = base.ToSerialized<MySerializedJumpGateServerAntenna>(false);
			return serialized;
		}
	}

	[ProtoContract]
	internal class MySerializedJumpGateServerAntenna : MySerializedCubeBlockBase
	{
		/// <summary>
		/// The connected server's address
		/// </summary>
		[ProtoMember(20)]
		public string ServerAddress;

		/// <summary>
		/// The connected server's password
		/// </summary>
		[ProtoMember(21)]
		public string ServerPassword;

	}
}
