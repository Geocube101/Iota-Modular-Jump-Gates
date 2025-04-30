using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateNodeVelocityChanged))]
	[MyComponentType(typeof(JumpGateNodeVelocityChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateNodeVelocityChangedEvent : MyJumpGateEventBase<double>
	{
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF6;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Node Velocity Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateNodeVelocityChangedEvent);
		public override string YesNoToolbarYesDescription => $"Node Velocity {((this.EventController.IsLowerOrEqualCondition) ? "<=" : ">=")} {MyJumpGateModSession.AutoconvertMetricUnits(this.TargetValue, "m/s", 2)}";
		public override string YesNoToolbarNoDescription => $"Node Velocity {((this.EventController.IsLowerOrEqualCondition) ? ">=" : "<=")} {MyJumpGateModSession.AutoconvertMetricUnits(this.TargetValue, "m/s", 2)}";

		protected override void CheckValueAgainstTarget(double new_value, double old_value, double target)
		{
			IMyEventControllerBlock event_controller = this.EventController;
			if (event_controller.IsLowerOrEqualCondition && new_value <= target && old_value > target) this.TriggerAction(0);
			else if (event_controller.IsLowerOrEqualCondition && new_value > target && old_value <= target) this.TriggerAction(1);
			else if (!event_controller.IsLowerOrEqualCondition && new_value >= target && old_value < target) this.TriggerAction(0);
			else if (!event_controller.IsLowerOrEqualCondition && new_value < target && old_value >= target) this.TriggerAction(1);
		}

		protected override double GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return Math.Round(jump_gate.JumpNodeVelocity.Length(), 4);
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsValid();
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlSlider node_velocity_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "TargetVelocity");
				node_velocity_sdr.Title = MyStringId.GetOrCompute("Velocity (m/s)");
				node_velocity_sdr.Tooltip = MyStringId.GetOrCompute("The jump node velocity (in meters per second) to check against");
				node_velocity_sdr.SupportsMultipleBlocks = false;
				node_velocity_sdr.Visible = block => block.Components.Get<JumpGateNodeVelocityChangedEvent>()?.IsSelected ?? false;
				node_velocity_sdr.SetLimits(0, 1e6f);
				node_velocity_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertMetricUnits(block.Components.Get<JumpGateNodeVelocityChangedEvent>().TargetValue, "m/s", 4));
				node_velocity_sdr.Getter = (block) => (float) block.Components.Get<JumpGateNodeVelocityChangedEvent>().TargetValue;
				node_velocity_sdr.Setter = (block, value) => {
					JumpGateNodeVelocityChangedEvent event_block = block.Components.Get<JumpGateNodeVelocityChangedEvent>();
					event_block.TargetValue = value;
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(node_velocity_sdr);
			}

			base.CreateTerminalControls<T, JumpGateNodeVelocityChangedEvent>();
		}
	}
}
