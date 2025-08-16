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
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGatePhaseChanged))]
	[MyComponentType(typeof(JumpGatePhaseChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGatePhaseChangedEvent : MyJumpGateEventBase<byte>
	{
		private static List<MyTerminalControlComboBoxItem> ComboBoxItems = null;
		private static object ComboBoxLock = new object();

		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => false;
		public override bool IsJumpGateSelectionUsed => true;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFFF;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGatePhaseChangedEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGatePhaseChangedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGatePhaseChangedEvent_YesDescription").Replace("{%1}", MyTexts.GetString($"PhaseText_{(MyJumpGatePhase) this.TargetValue}"));
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGatePhaseChangedEvent_NoDescription").Replace("{%1}", MyTexts.GetString($"PhaseText_{(MyJumpGatePhase) this.TargetValue}"));

		public static void Dispose()
		{
			JumpGatePhaseChangedEvent.ComboBoxItems?.Clear();
			JumpGatePhaseChangedEvent.ComboBoxItems = null;
			JumpGatePhaseChangedEvent.ComboBoxLock = null;
		}

		public JumpGatePhaseChangedEvent() : base()
		{
			lock (JumpGatePhaseChangedEvent.ComboBoxLock)
			{
				if (JumpGatePhaseChangedEvent.ComboBoxItems != null) return;
				JumpGatePhaseChangedEvent.ComboBoxItems = new List<MyTerminalControlComboBoxItem>();

				foreach (MyJumpGatePhase phase in Enum.GetValues(typeof(MyJumpGatePhase)))
				{
					if (phase == MyJumpGatePhase.NONE || phase == MyJumpGatePhase.INVALID) continue;
					MyTerminalControlComboBoxItem item = new MyTerminalControlComboBoxItem
					{
						Key = (byte) phase,
						Value = MyStringId.GetOrCompute(MyTexts.GetString($"PhaseText_{phase}")),
					};
					JumpGatePhaseChangedEvent.ComboBoxItems.Add(item);
				}
			}
		}

		protected override void OnLoad(MySerializedJumpGateEventInfo info)
		{
			base.OnLoad(info);
			this.TargetValue = ((MyJumpGatePhase) this.TargetValue == MyJumpGatePhase.NONE || (MyJumpGatePhase) this.TargetValue == MyJumpGatePhase.INVALID) ? ((byte) MyJumpGatePhase.IDLE) : this.TargetValue;
		}

		protected override void CheckValueAgainstTarget(byte new_value, byte old_value, byte target)
		{
			if ((MyJumpGatePhase) this.TargetValue == MyJumpGatePhase.NONE || (MyJumpGatePhase) this.TargetValue == MyJumpGatePhase.INVALID) return;
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
				IMyTerminalControlCombobox phase_cb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>(this.MODID_PREFIX + "Phase");
				phase_cb.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGatePhaseChangedEvent_Phase")}:");
				phase_cb.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGatePhaseChangedEvent_Phase_Tooltip"));
				phase_cb.SupportsMultipleBlocks = true;
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
