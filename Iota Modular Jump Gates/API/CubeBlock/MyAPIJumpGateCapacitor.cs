using IOTA.ModularJumpGates.CubeBlock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IOTA.ModularJumpGates.API.CubeBlock
{
	public class MyAPIJumpGateCapacitor : MyAPICubeBlockBase
	{
		public sealed class CapacitorBlockSettingsWrapper
		{
			internal MyJumpGateCapacitor.MyCapacitorBlockSettingsStruct BlockSettings;

			public bool RechargeEnabled { get { return this.BlockSettings.RechargeEnabled; } set { this.BlockSettings.RechargeEnabled = value; } }
			public double InternalChargeMW { get { return this.BlockSettings.InternalChargeMW; } set { this.BlockSettings.InternalChargeMW = value; } }
			public Color EmissiveColor { get { return this.BlockSettings.EmissiveColor; } set { this.BlockSettings.EmissiveColor = value; } }
		}

		new internal MyJumpGateCapacitor CubeBlock;

		public CapacitorBlockSettingsWrapper BlockSettings;

		internal MyAPIJumpGateCapacitor(MyJumpGateCapacitor block) : base(block)
		{
			this.CubeBlock = block;
			this.BlockSettings = new CapacitorBlockSettingsWrapper() { BlockSettings = block.BlockSettings };
		}

		#region Public API Variables
		/// <summary>
		/// This capacitor's stored charge in MegaWatts
		/// </summary>
		public double StoredChargeMW { get { return this.CubeBlock.StoredChargeMW; } }

		/// <summary>
		/// This capacitor's configuration variables
		/// </summary>
		public Configuration.LocalCapacitorConfiguration Configuration { get { return this.CubeBlock.CapacitorConfiguration; } }
		#endregion

		#region Public API Methods

		/// <summary>
		/// Drains the capacitor's charge
		/// </summary>
		/// <param name="power_mw">The power (in MegaWatts) to drain</param>
		/// <returns>The remaining power (in MegaWatts)</returns>
		public double DrainStoredCharge(double power_mw)
		{
			return this.CubeBlock.DrainStoredCharge(power_mw);
		}
		#endregion
	}
}
