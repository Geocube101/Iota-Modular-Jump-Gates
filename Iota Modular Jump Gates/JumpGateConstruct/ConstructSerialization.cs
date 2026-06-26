using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	internal partial class MyJumpGateConstruct
	{
		#region Public Methods
		/// <summary>
		/// Updates this construct data from a serialized controller
		/// </summary>
		/// <param name="grid">The serialized grid data</param>
		/// <param name="wrapper_only">Whether to store grid as null-wrapper suspended construct</param>
		/// <returns>Whether this construct was updated</returns>
		public bool FromSerialized(MySerializedJumpGateConstruct grid, bool wrapper_only = false)
		{
			if (this.Closed || grid == null || grid.IsClientRequest || MyNetworkInterface.IsServerLike) return false;
			bool initial_dirty = this.IsDirty;
			long gridid = grid.UUID.GetJumpGateGrid();
			if (gridid != this.CubeGridID) return false;

			lock (this.UpdateLock)
			{
				IMyCubeGrid new_grid = MyAPIGateway.Entities.GetEntityById(gridid) as IMyCubeGrid;
				Logger.Debug($"CUBE_GRID_SETUP::{gridid}, NULLGRID={new_grid == null}", 2);

				if (this.IsSuspended && new_grid != null && (grid.CubeGrids?.Any((subgrid_id) => ((IMyCubeGrid) MyAPIGateway.Entities.GetEntityById(subgrid_id)) == null) ?? true))
				{
					this.SuspensionLockUpdatingPaused = true;
					MyJumpGateModSession.Instance.QueuePartialConstructForReload(gridid);
					Logger.Debug($" ... PARTIAL_GRID >> MARKED_FOR_SINGLE_UPDATE_60");
					return true;
				}

				this.MarkClosed = false;
				this.SuspensionLockUpdatingPaused = false;
				this.PrimaryCubeGridCustomName = grid.ConstructName ?? new_grid?.CustomName;
				this.JumpGateBlocks.Clear();
				this.CommBlocks.Clear();
				List<long> new_gates = new List<long>(grid.JumpGates?.Count ?? 0);

				if (new_grid != null)
				{
					this.CubeGrid = new_grid;
					this.SetupConstruct(grid.CubeGrids?.Select((subgrid) => (IMyCubeGrid) MyAPIGateway.Entities.GetEntityById(subgrid))?.Where((subgrid) => subgrid != null));
				}

				if (grid.JumpGateDrives != null)
				{
					foreach (MySerializedJumpGateDrive serialized in grid.JumpGateDrives)
					{
						MyJumpGateDrive component = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateDrive>(serialized.UUID.GetBlock());

						if (component == null) component = new MyJumpGateDrive(serialized, this);
						else if (wrapper_only || component.IsClosed)
						{
							component.MarkForClose();
							component = new MyJumpGateDrive(serialized, this);
						}
						else component.FromSerialized(serialized, this);

						this.JumpGateBlocks[component.BlockID] = component;
					}
				}

				if (grid.JumpGateCapacitors != null)
				{
					foreach (MySerializedJumpGateCapacitor serialized in grid.JumpGateCapacitors)
					{
						MyJumpGateCapacitor component = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateCapacitor>(serialized.UUID.GetBlock());

						if (component == null) component = new MyJumpGateCapacitor(serialized, this);
						else if (wrapper_only || component.IsClosed)
						{
							component.MarkForClose();
							component = new MyJumpGateCapacitor(serialized, this);
						}
						else component.FromSerialized(serialized, this);

						this.JumpGateBlocks[component.BlockID] = component;
					}
				}

				if (grid.JumpGateControllers != null)
				{
					foreach (MySerializedJumpGateController serialized in grid.JumpGateControllers)
					{
						MyJumpGateController component = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateController>(serialized.UUID.GetBlock());

						if (component == null) component = new MyJumpGateController(serialized, this);
						else if (wrapper_only || component.IsClosed)
						{
							component.MarkForClose();
							component = new MyJumpGateController(serialized, this);
						}
						else component.FromSerialized(serialized, this);

						this.JumpGateBlocks[component.BlockID] = component;
					}
				}

				if (grid.JumpGateRemoteAntennas != null)
				{
					foreach (MySerializedJumpGateRemoteAntenna serialized in grid.JumpGateRemoteAntennas)
					{
						MyJumpGateRemoteAntenna component = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteAntenna>(serialized.UUID.GetBlock());

						if (component == null) component = new MyJumpGateRemoteAntenna(serialized, this);
						else if (wrapper_only || component.IsClosed)
						{
							component.MarkForClose();
							component = new MyJumpGateRemoteAntenna(serialized, this);
						}
						else component.FromSerialized(serialized, this);

						this.JumpGateBlocks[component.BlockID] = component;
					}
				}

				if (grid.JumpGateServerAntennas != null)
				{
					foreach (MySerializedJumpGateServerAntenna serialized in grid.JumpGateServerAntennas)
					{
						MyJumpGateServerAntenna component = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateServerAntenna>(serialized.UUID.GetBlock());

						if (component == null) component = new MyJumpGateServerAntenna(serialized, this);
						else if (wrapper_only || component.IsClosed)
						{
							component.MarkForClose();
							component = new MyJumpGateServerAntenna(serialized, this);
						}
						else component.FromSerialized(serialized, this);

						this.JumpGateBlocks[component.BlockID] = component;
					}
				}

				if (grid.JumpGateRemoteLinks != null)
				{
					foreach (MySerializedJumpGateRemoteLink serialized in grid.JumpGateRemoteLinks)
					{
						MyJumpGateRemoteLink component = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>(serialized.UUID.GetBlock());

						if (component == null) component = new MyJumpGateRemoteLink(serialized, this);
						else if (wrapper_only || component.IsClosed)
						{
							component.MarkForClose();
							component = new MyJumpGateRemoteLink(serialized, this);
						}
						else component.FromSerialized(serialized, this);

						this.JumpGateBlocks[component.BlockID] = component;
					}
				}

				if (grid.LaserAntennas != null)
				{
					foreach (long laser_antenna in grid.LaserAntennas)
					{
						IMyLaserAntenna block = MyAPIGateway.Entities.GetEntityById(laser_antenna) as IMyLaserAntenna;
						if (block != null) this.CommBlocks[block.EntityId] = block;
					}
				}

				if (grid.RadioAntennas != null)
				{
					foreach (long radio_antenna in grid.RadioAntennas)
					{
						IMyRadioAntenna block = MyAPIGateway.Entities.GetEntityById(radio_antenna) as IMyRadioAntenna;
						if (block != null) this.CommBlocks[block.EntityId] = block;
					}
				}

				if (grid.JumpGates != null)
				{
					foreach (MySerializedJumpGate serialized in grid.JumpGates)
					{
						long gate_id = serialized.UUID.GetJumpGate();
						new_gates.Add(gate_id);
						MyJumpGate jump_gate = null;

						if (this.JumpGates.TryGetValue(gate_id, out jump_gate) && !jump_gate.Closed) jump_gate.FromSerialized(serialized);
						else if (jump_gate == null) this.JumpGates[gate_id] = new MyJumpGate(serialized, this);
						else
						{
							MyJumpGateModSession.Instance.CloseGate(jump_gate);
							this.JumpGates[gate_id] = new MyJumpGate(serialized, this);
						}
					}
				}

				foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates)
				{
					if (!new_gates.Contains(pair.Key))
					{
						pair.Value.Dispose();
						MyJumpGateModSession.Instance.CloseGate(pair.Value);
					}
				}

				if (MyNetworkInterface.IsMultiplayerServer) this.SetDirty();
				else this.IsDirty = initial_dirty;
				if (new_grid == null) this.Suspend(gridid, false);
				return true;
			}
		}

		/// <summary>
		/// Serializes this construct's data
		/// </summary>
		/// <param name="as_client_request">If true, this will be an update request to server</param>
		/// <returns>The serialized grid data</returns>
		public MySerializedJumpGateConstruct ToSerialized(bool as_client_request)
		{
			if (as_client_request)
			{
				return new MySerializedJumpGateConstruct
				{
					UUID = JumpGateUUID.FromJumpGateGrid(this),
					IsClientRequest = true,
				};
			}
			else
			{
				return new MySerializedJumpGateConstruct
				{
					UUID = JumpGateUUID.FromJumpGateGrid(this),
					JumpGates = this.JumpGates?.Select((pair) => pair.Value.ToSerialized(false)).ToList(),
					JumpGateDrives = this.GetAttachedJumpGateDrives().Where((block) => !block.IsClosed).Select((drive) => drive.ToSerialized(false)).ToList(),
					JumpGateControllers = this.GetAttachedJumpGateControllers().Where((block) => !block.IsClosed).Select((controller) => controller.ToSerialized(false)).ToList(),
					JumpGateCapacitors = this.GetAttachedJumpGateCapacitors().Where((block) => !block.IsClosed).Select((capacitor) => capacitor.ToSerialized(false)).ToList(),
					JumpGateRemoteAntennas = this.GetAttachedJumpGateRemoteAntennas().Where((block) => !block.IsClosed).Select((antenna) => antenna.ToSerialized(false)).ToList(),
					JumpGateServerAntennas = this.GetAttachedJumpGateServerAntennas().Where((block) => !block.IsClosed).Select((antenna) => antenna.ToSerialized(false)).ToList(),
					JumpGateRemoteLinks = this.GetAttachedJumpGateRemoteLinks().Where((block) => !block.IsClosed).Select((link) => link.ToSerialized(false)).ToList(),
					LaserAntennas = this.GetAttachedLaserAntennas().Where((block) => !block.MarkedForClose).Select((antenna) => antenna.EntityId).ToList(),
					RadioAntennas = this.GetAttachedRadioAntennas().Where((block) => !block.MarkedForClose).Select((antenna) => antenna.EntityId).ToList(),
					CubeGrids = this.CubeGrids?.Where((grid) => !grid.Value.MarkedForClose).Select((pair) => pair.Value.EntityId).ToList(),
					ConstructName = this.PrimaryCubeGridCustomName,
					IsClientRequest = false,
				};
			}
		}
		#endregion
	}

	/// <summary>
	/// Class for holding serialized MyJumpGateConstruct data
	/// </summary>
	[ProtoContract]
	internal class MySerializedJumpGateConstruct
	{
		/// <summary>
		/// The construct's JumpGateUUID
		/// </summary>
		[ProtoMember(1)]
		public JumpGateUUID UUID;

		/// <summary>
		/// The serialized jump gates belonging to this construct
		/// </summary>
		[ProtoMember(2)]
		public List<MySerializedJumpGate> JumpGates;

		/// <summary>
		/// The serialized jump gate drives belonging to this construct
		/// </summary>
		[ProtoMember(3)]
		public List<MySerializedJumpGateDrive> JumpGateDrives;

		/// <summary>
		/// The serialized jump gate controllers belonging to this construct
		/// </summary>
		[ProtoMember(4)]
		public List<MySerializedJumpGateController> JumpGateControllers;

		/// <summary>
		/// The serialized jump gate capacitors belonging to this construct
		/// </summary>
		[ProtoMember(5)]
		public List<MySerializedJumpGateCapacitor> JumpGateCapacitors;

		/// <summary>
		/// The serialized jump gate server antennas belonging to this construct
		/// </summary>
		[ProtoMember(6)]
		public List<MySerializedJumpGateRemoteAntenna> JumpGateRemoteAntennas;

		/// <summary>
		/// The serialized jump gate server antennas belonging to this construct
		/// </summary>
		[ProtoMember(7)]
		public List<MySerializedJumpGateServerAntenna> JumpGateServerAntennas;

		/// <summary>
		/// The serialized jump gate remote links belonging to this construct
		/// </summary>
		[ProtoMember(8)]
		public List<MySerializedJumpGateRemoteLink> JumpGateRemoteLinks;

		/// <summary>
		/// The list of laser antenna IDs belonging to this construct
		/// </summary>
		[ProtoMember(9)]
		public List<long> LaserAntennas;

		/// <summary>
		/// The list of radio antenna IDs belonging to this construct
		/// </summary>
		[ProtoMember(10)]
		public List<long> RadioAntennas;

		/// <summary>
		/// The list of cube grid IDs belonging to this construct
		/// </summary>
		[ProtoMember(11)]
		public List<long> CubeGrids;

		/// <summary>
		/// The name of this construct's primary cube grid
		/// </summary>
		[ProtoMember(12)]
		public string ConstructName;

		/// <summary>
		/// If true, this data should be used by server to identify grid and send updated data
		/// </summary>
		[ProtoMember(13)]
		public bool IsClientRequest;
	}
}
