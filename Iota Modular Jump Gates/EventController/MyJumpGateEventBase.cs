using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController
{
	internal abstract class MyJumpGateEventBase<TargetedGateValueType> : MyEventProxyEntityComponent, IMyEventComponentWithGui where TargetedGateValueType : IComparable<TargetedGateValueType>
	{
		[ProtoContract]
		private sealed class MySerializedJumpGateEvent
		{
			[ProtoMember(1)]
			public long EntityID;
			[ProtoMember(2)]
			public MyObjectBuilder_ComponentBase SerializedEvent;
		}

		[ProtoContract]
		protected sealed class MySerializedJumpGateEventInfo
		{
			[ProtoMember(1)]
			public TargetedGateValueType TargetValue;
			[ProtoMember(2)]
			public List<long> SelectedJumpGates;
			[ProtoMember(3)]
			public Dictionary<string, string> MetaData;

			public void SetValue<T>(string key, T value)
			{
				this.MetaData[key] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(value));
			}

			public void RemoveValue(string key)
			{
				this.MetaData.Remove(key);
			}

			public T GetValue<T>(string key)
			{
				if (!this.MetaData.ContainsKey(key)) throw new KeyNotFoundException($"No key with the specified name \"{key}\"");
				return MyAPIGateway.Utilities.SerializeFromBinary<T>(Convert.FromBase64String(this.MetaData[key]));
			}

			public T GetValueOrDefault<T>(string key, T default_ = default(T))
			{
				return (this.MetaData.ContainsKey(key)) ? MyAPIGateway.Utilities.SerializeFromBinary<T>(Convert.FromBase64String(this.MetaData[key])) : default_;
			}
		}

		private static readonly List<MyJumpGate> TEMP_JumpGates = new List<MyJumpGate>();

		private bool _IsSelected = false;
		private bool NetworkRegistered = false;
		private ulong LastUpdateTimeEpoch = 0;
		private MySerializedJumpGateEventInfo DeserializedInfo = null;
		private readonly Dictionary<MyJumpGate, TargetedGateValueType> TargetedJumpGates = new Dictionary<MyJumpGate, TargetedGateValueType>();

		protected bool IsDirty = false;
		protected int LastActionTriggered { get; private set; } = 0;
		protected TargetedGateValueType TargetValue;
		protected IMyEventControllerBlock EventController => this.Entity as IMyEventControllerBlock;
		protected string MODID_PREFIX => $"{MyJumpGateModSession.MODID}.EventController.{this.EventDisplayName.String}.";

		public abstract bool IsThresholdUsed { get; }
		public abstract bool IsConditionSelectionUsed { get; }
		public abstract bool IsBlocksListUsed { get; }
		public abstract long UniqueSelectionId { get; }
		public abstract MyStringId EventDisplayName { get; }
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

				if (value)
				{
					((MyCubeGrid) event_controller.CubeGrid).Schedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update); ((MyCubeGrid) event_controller.CubeGrid).Schedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);
					this.OnSelected();
				}
				else
				{
					((MyCubeGrid) event_controller.CubeGrid).DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);
					this.OnUnselected();
				}
			}
		}

		private void OnNetworkUpdate(MyNetworkInterface.Packet packet)
		{
			MySerializedJumpGateEvent info = packet?.Payload<MySerializedJumpGateEvent>();
			if (info == null || info.EntityID != this.Entity.EntityId) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (packet.EpochTime < this.LastUpdateTimeEpoch) return;
				this.Deserialize(info.SerializedEvent);
				this.IsDirty = false;
				packet.Forward(0, true).Send();
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2))
			{
				this.Deserialize(info.SerializedEvent);
				this.IsDirty = false;
			}
		}

		private void Init()
		{
			if (this.NetworkRegistered || !MyJumpGateModSession.Network.Registered) return;
			MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_EVENT_CONTROLLER_EVENT, this.OnNetworkUpdate);
			this.NetworkRegistered = true;
		}

		private void Release()
		{
			IMyEventControllerBlock event_controller = this.EventController;
			if (event_controller == null || event_controller.MarkedForClose) return;
			if (this._IsSelected) ((MyCubeGrid) event_controller.CubeGrid).DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);
			if (!this.NetworkRegistered || !MyJumpGateModSession.Network.Registered) return;
			MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_EVENT_CONTROLLER_EVENT, this.OnNetworkUpdate);
			this.NetworkRegistered = false;
		}

		protected virtual void AppendCustomInfo(StringBuilder sb)
		{
			IMyEventControllerBlock event_controller = this.EventController;
			sb.Append($"Event: {this.EventDisplayName.String}\n");
			sb.Append($"Action Triggered: {this.LastActionTriggered}\n\n");
			string condition = (event_controller.IsLowerOrEqualCondition) ? "<=" : ">=";

			foreach (KeyValuePair<MyJumpGate, TargetedGateValueType> pair in this.TargetedJumpGates)
			{
				if (this.IsConditionSelectionUsed)
				{
					int compare = pair.Value.CompareTo(this.TargetValue);
					bool matched = (event_controller.IsLowerOrEqualCondition) ? (compare <= 0) : (compare >= 0);
					sb.Append($" - Jump Gate \"{pair.Key.GetPrintableName()}\" (ID {pair.Key.JumpGateID})\n - ... Condition: {pair.Value} {condition} {this.TargetValue}\n - ... Trigger Action {(matched ? 0 : 1)}\n");
				}
				else
				{
					sb.Append($" - Jump Gate \"{pair.Key.GetPrintableName()}\" (ID {pair.Key.JumpGateID})\n");
				}
			}
		}

		protected virtual void Update()
		{
			if (this.DeserializedInfo != null)
			{
				MyJumpGateConstruct construct = MyJumpGateModSession.Instance.GetJumpGateGrid(this.EventController.CubeGrid);
				if (construct == null || !construct.FullyInitialized) return;
				this.TargetValue = this.DeserializedInfo.TargetValue;

				if (this.DeserializedInfo.SelectedJumpGates != null)
				{
					foreach (long gateid in this.DeserializedInfo.SelectedJumpGates)
					{
						MyJumpGate gate = construct.GetJumpGate(gateid);
						if (gate == null || gate.Closed) continue;
						this.TargetedJumpGates[gate] = this.GetValueFromJumpGate(gate);
					}
				}

				this.OnLoad(this.DeserializedInfo);
				this.DeserializedInfo = null;
			}

			this.Poll(false);
			StringBuilder sb = this.EventController?.GetDetailedInfo();

			if (sb != null)
			{
				try
				{
					sb.Clear();
					sb.Append("- - - [[ Event Controller ]] - - -\n");
					this.AppendCustomInfo(sb);
				}
				catch (Exception e)
				{
					sb.Append(e.StackTrace);
				}
			}

			if (this.IsDirty && MyJumpGateModSession.Network.Registered)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_EVENT_CONTROLLER_EVENT,
					TargetID = 0,
					Broadcast = false,
				};

				packet.Payload(new MySerializedJumpGateEvent {
					EntityID = this.Entity.EntityId,
					SerializedEvent = this.Serialize(),
				});

				packet.Send();
			}
		}

		protected void TriggerAction(int index)
		{
			this.EventController?.TriggerAction(index);
			this.LastActionTriggered = index;
		}

		protected void Poll(bool force_check)
		{
			IMyEventControllerBlock event_controller = this.EventController;
			if (event_controller == null || event_controller.MarkedForClose) return;
			List<MyJumpGate> closed = new List<MyJumpGate>();
			Dictionary<MyJumpGate, TargetedGateValueType> updates = new Dictionary<MyJumpGate, TargetedGateValueType>(this.TargetedJumpGates.Count);

			foreach (KeyValuePair<MyJumpGate, TargetedGateValueType> pair in this.TargetedJumpGates)
			{
				MyJumpGate jump_gate = pair.Key;

				if (jump_gate.Closed)
				{
					closed.Add(jump_gate);
					continue;
				}

				TargetedGateValueType value = this.GetValueFromJumpGate(jump_gate);
				if (!force_check && (object.Equals(pair.Value, value) || value.CompareTo(pair.Value) == 0)) continue;
				updates[pair.Key] = value;
				this.CheckValueAgainstTarget(value, pair.Value, this.TargetValue);
			}

			foreach (KeyValuePair<MyJumpGate, TargetedGateValueType> pair in updates)
			{
				if (closed.Contains(pair.Key))
				{
					this.OnJumpGateRemoved(pair.Key);
					this.TargetedJumpGates.Remove(pair.Key);
				}
				else this.TargetedJumpGates[pair.Key] = pair.Value;
			}
		}

		protected void ReloadJumpGateValues()
		{
			foreach (MyJumpGate gate in this.TargetedJumpGates.Keys.ToList()) this.TargetedJumpGates[gate] = this.GetValueFromJumpGate(gate);
		}

		protected void ResetJumpGateValues(TargetedGateValueType value)
		{
			foreach (MyJumpGate gate in this.TargetedJumpGates.Keys.ToList()) this.TargetedJumpGates[gate] = value;
		}

		protected void CreateTerminalControls<T, EventType>() where T : IMyTerminalBlock where EventType : MyJumpGateEventBase<TargetedGateValueType>
		{
			{
				IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(this.MODID_PREFIX + "JumpGate");
				choose_jump_gate_lb.Title = MyStringId.GetOrCompute("Jump Gates");
				choose_jump_gate_lb.SupportsMultipleBlocks = false;
				choose_jump_gate_lb.Visible = block => block.Components.Get<EventType>()?.IsSelected ?? false;
				choose_jump_gate_lb.Multiselect = true;
				choose_jump_gate_lb.VisibleRowsCount = 5;
				choose_jump_gate_lb.ListContent = (block, content_list, preselect_list) => {
					MyJumpGateEventBase<TargetedGateValueType> event_block = block.Components.Get<EventType>();
					IMyEventControllerBlock event_controller = event_block?.EventController;
					MyJumpGateConstruct construct;
					if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
					construct.GetJumpGates(MyJumpGateEventBase<TargetedGateValueType>.TEMP_JumpGates);

					foreach (MyJumpGate jump_gate in MyJumpGateEventBase<TargetedGateValueType>.TEMP_JumpGates.OrderBy((gate) => gate.JumpGateID))
					{
						if (!jump_gate.Closed && this.IsJumpGateValidForList(jump_gate))
						{
							MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute($"Jump Gate with {jump_gate.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == jump_gate.JumpGateID)} drives\nJump Gate ID: {jump_gate.JumpGateID}"), jump_gate.JumpGateID);
							content_list.Add(item);
							if (event_block.TargetedJumpGates.ContainsKey(jump_gate)) preselect_list.Add(item);
						}
					}

					MyJumpGateEventBase<TargetedGateValueType>.TEMP_JumpGates.Clear();
				};

				choose_jump_gate_lb.ItemSelected = (block, selected) => {
					MyJumpGateEventBase<TargetedGateValueType> event_block = block.Components.Get<EventType>();
					IMyEventControllerBlock event_controller = event_block?.EventController;
					MyJumpGateConstruct construct;
					if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
					List<MyJumpGate> removed_gates = new List<MyJumpGate>(this.TargetedJumpGates.Keys);

					foreach (MyTerminalControlListBoxItem item in selected)
					{
						long selected_id = (long) item.UserData;
						MyJumpGate jump_gate = construct.GetJumpGate(selected_id);
						if (jump_gate == null || jump_gate.Closed || this.TargetedJumpGates.ContainsKey(jump_gate)) continue;
						event_block.TargetedJumpGates[jump_gate] = this.GetValueFromJumpGate(jump_gate);
						this.OnJumpGateAdded(jump_gate);
						removed_gates.Remove(jump_gate);
					}

					foreach (MyJumpGate closed in removed_gates)
					{
						this.OnJumpGateRemoved(closed);
						event_block.TargetedJumpGates.Remove(closed);
					}

					this.Poll(true);
				};

				MyAPIGateway.TerminalControls.AddControl<T>(choose_jump_gate_lb);
			}
		}

		protected virtual void OnSave(MySerializedJumpGateEventInfo info) { }

		protected virtual void OnLoad(MySerializedJumpGateEventInfo info) { }

		protected virtual void OnSelected() { }

		protected virtual void OnUnselected() { }

		protected virtual void OnJumpGateAdded(MyJumpGate jump_gate) { }

		protected virtual void OnJumpGateRemoved(MyJumpGate jump_gate) { }

		protected abstract void CheckValueAgainstTarget(TargetedGateValueType new_value, TargetedGateValueType old_value, TargetedGateValueType target);

		protected virtual bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return !jump_gate.Closed;
		}

		protected bool IsListeningToJumpGate(MyJumpGate jump_gate)
		{
			return this.TargetedJumpGates.ContainsKey(jump_gate);
		}

		protected virtual TargetedGateValueType GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return default(TargetedGateValueType);
		}

		protected List<MyJumpGate> GetTargetedJumpGates()
		{
			return new List<MyJumpGate>(this.TargetedJumpGates.Keys);
		}

		protected TargetedGateValueType this[MyJumpGate jump_gate]
		{
			get
			{
				return this.TargetedJumpGates[jump_gate];
			}
			set
			{
				this.TargetedJumpGates[jump_gate] = value;
			}
		}

		public void SetDirty()
		{
			this.IsDirty = true;
			this.LastUpdateTimeEpoch = (ulong) (DateTime.UtcNow - DateTime.MinValue).Ticks;
		}

		public override void OnAddedToContainer()
		{
			base.OnAddedToContainer();
			this.Init();
		}

		public override void OnBeforeRemovedFromContainer()
		{
			base.OnBeforeRemovedFromContainer();
			this.Release();
		}

		public virtual void NotifyValuesChanged() { }

		public virtual bool IsBlockValidForList(IMyTerminalBlock block)
		{
			return false;
		}

		public override bool IsSerialized()
		{
			return true;
		}

		public virtual void AddBlocks(List<IMyTerminalBlock> blocks) { }

		public virtual void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks) { }

		public abstract void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock;

		public override void Deserialize(MyObjectBuilder_ComponentBase builder)
		{
			base.Deserialize(builder);
			MyObjectBuilder_ModCustomComponent serialized_event = (MyObjectBuilder_ModCustomComponent) builder;
			try { this.DeserializedInfo = MyAPIGateway.Utilities.SerializeFromBinary<MySerializedJumpGateEventInfo>(Convert.FromBase64String(serialized_event.CustomModData)); }
			catch { this.DeserializedInfo = null; }
		}

		public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
		{
			MySerializedJumpGateEventInfo info = new MySerializedJumpGateEventInfo {
				TargetValue = this.TargetValue,
				SelectedJumpGates = this.TargetedJumpGates.Select((pair) => pair.Key.JumpGateID).ToList(),
				MetaData = new Dictionary<string, string>(),
			};

			this.OnSave(info);

			return new MyObjectBuilder_ModCustomComponent {
				ComponentType = this.ComponentTypeDebugString,
				CustomModData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(info)),
				RemoveExistingComponentOnNewInsert = true,
				SubtypeName = this.ComponentTypeDebugString,
			};
		}
	}
}
