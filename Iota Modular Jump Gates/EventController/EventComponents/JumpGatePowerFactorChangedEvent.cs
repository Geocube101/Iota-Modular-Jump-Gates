using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGatePowerFactorChanged))]
	[MyComponentType(typeof(JumpGatePowerFactorChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGatePowerFactorChangedEvent : MyJumpGateEventBase<double>
	{	
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF9;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGatePowerFactorChangedEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGatePowerFactorChangedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGatePowerFactorChangedEvent_YesDescription").Replace("{%0}", ((this.EventController.IsLowerOrEqualCondition) ? "<=" : ">=").Replace("{%1}", MyJumpGateModSession.AutoconvertSciNotUnits(this.TargetValue, 0).ToString()));
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGatePowerFactorChangedEvent_NoDescription").Replace("{%0}", ((this.EventController.IsLowerOrEqualCondition) ? ">=" : "<=").Replace("{%1}", MyJumpGateModSession.AutoconvertSciNotUnits(this.TargetValue, 0).ToString()));

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
			Vector3D? possible_endpoint = jump_gate.Controller?.BlockSettings.SelectedWaypoint()?.GetEndpoint();
			if (possible_endpoint == null) return 0;
			Vector3D endpoint = possible_endpoint.Value;
			double ratio = jump_gate.CalculateDistanceRatio(ref endpoint);
			return jump_gate.CalculatePowerFactorFromDistanceRatio(ratio);
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsComplete();
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlSlider power_factor_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "TargetPowerFactor");
				power_factor_sdr.Title = MyStringId.GetOrCompute(MyTexts.GetString("DetailedInfo_JumpGateController_PowerFactor"));
				power_factor_sdr.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGatePowerFactorChangedEvent_PowerFactor_Tooltip"));
				power_factor_sdr.SupportsMultipleBlocks = true;
				power_factor_sdr.Visible = block => block.Components.Get<JumpGatePowerFactorChangedEvent>()?.IsSelected ?? false;
				power_factor_sdr.SetLimits(0f, float.MaxValue);
				power_factor_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertSciNotUnits(block.Components.Get<JumpGatePowerFactorChangedEvent>().TargetValue, 4));
				power_factor_sdr.Getter = (block) => (float) (block.Components.Get<JumpGatePowerFactorChangedEvent>().TargetValue);
				power_factor_sdr.Setter = (block, value) => {
					JumpGatePowerFactorChangedEvent event_block = block.Components.Get<JumpGatePowerFactorChangedEvent>();
					event_block.TargetValue = value;
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(power_factor_sdr);
			}

			base.CreateTerminalControls<T, JumpGatePowerFactorChangedEvent>();
		}
	}
}
