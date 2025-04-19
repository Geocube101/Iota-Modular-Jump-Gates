using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
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
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGatePhaseChanged))]
	[MyComponentType(typeof(JumpGatePhaseChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGatePhaseChangedEvent : MyJumpGateEventSync, IMyEventComponentWithGui
	{
		[ProtoContract]
		private sealed class EventInfo
		{
			[ProtoMember(1)]
			public MyJumpGatePhase JumpGatePhase;
			[ProtoMember(2)]
			public List<long> SelectedJumpGates;
		}

		private static List<MyTerminalControlComboBoxItem> JumpGatePhaseValues = null;
		private static readonly List<MyJumpGate> TEMP_JumpGates = new List<MyJumpGate>();

		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".EventController.";

		private bool _IsSelected = false;
		private MyJumpGatePhase SelectedGatePhase = MyJumpGatePhase.INVALID;
		private IMyEventControllerBlock EventController => this.Entity as IMyEventControllerBlock;
		private EventInfo DeserializedInfo = null;
		private readonly Dictionary<MyJumpGate, MyJumpGatePhase> TargetedJumpGates = new Dictionary<MyJumpGate, MyJumpGatePhase>();
		
		public bool IsThresholdUsed => false;
		public bool IsConditionSelectionUsed => false;
		public bool IsBlocksListUsed => false;
		public long UniqueSelectionId => 0x7FFFFFFFFFFFFFFF;
		public MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Phase Changed");
		public override string ComponentTypeDebugString => nameof(JumpGatePhaseChangedEvent);

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

		public JumpGatePhaseChangedEvent() : base()
		{
			if (JumpGatePhaseChangedEvent.JumpGatePhaseValues != null) return;
			JumpGatePhaseChangedEvent.JumpGatePhaseValues = new List<MyTerminalControlComboBoxItem>();

			foreach (MyJumpGatePhase phase in Enum.GetValues(typeof(MyJumpGatePhase)))
			{
				if (phase == MyJumpGatePhase.NONE || phase == MyJumpGatePhase.INVALID) continue;
				char[] name = Enum.GetName(typeof(MyJumpGatePhase), phase).ToLower().ToCharArray();
				name[0] = Char.ToUpper(name[0]);
				JumpGatePhaseChangedEvent.JumpGatePhaseValues.Add(new MyTerminalControlComboBoxItem() { Key = (long) phase, Value = MyStringId.GetOrCompute(String.Join("", name)) });
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
				this.SelectedGatePhase = this.DeserializedInfo.JumpGatePhase;

				if (this.DeserializedInfo.SelectedJumpGates != null)
				{
					foreach (long gateid in this.DeserializedInfo.SelectedJumpGates)
					{
						MyJumpGate gate = construct.GetJumpGate(gateid);
						if (gate == null || gate.Closed) continue;
						this.TargetedJumpGates[gate] = gate.Phase;
					}
				}

				this.DeserializedInfo = null;
			}

			Dictionary<MyJumpGate, MyJumpGatePhase> phase_updates = new Dictionary<MyJumpGate, MyJumpGatePhase>(this.TargetedJumpGates.Count);

			foreach (KeyValuePair<MyJumpGate, MyJumpGatePhase> pair in this.TargetedJumpGates)
			{
				MyJumpGate jump_gate = pair.Key;
				if (jump_gate == null || jump_gate.Closed || jump_gate.Phase == pair.Value) continue;
				phase_updates[pair.Key] = jump_gate.Phase;
				if (jump_gate.Phase == this.SelectedGatePhase) event_controller.TriggerAction(0);
				else if (pair.Value == this.SelectedGatePhase) event_controller.TriggerAction(1);
			}

			foreach (KeyValuePair<MyJumpGate, MyJumpGatePhase> pair in phase_updates) this.TargetedJumpGates[pair.Key] = pair.Value;
		}

		public void AddBlocks(List<IMyTerminalBlock> blocks)
		{
			
		}

		public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
		{
			IMyTerminalControlCombobox choose_phase_cb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>(MODID_PREFIX + "PhaseChangedEvent_Phase");
			choose_phase_cb.Title = MyStringId.GetOrCompute("Jump Gate Phase");
			choose_phase_cb.SupportsMultipleBlocks = false;
			choose_phase_cb.Visible = block => block.Components.Get<JumpGatePhaseChangedEvent>().IsSelected;
			choose_phase_cb.ComboBoxContent = list => list.AddRange(JumpGatePhaseChangedEvent.JumpGatePhaseValues);
			choose_phase_cb.Getter = block => (long) block.Components.Get<JumpGatePhaseChangedEvent>().SelectedGatePhase;
			choose_phase_cb.Setter = (block, value) => block.Components.Get<JumpGatePhaseChangedEvent>().SelectedGatePhase = (MyJumpGatePhase) value;
			MyAPIGateway.TerminalControls.AddControl<T>(choose_phase_cb);

			IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(MODID_PREFIX + "PhaseChangedEvent_JumpGate");
			choose_jump_gate_lb.Title = MyStringId.GetOrCompute("Jump Gates");
			choose_jump_gate_lb.SupportsMultipleBlocks = false;
			choose_jump_gate_lb.Visible = block => block.Components.Get<JumpGatePhaseChangedEvent>().IsSelected;
			choose_jump_gate_lb.Multiselect = true;
			choose_jump_gate_lb.VisibleRowsCount = 5;
			choose_jump_gate_lb.ListContent = (block, content_list, preselect_list) => {
				JumpGatePhaseChangedEvent event_block = block.Components.Get<JumpGatePhaseChangedEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				construct.GetJumpGates(JumpGatePhaseChangedEvent.TEMP_JumpGates);

				foreach (MyJumpGate jump_gate in JumpGatePhaseChangedEvent.TEMP_JumpGates.OrderBy((gate) => gate.JumpGateID))
				{
					if (!jump_gate.Closed && jump_gate.IsValid() && jump_gate.Controller != null)
					{
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute($"Jump Gate with {jump_gate.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == jump_gate.JumpGateID)} drives\nJump Gate ID: {jump_gate.JumpGateID}"), jump_gate.JumpGateID);
						content_list.Add(item);
						if (event_block.TargetedJumpGates.ContainsKey(jump_gate)) preselect_list.Add(item);
					}
				}

				JumpGatePhaseChangedEvent.TEMP_JumpGates.Clear();
			};

			choose_jump_gate_lb.ItemSelected = (block, selected) => {
				JumpGatePhaseChangedEvent event_block = block.Components.Get<JumpGatePhaseChangedEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				event_block.TargetedJumpGates.Clear();

				foreach (MyTerminalControlListBoxItem item in selected)
				{
					long selected_id = (long) item.UserData;
					MyJumpGate jump_gate = construct.GetJumpGate(selected_id);
					if (jump_gate == null || jump_gate.Closed) continue;
					event_block.TargetedJumpGates[jump_gate] = jump_gate.Phase;
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
				JumpGatePhase = this.SelectedGatePhase,
				SelectedJumpGates = this.TargetedJumpGates.Select((pair) => pair.Key.JumpGateID).ToList(),
			};

			return new MyObjectBuilder_ModCustomComponent {
				ComponentType = nameof(JumpGatePhaseChangedEvent),
				CustomModData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(info)),
				RemoveExistingComponentOnNewInsert = true,
				SubtypeName = nameof(JumpGatePhaseChangedEvent)
			};
		}
	}
}
