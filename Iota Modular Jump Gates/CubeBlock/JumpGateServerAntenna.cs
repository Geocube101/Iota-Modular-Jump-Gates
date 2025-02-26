using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

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
		public MyJumpGateServerAntenna(MySerializedJumpGateServerAntenna serialized) : base(serialized)
		{
			this.FromSerialized(serialized);
		}
		#endregion

		public bool FromSerialized(MySerializedJumpGateServerAntenna antenna)
		{
			if (!base.FromSerialized(antenna)) return false;
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
