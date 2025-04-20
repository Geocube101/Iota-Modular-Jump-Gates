using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateControllerChanged))]
	[MyComponentType(typeof(JumpGateControllerChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateControllerChangedEvent : MyJumpGateEventBase<long>
	{
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF3;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Controller Attached");
		public override string ComponentTypeDebugString => nameof(JumpGateControllerChangedEvent);

		protected override void CheckValueAgainstTarget(long new_value, long old_value, long target)
		{
			if (new_value != -1 && old_value == -1) this.TriggerAction(0);
			else if (new_value == -1 && old_value != -1) this.TriggerAction(1);
		}

		protected override long GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return jump_gate.Controller?.BlockID ?? -1;
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			base.CreateTerminalControls<T, JumpGateControllerChangedEvent>();
		}
	}
}
