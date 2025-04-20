using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateRequiredPowerChanged))]
	[MyComponentType(typeof(JumpGateRequiredPowerChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateRequiredPowerChangedEvent : MyJumpGateEventBase<double>
	{	
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFFA;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Required Power Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateRequiredPowerChangedEvent);

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
			return jump_gate.CalculateTotalRequiredPower();
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsComplete();
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlSlider required_power_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "TargetRequiredPower");
				required_power_sdr.Title = MyStringId.GetOrCompute("Required Power (Mw)");
				required_power_sdr.Tooltip = MyStringId.GetOrCompute("The required power (in megawatts) check against");
				required_power_sdr.SupportsMultipleBlocks = false;
				required_power_sdr.Visible = block => block.Components.Get<JumpGateRequiredPowerChangedEvent>()?.IsSelected ?? false;
				required_power_sdr.SetLimits(0f, 1e24f);
				required_power_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertMetricUnits(block.Components.Get<JumpGateRequiredPowerChangedEvent>().TargetValue * 1e6, "w", 4));
				required_power_sdr.Getter = (block) => (float) (block.Components.Get<JumpGateRequiredPowerChangedEvent>().TargetValue);
				required_power_sdr.Setter = (block, value) => block.Components.Get<JumpGateRequiredPowerChangedEvent>().TargetValue = value;
				MyAPIGateway.TerminalControls.AddControl<T>(required_power_sdr);
			}

			base.CreateTerminalControls<T, JumpGateRequiredPowerChangedEvent>();
		}
	}
}
