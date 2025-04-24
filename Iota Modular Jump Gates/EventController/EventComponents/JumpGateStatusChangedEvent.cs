using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
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
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Status Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateStatusChangedEvent);

		public JumpGateStatusChangedEvent() : base()
		{
			lock (JumpGateStatusChangedEvent.ComboBoxLock)
			{
				if (JumpGateStatusChangedEvent.ComboBoxItems != null) return;
				JumpGateStatusChangedEvent.ComboBoxItems = new List<MyTerminalControlComboBoxItem>();

				foreach (MyJumpGateStatus status in Enum.GetValues(typeof(MyJumpGateStatus)))
				{
					if (status == MyJumpGateStatus.NONE || status == MyJumpGateStatus.INVALID) continue;
					char[] name = Enum.GetName(typeof(MyJumpGateStatus), status).ToLower().ToCharArray();
					name[0] = Char.ToUpper(name[0]);
					MyTerminalControlComboBoxItem item = new MyTerminalControlComboBoxItem
					{
						Key = (byte) status,
						Value = MyStringId.GetOrCompute(string.Join("", name)),
					};
					JumpGateStatusChangedEvent.ComboBoxItems.Add(item);
				}
			}
		}

		protected override void CheckValueAgainstTarget(byte new_value, byte old_value, byte target)
		{
			if (new_value == target && old_value != target) this.TriggerAction(0);
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
				status_cb.Title = MyStringId.GetOrCompute("Jump Gate Status");
				status_cb.Tooltip = MyStringId.GetOrCompute("The status of the jump gate as seen in the controller");
				status_cb.SupportsMultipleBlocks = false;
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
