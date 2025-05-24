using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using VRage;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateDriveCountChanged))]
	[MyComponentType(typeof(JumpGateDriveCountChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateDriveCountChangedEvent : MyJumpGateEventBase<int>
	{
		private bool IsWorkingOnly = false;
		private readonly List<MyJumpGateDrive> JumpGateDrives = new List<MyJumpGateDrive>();
		
		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => false;
		public override bool IsJumpGateSelectionUsed => true;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF4;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGateDriveCountChangedEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGateDriveCountChangedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGateDriveCountChangedEvent_YesDescription").Replace("{%0}", ((this.EventController.IsLowerOrEqualCondition) ? "<=" : ">=").Replace("{%1}", MyJumpGateModSession.AutoconvertSciNotUnits(this.TargetValue, 0).ToString()));
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGateDriveCountChangedEvent_NoDescription").Replace("{%0}", ((this.EventController.IsLowerOrEqualCondition) ? ">=" : "<=").Replace("{%1}", MyJumpGateModSession.AutoconvertSciNotUnits(this.TargetValue, 0).ToString()));

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
			info.SetValue("IsWorkingOnly", this.IsWorkingOnly);
		}

		protected override void OnLoad(MySerializedJumpGateEventInfo info)
		{
			base.OnLoad(info);
			this.IsWorkingOnly = info.GetValueOrDefault("IsWorkingOnly", true);
		}

		protected override int GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			if (this.IsWorkingOnly) this.JumpGateDrives.AddRange(jump_gate.GetWorkingJumpGateDrives());
			else this.JumpGateDrives.AddRange(jump_gate.GetJumpGateDrives());
			int count = this.JumpGateDrives.Count;
			this.JumpGateDrives.Clear();
			return count;
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlOnOffSwitch is_working_only = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>(this.MODID_PREFIX + "IsWorkingOnly");
				is_working_only.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateDriveCountChangedEvent_UseWorkingOnly"));
				is_working_only.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateDriveCountChangedEvent_UseWorkingOnly_Tooltip"));
				is_working_only.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Yes"));
				is_working_only.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_No"));
				is_working_only.SupportsMultipleBlocks = true;
				is_working_only.Visible = block => block.Components.Get<JumpGateDriveCountChangedEvent>()?.IsSelected ?? false;
				is_working_only.Getter = block => block.Components.Get<JumpGateDriveCountChangedEvent>().IsWorkingOnly;
				is_working_only.Setter = (block, value) => {
					JumpGateDriveCountChangedEvent event_block = block.Components.Get<JumpGateDriveCountChangedEvent>();
					event_block.IsWorkingOnly = value;
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(is_working_only);
			}

			{
				IMyTerminalControlSlider drive_count_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "TargetCount");
				drive_count_sdr.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("DetailedInfo_JumpGateController_DriveCount")}:");
				drive_count_sdr.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateControllerChangedEvent_DriveCount_Tooltip"));
				drive_count_sdr.SupportsMultipleBlocks = true;
				drive_count_sdr.Visible = block => block.Components.Get<JumpGateDriveCountChangedEvent>()?.IsSelected ?? false;
				drive_count_sdr.SetLimits(0f, 1e24f);
				drive_count_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertSciNotUnits(block.Components.Get<JumpGateDriveCountChangedEvent>().TargetValue, 0));
				drive_count_sdr.Getter = (block) => block.Components.Get<JumpGateDriveCountChangedEvent>().TargetValue;
				drive_count_sdr.Setter = (block, value) => {
					JumpGateDriveCountChangedEvent event_block = block.Components.Get<JumpGateDriveCountChangedEvent>();
					event_block.TargetValue = (int) value;
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(drive_count_sdr);
			}

			base.CreateTerminalControls<T, JumpGateDriveCountChangedEvent>();
		}
	}
}
