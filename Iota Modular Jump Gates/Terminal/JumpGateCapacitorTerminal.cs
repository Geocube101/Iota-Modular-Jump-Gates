using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.ProgramScripting.CubeBlock;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.Terminal
{
	internal static class MyJumpGateCapacitorTerminal
	{
		public static bool IsLoaded { get; private set; } = false;
		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".JumpGateCapacitor.";

		private static void SetupJumpGateCapacitorTerminalControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateCapacitor;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "capacitor_settings_label_lb");
				settings_label_lb.Label = MyStringId.GetOrCompute("Capacitor Settings");
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateCapacitor;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// OnOffSwitch [Recharge]
			{
				IMyTerminalControlOnOffSwitch do_capacitor_recharge = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + "RechargeOnOff");
				do_capacitor_recharge.Title = MyStringId.GetOrCompute("Recharge / Discharge");
				do_capacitor_recharge.Tooltip = MyStringId.GetOrCompute("Whether this capacitor recharges from or discharges to the grid");
				do_capacitor_recharge.SupportsMultipleBlocks = true;
				do_capacitor_recharge.Visible = MyJumpGateModSession.IsBlockJumpGateCapacitor;
				do_capacitor_recharge.Enabled = (block) => {
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					return capacitor != null && capacitor.TerminalBlock.IsFunctional && capacitor.JumpGateGrid != null && !capacitor.JumpGateGrid.Closed;
				};
				do_capacitor_recharge.OnText = MyStringId.GetOrCompute("On");
				do_capacitor_recharge.OffText = MyStringId.GetOrCompute("Off");

				do_capacitor_recharge.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block)?.BlockSettings?.RechargeEnabled ?? false;
				do_capacitor_recharge.Setter = (block, value) => {
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					if (capacitor == null || capacitor.MarkedForClose) return;
					capacitor.BlockSettings.RechargeEnabled = value;
					capacitor.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_capacitor_recharge);
			}

			// Color [Emissive Color]
			{
				IMyTerminalControlColor effect_color_shift_cl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyUpgradeModule>(MODID_PREFIX + "CapacitorEffectColorShift");
				effect_color_shift_cl.Title = MyStringId.GetOrCompute("Emissive Color");
				effect_color_shift_cl.Tooltip = MyStringId.GetOrCompute("Sets the capacitor bank emissive color");
				effect_color_shift_cl.SupportsMultipleBlocks = true;
				effect_color_shift_cl.Visible = MyJumpGateModSession.IsBlockJumpGateCapacitor;
				effect_color_shift_cl.Enabled = MyJumpGateModSession.IsBlockJumpGateCapacitor;
				effect_color_shift_cl.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block)?.BlockSettings?.EmissiveColor ?? Color.White;
				effect_color_shift_cl.Setter = (block, value) => {
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					if (capacitor == null || capacitor.MarkedForClose) return;
					capacitor.BlockSettings.EmissiveColor = value;
					capacitor.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(effect_color_shift_cl);
			}
		}

		private static void SetupJumpGateCapacitorTerminalActions()
		{

		}

		private static void SetupJumpGateCapacitorTerminalProperties()
		{
			
		}

		public static void Load(IMyModContext context)
		{
			if (MyJumpGateCapacitorTerminal.IsLoaded) return;
			MyJumpGateCapacitorTerminal.IsLoaded = true;
			MyJumpGateCapacitorTerminal.SetupJumpGateCapacitorTerminalControls();
			MyJumpGateCapacitorTerminal.SetupJumpGateCapacitorTerminalActions();
			MyJumpGateCapacitorTerminal.SetupJumpGateCapacitorTerminalProperties();
			MyPBJumpGateCapacitor.SetupBlockTerminal();
		}
	}
}
