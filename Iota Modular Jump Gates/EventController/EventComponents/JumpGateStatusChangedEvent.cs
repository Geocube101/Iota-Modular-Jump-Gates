using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateStatusChanged))]
	[MyComponentType(typeof(JumpGateStatusChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateStatusChangedEvent : MyJumpGateEventBase<byte>
	{
		private static List<MyTerminalControlComboBoxItem> ComboBoxItems = null;

		private static readonly object ComboBoxLock = new object();

		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFFE;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGateStatusChangedEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGateStatusChangedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGateStatusChangedEvent_YesDescription").Replace("{%1}", MyTexts.GetString($"StatusText_{(MyJumpGateStatus) this.TargetValue}"));
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGateStatusChangedEvent_NoDescription").Replace("{%1}", MyTexts.GetString($"StatusText_{(MyJumpGateStatus) this.TargetValue}"));

		public JumpGateStatusChangedEvent() : base()
		{
			lock (JumpGateStatusChangedEvent.ComboBoxLock)
			{
				if (JumpGateStatusChangedEvent.ComboBoxItems != null) return;
				JumpGateStatusChangedEvent.ComboBoxItems = new List<MyTerminalControlComboBoxItem>();

				foreach (MyJumpGateStatus status in Enum.GetValues(typeof(MyJumpGateStatus)))
				{
					if (status == MyJumpGateStatus.NONE || status == MyJumpGateStatus.INVALID) continue;
					MyTerminalControlComboBoxItem item = new MyTerminalControlComboBoxItem
					{
						Key = (byte) status,
						Value = MyStringId.GetOrCompute(MyTexts.GetString($"StatusText_{status}")),
					};
					JumpGateStatusChangedEvent.ComboBoxItems.Add(item);
				}
			}
		}

		protected override void OnLoad(MySerializedJumpGateEventInfo info)
		{
			base.OnLoad(info);
			this.TargetValue = ((MyJumpGateStatus) this.TargetValue == MyJumpGateStatus.NONE || (MyJumpGateStatus) this.TargetValue == MyJumpGateStatus.INVALID) ? ((byte) MyJumpGateStatus.IDLE) : this.TargetValue;
		}

		protected override void CheckValueAgainstTarget(byte new_value, byte old_value, byte target)
		{
			if ((MyJumpGateStatus) this.TargetValue == MyJumpGateStatus.NONE || (MyJumpGateStatus) this.TargetValue == MyJumpGateStatus.INVALID) return;
			else if (new_value == target && old_value != target) this.TriggerAction(0);
			else if (new_value != target && old_value == target) this.TriggerAction(1);
		}

		protected override byte GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return (byte) jump_gate.Phase;
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlCombobox status_cb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>(this.MODID_PREFIX + "Phase");
				status_cb.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateStatusChangedEvent_Status")}:");
				status_cb.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateStatusChangedEvent_Status_Tooltip"));
				status_cb.SupportsMultipleBlocks = true;
				status_cb.ComboBoxContent = (content) => content.AddList(JumpGateStatusChangedEvent.ComboBoxItems);
				status_cb.Getter = (block) => block.Components.Get<JumpGateStatusChangedEvent>()?.TargetValue ?? 0xFF;
				status_cb.Setter = (block, value) => {
					JumpGateStatusChangedEvent event_block = block.Components.Get<JumpGateStatusChangedEvent>();
					event_block.TargetValue = (byte) value;
					this.Poll(true);
					event_block.SetDirty();
				};
				status_cb.Visible = block => block.Components.Get<JumpGateStatusChangedEvent>()?.IsSelected ?? false;
				MyAPIGateway.TerminalControls.AddControl<T>(status_cb);
			}

			base.CreateTerminalControls<T, JumpGateStatusChangedEvent>();
		}
	}
}
