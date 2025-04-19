using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateEntityCountChanged))]
	[MyComponentType(typeof(JumpGateEntityCountChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateEntityCountChangedEvent : MyJumpGateEventSync, IMyEventComponentWithGui
	{
		[ProtoContract]
		private sealed class EventInfo
		{
			[ProtoMember(1)]
			public bool UseControllerFilter;
			[ProtoMember(2)]
			public uint TargetEntityCount;
			[ProtoMember(3)]
			public List<long> SelectedJumpGates;
		}

		private static readonly List<MyJumpGate> TEMP_JumpGates = new List<MyJumpGate>();

		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".EventController.";

		private bool _IsSelected = false;
		private bool IsControllerFiltered = true;
		private uint TargetEntityCount = 0;
		private IMyEventControllerBlock EventController => this.Entity as IMyEventControllerBlock;
		private EventInfo DeserializedInfo = null;
		private readonly Dictionary<MyJumpGate, int> TargetedJumpGates = new Dictionary<MyJumpGate, int>();
		
		public bool IsThresholdUsed => false;
		public bool IsConditionSelectionUsed => true;
		public bool IsBlocksListUsed => false;
		public long UniqueSelectionId => 0x7FFFFFFFFFFFFFFD;
		public MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Entity Count Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateEntityCountChangedEvent);

		public bool IsSelected
		{
			get
			{
				return this._IsSelected;
			}
			set
			{
				IMyEventControllerBlock event_controller = this.EventController;
				if (this._IsSelected == value || event_controller == null || event_controller.MarkedForClose) return;
				this._IsSelected = value;
				if (value) ((MyCubeGrid) event_controller.CubeGrid).Schedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);
				else ((MyCubeGrid) event_controller.CubeGrid).DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);
			}
		}

		protected override void Update()
		{
			base.Update();
			IMyEventControllerBlock event_controller = this.EventController;
			if (event_controller == null || event_controller.MarkedForClose) return;

			if (this.DeserializedInfo != null)
			{
				MyJumpGateConstruct construct = MyJumpGateModSession.Instance.GetJumpGateGrid(this.EventController.CubeGrid);
				if (construct == null || !construct.FullyInitialized) return;
				this.IsControllerFiltered = this.DeserializedInfo.UseControllerFilter;
				this.TargetEntityCount = this.DeserializedInfo.TargetEntityCount;

				if (this.DeserializedInfo.SelectedJumpGates != null)
				{
					foreach (long gateid in this.DeserializedInfo.SelectedJumpGates)
					{
						MyJumpGate gate = construct.GetJumpGate(gateid);
						if (gate == null || gate.Closed) continue;
						this.TargetedJumpGates[gate] = gate.GetJumpSpaceEntityCount(this.IsControllerFiltered);
					}
				}

				this.DeserializedInfo = null;
			}

			Dictionary<MyJumpGate, int> count_updates = new Dictionary<MyJumpGate, int>(this.TargetedJumpGates.Count);

			foreach (KeyValuePair<MyJumpGate, int> pair in this.TargetedJumpGates)
			{
				MyJumpGate jump_gate = pair.Key;
				int count = jump_gate?.GetJumpSpaceEntityCount() ?? 0;
				if (jump_gate == null || jump_gate.Closed || count == pair.Value) continue;
				count_updates[pair.Key] = count;
				if (event_controller.IsLowerOrEqualCondition && count <= this.TargetEntityCount && pair.Value > this.TargetEntityCount) event_controller.TriggerAction(0);
				else if (event_controller.IsLowerOrEqualCondition && count > this.TargetEntityCount && pair.Value <=  this.TargetEntityCount) event_controller.TriggerAction(1);
				else if (!event_controller.IsLowerOrEqualCondition && count >= this.TargetEntityCount && pair.Value < this.TargetEntityCount) event_controller.TriggerAction(0);
				else if (!event_controller.IsLowerOrEqualCondition && count < this.TargetEntityCount && pair.Value >= this.TargetEntityCount) event_controller.TriggerAction(1);
			}

			foreach (KeyValuePair<MyJumpGate, int> pair in count_updates) this.TargetedJumpGates[pair.Key] = pair.Value;
		}

		public void AddBlocks(List<IMyTerminalBlock> blocks)
		{
			
		}

		public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
		{
			IMyTerminalControlOnOffSwitch is_controller_filtered = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>(MODID_PREFIX + "EntityCountChangedEvent_IsFiltered");
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
				foreach (MyJumpGate gate in event_block.TargetedJumpGates.Keys.ToList()) event_block.TargetedJumpGates[gate] = gate.GetJumpSpaceEntityCount(event_block.IsControllerFiltered);
			};
			MyAPIGateway.TerminalControls.AddControl<T>(is_controller_filtered);

			IMyTerminalControlSlider entity_count_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(MODID_PREFIX + "EntityCountChangedEvent_Count");
			entity_count_sdr.Title = MyStringId.GetOrCompute("Entity Count");
			entity_count_sdr.Tooltip = MyStringId.GetOrCompute("The number of jump space entitites to check against");
			entity_count_sdr.SupportsMultipleBlocks = false;
			entity_count_sdr.Visible = block => block.Components.Get<JumpGateEntityCountChangedEvent>()?.IsSelected ?? false;
			entity_count_sdr.SetLimits(0f, uint.MaxValue);
			entity_count_sdr.Writer = (block, string_builder) => string_builder.Append(block.Components.Get<JumpGateEntityCountChangedEvent>().TargetEntityCount);
			entity_count_sdr.Getter = (block) => block.Components.Get<JumpGateEntityCountChangedEvent>().TargetEntityCount;
			entity_count_sdr.Setter = (block, value) => block.Components.Get<JumpGateEntityCountChangedEvent>().TargetEntityCount = (uint) value;
			MyAPIGateway.TerminalControls.AddControl<T>(entity_count_sdr);

			IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(MODID_PREFIX + "EntityCountChangedEvent_JumpGate");
			choose_jump_gate_lb.Title = MyStringId.GetOrCompute("Jump Gates");
			choose_jump_gate_lb.SupportsMultipleBlocks = false;
			choose_jump_gate_lb.Visible = block => block.Components.Get<JumpGateEntityCountChangedEvent>()?.IsSelected ?? false;
			choose_jump_gate_lb.Multiselect = true;
			choose_jump_gate_lb.VisibleRowsCount = 5;
			choose_jump_gate_lb.ListContent = (block, content_list, preselect_list) => {
				JumpGateEntityCountChangedEvent event_block = block.Components.Get<JumpGateEntityCountChangedEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				construct.GetJumpGates(JumpGateEntityCountChangedEvent.TEMP_JumpGates);

				foreach (MyJumpGate jump_gate in JumpGateEntityCountChangedEvent.TEMP_JumpGates.OrderBy((gate) => gate.JumpGateID))
				{
					if (!jump_gate.Closed && jump_gate.IsValid() && jump_gate.Controller != null)
					{
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute($"Jump Gate with {jump_gate.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == jump_gate.JumpGateID)} drives\nJump Gate ID: {jump_gate.JumpGateID}"), jump_gate.JumpGateID);
						content_list.Add(item);
						if (event_block.TargetedJumpGates.ContainsKey(jump_gate)) preselect_list.Add(item);
					}
				}

				JumpGateEntityCountChangedEvent.TEMP_JumpGates.Clear();
			};

			choose_jump_gate_lb.ItemSelected = (block, selected) => {
				JumpGateEntityCountChangedEvent event_block = block.Components.Get<JumpGateEntityCountChangedEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				event_block.TargetedJumpGates.Clear();

				foreach (MyTerminalControlListBoxItem item in selected)
				{
					long selected_id = (long) item.UserData;
					MyJumpGate jump_gate = construct.GetJumpGate(selected_id);
					if (jump_gate == null || jump_gate.Closed) continue;
					event_block.TargetedJumpGates[jump_gate] = jump_gate.GetJumpSpaceEntityCount(this.IsControllerFiltered);
				}
			};

			MyAPIGateway.TerminalControls.AddControl<T>(choose_jump_gate_lb);
		}

		public override void OnBeforeRemovedFromContainer()
		{
			base.OnBeforeRemovedFromContainer();
			IMyEventControllerBlock event_controller = this.EventController;
			if (event_controller == null || event_controller.MarkedForClose) return;
			if (this._IsSelected) ((MyCubeGrid) event_controller.CubeGrid).DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);
		}

		public bool IsBlockValidForList(IMyTerminalBlock block)
		{
			return true;
		}

		public void NotifyValuesChanged()
		{
				
		}

		public void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
		{

		}

		public override bool IsSerialized()
		{
			return true;
		}

		public override void Deserialize(MyObjectBuilder_ComponentBase builder)
		{
			base.Deserialize(builder);
			MyObjectBuilder_ModCustomComponent serialized_event = (MyObjectBuilder_ModCustomComponent) builder;
			this.DeserializedInfo = MyAPIGateway.Utilities.SerializeFromBinary<EventInfo>(Convert.FromBase64String(serialized_event.CustomModData));
		}

		public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
		{
			EventInfo info = new EventInfo {
				UseControllerFilter = this.IsControllerFiltered,
				TargetEntityCount = this.TargetEntityCount,
				SelectedJumpGates = this.TargetedJumpGates.Select((pair) => pair.Key.JumpGateID).ToList(),
			};

			return new MyObjectBuilder_ModCustomComponent
			{
				ComponentType = nameof(JumpGateEntityCountChangedEvent),
				CustomModData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(info)),
				RemoveExistingComponentOnNewInsert = true,
				SubtypeName = nameof(JumpGateEntityCountChangedEvent)
			};
		}
	}
}
