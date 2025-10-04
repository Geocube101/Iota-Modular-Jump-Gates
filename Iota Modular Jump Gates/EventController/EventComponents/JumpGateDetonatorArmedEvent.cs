using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateDetonatorArmed))]
	[MyComponentType(typeof(JumpGateDetonatorArmedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateDetonatorArmedEvent : MyJumpGateEventBase<bool>
	{
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => false;
		public override bool IsJumpGateSelectionUsed => true;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFEE;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGateDetonatorArmedEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGateDetonatorArmedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGateDetonatorArmedEvent_YesDescription");
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGateDetonatorArmedEvent_NoDescription");

		protected override void CheckValueAgainstTarget(bool new_value, bool old_value, bool target)
		{
			if (new_value == target && old_value != target) this.TriggerAction(1);
			else if (new_value != target && old_value == target) this.TriggerAction(0);
		}

		protected override bool GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return jump_gate.ControlObject?.BlockSettings?.GateDetonatorArmed() ?? false;
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsComplete();
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			base.CreateTerminalControls<T, JumpGateDetonatorArmedEvent>();
		}
	}
}
