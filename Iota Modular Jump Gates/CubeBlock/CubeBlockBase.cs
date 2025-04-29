using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace IOTA.ModularJumpGates.CubeBlock
{
	/// <summary>
	/// Class representing the base logic component for jump gate blocks
	/// </summary>
	internal class MyCubeBlockBase : MyGameLogicComponent
	{
		#region Public Static Variables
		/// <summary>
		/// The number of game ticks between network block syncs
		/// </summary>
		public static readonly uint ForceUpdateDelay = 300;

		/// <summary>
		/// The radii of this block's model
		/// </summary>
		public static Vector3D ModelBoundingBoxSize { get; private set; } = Vector3D.Zero;

		/// <summary>
		/// Master map of all Iota cube block instances
		/// </summary>
		public static readonly ConcurrentDictionary<long, MyCubeBlockBase> Instances = new ConcurrentDictionary<long, MyCubeBlockBase>();
		#endregion

		#region Private Variables
		/// <summary>
		/// The offset scroll value for this block's detailed info
		/// </summary>
		private int ScrollOffset = 0;

		/// <summary>
		/// The serialized data from server used to grab various values if this block is unloaded<br />
		/// Not used on server or singleplayer
		/// </summary>
		private MySerializedCubeBlockBase SerializedWrapperInfo = null;

		/// <summary>
		/// The block ID of this block if this block is a wrapper
		/// </summary>
		private long SuspendedTerminalBlockID;

		/// <summary>
		/// The world matrix of this block
		/// </summary>
		private MatrixD BlockWorldMatrix;

		/// <summary>
		/// The last detailed info string
		/// </summary>
		private string LastDetailedInfoString = "";
		#endregion

		#region Protected Variables
		/// <summary>
		/// If true, block requires full required power setting to be powered
		/// </summary>
		protected bool RequiresFullInput { get; private set; } = true;

		/// <summary>
		/// The time (in milliseconds since epoch) this block received it's last update
		/// </summary>
		protected ulong LastUpdateTime = 0;

		/// <summary>
		/// The block's mod storage component
		/// </summary>
		protected MyModStorageComponentBase ModStorageComponent { get; private set; }

		/// <summary>
		/// The block's resource sink
		/// </summary>
		protected MyResourceSinkComponent ResourceSink { get; private set; }
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this block should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty { get; protected set; } = false;

		/// <summary>
		/// Whether this block is large grid
		/// </summary>
		public bool IsLargeGrid { get; private set; }

		/// <summary>
		/// Whether this block is small grid
		/// </summary>
		public bool IsSmallGrid { get; private set; }

		/// <summary>
		/// Whether this is a wrapper around a block that isn't initialized<br />
		/// Always false on singleplayer or server
		/// </summary>
		public bool IsNullWrapper => this.TerminalBlock == null;

		/// <summary>
		/// The block ID of this block
		/// </summary>
		public long BlockID => this.TerminalBlock?.EntityId ?? this.SuspendedTerminalBlockID;

		/// <summary>
		/// The cube grid ID of this block
		/// </summary>
		public long CubeGridID => this.TerminalBlock?.CubeGrid.EntityId ?? this.SerializedWrapperInfo?.CubeGridID ?? -1;

		/// <summary>
		/// The construct ID of this block
		/// </summary>
		public long ConstructID => this.JumpGateGrid?.CubeGridID ?? this.SerializedWrapperInfo?.JumpGateGridID ?? -1;

		/// <summary>
		/// The player ID of this block's owner
		/// </summary>
		public long OwnerID => this.TerminalBlock?.OwnerId ?? this.SerializedWrapperInfo?.OwnerID ?? -1;

		/// <summary>
		/// The steam ID of this block's owner
		/// </summary>
		public ulong OwnerSteamID => (this.TerminalBlock == null) ? (this.SerializedWrapperInfo?.SteamOwnerID ?? 0) : MyAPIGateway.Players.TryGetSteamId(this.TerminalBlock.OwnerId);

		/// <summary>
		/// The block local game tick
		/// </summary>
		public ulong LocalGameTick { get; private set; } = 0;

		/// <summary>
		/// The grid size of this block
		/// </summary>
		public MyCubeSize CubeGridSize { get; private set; }

		/// <summary>
		/// The terminal block this component is bound to
		/// </summary>
		public IMyUpgradeModule TerminalBlock { get; private set; }

		/// <summary>
		/// The UTC date time representation of CubeBlockBase.LastUpdateTime
		/// </summary>
		public DateTime LastUpdateDateTimeUTC
		{
			get
			{
				return (DateTime.MinValue.ToUniversalTime() + new TimeSpan((long) this.LastUpdateTime));
			}

			set
			{
				this.LastUpdateTime = (ulong) (value.ToUniversalTime() - DateTime.MinValue).Ticks;
			}
		}

		/// <summary>
		/// The world matrix of this block
		/// </summary>
		public MatrixD WorldMatrix => this.TerminalBlock?.WorldMatrix ?? this.BlockWorldMatrix;

		/// <summary>
		/// The jump gate grid this component is bound to
		/// </summary>
		public MyJumpGateConstruct JumpGateGrid { get; private set; }
		#endregion

		#region Constructors
		public MyCubeBlockBase() : base() { }

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		public MyCubeBlockBase(MySerializedCubeBlockBase serialized) : base()
		{
			this.SuspendedTerminalBlockID = JumpGateUUID.FromGuid(serialized.UUID).GetBlock();
			this.FromSerialized(serialized);
		}
		#endregion

		#region "MyGameLogicComponent" Methods
		/// <summary>
		/// MyGameLogicComponent method (Overridable)<br />
		/// Called once before first tick<br />
		/// Sets the terminal block model bounding box size
		/// </summary>
		public override void UpdateOnceBeforeFrame()
		{
			base.UpdateOnceBeforeFrame();
			MyCubeBlockBase.ModelBoundingBoxSize = (MyCubeBlockBase.ModelBoundingBoxSize == Vector3D.Zero && !this.IsNullWrapper) ? new Vector3D(this.TerminalBlock.Model.BoundingBoxSize) : MyCubeBlockBase.ModelBoundingBoxSize;
			MyCubeBlockTerminal.Load(this.ModContext);
			MyJumpGateControllerTerminal.Load(this.ModContext);
			MyJumpGateCapacitorTerminal.Load(this.ModContext);
			MyJumpGateDriveTerminal.Load(this.ModContext);
		}

		/// <summary>
		/// MyGameLogicComponent method (Overridable)<br />
		/// Called every tick before simulation<br />
		/// Updates the power requirments for this block<br />
		/// Updates the attached JummpGateGrid
		/// </summary>
		public override void UpdateBeforeSimulation()
		{
			base.UpdateBeforeSimulation();
			if (this.TerminalBlock == null || this.TerminalBlock?.CubeGrid?.Physics == null) return;
			this.ResourceSink?.Update();

			if (!this.IsClosed() && (this.JumpGateGrid == null || !MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(this.JumpGateGrid) || !this.JumpGateGrid.HasCubeGrid(this.TerminalBlock.CubeGrid)))
			{
				MyJumpGateConstruct new_construct = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(this.TerminalBlock.CubeGrid);
				if (new_construct != this.JumpGateGrid) this.OnConstructChanged();
				this.JumpGateGrid = new_construct;
			}
		}

		/// <summary>
		/// MyGameLogicComponent method (Overridable)<br />
		/// Called every tick after simulation<br />
		/// Updates the power requirments for this block<br />
		/// Updates the detailed info string
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();
			if (this.TerminalBlock == null || this.TerminalBlock.MarkedForClose) return;
			++this.LocalGameTick;
			if (MyAPIGateway.Gui.GetCurrentScreen != VRage.Game.ModAPI.MyTerminalPageEnum.ControlPanel) return;
			this.TerminalBlock.RefreshCustomInfo();
			this.TerminalBlock.SetDetailedInfoDirty();
		}

		/// <summary>
		/// Marks this block for removal
		/// </summary>
		public override void MarkForClose()
		{
			base.MarkForClose();

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsMultiplayerServer)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.CLOSE_BLOCK,
					TargetID = 0,
					Broadcast = true,
				};
				packet.Payload(this.BlockID);
				packet.Send();
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.CLOSE_BLOCK, this.OnNetworkBlockClose);
			}

			MyCubeBlockBase.Instances.Remove(this.BlockID);
		}

		public override void Close()
		{
			this.Clean();
			base.Close();
		}
		#endregion

		#region "object" Methods
		/// <summary>
		/// Checks if this block equals another
		/// </summary>
		/// <param name="obj">The object to check</param>
		/// <returns>Equality</returns>
		public override bool Equals(object obj)
		{
			return obj != null && obj is MyCubeBlockBase && ((MyCubeBlockBase) obj).BlockID == this.BlockID;
		}

		/// <summary>
		/// Gets the hashcode for this object
		/// </summary>
		/// <returns>This terminal block's hashcode</returns>
		public override int GetHashCode()
		{
			return (int) (this.BlockID & uint.MaxValue);
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Wrapper for writing info to the terminal block's detailed info section
		/// </summary>
		/// <param name="block">The terminal block to write to</param>
		/// <param name="sb">The "detailed info" string builder</param>
		private void _AppendCustomInfo(IMyTerminalBlock block, StringBuilder sb)
		{
			if (block != null && block == this.TerminalBlock)
			{
				StringBuilder local_sb = new StringBuilder();
				bool working = this.IsWorking();

				if (!working && this.TerminalBlock.IsFunctional) local_sb.Append("\n[color=#FFFF0000]- OFFLINE -[/color]\n");
				else if (!working)
				{
					Random random = new Random((int) this.LocalGameTick);
					foreach (char c in "\n- OFFLINE -\n") local_sb.Append($"[color=#FF{(byte) (255 - random.NextDouble() * 255):X2}0000]{c}[/color]");
				}

				try
				{
					int count = local_sb.Length;
					this.AppendCustomInfo(local_sb);
					string result = local_sb.ToString();
					this.LastDetailedInfoString = (result.Length == count) ? this.LastDetailedInfoString : result;
				}
				catch (NullReferenceException e)
				{
					local_sb.Append($"Detailed Info Error:\n{e.Message}\n ... \n{e.StackTrace}");
				}

				string[] lines = this.LastDetailedInfoString.Trim(' ', '\t', '\n').Split('\n');
				int max_length = 9;

				if (lines.Length <= max_length)
				{
					sb.AppendStringBuilder(local_sb);
					return;
				}

				string last_color = "";
				int scroll = Math.Min(lines.Length - max_length, this.ScrollOffset);

				for (int i = scroll; i > 0; --i)
				{
					string line = lines[i - 1];
					int null_color = line.LastIndexOf("[/color]");
					int color = line.LastIndexOf("[color=#");
					
					if (null_color > color)
					{
						last_color = "";
						break;
					}
					else if (color > null_color)
					{
						last_color = line.Substring(color, 17);
						break;
					}
				}

				if (scroll > 0) sb.AppendLine(new string('^', 30));
				else sb.Append('\n');
				sb.Append(last_color);

				for (int i = scroll; i < scroll + max_length; ++i)
				{
					string line = lines[i];
					sb.AppendLine(line);
					int null_color = line.LastIndexOf("[/color]");
					int color = line.LastIndexOf("[color=#");
					if (null_color > color) last_color = "";
					else if (color > null_color) last_color = "[/color]";
				}
				
				sb.Append(last_color);
				if (scroll + max_length < lines.Length) sb.AppendLine(new string('v', 30));
				this.ScrollOffset = scroll;
			}
		}

		/// <summary>
		/// Callback handing server-side block closure event
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkBlockClose(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PhaseFrame != 1 || packet.SenderID != 0 || this.TerminalBlock == null || this.TerminalBlock.MarkedForClose || packet.Payload<long>() != this.TerminalBlock.EntityId) return;
			this.TerminalBlock.Close();
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Overridable<br />
		/// Override this to add info to the detailed info screen<br />
		/// Checks are already done to ensure the block is this
		/// </summary>
		/// <param name="sb">The string builder to append your information to</param>
		protected virtual void AppendCustomInfo(StringBuilder sb) { }

		/// <summary>
		/// Cleans this block<br />
		/// Blocks should not be used after this point<br />
		/// Should be called only on block close
		/// </summary>
		protected virtual void Clean()
		{
			this.ModStorageComponent = null;
			this.ResourceSink = null;
			this.JumpGateGrid = null;
			this.SerializedWrapperInfo = null;
		}

		/// <summary>
		/// Overridable<br />
		/// Init method for initializing a new Iota cube block
		/// </summary>
		/// <param name="object_builder">Object builder as given</param>
		/// <param name="update_enum">The update order of the block (updates every frame regardless to update power usage)</param>
		/// <param name="power_drain_mw">How much static power to draw in MW</param>
		/// <param name="entity_component_guid">The mod storage GUID</param>
		/// <param name="requires_full_input">If true, block requires full specified input power to function</param>
		protected void Init(MyObjectBuilder_EntityBase object_builder, MyEntityUpdateEnum update_enum, float power_drain_mw = 0, Guid entity_component_guid = new Guid(), bool requires_full_input = true)
		{
			base.Init(object_builder);
			this.TerminalBlock = this.Entity as IMyUpgradeModule;
			this.CubeGridSize = this.TerminalBlock.CubeGrid.GridSizeEnum;
			this.IsLargeGrid = this.TerminalBlock.CubeGrid.GridSizeEnum == MyCubeSize.Large;
			this.IsSmallGrid = this.TerminalBlock.CubeGrid.GridSizeEnum == MyCubeSize.Small;
			this.TerminalBlock.AppendingCustomInfo += this._AppendCustomInfo;
			this.NeedsUpdate = update_enum | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
			this.RequiresFullInput = requires_full_input;
			if (this.TerminalBlock.Storage == null) this.TerminalBlock.Storage = (entity_component_guid == Guid.Empty) ? null : new MyModStorageComponent { [entity_component_guid] = "" };
			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsMultiplayerClient) MyJumpGateModSession.Network.On(MyPacketTypeEnum.CLOSE_BLOCK, this.OnNetworkBlockClose);
			this.ModStorageComponent = this.TerminalBlock.Storage;

			if (this.TerminalBlock.ResourceSink == null)
			{
				this.ResourceSink = new MyResourceSinkComponent();
				MyResourceSinkInfo sink_info = new MyResourceSinkInfo();
				sink_info.ResourceTypeId = MyResourceDistributorComponent.ElectricityId;
				sink_info.RequiredInputFunc = () => power_drain_mw;
				sink_info.MaxRequiredInput = 0;
				
				this.ResourceSink.Init(VRage.Utils.MyStringHash.GetOrCompute("Utility"), sink_info, this.Entity as MyCubeBlock);
				this.TerminalBlock.Components.Add(this.ResourceSink);
			}
			else
			{
				this.ResourceSink = this.TerminalBlock.Components.Get<MyResourceSinkComponent>();
				this.ResourceSink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, () => power_drain_mw);
				this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, 0);
				this.ResourceSink.Update();
			}

			this.TerminalBlock.Init();
			MyCubeBlockBase.Instances.TryAdd(this.TerminalBlock.EntityId, this);
		}

		/// <summary>
		/// Overridable<br />
		/// Init method for initializing a new Iota cube block
		/// </summary>
		/// <param name="object_builder">Object builder as given</param>
		/// <param name="update_enum">The update order of the block (updates every frame regardless to update power usage)</param>
		/// <param name="power_draw_mw">How much power to draw in MW (evaluated every tick before simulation)</param>
		/// <param name="entity_component_guid">The mod storage GUID</param>
		/// <param name="requires_full_input">If true, block requires full specified input power to function</param>
		protected void Init(MyObjectBuilder_EntityBase object_builder, MyEntityUpdateEnum update_enum, Func<float> power_draw_mw = null, Guid entity_component_guid = new Guid(), bool requires_full_input = true)
		{
			base.Init(object_builder);
			this.TerminalBlock = this.Entity as IMyUpgradeModule;
			this.CubeGridSize = this.TerminalBlock.CubeGrid.GridSizeEnum;
			this.IsLargeGrid = this.TerminalBlock.CubeGrid.GridSizeEnum == MyCubeSize.Large;
			this.IsSmallGrid = this.TerminalBlock.CubeGrid.GridSizeEnum == MyCubeSize.Small;
			this.TerminalBlock.AppendingCustomInfo += this._AppendCustomInfo;
			this.NeedsUpdate = update_enum | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
			this.RequiresFullInput = requires_full_input;
			if (this.TerminalBlock.Storage == null) this.TerminalBlock.Storage = (entity_component_guid == Guid.Empty) ? null : new MyModStorageComponent { [entity_component_guid] = "" };
			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsMultiplayerClient) MyJumpGateModSession.Network.On(MyPacketTypeEnum.CLOSE_BLOCK, this.OnNetworkBlockClose);
			this.ModStorageComponent = this.TerminalBlock.Storage;

			if (this.TerminalBlock.ResourceSink == null)
			{
				this.ResourceSink = new MyResourceSinkComponent();
				MyResourceSinkInfo sink_info = new MyResourceSinkInfo();
				sink_info.ResourceTypeId = MyResourceDistributorComponent.ElectricityId;
				sink_info.RequiredInputFunc = power_draw_mw == null ? () => 0 : power_draw_mw;
				sink_info.MaxRequiredInput = 0;

				this.ResourceSink.Init(VRage.Utils.MyStringHash.GetOrCompute("Utility"), sink_info, this.Entity as MyCubeBlock);
				this.ResourceSink.AddType(ref sink_info);
				this.TerminalBlock.Components.Add(this.ResourceSink);
			}
			else
			{
				this.ResourceSink = this.TerminalBlock.Components.Get<MyResourceSinkComponent>();
				this.ResourceSink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, power_draw_mw == null ? () => 0 : power_draw_mw);
				this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, 0);
				this.ResourceSink.Update();
			}

			this.TerminalBlock.Init();
			MyCubeBlockBase.Instances.TryAdd(this.TerminalBlock.EntityId, this);
		}

		/// <summary>
		/// Overridable<br />
		/// Called when this block's cube grid changes
		/// </summary>
		protected virtual void OnConstructChanged() { }
		#endregion

		#region Public Methods
		/// <summary>
		/// Sets the internal "IsDirty" flag to true
		/// <br />
		/// Marks block for sync on next component tick
		/// </summary>
		public void SetDirty()
		{
			this.IsDirty = true;
			this.LastUpdateDateTimeUTC = DateTime.UtcNow;
		}

		/// <summary>
		/// Sets this block's detailed info scroll offset
		/// </summary>
		/// <param name="scroll_y">The number of lines scrolled</param>
		public void SetScroll(int scroll_y)
		{
			this.ScrollOffset = Math.Max(0, scroll_y);
		}

		/// <summary>
		/// Gets whether this component or it's attached block is closed
		/// </summary>
		/// <returns>Closedness</returns>
		public bool IsClosed()
		{
			if (this.IsNullWrapper) return this.SerializedWrapperInfo?.IsClosed ?? true;
			else return this.TerminalBlock == null || this.TerminalBlock.Closed || this.TerminalBlock.MarkedForClose || this.TerminalBlock.CubeGrid?.Physics == null;
		}

		/// <summary>
		/// Gets whether block is powered
		/// </summary>
		/// <returns>Whether current input >= required input</returns>
		public bool IsPowered()
		{
			if (this.IsNullWrapper) return this.SerializedWrapperInfo?.IsPowered ?? false;
			else if (this.ResourceSink == null) return true;
			else if (this.RequiresFullInput) return this.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) >= this.ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId);
			else return this.ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) <= 0 || this.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) > 0;
		}

		/// <summary>
		/// Gets whether block is enabled
		/// </summary>
		/// <returns>Enabledness</returns>
		public bool IsEnabled()
		{
			if (this.IsNullWrapper) return this.SerializedWrapperInfo?.IsEnabled ?? false;
			else return this.TerminalBlock.Enabled;
		}

		/// <summary>
		/// Gets block's working status: (not closed, enabled, powered)
		/// </summary>
		/// <returns>Workingness</returns>
		public bool IsWorking()
		{
			if (this.IsNullWrapper) return this.SerializedWrapperInfo?.IsWorking ?? false;
			else return !this.IsClosed() && this.IsEnabled() && this.IsPowered() && this.TerminalBlock.IsFunctional;
		}

		/// <summary>
		/// Updates this block data from a serialized block
		/// </summary>
		/// <param name="block">The serialized block data</param>
		/// <returns>Whether this block was updated</returns>
		public bool FromSerialized(MySerializedCubeBlockBase block)
		{
			if (JumpGateUUID.FromGuid(block.UUID).GetBlock() != this.BlockID || block.IsClientRequest) return false;
			this.SerializedWrapperInfo = block;
			this.BlockWorldMatrix = BoundingEllipsoidD.FromSerialized(Convert.FromBase64String(block.WorldMatrix), 0).WorldMatrix;
			this.JumpGateGrid = MyJumpGateModSession.Instance.GetJumpGateGrid(block.JumpGateGridID);
			return true;
		}

		/// <summary>
		/// Scrolls this block's detailed info
		/// </summary>
		/// <param name="scroll_y">The number of lines to scroll</param>
		/// <returns>The new absolute scroll offset</returns>
		public int Scroll(int scroll_y)
		{
			this.ScrollOffset = Math.Max(0, this.ScrollOffset - scroll_y);
			return this.ScrollOffset;
		}

		public MySerializedCubeBlockBase ToSerialized(bool as_client_request)
		{
			return this.ToSerialized<MySerializedCubeBlockBase>(as_client_request);
		}

		public T ToSerialized<T>(bool as_client_request) where T : MySerializedCubeBlockBase, new()
		{
			return (as_client_request) ?
				new T {
					UUID = JumpGateUUID.FromBlock(this).ToGuid(),
					IsClientRequest = true,
				} :
				new T {
					UUID = JumpGateUUID.FromBlock(this).ToGuid(),
					IsPowered = this.IsPowered(),
					IsEnabled = this.IsEnabled(),
					IsWorking = this.IsWorking(),
					IsClosed = this.IsClosed(),
					WorldMatrix = Convert.ToBase64String(new BoundingEllipsoidD(Vector3D.Zero, this.WorldMatrix).ToSerialized()),
					CubeGridID = this.CubeGridID,
					JumpGateGridID = this.ConstructID,
					OwnerID = this.OwnerID,
					SteamOwnerID = this.OwnerSteamID,
					IsClientRequest = false,
				};
		}
		#endregion
	}

	[ProtoContract]
	[ProtoInclude(100, typeof(MySerializedJumpGateController))]
	[ProtoInclude(200, typeof(MySerializedJumpGateCapacitor))]
	[ProtoInclude(300, typeof(MySerializedJumpGateDrive))]
	[ProtoInclude(400, typeof(MySerializedJumpGateServerAntenna))]
	internal class MySerializedCubeBlockBase
	{
		/// <summary>
		/// The block's JumpGateUUID as a Guid
		/// </summary>
		[ProtoMember(1)]
		public Guid UUID;

		/// <summary>
		/// If true, this data should be used by server to identify block and send updated data
		/// </summary>
		[ProtoMember(2)]
		public bool IsClientRequest;

		/// <summary>
		/// Whether the block is powered
		/// </summary>
		[ProtoMember(3)]
		public bool IsPowered;

		/// <summary>
		/// Whether the block is enabled
		/// </summary>
		[ProtoMember(4)]
		public bool IsEnabled;

		/// <summary>
		/// Whether the block is working
		/// </summary>
		[ProtoMember(5)]
		public bool IsWorking;

		/// <summary>
		/// Whether this block is closed
		/// </summary>
		[ProtoMember(6)]
		public bool IsClosed;

		/// <summary>
		/// The world matrix of this block
		/// </summary>
		[ProtoMember(7)]
		public string WorldMatrix;

		/// <summary>
		/// The cube grid ID of this block
		/// </summary>
		[ProtoMember(8)]
		public long CubeGridID;

		/// <summary>
		/// The construct ID of this block
		/// </summary>
		[ProtoMember(9)]
		public long JumpGateGridID;

		/// <summary>
		/// The player ID of this block's owner
		/// </summary>
		[ProtoMember(10)]
		public long OwnerID;

		/// <summary>
		/// The steam ID of this block's owner
		/// </summary>
		[ProtoMember(11)]
		public ulong SteamOwnerID;
	}
}
