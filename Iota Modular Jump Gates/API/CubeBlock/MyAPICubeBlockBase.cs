using IOTA.ModularJumpGates.CubeBlock;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRageMath;

namespace IOTA.ModularJumpGates.API.CubeBlock
{
	public class MyAPICubeBlockBase : MyAPIInterface
	{
		internal MyCubeBlockBase CubeBlock;

		internal MyAPICubeBlockBase(MyCubeBlockBase cube_block)
		{
			this.CubeBlock = cube_block;
		}

		#region Public API Variables
		/// <summary>
		/// Whether this block should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty => this.CubeBlock.IsDirty;

		/// <summary>
		/// Whether this block is large grid
		/// </summary>
		public bool IsLargeGrid => this.CubeBlock.IsLargeGrid;

		/// <summary>
		/// Whether this block is small grid
		/// </summary>
		public bool IsSmallGrid => this.CubeBlock.IsSmallGrid;

		/// <summary>
		/// Whether this is a wrapper around a block that isn't initialized<br />
		/// Always false on singleplayer or server
		/// </summary>
		public bool IsNullWrapper => this.CubeBlock.IsNullWrapper;

		/// <summary>
		/// The block ID of this block
		/// </summary>
		public long BlockID => this.CubeBlock.BlockID;

		/// <summary>
		/// The cube grid ID of this block
		/// </summary>
		public long CubeGridID => this.CubeBlock.CubeGridID;

		/// <summary>
		/// The construct ID of this block
		/// </summary>
		public long ConstructID => this.CubeBlock.ConstructID;

		/// <summary>
		/// The player ID of this block's owner
		/// </summary>
		public long OwnerID => this.CubeBlock.OwnerID;

		/// <summary>
		/// The steam ID of this block's owner
		/// </summary>
		public ulong OwnerSteamID => this.CubeBlock.OwnerSteamID;

		/// <summary>
		/// The block local game tick
		/// </summary>
		public ulong LocalGameTick => this.CubeBlock.LocalGameTick;

		/// <summary>
		/// The grid size of this block
		/// </summary>
		public MyCubeSize CubeGridSize => this.CubeBlock.CubeGridSize;

		/// <summary>
		/// The terminal block this component is bound to
		/// </summary>
		public IMyUpgradeModule TerminalBlock => this.CubeBlock.TerminalBlock;

		/// <summary>
		/// The UTC date time representation of CubeBlockBase.LastUpdateTime
		/// </summary>
		public DateTime LastUpdateDateTimeUTC => this.CubeBlock.LastUpdateDateTimeUTC;

		/// <summary>
		/// The world matrix of this block
		/// </summary>
		public MatrixD WorldMatrix => this.CubeBlock.WorldMatrix;

		/// <summary>
		/// The jump gate grid this component is bound to
		/// </summary>
		public MyAPIJumpGateConstruct JumpGateGrid => MyAPISession.GetNewConstruct(this.CubeBlock.JumpGateGrid);
		#endregion

		#region Public API Methods
		/// <summary>
		/// Sets the internal "IsDirty" flag to true
		/// <br />
		/// Marks block for sync on next component tick
		/// </summary>
		public void SetDirty()
		{
			this.CubeBlock.SetDirty();
		}

		/// <summary>
		/// Gets whether this component or it's attached block is closed
		/// </summary>
		/// <returns>Closedness</returns>
		public bool IsClosed()
		{
			return this.CubeBlock.IsClosed();
		}

		/// <summary>
		/// Gets whether block is powered
		/// </summary>
		/// <returns>Whether current input >= required input</returns>
		public bool IsPowered()
		{
			return this.CubeBlock.IsPowered();
		}

		/// <summary>
		/// Gets whether block is enabled (not closed)
		/// </summary>
		/// <returns>Enabledness</returns>
		public bool IsEnabled()
		{
			return this.CubeBlock.IsEnabled();
		}

		/// <summary>
		/// Gets block's working status: (not closed, enabled, powered)
		/// </summary>
		/// <returns>Workingness</returns>
		public bool IsWorking()
		{
			return this.CubeBlock.IsWorking();
		}
		#endregion
	}
}
