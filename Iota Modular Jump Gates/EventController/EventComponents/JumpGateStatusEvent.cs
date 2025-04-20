using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateStatusChanged))]
	[MyComponentType(typeof(JumpGateStatusChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateStatusChangedEvent : MyJumpGateEventSync, IMyEventComponentWithGui
	{
		[ProtoContract]
		private sealed class EventInfo
		{
			[ProtoMember(1)]
			public MyJumpGateStatus JumpGateStatus;
			[ProtoMember(2)]
			public List<long> SelectedJumpGates;
		}

		private static List<MyTerminalControlComboBoxItem> JumpGateStatusValues = null;
		private static readonly List<MyJumpGate> TEMP_JumpGates = new List<MyJumpGate>();

		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".EventController.";

		private bool _IsSelected = false;
		private MyJumpGateStatus SelectedGateStatus = MyJumpGateStatus.INVALID;
		private IMyEventControllerBlock EventController => this.Entity as IMyEventControllerBlock;
		private EventInfo DeserializedInfo = null;
		private readonly Dictionary<MyJumpGate, MyJumpGateStatus> TargetedJumpGates = new Dictionary<MyJumpGate, MyJumpGateStatus>();
		
		public bool IsThresholdUsed => false;
		public bool IsConditionSelectionUsed => false;
		public bool IsBlocksListUsed => false;
		public long UniqueSelectionId => 0x7FFFFFFFFFFFFFFE;
		public MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Status Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateStatusChangedEvent);

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

		public JumpGateStatusChangedEvent() : base()
		{
			if (JumpGateStatusChangedEvent.JumpGateStatusValues != null) return;
			JumpGateStatusChangedEvent.JumpGateStatusValues = new List<MyTerminalControlComboBoxItem>();

			foreach (MyJumpGateStatus status in Enum.GetValues(typeof(MyJumpGateStatus)))
			{
				if (status == MyJumpGateStatus.NONE || status == MyJumpGateStatus.INVALID) continue;
				char[] name = Enum.GetName(typeof(MyJumpGateStatus), status).ToLower().ToCharArray();
				name[0] = Char.ToUpper(name[0]);
				JumpGateStatusChangedEvent.JumpGateStatusValues.Add(new MyTerminalControlComboBoxItem() { Key = (long) status, Value = MyStringId.GetOrCompute(String.Join("", name)) });
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
				this.SelectedGateStatus = this.DeserializedInfo.JumpGateStatus;

				if (this.DeserializedInfo.SelectedJumpGates != null)
				{
					foreach (long gateid in this.DeserializedInfo.SelectedJumpGates)
					{
						MyJumpGate gate = construct.GetJumpGate(gateid);
						if (gate == null || gate.Closed) continue;
						this.TargetedJumpGates[gate] = gate.Status;
					}
				}

				this.DeserializedInfo = null;
			}

			Dictionary<MyJumpGate, MyJumpGateStatus?> status_updates = new Dictionary<MyJumpGate, MyJumpGateStatus?>(this.TargetedJumpGates.Count);

			foreach (KeyValuePair<MyJumpGate, MyJumpGateStatus> pair in this.TargetedJumpGates)
			{
				MyJumpGate jump_gate = pair.Key;

				if (jump_gate == null || jump_gate.Closed)
				{
					status_updates[jump_gate] = null;
					continue;
				}

				if (jump_gate.Status == pair.Value) continue;
				status_updates[pair.Key] = jump_gate.Status;
				if (jump_gate.Status == this.SelectedGateStatus) event_controller.TriggerAction(0);
				else if (pair.Value == this.SelectedGateStatus) event_controller.TriggerAction(1);
			}

			foreach (KeyValuePair<MyJumpGate, MyJumpGateStatus?> pair in status_updates)
			{
				if (pair.Value == null) this.TargetedJumpGates.Remove(pair.Key);
				else this.TargetedJumpGates[pair.Key] = pair.Value.Value;
			}
		}

		public void AddBlocks(List<IMyTerminalBlock> blocks)
		{
			
		}

		public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
		{
			IMyTerminalControlCombobox choose_status_cb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>(MODID_PREFIX + "StatusChangedEvent_Status");
			choose_status_cb.Title = MyStringId.GetOrCompute("Jump Gate Status");
			choose_status_cb.SupportsMultipleBlocks = false;
			choose_status_cb.Visible = block => block.Components.Get<JumpGateStatusChangedEvent>().IsSelected;
			choose_status_cb.ComboBoxContent = list => list.AddRange(JumpGateStatusChangedEvent.JumpGateStatusValues);
			choose_status_cb.Getter = block => (long) block.Components.Get<JumpGateStatusChangedEvent>().SelectedGateStatus;
			choose_status_cb.Setter = (block, value) => block.Components.Get<JumpGateStatusChangedEvent>().SelectedGateStatus = (MyJumpGateStatus) value;
			MyAPIGateway.TerminalControls.AddControl<T>(choose_status_cb);

			IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(MODID_PREFIX + "StatusChangedEvent_JumpGate");
			choose_jump_gate_lb.Title = MyStringId.GetOrCompute("Jump Gates");
			choose_jump_gate_lb.SupportsMultipleBlocks = false;
			choose_jump_gate_lb.Visible = block => block.Components.Get<JumpGateStatusChangedEvent>().IsSelected;
			choose_jump_gate_lb.Multiselect = true;
			choose_jump_gate_lb.VisibleRowsCount = 5;
			choose_jump_gate_lb.ListContent = (block, content_list, preselect_list) => {
				JumpGateStatusChangedEvent event_block = block.Components.Get<JumpGateStatusChangedEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				construct.GetJumpGates(JumpGateStatusChangedEvent.TEMP_JumpGates);

				foreach (MyJumpGate jump_gate in JumpGateStatusChangedEvent.TEMP_JumpGates.OrderBy((gate) => gate.JumpGateID))
				{
					if (!jump_gate.Closed && jump_gate.IsValid() && jump_gate.Controller != null)
					{
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute($"Jump Gate with {jump_gate.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == jump_gate.JumpGateID)} drives\nJump Gate ID: {jump_gate.JumpGateID}"), jump_gate.JumpGateID);
						content_list.Add(item);
						if (event_block.TargetedJumpGates.ContainsKey(jump_gate)) preselect_list.Add(item);
					}
				}

				JumpGateStatusChangedEvent.TEMP_JumpGates.Clear();
			};

			choose_jump_gate_lb.ItemSelected = (block, selected) => {
				JumpGateStatusChangedEvent event_block = block.Components.Get<JumpGateStatusChangedEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				event_block.TargetedJumpGates.Clear();

				foreach (MyTerminalControlListBoxItem item in selected)
				{
					long selected_id = (long) item.UserData;
					MyJumpGate jump_gate = construct.GetJumpGate(selected_id);
					if (jump_gate == null || jump_gate.Closed) continue;
					event_block.TargetedJumpGates[jump_gate] = jump_gate.Status;
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
			EventInfo info = new EventInfo
			{
				JumpGateStatus = this.SelectedGateStatus,
				SelectedJumpGates = this.TargetedJumpGates.Select((pair) => pair.Key.JumpGateID).ToList(),
			};

			return new MyObjectBuilder_ModCustomComponent
			{
				ComponentType = nameof(JumpGateStatusChangedEvent),
				CustomModData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(info)),
				RemoveExistingComponentOnNewInsert = true,
				SubtypeName = nameof(JumpGateStatusChangedEvent)
			};
		}
	}
}
