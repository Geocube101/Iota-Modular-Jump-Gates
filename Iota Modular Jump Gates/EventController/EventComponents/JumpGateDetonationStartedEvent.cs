using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateDetonationStarted))]
	[MyComponentType(typeof(JumpGateDetonationStartedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateDetonationStartedEvent : MyJumpGateEventBase<bool>
	{
		private bool? TriggerIndex = null;
		private readonly List<MyJumpGate> ListeningJumpGates = new List<MyJumpGate>();

		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => false;
		public override bool IsJumpGateSelectionUsed => true;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFEF;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGateDetonationStartedEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGateDetonationStartedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGateDetonationStartedEvent_YesDescription");
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGateDetonationStartedEvent_NoDescription");

		private void OnDetonationStart(MyJumpGate caller, float timer)
		{
			if (!this.IsListeningToJumpGate(caller))
			{
				caller.OnGateDetonation -= this.OnDetonationStart;
				return;
			}
			
			this.TriggerIndex = timer != -1;
			MyAPIGateway.Utilities.ShowNotification($"TIMER={timer}, TRIGGERDEX={this.TriggerIndex}");
		}

		protected override void Update()
		{
			base.Update();

			if (this.TriggerIndex != null)
			{
				this.TriggerAction((this.TriggerIndex.Value) ? 0 : 1);
				this.TriggerIndex = null;
			}
		}

		protected override void CheckValueAgainstTarget(bool new_value, bool old_value, bool target)
		{
			
		}

		protected override bool GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			if (this.ListeningJumpGates.Contains(jump_gate)) return true;
			this.OnJumpGateAdded(jump_gate);
			return true;
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsValid();
		}

		protected override void OnJumpGateAdded(MyJumpGate jump_gate)
		{
			base.OnJumpGateAdded(jump_gate);
			if (!MyNetworkInterface.IsServerLike || this.ListeningJumpGates.Contains(jump_gate)) return;
			jump_gate.OnGateDetonation += this.OnDetonationStart;
			this.ListeningJumpGates.Add(jump_gate);
		}

		protected override void OnJumpGateRemoved(MyJumpGate jump_gate)
		{
			base.OnJumpGateAdded(jump_gate);
			if (!MyNetworkInterface.IsServerLike) return;
			jump_gate.OnGateDetonation -= this.OnDetonationStart;
			this.ListeningJumpGates.Remove(jump_gate);
		}

		protected override void OnSelected()
		{
			base.OnSelected();
			foreach (MyJumpGate listener in this.ListeningJumpGates) listener.OnGateDetonation += this.OnDetonationStart;
		}

		protected override void OnUnselected()
		{
			base.OnUnselected();
			foreach (MyJumpGate listener in this.ListeningJumpGates) listener.OnGateDetonation -= this.OnDetonationStart;
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			base.CreateTerminalControls<T, JumpGateDetonationStartedEvent>();
		}
	}
}
