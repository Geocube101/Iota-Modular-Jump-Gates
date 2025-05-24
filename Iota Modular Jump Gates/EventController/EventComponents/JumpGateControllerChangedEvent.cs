using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using VRage;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateControllerChanged))]
	[MyComponentType(typeof(JumpGateControllerChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateControllerChangedEvent : MyJumpGateEventBase<long>
	{
		private enum MyControllerConnectionType : byte { ALL, DIRECT, REMOTE };

		private MyControllerConnectionType ConnectionType = MyControllerConnectionType.ALL;

		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => false;
		public override bool IsJumpGateSelectionUsed => true;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF3;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGateControllerChangedEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGateControllerChangedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGateControllerChangedEvent_YesDescription");
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGateControllerChangedEvent_NoDescription");

		protected override void CheckValueAgainstTarget(long new_value, long old_value, long target)
		{
			MyJumpGateConstruct construct = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(this.EventController.CubeGrid);
			if (construct == null) return;
			MyJumpGateController controller = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateController>(new_value) ?? MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateController>(old_value);
			bool remote = controller != null && controller.JumpGateGrid != construct;
			bool allowed = this.ConnectionType == MyControllerConnectionType.ALL || (this.ConnectionType == MyControllerConnectionType.DIRECT && !remote) || (this.ConnectionType == MyControllerConnectionType.REMOTE && remote);
			if (new_value != -1 && old_value == -1 && allowed) this.TriggerAction(0);
			else if (new_value == -1 && old_value != -1 && allowed) this.TriggerAction(1);
		}

		protected override void OnSave(MySerializedJumpGateEventInfo info)
		{
			base.OnSave(info);
			info.SetValue("ConnectionType", this.ConnectionType);
		}

		protected override void OnLoad(MySerializedJumpGateEventInfo info)
		{
			base.OnLoad(info);
			this.ConnectionType = info.GetValueOrDefault("ConnectionType", MyControllerConnectionType.ALL);
		}

		protected override long GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return jump_gate.Controller?.BlockID ?? -1;
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			{
				IMyTerminalControlSlider connection_type_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(this.MODID_PREFIX + "ConnectionType");
				connection_type_sdr.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateControllerChangedEvent_ConnectionType")}:");
				connection_type_sdr.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateControllerChangedEvent_ConnectionType_Tooltip"));
				connection_type_sdr.SupportsMultipleBlocks = true;
				connection_type_sdr.Visible = block => block.Components.Get<JumpGateControllerChangedEvent>()?.IsSelected ?? false;
				connection_type_sdr.SetLimits(0, 2);
				connection_type_sdr.Writer = (block, string_builder) => string_builder.Append(MyTexts.GetString($"ConnectionTypeText_{block.Components.Get<JumpGateControllerChangedEvent>().ConnectionType}"));
				connection_type_sdr.Getter = (block) => (byte) block.Components.Get<JumpGateControllerChangedEvent>().ConnectionType;
				connection_type_sdr.Setter = (block, value) => {
					JumpGateControllerChangedEvent event_block = block.Components.Get<JumpGateControllerChangedEvent>();
					event_block.ConnectionType = (MyControllerConnectionType) ((byte) Math.Round(MathHelper.Clamp(value, 0, 2)));
					event_block.SetDirty();
					connection_type_sdr.UpdateVisual();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(connection_type_sdr);
			}

			base.CreateTerminalControls<T, JumpGateControllerChangedEvent>();
		}
	}
}
