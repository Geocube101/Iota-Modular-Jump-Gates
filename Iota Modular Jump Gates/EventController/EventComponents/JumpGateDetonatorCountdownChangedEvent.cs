using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using VRage;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateDetonatorCountdownChanged))]
	[MyComponentType(typeof(JumpGateDetonatorCountdownChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateDetonatorCountdownChangedEvent : MyJumpGateEventBase<float>
	{
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => false;
		public override bool IsJumpGateSelectionUsed => true;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF0;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGateDetonatorCountdownChangedEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGateDetonatorCountdownChangedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGateDetonatorCountdownChangedEvent_YesDescription").Replace("{%0}", ((this.EventController.IsLowerOrEqualCondition) ? "<=" : ">=").Replace("{%1}", MyJumpGateModSession.AutoconvertMetricUnits(this.TargetValue, "m", 2).ToString()));
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGateDetonatorCountdownChangedEvent_NoDescription").Replace("{%0}", ((this.EventController.IsLowerOrEqualCondition) ? ">=" : "<=").Replace("{%1}", MyJumpGateModSession.AutoconvertMetricUnits(this.TargetValue, "m", 2).ToString()));

		protected override void CheckValueAgainstTarget(float new_value, float old_value, float target)
		{
			IMyEventControllerBlock event_controller = this.EventController;
			if (event_controller.IsLowerOrEqualCondition && new_value <= target && old_value > target) this.TriggerAction(0);
			else if (event_controller.IsLowerOrEqualCondition && new_value > target && old_value <= target) this.TriggerAction(1);
			else if (!event_controller.IsLowerOrEqualCondition && new_value >= target && old_value < target) this.TriggerAction(0);
			else if (!event_controller.IsLowerOrEqualCondition && new_value < target && old_value >= target) this.TriggerAction(1);
		}

		protected override float GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			float timer = jump_gate.ControlObject?.BlockSettings?.GateDetonationTime() ?? float.MaxValue;
			return (jump_gate.ManualDetonationTimeout == -1) ? timer : (jump_gate.ManualDetonationTimeout / 60f - 1f);
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsComplete();
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlSlider target_distance_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "TargetTime");
				target_distance_sdr.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateDetonatorCountdownChangedEvent_Seconds")} (s):");
				target_distance_sdr.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateDetonatorCountdownChangedEvent_Seconds_Tooltip"));
				target_distance_sdr.SupportsMultipleBlocks = true;
				target_distance_sdr.Visible = block => block.Components.Get<JumpGateDetonatorCountdownChangedEvent>()?.IsSelected ?? false;
				target_distance_sdr.SetLimits(0, 3600);
				target_distance_sdr.Writer = (block, string_builder) => {
					float timeout = block.Components.Get<JumpGateDetonatorCountdownChangedEvent>().TargetValue;
					string_builder.Append($"{(int) Math.Floor(timeout / 3600):00}:{(int) Math.Floor(timeout % 3600 / 60d):00}:{(int) Math.Floor(timeout % 60d):00}");
				};
				target_distance_sdr.Getter = (block) => block.Components.Get<JumpGateDetonatorCountdownChangedEvent>().TargetValue;
				target_distance_sdr.Setter = (block, value) => {
					JumpGateDetonatorCountdownChangedEvent event_block = block.Components.Get<JumpGateDetonatorCountdownChangedEvent>();
					event_block.TargetValue = value;
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(target_distance_sdr);
			}

			base.CreateTerminalControls<T, JumpGateDetonatorCountdownChangedEvent>();
		}
	}
}
