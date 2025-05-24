using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventCapacitorChargePercentChanged))]
	[MyComponentType(typeof(CapacitorChargePercentChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class CapacitorChargePercentChangedEvent : MyJumpGateEventBase<bool>
	{
		private readonly ConcurrentDictionary<MyJumpGateCapacitor, float> ListeningCapacitors = new ConcurrentDictionary<MyJumpGateCapacitor, float>();

		public override bool IsThresholdUsed => true;
		public override bool IsConditionSelectionUsed => true;
		public override bool IsBlocksListUsed => true;
		public override bool IsJumpGateSelectionUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFF1;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_CapacitorChargePercentChangedEvent"));
		public override string ComponentTypeDebugString => nameof(CapacitorChargePercentChangedEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_CapacitorChargePercentChangedEvent_YesDescription");
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_CapacitorChargePercentChangedEvent_NoDescription");

		protected override void Update()
		{
			base.Update();
			IMyEventControllerBlock event_controller = this.EventController;

			if (event_controller != null)
			{
				foreach (KeyValuePair<MyJumpGateCapacitor, float> pair in this.ListeningCapacitors)
				{
					MyJumpGateCapacitor capacitor = pair.Key;
					float new_value = (float) (capacitor.StoredChargeMW / capacitor.CapacitorConfiguration.MaxCapacitorChargeMW);
					float old_value = pair.Value;
					float target = event_controller.Threshold;
					this.ListeningCapacitors[capacitor] = new_value;
					if (event_controller.IsLowerOrEqualCondition && new_value <= target && old_value > target) this.TriggerAction(0);
					else if (event_controller.IsLowerOrEqualCondition && new_value > target && old_value <= target) this.TriggerAction(1);
					else if (!event_controller.IsLowerOrEqualCondition && new_value >= target && old_value < target) this.TriggerAction(0);
					else if (!event_controller.IsLowerOrEqualCondition && new_value < target && old_value >= target) this.TriggerAction(1);
				}
			}

			this.RemoveBlocks(this.ListeningCapacitors.Select((pair) => pair.Key).Where((capacitor) => capacitor.Closed).Select((capacitor) => capacitor.TerminalBlock));
		}

		protected override void CheckValueAgainstTarget(bool new_value, bool old_value, bool target) { }

		protected override void AppendCustomInfo(StringBuilder sb)
		{
			base.AppendCustomInfo(sb);
			sb.Append($"{MyTexts.GetString("DetailedInfo_CapacitorChargePercentChangedEvent_ListeningCapacitors")}:\n");

			foreach (KeyValuePair<MyJumpGateCapacitor, float> pair in this.ListeningCapacitors)
			{
				string name = pair.Key.TerminalBlock?.CustomName ?? pair.Key.BlockID.ToString();
				sb.Append($" - {MyTexts.GetString("DetailedInfo_CapacitorChargePercentChangedEvent_CapacitorCharge").Replace("{%0}", name).Replace("{%1}", $"{Math.Round(pair.Value * 100, 2)}%")}\n");
			}
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			base.CreateTerminalControls<T, JumpGateEntityEnteredEvent>();
		}

		public override bool IsBlockValidForList(IMyTerminalBlock block)
		{
			MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
			return capacitor != null && !capacitor.Closed;
		}

		public override void AddBlocks(List<IMyTerminalBlock> blocks)
		{
			base.AddBlocks(blocks);

			foreach (IMyTerminalBlock block in blocks)
			{
				MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
				if (capacitor == null || capacitor.Closed || this.ListeningCapacitors.ContainsKey(capacitor)) continue;
				this.ListeningCapacitors[capacitor] = (float) (capacitor.StoredChargeMW / capacitor.CapacitorConfiguration.MaxCapacitorChargeMW);
			}
		}

		public override void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
		{
			base.RemoveBlocks(blocks);

			foreach (IMyTerminalBlock block in blocks)
			{
				MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
				if (capacitor == null || !this.ListeningCapacitors.ContainsKey(capacitor)) continue;
				this.ListeningCapacitors.Remove(capacitor);
			}
		}
	}
}
