using ProtoBuf;
using Sandbox.Game.EntityComponents;
using System;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace IOTA.ModularJumpGates.EventController
{
	internal abstract class MyJumpGateEventSync : MyEventProxyEntityComponent
	{
		[ProtoContract]
		private sealed class MySerializedJumpGateEventInfo
		{
			[ProtoMember(1)]
			public long EntityID;
			[ProtoMember(2)]
			public MyObjectBuilder_ComponentBase SerializedEvent;
		}

		private bool NetworkRegistered = false;
		private ulong LastUpdateTimeEpoch = 0;
		protected bool IsDirty = false;

		private void OnNetworkUpdate(MyNetworkInterface.Packet packet)
		{
			MySerializedJumpGateEventInfo info = packet?.Payload<MySerializedJumpGateEventInfo>();
			if (info == null || info.EntityID != this.Entity.EntityId) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (packet.EpochTime < this.LastUpdateTimeEpoch) return;
				this.Deserialize(info.SerializedEvent);
				this.IsDirty = false;
				packet.Forward(0, true).Send();
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2))
			{
				this.Deserialize(info.SerializedEvent);
				this.IsDirty = false;
			}
		}

		private void Init()
		{
			if (this.NetworkRegistered || !MyJumpGateModSession.Network.Registered) return;
			MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_EVENT_CONTROLLER_EVENT, this.OnNetworkUpdate);
			this.NetworkRegistered = true;
		}

		private void Release()
		{
			if (!this.NetworkRegistered || !MyJumpGateModSession.Network.Registered) return;
			MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_EVENT_CONTROLLER_EVENT, this.OnNetworkUpdate);
			this.NetworkRegistered = false;
		}

		protected virtual void Update()
		{
			if (!this.IsDirty || !MyJumpGateModSession.Network.Registered) return;

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
				PacketType = MyPacketTypeEnum.UPDATE_EVENT_CONTROLLER_EVENT,
				TargetID = 0,
				Broadcast = false,
			};

			packet.Payload(new MySerializedJumpGateEventInfo {
				EntityID = this.Entity.EntityId,
				SerializedEvent = this.Serialize(),
			});

			packet.Send();
		}

		public void SetDirty()
		{
			this.IsDirty = true;
			this.LastUpdateTimeEpoch = (ulong) (DateTime.UtcNow - DateTime.MinValue).Ticks;
		}

		public override void OnAddedToContainer()
		{
			base.OnAddedToContainer();
			this.Init();
		}

		public override void OnBeforeRemovedFromContainer()
		{
			base.OnBeforeRemovedFromContainer();
			this.Release();
		}
	}
}
