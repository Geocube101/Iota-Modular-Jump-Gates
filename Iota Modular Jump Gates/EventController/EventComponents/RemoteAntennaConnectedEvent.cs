using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventRemoteAntennaConnected))]
	[MyComponentType(typeof(RemoteAntennaConnectedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class RemoteAntennaConnectedEvent : MyJumpGateEventBase<bool>
	{
		private bool? TriggerIndex = null;
		private readonly ConcurrentLinkedHashSet<MyJumpGateRemoteAntenna> ListeningAntennas = new ConcurrentLinkedHashSet<MyJumpGateRemoteAntenna>();

		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => true;
		public override bool IsJumpGateSelectionUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF2;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_RemoteAntennaConnectedEvent"));
		public override string ComponentTypeDebugString => nameof(RemoteAntennaConnectedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_RemoteAntennaConnectedEvent_YesDescription");
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_RemoteAntennaConnectedEvent_NoDescription");

		private void OnAntennaConnectionChanged(MyJumpGateRemoteAntenna caller, MyJumpGateController outbound_controller, MyJumpGate inbound_controllee, byte channel, bool connecting)
		{
			if (!this.ListeningAntennas.Contains(caller))
			{
				caller.OnAntennaConnection -= this.OnAntennaConnectionChanged;
				return;
			}
			
			this.TriggerIndex = connecting;
		}

		protected override void Update()
		{
			base.Update();

			if (this.TriggerIndex != null)
			{
				this.TriggerAction((this.TriggerIndex.Value) ? 0 : 1);
				this.TriggerIndex = null;
			}

			this.RemoveBlocks(this.ListeningAntennas.Where((antenna) => antenna.Closed).Select((antenna) => antenna.TerminalBlock));
		}

		protected override void CheckValueAgainstTarget(bool new_value, bool old_value, bool target) { }

		protected override void OnSelected()
		{
			base.OnSelected();
			foreach (MyJumpGateRemoteAntenna listener in this.ListeningAntennas) listener.OnAntennaConnection += this.OnAntennaConnectionChanged;
		}

		protected override void OnUnselected()
		{
			base.OnUnselected();
			foreach (MyJumpGateRemoteAntenna listener in this.ListeningAntennas) listener.OnAntennaConnection -= this.OnAntennaConnectionChanged;
		}

		protected override void AppendCustomInfo(StringBuilder sb)
		{
			base.AppendCustomInfo(sb);
			sb.Append($"{MyTexts.GetString("DetailedInfo_RemoteAntennaConnectedEvent_ListeningAntennas")}:\n");

			foreach (MyJumpGateRemoteAntenna antenna in this.ListeningAntennas)
			{
				string name = antenna.TerminalBlock?.CustomName ?? antenna.BlockID.ToString();
				IEnumerable<byte> channels = Enumerable.Range(0, MyJumpGateRemoteAntenna.ChannelCount).Select((channel) => (byte) channel).Where((channel) => antenna.GetConnectedRemoteAntenna(channel) != null);
				sb.Append($" - {MyTexts.GetString("DetailedInfo_RemoteAntennaConnectedEvent_AntennaOnChannels").Replace("{%0}", name).Replace("{%1}", string.Join("/", channels))}\n");
			}
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			base.CreateTerminalControls<T, JumpGateEntityEnteredEvent>();
		}

		public override bool IsBlockValidForList(IMyTerminalBlock block)
		{
			MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
			return antenna != null && !antenna.Closed;
		}

		public override void OnBeforeRemovedFromContainer()
		{
			base.OnBeforeRemovedFromContainer();
			foreach (MyJumpGateRemoteAntenna antenna in this.ListeningAntennas) antenna.OnAntennaConnection -= this.OnAntennaConnectionChanged;
			this.ListeningAntennas.Clear();
		}

		public override void AddBlocks(List<IMyTerminalBlock> blocks)
		{
			base.AddBlocks(blocks);

			foreach (IMyTerminalBlock block in blocks)
			{
				MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
				if (antenna == null || antenna.Closed || this.ListeningAntennas.Contains(antenna)) continue;
				this.ListeningAntennas.Add(antenna);
				antenna.OnAntennaConnection += this.OnAntennaConnectionChanged;
			}
		}

		public override void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
		{
			base.RemoveBlocks(blocks);

			foreach (IMyTerminalBlock block in blocks)
			{
				MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
				if (antenna == null || !this.ListeningAntennas.Contains(antenna)) continue;
				this.ListeningAntennas.Remove(antenna);
				antenna.OnAntennaConnection -= this.OnAntennaConnectionChanged;
			}
		}
	}
}
