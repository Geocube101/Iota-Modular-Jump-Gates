using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateEffectiveRadiusChanged))]
	[MyComponentType(typeof(JumpGateEffectiveRadiusChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateEffectiveRadiusChangedEvent : MyJumpGateEventBase<double>
	{
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF7;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Effective Radius Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateEffectiveRadiusChangedEvent);

		protected override void CheckValueAgainstTarget(double new_value, double old_value, double target)
		{
			IMyEventControllerBlock event_controller = this.EventController;
			if (event_controller.IsLowerOrEqualCondition && new_value <= target && old_value > target) this.TriggerAction(0);
			else if (event_controller.IsLowerOrEqualCondition && new_value > target && old_value <= target) this.TriggerAction(1);
			else if (!event_controller.IsLowerOrEqualCondition && new_value >= target && old_value < target) this.TriggerAction(0);
			else if (!event_controller.IsLowerOrEqualCondition && new_value < target && old_value >= target) this.TriggerAction(1);
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsComplete();
		}

		protected override double GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return jump_gate.GetEffectiveJumpEllipse().Radii.X;
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlSlider radius_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "TargetRadius");
				radius_sdr.Title = MyStringId.GetOrCompute("Radius (m)");
				radius_sdr.Tooltip = MyStringId.GetOrCompute("The effective jump space radius (in meters) to check against");
				radius_sdr.SupportsMultipleBlocks = false;
				radius_sdr.Visible = block => block.Components.Get<JumpGateEffectiveRadiusChangedEvent>()?.IsSelected ?? false;
				radius_sdr.SetLimits((block) => 0, (block) => (float) Math.Round(Math.Max(MyJumpGateModSession.Configuration.DriveConfiguration.LargeDriveRaycastDistance, MyJumpGateModSession.Configuration.DriveConfiguration.SmallDriveRaycastDistance) * 1.5));
				radius_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertMetricUnits(block.Components.Get<JumpGateEffectiveRadiusChangedEvent>().TargetValue, "m", 4));
				radius_sdr.Getter = (block) => (float) block.Components.Get<JumpGateEffectiveRadiusChangedEvent>().TargetValue;
				radius_sdr.Setter = (block, value) => block.Components.Get<JumpGateEffectiveRadiusChangedEvent>().TargetValue = value;
				MyAPIGateway.TerminalControls.AddControl<T>(radius_sdr);
			}

			base.CreateTerminalControls<T, JumpGateEffectiveRadiusChangedEvent>();
		}
	}
}
