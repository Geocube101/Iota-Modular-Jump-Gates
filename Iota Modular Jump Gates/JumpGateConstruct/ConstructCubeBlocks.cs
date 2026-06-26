using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	internal partial class MyJumpGateConstruct
	{
		#region Private Variables
		/// <summary>
		/// A collection for hopefully faster iterating of grid blocks
		/// </summary>
		private ConcurrentLinkedHashSet<IMySlimBlock> GridBlocks = new ConcurrentLinkedHashSet<IMySlimBlock>();

		/// <summary>
		/// The master map of all jump gate blocks this construct has
		/// </summary>
		private ConcurrentDictionary<long, MyCubeBlockBase> JumpGateBlocks = new ConcurrentDictionary<long, MyCubeBlockBase>();

		/// <summary>
		/// The master map of comm blocks this construct has
		/// </summary>
		private ConcurrentDictionary<long, IMyTerminalBlock> CommBlocks = new ConcurrentDictionary<long, IMyTerminalBlock>();
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback for when a block is added to this construct
		/// </summary>
		/// <param name="block">The added block</param>
		private void OnBlockAdded(IMySlimBlock block)
		{
			if (this.MarkClosed || this.Closed) return;
			this.GridBlocks?.Add(block);
			this.UpdateDriveMaxRaycastDistances = true;
			if (block.FatBlock == null || block.FatBlock.MarkedForClose || block.FatBlock.Closed) return;

			if (block.FatBlock is IMyRadioAntenna)
			{
				IMyRadioAntenna fat_block = block.FatBlock as IMyRadioAntenna;
				this.CommBlocks[fat_block.EntityId] = fat_block;
				this.SetDirty();
			}
			else if (block.FatBlock is IMyLaserAntenna)
			{
				IMyLaserAntenna fat_block = block.FatBlock as IMyLaserAntenna;
				this.CommBlocks[fat_block.EntityId] = fat_block;
				this.SetDirty();
			}
			else if (block.FatBlock is IMyBeacon)
			{
				IMyBeacon fat_block = block.FatBlock as IMyBeacon;
				this.CommBlocks[fat_block.EntityId] = fat_block;
				this.SetDirty();
			}
			else if (block.FatBlock is IMyUpgradeModule)
			{
				IMyUpgradeModule fat_block = block.FatBlock as IMyUpgradeModule;
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(fat_block);

				if (block_base is MyJumpGateDrive)
				{
					MyJumpGateDrive drive = block_base as MyJumpGateDrive;

					if (!this.JumpGateBlocks.TryAdd(fat_block.EntityId, drive))
					{
						MyJumpGateDrive old_drive = this.GetDrive(fat_block.EntityId);
						lock (this.UpdateLock) this.DriveCombinations?.RemoveAll((pair) => pair.Key == old_drive || pair.Value == old_drive);
						this.JumpGateBlocks[fat_block.EntityId] = drive;
					}

					lock (this.UpdateLock)
					{
						foreach (MyJumpGateDrive existing in this.GetAttachedJumpGateDrives())
						{
							if (drive != existing && drive.CubeGridSize == existing.CubeGridSize)
							{
								this.DriveCombinations?.Add(new KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>(drive, existing));
							}
						}
					}

					this.MarkUpdateJumpGates = true;
					this.SetDirty();
				}
				else if (block_base != null)
				{
					this.JumpGateBlocks[fat_block.EntityId] = block_base;
					this.SetDirty();
				}
			}
		}

		/// <summary>
		/// Callback for when a block is removed from this construct
		/// </summary>
		/// <param name="block">The removed block</param>
		private void OnBlockRemoved(IMySlimBlock block)
		{
			this.UpdateDriveMaxRaycastDistances = true;
			if (block.FatBlock == null || this.MarkClosed || this.Closed) return;
			this.CommBlocks.Remove(block.FatBlock.EntityId);
			this.GridBlocks.Remove(block);

			if (block.FatBlock is IMyUpgradeModule)
			{
				MyCubeBlockBase block_base;
				this.JumpGateBlocks.TryRemove(block.FatBlock.EntityId, out block_base);

				if (block_base is MyJumpGateDrive)
				{
					MyJumpGateDrive drive = block_base as MyJumpGateDrive;
					this.MarkUpdateJumpGates = true;
					lock (this.UpdateLock) this.DriveCombinations?.RemoveAll((pair) => pair.Key == drive || pair.Value == drive);
				}

				this.SetDirty();
			}
		}

		/// <summary>
		/// Updates the drive combinations list if null
		/// </summary>
		/// <param name="full_gate_update">Whether to undergo a full gate update</param>
		private void UpdateDriveCombinations(ref bool full_gate_update)
		{
			if (this.DriveCombinations != null) return;
			List<MyJumpGateDrive> indexable_large_drives = new List<MyJumpGateDrive>();
			List<MyJumpGateDrive> indexable_small_drives = new List<MyJumpGateDrive>();
			this.DriveCombinations = new List<KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>>();
			full_gate_update = true;

			foreach (MyJumpGateDrive drive in this.GetAttachedJumpGateDrives())
			{
				if (drive.IsLargeGrid) indexable_large_drives.Add(drive);
				else if (drive.IsSmallGrid) indexable_small_drives.Add(drive);
				else throw new InvalidOperationException($"A block was neither large nor small grid - DRIVE::{drive.BlockID}");
			}

			for (int i = 0; i < indexable_large_drives.Count - 1; ++i)
			{
				for (int j = i + 1; j < indexable_large_drives.Count; ++j)
				{
					this.DriveCombinations.Add(new KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>(indexable_large_drives[i], indexable_large_drives[j]));
				}
			}

			for (int i = 0; i < indexable_small_drives.Count - 1; ++i)
			{
				for (int j = i + 1; j < indexable_small_drives.Count; ++j)
				{
					this.DriveCombinations.Add(new KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>(indexable_small_drives[i], indexable_small_drives[j]));
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Gets a list of blocks from this construct that intersect the specified bounding ellipsoid
		/// </summary>
		/// <param name="ellipsoid">The bounding ellipsoid</param>
		/// <param name="contained_blocks">An optional collection to populate containing all blocks within the ellipsoid</param>
		/// <param name="uncontained_blocks">An optional collection to populate containing all blocks outside the ellipsoid</param>
		public void GetBlocksIntersectingEllipsoid(ref BoundingEllipsoidD ellipsoid, ICollection<IMySlimBlock> contained_blocks = null, ICollection<IMySlimBlock> uncontained_blocks = null)
		{
			if (this.Closed || contained_blocks == null && uncontained_blocks == null) return;
			Vector3D position;

			foreach (IMySlimBlock block in this.GridBlocks)
			{
				if (block == null || (block.FatBlock?.MarkedForClose ?? false)) continue;
				position = block.CubeGrid.GridIntegerToWorld(block.Position);
				if (contained_blocks != null && ellipsoid.IsPointInEllipse(position)) contained_blocks.Add(block);
				else uncontained_blocks?.Add(block);
			}
		}

		/// <summary>
		/// Gets the number of blocks matching the specified predicate<br />
		/// Gets the number of all blocks if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter blocks by</param>
		/// <returns>The number of matching blocks</returns>
		public int GetConstructBlockCount(Func<IMySlimBlock, bool> predicate = null)
		{
			if (this.Closed) return 0;
			return (predicate == null) ? this.GridBlocks.Count : this.GridBlocks.Count(predicate);
		}

		/// <summary>
		/// Gets a laser antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The laser antenna's ID</param>
		/// <returns>The laser antenna</returns>
		public IMyLaserAntenna GetLaserAntenna(long id)
		{
			return this.CommBlocks?.GetValueOrDefault(id, null) as IMyLaserAntenna;
		}

		/// <summary>
		/// Gets a radio antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The radio antenna's ID</param>
		/// <returns>The radio antenna</returns>
		public IMyRadioAntenna GetRadioAntenna(long id)
		{
			return this.CommBlocks?.GetValueOrDefault(id, null) as IMyRadioAntenna;
		}

		/// <summary>
		/// Gets a beacon by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The beacon's ID</param>
		/// <returns>The beacon</returns>
		public IMyBeacon GetBeacon(long id)
		{
			return this.CommBlocks?.GetValueOrDefault(id, null) as IMyBeacon;
		}

		/// <summary>
		/// Gets an enumerator to all grid blocks within this construct
		/// </summary>
		/// <returns>An enumerator for all blocks</returns>
		public IEnumerator<IMySlimBlock> GetConstructBlocks()
		{
			return this.GridBlocks?.GetEnumerator();
		}

		/// <summary>
		/// Gets a cube block by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The block's ID</param>
		/// <returns>The block</returns>
		public MyCubeBlockBase GetCubeBlock(long id)
		{
			return this.JumpGateBlocks?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets a capacitor by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The capacitor's ID</param>
		/// <returns>The capacitor</returns>
		public MyJumpGateCapacitor GetCapacitor(long id)
		{
			return this.JumpGateBlocks?.GetValueOrDefault(id, null) as MyJumpGateCapacitor;
		}

		/// <summary>
		/// Gets a drive by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The drive's ID</param>
		/// <returns>The drive</returns>
		public MyJumpGateDrive GetDrive(long id)
		{
			return this.JumpGateBlocks?.GetValueOrDefault(id, null) as MyJumpGateDrive;
		}

		/// <summary>
		/// Gets a remote antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The remote antenna's ID</param>
		/// <returns>The remote antenna</returns>
		public MyJumpGateRemoteAntenna GetRemoteAntenna(long id)
		{
			return this.JumpGateBlocks?.GetValueOrDefault(id, null) as MyJumpGateRemoteAntenna;
		}

		/// <summary>
		/// Gets a server antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The server antenna's ID</param>
		/// <returns>The server antenna</returns>
		public MyJumpGateServerAntenna GetServerAntenna(long id)
		{
			return this.JumpGateBlocks?.GetValueOrDefault(id, null) as MyJumpGateServerAntenna;
		}

		/// <summary>
		/// Gets a remote link by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The remote link's ID</param>
		/// <returns>The remote link</returns>
		public MyJumpGateRemoteLink GetRemoteLink(long id)
		{
			return this.JumpGateBlocks?.GetValueOrDefault(id, null) as MyJumpGateRemoteLink;
		}

		/// <summary>
		/// Gets a controller by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The controller's ID</param>
		/// <returns>The controller</returns>
		public MyJumpGateController GetController(long id)
		{
			return this.JumpGateBlocks?.GetValueOrDefault(id, null) as MyJumpGateController;
		}

		/// <summary>
		/// Gets all blocks inheriting from the specified type
		/// </summary>
		/// <typeparam name="T">The block type</typeparam>
		/// <returns>An IEnumerable referencing all matching blocks</returns>
		public IEnumerable<T> GetAllSlimBlocksOfType<T>() where T : IMySlimBlock
		{
			return (this.Closed) ? Enumerable.Empty<T>() : this.GridBlocks.Where((block) => block is T).Select((block) => (T) block);
		}

		/// <summary>
		/// Gets all blocks inheriting from the specified type
		/// </summary>
		/// <typeparam name="T">The block type</typeparam>
		/// <returns>An IEnumerable referencing all matching blocks</returns>
		public IEnumerable<T> GetAllFatBlocksOfType<T>() where T : IMyCubeBlock
		{
			return (this.Closed) ? Enumerable.Empty<T>() : this.GridBlocks.Where((block) => block.FatBlock != null && block.FatBlock is T).Select((block) => (T) block.FatBlock);
		}

		/// <summary>
		/// Gets all jump gate controllers in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all controllers</returns>
		public IEnumerable<MyJumpGateController> GetAttachedJumpGateControllers()
		{
			return this.JumpGateBlocks?.Where((pair) => pair.Value is MyJumpGateController).Select((pair) => (MyJumpGateController) pair.Value) ?? Enumerable.Empty<MyJumpGateController>();
		}

		/// <summary>
		/// All jump gate drives in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all drives</returns>
		public IEnumerable<MyJumpGateDrive> GetAttachedJumpGateDrives()
		{
			return this.JumpGateBlocks?.Where((pair) => pair.Value is MyJumpGateDrive).Select((pair) => (MyJumpGateDrive) pair.Value) ?? Enumerable.Empty<MyJumpGateDrive>();
		}

		/// <summary>
		/// Gets all jump gate drives in this construct not bound to a jump gate
		/// </summary>
		/// <returns>An IEnumerable referencing all unassociated drives</returns>
		public IEnumerable<MyJumpGateDrive> GetAttachedUnassociatedJumpGateDrives()
		{
			return this.JumpGateBlocks?.Where((pair) => pair.Value is MyJumpGateDrive).Select((pair) => (MyJumpGateDrive) pair.Value).Where((drive) => drive.JumpGateID == -1) ?? Enumerable.Empty<MyJumpGateDrive>();
		}

		/// <summary>
		/// Gets all jump gate capacitors in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all capacitors</returns>
		public IEnumerable<MyJumpGateCapacitor> GetAttachedJumpGateCapacitors()
		{
			return this.JumpGateBlocks?.Where((pair) => pair.Value is MyJumpGateCapacitor).Select((pair) => (MyJumpGateCapacitor) pair.Value) ?? Enumerable.Empty<MyJumpGateCapacitor>();
		}

		/// <summary>
		/// Gets all jump gate remote antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all remote antennas</returns>
		public IEnumerable<MyJumpGateRemoteAntenna> GetAttachedJumpGateRemoteAntennas()
		{
			return this.JumpGateBlocks?.Where((pair) => pair.Value is MyJumpGateRemoteAntenna).Select((pair) => (MyJumpGateRemoteAntenna) pair.Value) ?? Enumerable.Empty<MyJumpGateRemoteAntenna>();
		}

		/// <summary>
		/// Gets all jump gate server antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all server antennas</returns>
		public IEnumerable<MyJumpGateServerAntenna> GetAttachedJumpGateServerAntennas()
		{
			return this.JumpGateBlocks?.Where((pair) => pair.Value is MyJumpGateServerAntenna).Select((pair) => (MyJumpGateServerAntenna) pair.Value) ?? Enumerable.Empty<MyJumpGateServerAntenna>();
		}

		/// <summary>
		/// Gets all jump gate remote links in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all remote links</returns>
		public IEnumerable<MyJumpGateRemoteLink> GetAttachedJumpGateRemoteLinks()
		{
			return this.JumpGateBlocks?.Where((pair) => pair.Value is MyJumpGateRemoteLink).Select((pair) => (MyJumpGateRemoteLink) pair.Value) ?? Enumerable.Empty<MyJumpGateRemoteLink>();
		}

		/// <summary>
		/// Gets all laser antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all laser antennas</returns>
		public IEnumerable<IMyLaserAntenna> GetAttachedLaserAntennas()
		{
			return this.CommBlocks?.Where((pair) => pair.Value is IMyLaserAntenna).Select((pair) => (IMyLaserAntenna) pair.Value) ?? Enumerable.Empty<IMyLaserAntenna>();
		}

		/// <summary>
		/// Gets all radio antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all radio antennas</returns>
		public IEnumerable<IMyRadioAntenna> GetAttachedRadioAntennas()
		{
			return this.CommBlocks?.Where((pair) => pair.Value is IMyRadioAntenna).Select((pair) => (IMyRadioAntenna) pair.Value) ?? Enumerable.Empty<IMyRadioAntenna>();
		}

		/// <summary>
		/// Gets all beacons in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all beacons</returns>
		public IEnumerable<IMyBeacon> GetAttachedBeacons()
		{
			return this.CommBlocks?.Where((pair) => pair.Value is IMyBeacon).Select((pair) => (IMyBeacon) pair.Value) ?? Enumerable.Empty<IMyBeacon>();
		}
		#endregion
	}
}
