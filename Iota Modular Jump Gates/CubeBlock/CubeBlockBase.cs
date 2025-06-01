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
using VRage;
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
	internal class MyCubeBlockBase : MyGameLogicComponent, IEquatable<MyCubeBlockBase>
	{
		public struct MyBlockPowerInfo
		{
			public double CurrentInput;
			public double RequiredInput;
			public double MaxRequiredInput;
		}

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
		/// Whether "UpdateOnceAfterInit" may be called<br />
		/// Will be false as long as block didn't complete "UpdateOnceBeforeFrame"
		/// </summary>
		private bool InitFrameAvailable = false;

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
		/// Contains information on this block's current power draw
		/// </summary>
		private MyBlockPowerInfo BlockPowerInfo = new MyBlockPowerInfo();

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
		/// True if this block component is initialized
		/// </summary>
		public readonly bool Constructed = false;

		/// <summary>
		/// Whether this block's "UpdateOnceAfterInit" was called
		/// </summary>
		public bool IsInitFrameCalled { get; private set; } = false;

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
		/// Whether this component or it's attached block is closed
		/// </summary>
		public virtual bool IsClosed => (this.IsNullWrapper) ? (this.SerializedWrapperInfo?.IsClosed ?? true) : (this.TerminalBlock.Closed || this.TerminalBlock.MarkedForClose || this.TerminalBlock.CubeGrid?.Physics == null);

		/// <summary>
		/// Whether block is powered<br />
		/// <i>"current input >= required input"</i>
		/// </summary>
		public virtual bool IsPowered
		{
			get
			{
				if (this.IsNullWrapper) return this.SerializedWrapperInfo?.IsPowered ?? false;
				else if (this.ResourceSink == null) return true;
				else if (this.RequiresFullInput) return this.BlockPowerInfo.CurrentInput >= this.BlockPowerInfo.RequiredInput;
				else return this.BlockPowerInfo.RequiredInput <= 0 || this.BlockPowerInfo.CurrentInput > 0;
			}
		}

		/// <summary>
		/// Whether block is enabled
		/// </summary>
		public virtual bool IsEnabled => (this.IsNullWrapper) ? (this.SerializedWrapperInfo?.IsEnabled ?? false) : this.TerminalBlock.Enabled;

		/// <summary>
		/// Whether this block is working: (not closed, enabled, powered)
		/// </summary>
		public virtual bool IsWorking => (this.IsNullWrapper) ? (this.SerializedWrapperInfo?.IsWorking ?? false) : (!this.IsClosed && this.IsEnabled && this.IsPowered && this.TerminalBlock.IsFunctional);

		/// <summary>
		/// Whether this block is marked for close
		/// </summary>
		public virtual new bool MarkedForClose => base.MarkedForClose || this.Closed || this.IsClosed;

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

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first MyCubeBlockBase operand</param>
		/// <param name="b">The second MyCubeBlockBase operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(MyCubeBlockBase a, MyCubeBlockBase b)
		{
			if (object.ReferenceEquals(a, b)) return true;
			else if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
			else if (object.ReferenceEquals(b, null)) return object.ReferenceEquals(a, null);
			else return a.Equals(b);
		}

		/// <summary>
		/// Overloads inequality operator "!=" to check inequality
		/// </summary>
		/// <param name="a">The first MyCubeBlockBase operand</param>
		/// <param name="b">The second MyCubeBlockBase operand</param>
		/// <returns>Inequality</returns>
		public static bool operator !=(MyCubeBlockBase a, MyCubeBlockBase b)
		{
			return !(a == b);
		}
		#endregion

		#region Constructors
		public MyCubeBlockBase() : base()
		{
			this.Constructed = true;
		}

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		public MyCubeBlockBase(MySerializedCubeBlockBase serialized, MyJumpGateConstruct parent = null) : base()
		{
			this.SuspendedTerminalBlockID = serialized.UUID.GetBlock();
			this.FromSerialized(serialized, parent);
			this.Constructed = true;
			if (this.JumpGateGrid == null) throw new NullReferenceException("Parent construct was null");
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
			this.InitFrameAvailable = true;
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

			if (!this.IsClosed && (this.JumpGateGrid == null || !MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(this.JumpGateGrid) || !this.JumpGateGrid.HasCubeGrid(this.TerminalBlock.CubeGrid)))
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
			++this.LocalGameTick;

			if (!this.IsInitFrameCalled && MyJumpGateModSession.Instance.InitializationComplete && this.InitFrameAvailable)
			{
				this.UpdateOnceAfterInit();
				this.IsInitFrameCalled = true;
				Logger.Debug($"[{this.BlockID}] ({this.ToString()}) UPDATE_ONCE_AFTER_INIT", 5);
			}

			if (this.ResourceSink != null)
			{
				MyDefinitionId id = MyResourceDistributorComponent.ElectricityId;
				this.BlockPowerInfo.CurrentInput = this.ResourceSink.CurrentInputByType(id);
				this.BlockPowerInfo.RequiredInput = this.ResourceSink.RequiredInputByType(id);
				this.BlockPowerInfo.MaxRequiredInput = this.ResourceSink.MaxRequiredInputByType(id);
			}

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
			return obj is MyCubeBlockBase && this.Equals((MyCubeBlockBase) obj);
		}

		/// <summary>
		/// Checks if this block equals another
		/// </summary>
		/// <param name="obj">The object to check</param>
		/// <returns>Equality</returns>
		public bool Equals(MyCubeBlockBase other)
		{
			return object.ReferenceEquals(this, other) || (other != null && other.BlockID == this.BlockID);
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
				bool working = this.IsWorking;

				if (!working && this.TerminalBlock.IsFunctional) local_sb.Append($"\n[color=#FFFF0000]- {MyTexts.GetString("GeneralText_Offline")} -[/color]\n");
				else if (!working)
				{
					Random random = new Random((int) this.LocalGameTick);
					foreach (char c in $"\n- {MyTexts.GetString("GeneralText_Offline")} -\n") local_sb.Append($"[color=#FF{(byte) (255 - random.NextDouble() * 255):X2}0000]{c}[/color]");
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

		/// <summary>
		/// Overridable<br />
		/// Called once when all session constructs are initialized
		/// </summary>
		protected virtual void UpdateOnceAfterInit() { }
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
		/// Reloads this block's internal configuration values if applicable
		/// </summary>
		public virtual void ReloadConfigurations() { }

		/// <summary>
		/// Updates this block data from a serialized block
		/// </summary>
		/// <param name="block">The serialized block data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		/// <returns>Whether this block was updated</returns>
		public bool FromSerialized(MySerializedCubeBlockBase block, MyJumpGateConstruct parent = null)
		{
			if (block == null || (this.Constructed && block.UUID.GetBlock() != this.BlockID) || block.IsClientRequest) return false;
			this.SerializedWrapperInfo = block;
			this.BlockWorldMatrix = MatrixD.CreateWorld(block.WorldMatrix[0], block.WorldMatrix[1], block.WorldMatrix[2]);
			this.JumpGateGrid = parent ?? MyJumpGateModSession.Instance.GetJumpGateGrid(block.JumpGateGridID) ?? this.JumpGateGrid;
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
					UUID = JumpGateUUID.FromBlock(this),
					IsClientRequest = true,
				} :
				new T {
					UUID = JumpGateUUID.FromBlock(this),
					IsPowered = this.IsPowered,
					IsEnabled = this.IsEnabled,
					IsWorking = this.IsWorking,
					IsClosed = this.IsClosed,
					WorldMatrix = new Vector3D[3] { this.WorldMatrix.Translation, this.WorldMatrix.Forward, this.WorldMatrix.Up },
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
	[ProtoInclude(400, typeof(MySerializedJumpGateRemoteAntenna))]
	[ProtoInclude(500, typeof(MySerializedJumpGateServerAntenna))]
	internal class MySerializedCubeBlockBase
	{
		/// <summary>
		/// The block's JumpGateUUID
		/// </summary>
		[ProtoMember(1)]
		public JumpGateUUID UUID;

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
		public Vector3D[] WorldMatrix;

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
