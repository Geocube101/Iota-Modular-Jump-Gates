using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateEntityMassChanged))]
	[MyComponentType(typeof(JumpGateEntityMassChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateEntityMassChangedEvent : MyJumpGateEventSync, IMyEventComponentWithGui
	{
		[ProtoContract]
		private sealed class EventInfo
		{
			[ProtoMember(1)]
			public bool UseControllerFilter;
			[ProtoMember(2)]
			public double TargetEntityMass;
			[ProtoMember(3)]
			public List<long> SelectedJumpGates;
		}

		private static readonly List<MyJumpGate> TEMP_JumpGates = new List<MyJumpGate>();

		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".EventController.";

		private bool _IsSelected = false;
		private bool IsControllerFiltered = true;
		private double TargetEntityMass = 0;
		private IMyEventControllerBlock EventController => this.Entity as IMyEventControllerBlock;
		private EventInfo DeserializedInfo = null;
		private readonly Dictionary<MyJumpGate, double> TargetedJumpGates = new Dictionary<MyJumpGate, double>();
		private readonly Dictionary<MyEntity, float> TEMP_JumpGateEntities = new Dictionary<MyEntity, float>();
		
		public bool IsThresholdUsed => false;
		public bool IsConditionSelectionUsed => true;
		public bool IsBlocksListUsed => false;
		public long UniqueSelectionId => 0x7FFFFFFFFFFFFFFB;
		public MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Entity Mass Changed");
		public override string ComponentTypeDebugString => nameof(JumpGateEntityMassChangedEvent);

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
				this.TargetEntityMass = this.DeserializedInfo.TargetEntityMass;

				if (this.DeserializedInfo.SelectedJumpGates != null)
				{
					foreach (long gateid in this.DeserializedInfo.SelectedJumpGates)
					{
						MyJumpGate gate = construct.GetJumpGate(gateid);
						if (gate == null || gate.Closed) continue;
						gate.GetEntitiesInJumpSpace(this.TEMP_JumpGateEntities, this.IsControllerFiltered);
						double mass = this.TEMP_JumpGateEntities.Sum((_pair) => (double) _pair.Value);
						this.TEMP_JumpGateEntities.Clear();
						this.TargetedJumpGates[gate] = mass;
					}
				}

				this.DeserializedInfo = null;
			}

			Dictionary<MyJumpGate, double?> mass_updates = new Dictionary<MyJumpGate, double?>(this.TargetedJumpGates.Count);

			foreach (KeyValuePair<MyJumpGate, double> pair in this.TargetedJumpGates)
			{
				MyJumpGate jump_gate = pair.Key;

				if (jump_gate == null || jump_gate.Closed)
				{
					mass_updates[jump_gate] = null;
					continue;
				}

				jump_gate.GetEntitiesInJumpSpace(this.TEMP_JumpGateEntities, this.IsControllerFiltered);
				double mass = this.TEMP_JumpGateEntities.Sum((_pair) => (double) _pair.Value);
				this.TEMP_JumpGateEntities.Clear();
				if (mass == pair.Value) continue;
				mass_updates[pair.Key] = mass;
				if (event_controller.IsLowerOrEqualCondition && mass <= this.TargetEntityMass && pair.Value > this.TargetEntityMass) event_controller.TriggerAction(0);
				else if (event_controller.IsLowerOrEqualCondition && mass > this.TargetEntityMass && pair.Value <=  this.TargetEntityMass) event_controller.TriggerAction(1);
				else if (!event_controller.IsLowerOrEqualCondition && mass >= this.TargetEntityMass && pair.Value < this.TargetEntityMass) event_controller.TriggerAction(0);
				else if (!event_controller.IsLowerOrEqualCondition && mass < this.TargetEntityMass && pair.Value >= this.TargetEntityMass) event_controller.TriggerAction(1);
			}

			foreach (KeyValuePair<MyJumpGate, double?> pair in mass_updates)
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
			IMyTerminalControlOnOffSwitch is_controller_filtered = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>(MODID_PREFIX + "EntityMassChangedEvent_IsFiltered");
			is_controller_filtered.Title = MyStringId.GetOrCompute("Use Filtered Entities");
			is_controller_filtered.Tooltip = MyStringId.GetOrCompute("Whether to respect attached controller filter settings");
			is_controller_filtered.OnText = MyStringId.GetOrCompute("Yes");
			is_controller_filtered.OffText = MyStringId.GetOrCompute("No");
			is_controller_filtered.SupportsMultipleBlocks = false;
			is_controller_filtered.Visible = block => block.Components.Get<JumpGateEntityMassChangedEvent>()?.IsSelected ?? false;
			is_controller_filtered.Getter = block => block.Components.Get<JumpGateEntityMassChangedEvent>().IsControllerFiltered;
			is_controller_filtered.Setter = (block, value) => {
				JumpGateEntityMassChangedEvent event_block = block.Components.Get<JumpGateEntityMassChangedEvent>();
				event_block.IsControllerFiltered = value;
				foreach (MyJumpGate gate in event_block.TargetedJumpGates.Keys.ToList()) event_block.TargetedJumpGates[gate] = gate.GetJumpSpaceEntityCount(event_block.IsControllerFiltered);
			};
			MyAPIGateway.TerminalControls.AddControl<T>(is_controller_filtered);

			IMyTerminalControlSlider entity_mass_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(MODID_PREFIX + "EntityMassChangedEvent_Mass");
			entity_mass_sdr.Title = MyStringId.GetOrCompute("Entity Mass (Kg)");
			entity_mass_sdr.Tooltip = MyStringId.GetOrCompute("The mass (in kilograms) of jump space entitites to check against");
			entity_mass_sdr.SupportsMultipleBlocks = false;
			entity_mass_sdr.Visible = block => block.Components.Get<JumpGateEntityMassChangedEvent>()?.IsSelected ?? false;
			entity_mass_sdr.SetLimits(0f, float.MaxValue);
			entity_mass_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertMetricUnits(block.Components.Get<JumpGateEntityMassChangedEvent>().TargetEntityMass * 1e3, "g", 4));
			entity_mass_sdr.Getter = (block) => (float) block.Components.Get<JumpGateEntityMassChangedEvent>().TargetEntityMass * 1e3f;
			entity_mass_sdr.Setter = (block, value) => block.Components.Get<JumpGateEntityMassChangedEvent>().TargetEntityMass = value / 1e3;
			MyAPIGateway.TerminalControls.AddControl<T>(entity_mass_sdr);

			IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(MODID_PREFIX + "EntityMassChangedEvent_JumpGate");
			choose_jump_gate_lb.Title = MyStringId.GetOrCompute("Jump Gates");
			choose_jump_gate_lb.SupportsMultipleBlocks = false;
			choose_jump_gate_lb.Visible = block => block.Components.Get<JumpGateEntityMassChangedEvent>()?.IsSelected ?? false;
			choose_jump_gate_lb.Multiselect = true;
			choose_jump_gate_lb.VisibleRowsCount = 5;
			choose_jump_gate_lb.ListContent = (block, content_list, preselect_list) => {
				JumpGateEntityMassChangedEvent event_block = block.Components.Get<JumpGateEntityMassChangedEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				construct.GetJumpGates(JumpGateEntityMassChangedEvent.TEMP_JumpGates);

				foreach (MyJumpGate jump_gate in JumpGateEntityMassChangedEvent.TEMP_JumpGates.OrderBy((gate) => gate.JumpGateID))
				{
					if (!jump_gate.Closed && jump_gate.IsValid() && jump_gate.Controller != null)
					{
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute($"Jump Gate with {jump_gate.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == jump_gate.JumpGateID)} drives\nJump Gate ID: {jump_gate.JumpGateID}"), jump_gate.JumpGateID);
						content_list.Add(item);
						if (event_block.TargetedJumpGates.ContainsKey(jump_gate)) preselect_list.Add(item);
					}
				}

				JumpGateEntityMassChangedEvent.TEMP_JumpGates.Clear();
			};

			choose_jump_gate_lb.ItemSelected = (block, selected) => {
				JumpGateEntityMassChangedEvent event_block = block.Components.Get<JumpGateEntityMassChangedEvent>();
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
				TargetEntityMass = this.TargetEntityMass,
				SelectedJumpGates = this.TargetedJumpGates.Select((pair) => pair.Key.JumpGateID).ToList(),
			};

			return new MyObjectBuilder_ModCustomComponent
			{
				ComponentType = nameof(JumpGateEntityMassChangedEvent),
				CustomModData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(info)),
				RemoveExistingComponentOnNewInsert = true,
				SubtypeName = nameof(JumpGateEntityMassChangedEvent)
			};
		}
	}
}
