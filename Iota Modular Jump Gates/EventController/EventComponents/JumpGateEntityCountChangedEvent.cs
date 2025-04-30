using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateEntityCountChanged))]
	[MyComponentType(typeof(JumpGateEntityCountChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateEntityCountChangedEvent : MyJumpGateEventBase<int>
	{
		private bool IsControllerFiltered = true;
		
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFFD;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Entity Count Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateEntityCountChangedEvent);
		public override string YesNoToolbarYesDescription => $"Number of Entities {((this.EventController.IsLowerOrEqualCondition) ? "<=" : ">=")} {MyJumpGateModSession.AutoconvertSciNotUnits(this.TargetValue, 0)}";
		public override string YesNoToolbarNoDescription => $"Number of Entities {((this.EventController.IsLowerOrEqualCondition) ? ">=" : "<=")} {MyJumpGateModSession.AutoconvertSciNotUnits(this.TargetValue, 0)}";

		protected override void CheckValueAgainstTarget(int new_value, int old_value, int target)
		{
			IMyEventControllerBlock event_controller = this.EventController;
			if (event_controller.IsLowerOrEqualCondition && new_value <= target && old_value > target) this.TriggerAction(0);
			else if (event_controller.IsLowerOrEqualCondition && new_value > target && old_value <= target) this.TriggerAction(1);
			else if (!event_controller.IsLowerOrEqualCondition && new_value >= target && old_value < target) this.TriggerAction(0);
			else if (!event_controller.IsLowerOrEqualCondition && new_value < target && old_value >= target) this.TriggerAction(1);
		}

		protected override void OnSave(MySerializedJumpGateEventInfo info)
		{
			base.OnSave(info);
			info.SetValue("IsControllerFiltered", this.IsControllerFiltered);
		}

		protected override void OnLoad(MySerializedJumpGateEventInfo info)
		{
			base.OnLoad(info);
			this.IsControllerFiltered = info.GetValueOrDefault("IsControllerFiltered", true);
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsComplete();
		}

		protected override int GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return jump_gate.GetJumpSpaceEntityCount(this.IsControllerFiltered);
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlOnOffSwitch is_controller_filtered = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>(this.MODID_PREFIX + "IsFiltered");
				is_controller_filtered.Title = MyStringId.GetOrCompute("Use Filtered Entities");
				is_controller_filtered.Tooltip = MyStringId.GetOrCompute("Whether to respect attached controller filter settings");
				is_controller_filtered.OnText = MyStringId.GetOrCompute("Yes");
				is_controller_filtered.OffText = MyStringId.GetOrCompute("No");
				is_controller_filtered.SupportsMultipleBlocks = false;
				is_controller_filtered.Visible = block => block.Components.Get<JumpGateEntityCountChangedEvent>()?.IsSelected ?? false;
				is_controller_filtered.Getter = block => block.Components.Get<JumpGateEntityCountChangedEvent>().IsControllerFiltered;
				is_controller_filtered.Setter = (block, value) => {
					JumpGateEntityCountChangedEvent event_block = block.Components.Get<JumpGateEntityCountChangedEvent>();
					event_block.IsControllerFiltered = value;
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(is_controller_filtered);
			}

			{
				IMyTerminalControlSlider entity_count_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "TargetCount");
				entity_count_sdr.Title = MyStringId.GetOrCompute("Entity Count");
				entity_count_sdr.Tooltip = MyStringId.GetOrCompute("The number of jump space entitites to check against");
				entity_count_sdr.SupportsMultipleBlocks = false;
				entity_count_sdr.Visible = block => block.Components.Get<JumpGateEntityCountChangedEvent>()?.IsSelected ?? false;
				entity_count_sdr.SetLimits(0f, uint.MaxValue);
				entity_count_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertSciNotUnits(block.Components.Get<JumpGateEntityCountChangedEvent>().TargetValue, 0));
				entity_count_sdr.Getter = (block) => block.Components.Get<JumpGateEntityCountChangedEvent>().TargetValue;
				entity_count_sdr.Setter = (block, value) => {
					JumpGateEntityCountChangedEvent event_block = block.Components.Get<JumpGateEntityCountChangedEvent>();
					event_block.TargetValue = (int) value;
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(entity_count_sdr);
			}

			base.CreateTerminalControls<T, JumpGateEntityCountChangedEvent>();
		}
	}
}
