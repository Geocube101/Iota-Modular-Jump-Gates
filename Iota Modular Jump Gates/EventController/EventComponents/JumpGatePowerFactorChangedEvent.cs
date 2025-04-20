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
using VRageMath;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGatePowerFactorChanged))]
	[MyComponentType(typeof(JumpGatePowerFactorChangedEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGatePowerFactorChangedEvent : MyJumpGateEventSync, IMyEventComponentWithGui
	{
		[ProtoContract]
		private sealed class EventInfo
		{
			[ProtoMember(1)]
			public bool UseControllerFilter;
			[ProtoMember(2)]
			public double TargetPowerFactor;
			[ProtoMember(3)]
			public List<long> SelectedJumpGates;
		}

		private static readonly List<MyJumpGate> TEMP_JumpGates = new List<MyJumpGate>();

		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".EventController.";

		private bool _IsSelected = false;
		private bool IsControllerFiltered = true;
		private double TargetPowerFactor = 0;
		private IMyEventControllerBlock EventController => this.Entity as IMyEventControllerBlock;
		private EventInfo DeserializedInfo = null;
		private readonly Dictionary<MyJumpGate, double> TargetedJumpGates = new Dictionary<MyJumpGate, double>();

		public bool IsThresholdUsed => false;
		public bool IsConditionSelectionUsed => true;
		public bool IsBlocksListUsed => false;
		public long UniqueSelectionId => 0x7FFFFFFFFFFFFFF9;
		public MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Power Factor Changed");
		public override string ComponentTypeDebugString => nameof(JumpGatePowerFactorChangedEvent);

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
				this.TargetPowerFactor = this.DeserializedInfo.TargetPowerFactor;

				if (this.DeserializedInfo.SelectedJumpGates != null)
				{
					foreach (long gateid in this.DeserializedInfo.SelectedJumpGates)
					{
						MyJumpGate gate = construct.GetJumpGate(gateid);
						if (gate == null || gate.Closed) continue;
						Vector3D? endpoint = gate.Controller?.BlockSettings.SelectedWaypoint()?.GetEndpoint();
						double factor = 0;

						if (endpoint != null)
						{
							Vector3D _endpoint = endpoint.Value;
							factor = gate.CalculatePowerFactorFromDistanceRatio(gate.CalculateDistanceRatio(ref _endpoint));
						}

						this.TargetedJumpGates[gate] = factor;
					}
				}

				this.DeserializedInfo = null;
			}

			Dictionary<MyJumpGate, double?> factor_updates = new Dictionary<MyJumpGate, double?>(this.TargetedJumpGates.Count);

			foreach (KeyValuePair<MyJumpGate, double> pair in this.TargetedJumpGates)
			{
				MyJumpGate jump_gate = pair.Key;

				if (jump_gate == null || jump_gate.Closed)
				{
					factor_updates[jump_gate] = null;
					continue;
				}

				Vector3D? endpoint = jump_gate.Controller?.BlockSettings.SelectedWaypoint()?.GetEndpoint();
				double factor = 0;

				if (endpoint != null)
				{
					Vector3D _endpoint = endpoint.Value;
					factor = jump_gate.CalculatePowerFactorFromDistanceRatio(jump_gate.CalculateDistanceRatio(ref _endpoint));
				}

				if (factor == pair.Value) continue;
				factor_updates[pair.Key] = factor;
				if (event_controller.IsLowerOrEqualCondition && factor <= this.TargetPowerFactor && pair.Value > this.TargetPowerFactor) event_controller.TriggerAction(0);
				else if (event_controller.IsLowerOrEqualCondition && factor > this.TargetPowerFactor && pair.Value <= this.TargetPowerFactor) event_controller.TriggerAction(1);
				else if (!event_controller.IsLowerOrEqualCondition && factor >= this.TargetPowerFactor && pair.Value < this.TargetPowerFactor) event_controller.TriggerAction(0);
				else if (!event_controller.IsLowerOrEqualCondition && factor < this.TargetPowerFactor && pair.Value >= this.TargetPowerFactor) event_controller.TriggerAction(1);
			}

			foreach (KeyValuePair<MyJumpGate, double?> pair in factor_updates)
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
			IMyTerminalControlOnOffSwitch is_controller_filtered = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>(MODID_PREFIX + "PowerFactorChangedEvent_IsFiltered");
			is_controller_filtered.Title = MyStringId.GetOrCompute("Use Filtered Entities");
			is_controller_filtered.Tooltip = MyStringId.GetOrCompute("Whether to respect attached controller filter settings");
			is_controller_filtered.OnText = MyStringId.GetOrCompute("Yes");
			is_controller_filtered.OffText = MyStringId.GetOrCompute("No");
			is_controller_filtered.SupportsMultipleBlocks = false;
			is_controller_filtered.Visible = block => block.Components.Get<JumpGatePowerFactorChangedEvent>()?.IsSelected ?? false;
			is_controller_filtered.Getter = block => block.Components.Get<JumpGatePowerFactorChangedEvent>().IsControllerFiltered;
			is_controller_filtered.Setter = (block, value) => {
				JumpGatePowerFactorChangedEvent event_block = block.Components.Get<JumpGatePowerFactorChangedEvent>();
				event_block.IsControllerFiltered = value;
				foreach (MyJumpGate gate in event_block.TargetedJumpGates.Keys.ToList()) event_block.TargetedJumpGates[gate] = gate.GetJumpSpaceEntityCount(event_block.IsControllerFiltered);
			};
			MyAPIGateway.TerminalControls.AddControl<T>(is_controller_filtered);

			IMyTerminalControlSlider required_power_sdr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(MODID_PREFIX + "PowerFactorChangedEvent_PowerFactor");
			required_power_sdr.Title = MyStringId.GetOrCompute("Power Factor");
			required_power_sdr.Tooltip = MyStringId.GetOrCompute("The power factor to check");
			required_power_sdr.SupportsMultipleBlocks = false;
			required_power_sdr.Visible = block => block.Components.Get<JumpGatePowerFactorChangedEvent>()?.IsSelected ?? false;
			required_power_sdr.SetLimits(0f, float.MaxValue);
			required_power_sdr.Writer = (block, string_builder) => string_builder.Append(MyJumpGateModSession.AutoconvertSciNotUnits(block.Components.Get<JumpGatePowerFactorChangedEvent>().TargetPowerFactor, 4));
			required_power_sdr.Getter = (block) => (float) block.Components.Get<JumpGatePowerFactorChangedEvent>().TargetPowerFactor;
			required_power_sdr.Setter = (block, value) => block.Components.Get<JumpGatePowerFactorChangedEvent>().TargetPowerFactor = value;
			MyAPIGateway.TerminalControls.AddControl<T>(required_power_sdr);

			IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(MODID_PREFIX + "PowerFactorChangedEvent_JumpGate");
			choose_jump_gate_lb.Title = MyStringId.GetOrCompute("Jump Gates");
			choose_jump_gate_lb.SupportsMultipleBlocks = false;
			choose_jump_gate_lb.Visible = block => block.Components.Get<JumpGatePowerFactorChangedEvent>()?.IsSelected ?? false;
			choose_jump_gate_lb.Multiselect = true;
			choose_jump_gate_lb.VisibleRowsCount = 5;
			choose_jump_gate_lb.ListContent = (block, content_list, preselect_list) => {
				JumpGatePowerFactorChangedEvent event_block = block.Components.Get<JumpGatePowerFactorChangedEvent>();
				IMyEventControllerBlock event_controller = event_block?.EventController;
				MyJumpGateConstruct construct;
				if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
				construct.GetJumpGates(JumpGatePowerFactorChangedEvent.TEMP_JumpGates);

				foreach (MyJumpGate jump_gate in JumpGatePowerFactorChangedEvent.TEMP_JumpGates.OrderBy((gate) => gate.JumpGateID))
				{
					if (!jump_gate.Closed && jump_gate.IsValid() && jump_gate.Controller != null)
					{
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute($"Jump Gate with {jump_gate.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == jump_gate.JumpGateID)} drives\nJump Gate ID: {jump_gate.JumpGateID}"), jump_gate.JumpGateID);
						content_list.Add(item);
						if (event_block.TargetedJumpGates.ContainsKey(jump_gate)) preselect_list.Add(item);
					}
				}

				JumpGatePowerFactorChangedEvent.TEMP_JumpGates.Clear();
			};

			choose_jump_gate_lb.ItemSelected = (block, selected) => {
				JumpGatePowerFactorChangedEvent event_block = block.Components.Get<JumpGatePowerFactorChangedEvent>();
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
			EventInfo info = new EventInfo
			{
				UseControllerFilter = this.IsControllerFiltered,
				TargetPowerFactor = this.TargetPowerFactor,
				SelectedJumpGates = this.TargetedJumpGates.Select((pair) => pair.Key.JumpGateID).ToList(),
			};

			return new MyObjectBuilder_ModCustomComponent
			{
				ComponentType = nameof(JumpGatePowerFactorChangedEvent),
				CustomModData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(info)),
				RemoveExistingComponentOnNewInsert = true,
				SubtypeName = nameof(JumpGatePowerFactorChangedEvent)
			};
		}
	}
}
