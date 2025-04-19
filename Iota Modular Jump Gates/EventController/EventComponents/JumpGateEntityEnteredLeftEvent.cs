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
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateEntityEnteredLeft))]
	[MyComponentType(typeof(JumpGateEntityEnteredLeftEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateEntityEnteredLeftEvent : MyJumpGateEventSync, IMyEventComponentWithGui
	{
		[ProtoContract]
		private sealed class EventInfo
		{
			[ProtoMember(1)]
			public List<long> SelectedJumpGates;
		}

		private static readonly List<MyJumpGate> TEMP_JumpGates = new List<MyJumpGate>();

		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".EventController.";

		private bool? TriggerIndex = null;
		private bool _IsSelected = false;
		private IMyEventControllerBlock EventController => this.Entity as IMyEventControllerBlock;
		private EventInfo DeserializedInfo = null;
		private readonly Dictionary<MyJumpGate, bool> TargetedJumpGates = new Dictionary<MyJumpGate, bool>();
		
		public bool IsThresholdUsed => false;
		public bool IsConditionSelectionUsed => false;
		public bool IsBlocksListUsed => false;
		public long UniqueSelectionId => 0x7FFFFFFFFFFFFFFC;
		public MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Entity Entered");
		public override string ComponentTypeDebugString => nameof(JumpGateEntityEnteredLeftEvent);

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
				Dictionary<MyJumpGate, bool> updated_callbacks = new Dictionary<MyJumpGate, bool>(this.TargetedJumpGates.Count);

				if (value)
				{
					((MyCubeGrid) event_controller.CubeGrid).Schedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);

					foreach (KeyValuePair<MyJumpGate, bool> pair in this.TargetedJumpGates)
					{
						if (pair.Value) continue;
						updated_callbacks[pair.Key] = true;
						pair.Key.OnEntityCollision(this.OnEntityCollision);
					}

					foreach (KeyValuePair<MyJumpGate, bool> pair in updated_callbacks) this.TargetedJumpGates[pair.Key] = pair.Value;
				}
				else
				{
					((MyCubeGrid) event_controller.CubeGrid).DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);

					foreach (KeyValuePair<MyJumpGate, bool> pair in this.TargetedJumpGates)
					{
						if (!pair.Value) continue;
						updated_callbacks[pair.Key] = false;
						pair.Key.OffEntityCollision(this.OnEntityCollision);
					}

					foreach (KeyValuePair<MyJumpGate, bool> pair in updated_callbacks) this.TargetedJumpGates[pair.Key] = pair.Value;
				}
			}
		}

		private void OnEntityCollision(MyJumpGate jump_gate, MyEntity entity, bool is_entering)
		{
			if (jump_gate == null || jump_gate.Closed || !this.TargetedJumpGates.ContainsKey(jump_gate) || entity == null || entity.Physics == null || entity.MarkedForClose) return;
			this.TriggerIndex = is_entering;
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

				if (this.DeserializedInfo.SelectedJumpGates != null)
				{
					foreach (long gateid in this.DeserializedInfo.SelectedJumpGates)
					{
						MyJumpGate gate = construct.GetJumpGate(gateid);
						if (gate == null || gate.Closed) continue;
						this.TargetedJumpGates[gate] = gate.IsEntityCollisionCallbackRegistered(this.OnEntityCollision);
					}
				}

				this.DeserializedInfo = null;
			}

			Dictionary<MyJumpGate, bool> updated_callbacks = new Dictionary<MyJumpGate, bool>(this.TargetedJumpGates.Count);

			foreach (KeyValuePair<MyJumpGate, bool> pair in this.TargetedJumpGates)
			{
				if (pair.Value) continue;
				updated_callbacks[pair.Key] = true;
				pair.Key.OnEntityCollision(this.OnEntityCollision);
			}

			foreach (KeyValuePair<MyJumpGate, bool> pair in updated_callbacks) this.TargetedJumpGates[pair.Key] = pair.Value;

			if (this.TriggerIndex != null)
			{
				event_controller.TriggerAction((this.TriggerIndex.Value) ? 0 : 1);
				this.TriggerIndex = null;
			}
		}

		public void AddBlocks(List<IMyTerminalBlock> blocks)
		{
			
		}

		public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
		{
			IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(MODID_PREFIX + "EntityEnteredEvent_JumpGate");
			choose_jump_gate_lb.Title = MyStringId.GetOrCompute("Jump Gates");
			choose_jump_gate_lb.SupportsMultipleBlocks = false;
			choose_jump_gate_lb.Visible = block => block.Components.Get<JumpGateEntityEnteredLeftEvent>().IsSelected;
			choose_jump_gate_lb.Multiselect = true;
			choose_jump_gate_lb.VisibleRowsCount = 5;
			choose_jump_gate_lb.ListContent = (block, content_list, preselect_list) => {
				JumpGateEntityEnteredLeftEvent event_block = block.Components.Get<JumpGateEntityEnteredLeftEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				construct.GetJumpGates(JumpGateEntityEnteredLeftEvent.TEMP_JumpGates);

				foreach (MyJumpGate jump_gate in JumpGateEntityEnteredLeftEvent.TEMP_JumpGates.OrderBy((gate) => gate.JumpGateID))
				{
					if (!jump_gate.Closed && jump_gate.IsValid() && jump_gate.Controller != null)
					{
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute($"Jump Gate with {jump_gate.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == jump_gate.JumpGateID)} drives\nJump Gate ID: {jump_gate.JumpGateID}"), jump_gate.JumpGateID);
						content_list.Add(item);
						if (event_block.TargetedJumpGates.ContainsKey(jump_gate)) preselect_list.Add(item);
					}
				}

				JumpGateEntityEnteredLeftEvent.TEMP_JumpGates.Clear();
			};

			choose_jump_gate_lb.ItemSelected = (block, selected) => {
				JumpGateEntityEnteredLeftEvent event_block = block.Components.Get<JumpGateEntityEnteredLeftEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				List<MyJumpGate> remaining_gates = new List<MyJumpGate>(event_block.TargetedJumpGates.Keys);

				foreach (MyTerminalControlListBoxItem item in selected)
				{
					long selected_id = (long) item.UserData;
					MyJumpGate jump_gate = construct.GetJumpGate(selected_id);
					if (jump_gate == null || jump_gate.Closed) continue;
					event_block.TargetedJumpGates[jump_gate] = jump_gate.IsEntityCollisionCallbackRegistered(this.OnEntityCollision);
					remaining_gates.Remove(jump_gate);
				}

				foreach (MyJumpGate gate in remaining_gates)
				{
					gate.OffEntityCollision(this.OnEntityCollision);
					event_block.TargetedJumpGates.Remove(gate);
				}
			};

			MyAPIGateway.TerminalControls.AddControl<T>(choose_jump_gate_lb);
		}

		public override void OnAddedToContainer()
		{
			base.OnAddedToContainer();
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
				SelectedJumpGates = this.TargetedJumpGates.Select((pair) => pair.Key.JumpGateID).ToList(),
			};

			return new MyObjectBuilder_ModCustomComponent {
				ComponentType = nameof(JumpGateEntityEnteredLeftEvent),
				CustomModData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(info)),
				RemoveExistingComponentOnNewInsert = true,
				SubtypeName = nameof(JumpGateEntityEnteredLeftEvent)
			};
		}
	}
}
