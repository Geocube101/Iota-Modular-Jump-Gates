using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateTargetDistanceChanged))]
	[MyComponentType(typeof(JumpGateTargetDistanceChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateTargetDistanceChangedEvent : MyJumpGateEventBase<double>
	{
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF5;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Target Distance Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateTargetDistanceChangedEvent);

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
			Vector3D? endpoint = jump_gate.Controller?.BlockSettings.SelectedWaypoint()?.GetEndpoint();
			return (endpoint == null) ? 0 : (Vector3D.Distance(endpoint.Value, jump_gate.WorldJumpNode) / 1e3);
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsComplete();
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlSlider target_distance_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "TargetRadius");
				target_distance_sdr.Title = MyStringId.GetOrCompute("Distance (km)");
				target_distance_sdr.Tooltip = MyStringId.GetOrCompute("The distance (in kilometers) between the jump node and destination to check against");
				target_distance_sdr.SupportsMultipleBlocks = false;
				target_distance_sdr.Visible = block => block.Components.Get<JumpGateTargetDistanceChangedEvent>()?.IsSelected ?? false;
				target_distance_sdr.SetLimits(0, 1e24f);
				target_distance_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertMetricUnits(block.Components.Get<JumpGateTargetDistanceChangedEvent>().TargetValue * 1e3, "m", 4));
				target_distance_sdr.Getter = (block) => (float) block.Components.Get<JumpGateTargetDistanceChangedEvent>().TargetValue;
				target_distance_sdr.Setter = (block, value) => block.Components.Get<JumpGateTargetDistanceChangedEvent>().TargetValue = value;
				MyAPIGateway.TerminalControls.AddControl<T>(target_distance_sdr);
			}

			base.CreateTerminalControls<T, JumpGateTargetDistanceChangedEvent>();
		}
	}
}
