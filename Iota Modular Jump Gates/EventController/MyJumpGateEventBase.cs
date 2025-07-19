using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
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
			public List<KeyValuePair<long, byte>> SelectedRemoteJumpGates;
			[ProtoMember(4)]
			public Dictionary<string, string> MetaData;

			public void SetValue<T>(string key, T value)
			{
				if (this.MetaData == null) this.MetaData = new Dictionary<string, string>();
				this.MetaData[key] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(value));
			}

			public void RemoveValue(string key)
			{
				this.MetaData?.Remove(key);
			}

			public T GetValue<T>(string key)
			{
				if (this.MetaData == null || !this.MetaData.ContainsKey(key)) throw new KeyNotFoundException($"No key with the specified name \"{key}\"");
				return MyAPIGateway.Utilities.SerializeFromBinary<T>(Convert.FromBase64String(this.MetaData[key]));
			}

			public T GetValueOrDefault<T>(string key, T default_ = default(T))
			{
				return (this.MetaData != null && this.MetaData.ContainsKey(key)) ? MyAPIGateway.Utilities.SerializeFromBinary<T>(Convert.FromBase64String(this.MetaData[key])) : default_;
			}
		}

		private bool _IsSelected = false;
		private bool NetworkRegistered = false;
		private ulong LastUpdateTimeEpoch = 0;
		private MySerializedJumpGateEventInfo DeserializedInfo = null;
		private readonly Dictionary<MyJumpGate, TargetedGateValueType> TargetedJumpGates = new Dictionary<MyJumpGate, TargetedGateValueType>();
		private readonly Dictionary<MyJumpGate, TargetedGateValueType> TargetedRemoteJumpGates = new Dictionary<MyJumpGate, TargetedGateValueType>();
		private readonly List<KeyValuePair<MyJumpGateRemoteAntenna, byte>> TargetedRemoteAntennas = new List<KeyValuePair<MyJumpGateRemoteAntenna, byte>>();

		protected bool IsDirty = false;
		protected int LastActionTriggered { get; private set; } = 0;
		protected TargetedGateValueType TargetValue;
		protected IMyEventControllerBlock EventController => this.Entity as IMyEventControllerBlock;
		protected string MODID_PREFIX => $"{MyJumpGateModSession.MODID}.EventController.{this.EventDisplayName.String}.";

		public abstract bool IsThresholdUsed { get; }
		public abstract bool IsConditionSelectionUsed { get; }
		public abstract bool IsBlocksListUsed { get; }
		public abstract bool IsJumpGateSelectionUsed { get; }
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
				if (this._IsSelected == value) return;
				this._IsSelected = value;
				MyCubeGrid grid = (MyCubeGrid) this.EventController?.CubeGrid;

				if (value)
				{
					grid?.Schedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);
					this.OnSelected();
					this.SetDirty();
				}
				else
				{
					grid?.DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);
					this.OnUnselected();
					this.SetDirty();
				}
			}
		}

		public virtual string YesNoToolbarYesDescription => "Condition Accepted";

		public virtual string YesNoToolbarNoDescription => "Condition Rejected";

		private void OnNetworkUpdate(MyNetworkInterface.Packet packet)
		{
			MySerializedJumpGateEvent info = packet?.Payload<MySerializedJumpGateEvent>();
			if (info == null || this.Entity == null || info.EntityID != this.Entity.EntityId) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (packet.EpochTime < this.LastUpdateTimeEpoch) return;
				this.Deserialize(info.SerializedEvent);
				this.IsDirty = false;
				this.LastUpdateTimeEpoch = packet.EpochTime;
				packet.Forward(0, true).Send();
				Logger.Debug($"Updated event controller event \"{this.EventDisplayName}\" @ {this.Entity.EntityId}", 4);
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2))
			{
				this.Deserialize(info.SerializedEvent);
				this.IsDirty = false;
				this.LastUpdateTimeEpoch = packet.EpochTime;
				Logger.Debug($"Updated event controller event \"{this.EventDisplayName}\" @ {this.Entity.EntityId}", 4);
			}
		}

		private void Init()
		{
			if (this.NetworkRegistered || !MyJumpGateModSession.Network.Registered) return;
			this.NetworkRegistered = true;
			MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_EVENT_CONTROLLER_EVENT, this.OnNetworkUpdate);
		}

		private void Release()
		{
			IMyEventControllerBlock event_controller = this.EventController;
			if (event_controller != null && this._IsSelected) ((MyCubeGrid) event_controller.CubeGrid).DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, this.Update);
			if (this.NetworkRegistered && MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_EVENT_CONTROLLER_EVENT, this.OnNetworkUpdate);
			foreach (KeyValuePair<MyJumpGate, TargetedGateValueType> pair in this.TargetedJumpGates.Concat(this.TargetedRemoteJumpGates)) this.OnJumpGateRemoved(pair.Key);
			foreach (KeyValuePair<MyJumpGateRemoteAntenna, byte> pair in this.TargetedRemoteAntennas) pair.Key.OnAntennaConnection -= this.OnAntennaConnectionChanged;
			this.TargetedJumpGates.Clear();
			this.TargetedRemoteAntennas.Clear();
			this.TargetedRemoteJumpGates.Clear();
			this.NetworkRegistered = false;
		}

		private void OnAntennaConnectionChanged(MyJumpGateRemoteAntenna antenna, MyJumpGateController remote_controller, MyJumpGate remote_gate, byte channel, bool is_connecting)
		{
			if (is_connecting)
			{
				this.TargetedRemoteJumpGates[remote_gate] = this.GetValueFromJumpGate(remote_gate);
				this.OnJumpGateAdded(remote_gate);
			}
			else if (remote_gate != null)
			{
				this.TargetedRemoteJumpGates.Remove(remote_gate);
				this.OnJumpGateRemoved(remote_gate);
			}
		}

		protected virtual void AppendCustomInfo(StringBuilder sb)
		{
			IMyEventControllerBlock event_controller = this.EventController;
			string header = MyTexts.GetString("DetailedInfo_JumpGateEventBase_Header").Replace("{%0}", this.EventDisplayName.String).Replace("{%1}", this.LastActionTriggered.ToString());
			sb.Append($"{header}\n\n");
			string condition = (event_controller.IsLowerOrEqualCondition) ? "<=" : ">=";

			foreach (KeyValuePair<MyJumpGate, TargetedGateValueType> pair in this.TargetedJumpGates.Concat(this.TargetedRemoteJumpGates))
			{
				if (this.IsConditionSelectionUsed)
				{
					int compare = pair.Value.CompareTo(this.TargetValue);
					bool matched = (event_controller.IsLowerOrEqualCondition) ? (compare <= 0) : (compare >= 0);
					string entry = MyTexts.GetString("DetailedInfo_JumpGateEventBase_ConditionalEntry")
						.Replace("{%0}", pair.Key.GetPrintableName())
						.Replace("{%1}", pair.Key.JumpGateID.ToString())
						.Replace("{%2}", pair.Value.ToString())
						.Replace("{%3}", condition)
						.Replace("{%4}", this.TargetValue.ToString())
						.Replace("{%5}", (matched ? "0" : "1"));
					sb.AppendLine(entry);
				}
				else
				{
					string entry = MyTexts.GetString("DetailedInfo_JumpGateEventBase_StandardEntry")
						.Replace("{%0}", pair.Key.GetPrintableName())
						.Replace("{%1}", pair.Key.JumpGateID.ToString());
					sb.AppendLine(entry);
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
					List<MyJumpGate> closed_gates = new List<MyJumpGate>(this.TargetedJumpGates.Keys);

					foreach (long gateid in this.DeserializedInfo.SelectedJumpGates)
					{
						MyJumpGate gate = construct.GetJumpGate(gateid);
						if (gate == null || gate.Closed) continue;
						if (!this.TargetedJumpGates.ContainsKey(gate)) this.OnJumpGateAdded(gate);
						closed_gates.Remove(gate);
						this.TargetedJumpGates[gate] = this.GetValueFromJumpGate(gate);
					}

					foreach (MyJumpGate gate in closed_gates)
					{
						this.OnJumpGateRemoved(gate);
						this.TargetedJumpGates.Remove(gate);
					}
				}

				if (this.DeserializedInfo.SelectedRemoteJumpGates != null)
				{
					List<KeyValuePair<MyJumpGateRemoteAntenna, byte>> closed_antennas = new List<KeyValuePair<MyJumpGateRemoteAntenna, byte>>(this.TargetedRemoteAntennas);
					List<MyJumpGate> closed_gates = new List<MyJumpGate>(this.TargetedRemoteJumpGates.Keys);

					foreach (KeyValuePair<long, byte> pair in this.DeserializedInfo.SelectedRemoteJumpGates)
					{
						MyJumpGateRemoteAntenna antenna = construct.GetRemoteAntenna(pair.Key);
						if (antenna == null || antenna.Closed) continue;
						KeyValuePair<MyJumpGateRemoteAntenna, byte> new_pair = new KeyValuePair<MyJumpGateRemoteAntenna, byte>(antenna, pair.Value);

						if (this.TargetedRemoteAntennas.Contains(new_pair))
						{
							closed_antennas.Remove(new_pair);
						}
						else
						{
							this.TargetedRemoteAntennas.Add(new_pair);
							antenna.OnAntennaConnection += this.OnAntennaConnectionChanged;
						}

						MyJumpGate remote_gate = antenna.GetConnectedControlledJumpGate(pair.Value);
						if (remote_gate == null || remote_gate.Closed) continue;
						if (!this.TargetedRemoteJumpGates.ContainsKey(remote_gate)) this.OnJumpGateAdded(remote_gate);
						closed_gates.Remove(remote_gate);
						this.TargetedRemoteJumpGates[remote_gate] = this.GetValueFromJumpGate(remote_gate);
					}

					foreach (KeyValuePair<MyJumpGateRemoteAntenna, byte> pair in closed_antennas)
					{
						pair.Key.OnAntennaConnection -= this.OnAntennaConnectionChanged;
						this.TargetedRemoteAntennas.Remove(pair);
					}

					foreach (MyJumpGate gate in closed_gates)
					{
						this.OnJumpGateRemoved(gate);
						this.TargetedRemoteJumpGates.Remove(gate);
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
					Broadcast = MyNetworkInterface.IsMultiplayerServer,
				};

				packet.Payload(new MySerializedJumpGateEvent {
					EntityID = this.Entity.EntityId,
					SerializedEvent = this.Serialize(),
				});

				packet.Send();
				this.IsDirty = false;
			}
		}

		protected void TriggerAction(int index)
		{
			if (index == this.LastActionTriggered) return;
			this.EventController?.TriggerAction(index);
			this.LastActionTriggered = index;
		}

		protected void Poll(bool force_check)
		{
			IMyEventControllerBlock event_controller = this.EventController;
			if (MyNetworkInterface.IsStandaloneMultiplayerClient || event_controller == null || event_controller.MarkedForClose) return;
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

			closed.Clear();
			updates.Clear();

			foreach (KeyValuePair<MyJumpGate, TargetedGateValueType> pair in this.TargetedRemoteJumpGates)
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
					this.TargetedRemoteJumpGates.Remove(pair.Key);
				}
				else this.TargetedRemoteJumpGates[pair.Key] = pair.Value;
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
			if (this.IsJumpGateSelectionUsed)
			{
				IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(this.MODID_PREFIX + "JumpGate");
				choose_jump_gate_lb.Title = MyStringId.GetOrCompute(MyTexts.GetString($"Terminal_JumpGateEventBase_JumpGates"));
				choose_jump_gate_lb.SupportsMultipleBlocks = false;
				choose_jump_gate_lb.Visible = block => block.Components.Get<EventType>()?.IsSelected ?? false;
				choose_jump_gate_lb.Multiselect = true;
				choose_jump_gate_lb.VisibleRowsCount = 5;
				choose_jump_gate_lb.ListContent = (block, content_list, preselect_list) => {
					MyJumpGateEventBase<TargetedGateValueType> event_block = block.Components.Get<EventType>();
					IMyEventControllerBlock event_controller = event_block?.EventController;
					MyJumpGateConstruct construct;
					if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;

					foreach (MyJumpGate jump_gate in construct.GetJumpGates().OrderBy((gate) => gate.JumpGateID))
					{
						if (!jump_gate.Closed && this.IsJumpGateValidForList(jump_gate))
						{
							string tooltip = $"{MyTexts.GetString("Terminal_JumpGateController_ActiveJumpGateTooltip0").Replace("{%0}", jump_gate.GetJumpGateDrives().Count().ToString()).Replace("{%1}", jump_gate.JumpGateID.ToString())}";
							MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute(tooltip), jump_gate);
							content_list.Add(item);
							if (event_block.TargetedJumpGates.ContainsKey(jump_gate)) preselect_list.Add(item);
						}
					}

					foreach (MyJumpGateRemoteAntenna antenna in construct.GetAttachedJumpGateRemoteAntennas())
					{
						string name = ((antenna.TerminalBlock?.CustomName?.Length ?? 0) == 0) ? antenna.BlockID.ToString() : antenna.TerminalBlock.CustomName;

						for (byte channel = 0; channel < MyJumpGateRemoteAntenna.ChannelCount; ++channel)
						{
							if (antenna.Closed) continue;
							KeyValuePair<MyJumpGateRemoteAntenna, byte> pair = new KeyValuePair<MyJumpGateRemoteAntenna, byte>(antenna, channel);
							string tooltip = MyTexts.GetString("Terminal_JumpGateController_ActiveJumpGateTooltip1").Replace("{%0}", channel.ToString());
							MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{name}"), MyStringId.GetOrCompute(tooltip), pair);
							content_list.Add(item);
							if (event_block.TargetedRemoteAntennas.Contains(pair)) preselect_list.Add(item);
						}
					}
				};
				choose_jump_gate_lb.ItemSelected = (block, selected) => {
					MyJumpGateEventBase<TargetedGateValueType> event_block = block.Components.Get<EventType>();
					IMyEventControllerBlock event_controller = event_block?.EventController;
					MyJumpGateConstruct construct;
					if (event_controller == null || event_controller.MarkedForClose || (construct = MyJumpGateModSession.Instance.GetJumpGateGrid(event_controller.CubeGrid)) == null) return;
					List<MyJumpGate> removed_gates = new List<MyJumpGate>(event_block.TargetedJumpGates.Keys);
					List<KeyValuePair<MyJumpGateRemoteAntenna, byte>> removed_remote_gates = new List<KeyValuePair<MyJumpGateRemoteAntenna, byte>>(event_block.TargetedRemoteAntennas);

					foreach (MyTerminalControlListBoxItem item in selected)
					{
						if (item.UserData is MyJumpGate)
						{
							MyJumpGate jump_gate = (MyJumpGate) item.UserData;
							if (jump_gate == null || jump_gate.Closed || event_block.TargetedJumpGates.ContainsKey(jump_gate)) continue;
							event_block.TargetedJumpGates[jump_gate] = event_block.GetValueFromJumpGate(jump_gate);
							event_block.OnJumpGateAdded(jump_gate);
							removed_gates.Remove(jump_gate);
						}
						else if (item.UserData is KeyValuePair<MyJumpGateRemoteAntenna, byte>)
						{
							KeyValuePair<MyJumpGateRemoteAntenna, byte> pair = (KeyValuePair<MyJumpGateRemoteAntenna, byte>) item.UserData;
							if (pair.Key == null || pair.Key.Closed || pair.Value >= MyJumpGateRemoteAntenna.ChannelCount || event_block.TargetedRemoteAntennas.Contains(pair)) continue;
							event_block.TargetedRemoteAntennas.Add(pair);
							removed_remote_gates.Remove(pair);
							MyJumpGate remote_gate = pair.Key.GetConnectedControlledJumpGate(pair.Value);
							if (remote_gate == null) continue;
							event_block.OnJumpGateAdded(remote_gate);
							event_block.TargetedRemoteJumpGates[remote_gate] = event_block.GetValueFromJumpGate(remote_gate);
						}
					}

					foreach (MyJumpGate closed in removed_gates)
					{
						event_block.OnJumpGateRemoved(closed);
						event_block.TargetedJumpGates.Remove(closed);
					}

					foreach (KeyValuePair<MyJumpGateRemoteAntenna, byte> pair in removed_remote_gates)
					{
						event_block.TargetedRemoteAntennas.Remove(pair);
						MyJumpGate remote_gate = pair.Key.GetConnectedControlledJumpGate(pair.Value);
						if (remote_gate == null) continue;
						event_block.OnJumpGateRemoved(remote_gate);
						event_block.TargetedRemoteJumpGates.Remove(remote_gate);
					}

					event_block.Poll(true);
					event_block.SetDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<T>(choose_jump_gate_lb);
			}
		}

		protected virtual void OnSave(MySerializedJumpGateEventInfo info) { }

		protected virtual void OnLoad(MySerializedJumpGateEventInfo info) { }

		protected virtual void OnSelected()
		{
			foreach (KeyValuePair<MyJumpGateRemoteAntenna, byte> pair in this.TargetedRemoteAntennas) pair.Key.OnAntennaConnection += this.OnAntennaConnectionChanged;
		}

		protected virtual void OnUnselected()
		{
			foreach (KeyValuePair<MyJumpGateRemoteAntenna, byte> pair in this.TargetedRemoteAntennas) pair.Key.OnAntennaConnection -= this.OnAntennaConnectionChanged;
		}

		protected virtual void OnJumpGateAdded(MyJumpGate jump_gate) { }

		protected virtual void OnJumpGateRemoved(MyJumpGate jump_gate) { }

		protected abstract void CheckValueAgainstTarget(TargetedGateValueType new_value, TargetedGateValueType old_value, TargetedGateValueType target);

		protected virtual bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return jump_gate != null && !jump_gate.Closed;
		}

		protected bool IsListeningToJumpGate(MyJumpGate jump_gate)
		{
			return jump_gate != null && (this.TargetedJumpGates.ContainsKey(jump_gate) || this.TargetedRemoteJumpGates.ContainsKey(jump_gate));
		}

		protected virtual TargetedGateValueType GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			return default(TargetedGateValueType);
		}

		protected List<MyJumpGate> GetTargetedJumpGates()
		{
			return this.TargetedJumpGates.Concat(this.TargetedRemoteJumpGates).Select((pair) => pair.Key).ToList();
		}

		public MyJumpGateEventBase()
		{
			this.Init();
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
				SelectedRemoteJumpGates = this.TargetedRemoteAntennas.Select((pair) => new KeyValuePair<long, byte>(pair.Key.BlockID, pair.Value)).ToList(),
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
