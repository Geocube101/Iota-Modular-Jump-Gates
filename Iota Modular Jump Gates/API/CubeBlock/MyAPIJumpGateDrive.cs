using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using Sandbox.Game.EntityComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using static IOTA.ModularJumpGates.API.CubeBlock.MyAPIJumpGateCapacitor;

namespace IOTA.ModularJumpGates.API.CubeBlock
{
	public class MyAPIJumpGateDrive : MyAPICubeBlockBase
	{
		new internal MyJumpGateDrive CubeBlock;

		internal MyAPIJumpGateDrive(MyJumpGateDrive block) : base(block)
		{
			this.CubeBlock = block;
		}

		#region Public API Variables
		/// <summary>
		/// The stored drive charge in MegaWatts
		/// </summary>
		public double StoredChargeMW { get { return this.CubeBlock.StoredChargeMW; } }

		/// <summary>
		/// The maximum possible distance this drive can raycast<br />
		/// This takes into account construct obstructions
		/// </summary>
		public double MaxRaycastDistance { get { return this.CubeBlock.MaxRaycastDistance; } }

		/// <summary>
		/// The current wattage override or 0 if no override is set
		/// </summary>
		public double WattageSinkOverride { get { return this.CubeBlock.GetWattageSinkOverride(); } }

		/// <summary>
		/// The ID of the jump gate this drive is linked to or -1 if not linked
		/// </summary>
		public long JumpGateID { get { return this.CubeBlock.JumpGateID; } }

		/// <summary>
		/// The color of the emitter emissives
		/// </summary>
		public Color DriveEmitterColor { get { return this.CubeBlock.DriveEmitterColor; } }

		/// <summary>
		/// This drive's configuration variables
		/// </summary>
		public Configuration.LocalDriveConfiguration Configuration { get { return this.CubeBlock.DriveConfiguration; } }
		#endregion

		#region Public API Methods
		/// <summary>
		/// Sets an override to change this block's power usage
		/// </summary>
		/// <param name="override_mw">The new value in Mega-Watts</param>
		public void SetWattageSinkOverride(double override_mw)
		{
			this.CubeBlock.SetWattageSinkOverride(override_mw);
		}

		/// <summary>
		/// Gets the amount of power being drawn by this drive's power sink
		/// </summary>
		/// <returns>The current input power in Mega-Watts</returns>
		public double GetCurrentWattageSinkInput()
		{
			return this.GetCurrentWattageSinkInput();
		}

		/// <summary>
		/// Drains the capacitor's charge
		/// </summary>
		/// <param name="power_mw">The power (in Mega-Watts) to drain</param>
		/// <returns>The remaining power</returns>
		public double DrainStoredCharge(double power_mw)
		{
			return this.CubeBlock.DrainStoredCharge(power_mw);
		}

		/// <summary>
		/// Gets the endpoint for a ray starting at the end of the collision mesh and ending after the specified distance
		/// </summary>
		/// <param name="distance">The distance to cast</param>
		/// <returns>A world coordinate indicating the raycast endpoint</returns>
		public Vector3D GetDriveRaycastEndpoint(double distance)
		{
			return this.CubeBlock.GetDriveRaycastEndpoint(distance);
		}

		/// <summary>
		/// Gets the endpoint for a ray starting at the block's center and ending at the end of the collision mesh
		/// </summary>
		/// <returns>A world coordinate indicating the raycast start point</returns>
		public Vector3D GetDriveRaycastStartpoint()
		{
			return this.CubeBlock.GetDriveRaycastStartpoint();
		}
		#endregion
	}
}