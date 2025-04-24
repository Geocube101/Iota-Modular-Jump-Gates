using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
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
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF4;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Drive Count Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateDriveCountChangedEvent);

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
			if (this.IsWorkingOnly) jump_gate.GetWorkingJumpGateDrives(this.JumpGateDrives);
			else jump_gate.GetJumpGateDrives(this.JumpGateDrives);
			int count = this.JumpGateDrives.Count;
			this.JumpGateDrives.Clear();
			return count;
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlOnOffSwitch is_working_only = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>(this.MODID_PREFIX + "IsWorkingOnly");
				is_working_only.Title = MyStringId.GetOrCompute("Use Only Working Drives");
				is_working_only.Tooltip = MyStringId.GetOrCompute("Whether to use only working jump gate drives");
				is_working_only.OnText = MyStringId.GetOrCompute("Yes");
				is_working_only.OffText = MyStringId.GetOrCompute("No");
				is_working_only.SupportsMultipleBlocks = false;
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
				drive_count_sdr.Title = MyStringId.GetOrCompute("Drive Count");
				drive_count_sdr.Tooltip = MyStringId.GetOrCompute("The number of jump gate drives to check against");
				drive_count_sdr.SupportsMultipleBlocks = false;
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
