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
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGatePhaseChanged))]
	[MyComponentType(typeof(JumpGatePhaseChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGatePhaseChangedEvent : MyJumpGateEventBase<byte>
	{
		private static List<MyTerminalControlComboBoxItem> ComboBoxItems = null;
		private static readonly object ComboBoxLock = new object();

		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFFF;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Phase Changed");
		public override string ComponentTypeDebugString => nameof(JumpGatePhaseChangedEvent);
		public override string YesNoToolbarYesDescription => $"Jump Gate Phase == {(MyJumpGatePhase) this.TargetValue}";
		public override string YesNoToolbarNoDescription => $"Jump Gate Phase != {(MyJumpGatePhase) this.TargetValue}";

		public JumpGatePhaseChangedEvent() : base()
		{
			lock (JumpGatePhaseChangedEvent.ComboBoxLock)
			{
				if (JumpGatePhaseChangedEvent.ComboBoxItems != null) return;
				JumpGatePhaseChangedEvent.ComboBoxItems = new List<MyTerminalControlComboBoxItem>();

				foreach (MyJumpGatePhase phase in Enum.GetValues(typeof(MyJumpGatePhase)))
				{
					if (phase == MyJumpGatePhase.NONE || phase == MyJumpGatePhase.INVALID) continue;
					char[] name = Enum.GetName(typeof(MyJumpGatePhase), phase).ToLower().ToCharArray();
					name[0] = Char.ToUpper(name[0]);
					MyTerminalControlComboBoxItem item = new MyTerminalControlComboBoxItem
					{
						Key = (byte) phase,
						Value = MyStringId.GetOrCompute(string.Join("", name)),
					};
					JumpGatePhaseChangedEvent.ComboBoxItems.Add(item);
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
				IMyTerminalControlCombobox phase_cb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>(this.MODID_PREFIX + "Phase");
				phase_cb.Title = MyStringId.GetOrCompute("Jump Gate Phase");
				phase_cb.Tooltip = MyStringId.GetOrCompute("The phase of the jump gate as seen in the controller");
				phase_cb.SupportsMultipleBlocks = false;
				phase_cb.ComboBoxContent = (content) => content.AddList(JumpGatePhaseChangedEvent.ComboBoxItems);
				phase_cb.Getter = (block) => block.Components.Get<JumpGatePhaseChangedEvent>()?.TargetValue ?? 0xFF;
				phase_cb.Setter = (block, value) => {
					JumpGatePhaseChangedEvent event_block = block.Components.Get<JumpGatePhaseChangedEvent>();
					event_block.TargetValue = (byte) value;
					this.Poll(true);
					event_block.SetDirty();
				};
				phase_cb.Visible = block => block.Components.Get<JumpGatePhaseChangedEvent>()?.IsSelected ?? false;
				MyAPIGateway.TerminalControls.AddControl<T>(phase_cb);
			}

			base.CreateTerminalControls<T, JumpGatePhaseChangedEvent>();
		}
	}
}
