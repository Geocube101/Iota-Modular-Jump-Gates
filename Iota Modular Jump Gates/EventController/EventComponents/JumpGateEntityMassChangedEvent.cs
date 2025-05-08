using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Linq;
using VRage;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateEntityMassChanged))]
	[MyComponentType(typeof(JumpGateEntityMassChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateEntityMassChangedEvent : MyJumpGateEventBase<double>
	{
		private bool IsControllerFiltered = true;
		
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFFB;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGateEntityMassChangedEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGateEntityMassChangedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGateEntityMassChangedEvent_YesDescription").Replace("{%0}", ((this.EventController.IsLowerOrEqualCondition) ? "<=" : ">=").Replace("{%1}", MyJumpGateModSession.AutoconvertMetricUnits(this.TargetValue * 1e3, "g", 2).ToString()));
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGateEntityMassChangedEvent_NoDescription").Replace("{%0}", ((this.EventController.IsLowerOrEqualCondition) ? ">=" : "<=").Replace("{%1}", MyJumpGateModSession.AutoconvertMetricUnits(this.TargetValue * 1e3, "g", 2).ToString()));

		protected override void CheckValueAgainstTarget(double new_value, double old_value, double target)
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

		protected override double GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return jump_gate.GetEntitiesInJumpSpace().Sum((pair) => (double) pair.Value);
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsValid();
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlOnOffSwitch is_controller_filtered = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>(this.MODID_PREFIX + "IsFiltered");
				is_controller_filtered.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateEntityCountChangedEvent_UseFilteredOnly"));
				is_controller_filtered.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateEntityCountChangedEvent_UseFilteredOnly_Tooltip"));
				is_controller_filtered.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Yes"));
				is_controller_filtered.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_No"));
				is_controller_filtered.SupportsMultipleBlocks = true;
				is_controller_filtered.Visible = block => block.Components.Get<JumpGateEntityMassChangedEvent>()?.IsSelected ?? false;
				is_controller_filtered.Getter = block => block.Components.Get<JumpGateEntityMassChangedEvent>().IsControllerFiltered;
				is_controller_filtered.Setter = (block, value) => {
					JumpGateEntityMassChangedEvent event_block = block.Components.Get<JumpGateEntityMassChangedEvent>();
					event_block.IsControllerFiltered = value;
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(is_controller_filtered);
			}

			{
				IMyTerminalControlSlider entity_mass_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "TargetMass");
				entity_mass_sdr.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateEntityMassChangedEvent_EntityMass")} (Kg):");
				entity_mass_sdr.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateEntityMassChangedEvent_EntityMass_Tooltip"));
				entity_mass_sdr.SupportsMultipleBlocks = true;
				entity_mass_sdr.Visible = block => block.Components.Get<JumpGateEntityMassChangedEvent>()?.IsSelected ?? false;
				entity_mass_sdr.SetLimits(0f, 1e24f);
				entity_mass_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertMetricUnits(block.Components.Get<JumpGateEntityMassChangedEvent>().TargetValue * 1e3, "g", 4));
				entity_mass_sdr.Getter = (block) => (float) (block.Components.Get<JumpGateEntityMassChangedEvent>().TargetValue);
				entity_mass_sdr.Setter = (block, value) => {
					JumpGateEntityMassChangedEvent event_block = block.Components.Get<JumpGateEntityMassChangedEvent>();
					event_block.TargetValue = value;
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(entity_mass_sdr);
			}

			base.CreateTerminalControls<T, JumpGateEntityMassChangedEvent>();
		}
	}
}
