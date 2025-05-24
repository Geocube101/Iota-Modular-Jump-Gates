using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Text;
using VRage;
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
				settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateCapacitor_Label"));
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateCapacitor;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// OnOffSwitch [Recharge]
			{
				IMyTerminalControlOnOffSwitch do_capacitor_recharge = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + "RechargeOnOff");
				do_capacitor_recharge.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateCapacitor_Recharge")} / {MyTexts.GetString("Terminal_JumpGateCapacitor_Discharge")}");
				do_capacitor_recharge.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateCapacitor_Recharge_Tooltip"));
				do_capacitor_recharge.SupportsMultipleBlocks = true;
				do_capacitor_recharge.Visible = MyJumpGateModSession.IsBlockJumpGateCapacitor;
				do_capacitor_recharge.Enabled = (block) => {
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					return capacitor != null && capacitor.TerminalBlock.IsFunctional && capacitor.JumpGateGrid != null && !capacitor.JumpGateGrid.Closed;
				};
				do_capacitor_recharge.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_On"));
				do_capacitor_recharge.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Off"));

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
				effect_color_shift_cl.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateCapacitor_EmissiveColor"));
				effect_color_shift_cl.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateCapacitor_EmissiveColor_Tooltip"));
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
			{
				// Set True
				IMyTerminalAction enable_recharge_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + $"EnableRecharge");
				enable_recharge_action.Name = new StringBuilder(MyTexts.GetString("Terminal_JumpGateCapacitor_EnableRechargeMode"));
				enable_recharge_action.ValidForGroups = true;
				enable_recharge_action.Icon = @"Textures\GUI\Icons\Actions\StationSwitchOn.dds";
				enable_recharge_action.Enabled = MyJumpGateModSession.IsBlockJumpGateCapacitor;

				enable_recharge_action.Action = (block) => {
					if (!enable_recharge_action.Enabled(block)) return;
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					capacitor.BlockSettings.RechargeEnabled = true;
					capacitor.SetDirty();
				};

				enable_recharge_action.Writer = (block, string_builder) => {
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					if (capacitor == null) return;
					else if (!capacitor.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else string_builder.Append(MyTexts.GetString((capacitor.BlockSettings.RechargeEnabled) ? "Terminal_JumpGateCapacitor_Recharge" : "Terminal_JumpGateCapacitor_Discharge"));
				};

				enable_recharge_action.InvalidToolbarTypes = new List<MyToolbarType>() {
						MyToolbarType.Seat
					};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(enable_recharge_action);

				// Set False
				IMyTerminalAction disable_recharge_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + $"DisableRecharge");
				disable_recharge_action.Name = new StringBuilder(MyTexts.GetString("Terminal_JumpGateCapacitor_DisableRechargeMode"));
				disable_recharge_action.ValidForGroups = true;
				disable_recharge_action.Icon = @"Textures\GUI\Icons\Actions\StationSwitchOff.dds";
				disable_recharge_action.Enabled = MyJumpGateModSession.IsBlockJumpGateCapacitor;

				disable_recharge_action.Action = (block) => {
					if (!enable_recharge_action.Enabled(block)) return;
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					capacitor.BlockSettings.RechargeEnabled = false;
					capacitor.SetDirty();
				};

				disable_recharge_action.Writer = (block, string_builder) => {
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					if (capacitor == null) return;
					else if (!capacitor.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else string_builder.Append(MyTexts.GetString((capacitor.BlockSettings.RechargeEnabled) ? "Terminal_JumpGateCapacitor_Recharge" : "Terminal_JumpGateCapacitor_Discharge"));
				};

				disable_recharge_action.InvalidToolbarTypes = new List<MyToolbarType>() {
						MyToolbarType.Seat
					};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(disable_recharge_action);

				// Toggle
				IMyTerminalAction toggle_recharge_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + $"ToggleRecharge");
				toggle_recharge_action.Name = new StringBuilder(MyTexts.GetString("Terminal_JumpGateCapacitor_ToggleRechargeMode"));
				toggle_recharge_action.ValidForGroups = true;
				toggle_recharge_action.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
				toggle_recharge_action.Enabled = MyJumpGateModSession.IsBlockJumpGateCapacitor;

				toggle_recharge_action.Action = (block) => {
					if (!enable_recharge_action.Enabled(block)) return;
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					capacitor.BlockSettings.RechargeEnabled = !capacitor.BlockSettings.RechargeEnabled;
					capacitor.SetDirty();
				};

				toggle_recharge_action.Writer = (block, string_builder) => {
					MyJumpGateCapacitor capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block);
					if (capacitor == null) return;
					else if (!capacitor.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else string_builder.Append(MyTexts.GetString((capacitor.BlockSettings.RechargeEnabled) ? "Terminal_JumpGateCapacitor_Recharge" : "Terminal_JumpGateCapacitor_Discharge"));
				};

				toggle_recharge_action.InvalidToolbarTypes = new List<MyToolbarType>() {
						MyToolbarType.Seat
					};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(toggle_recharge_action);
			}
		}

		private static void SetupJumpGateCapacitorTerminalProperties()
		{
			
		}

		public static void Load(IMyModContext context)
		{
			List<IMyTerminalControl> controls;
			MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
			if (MyJumpGateCapacitorTerminal.IsLoaded || controls.Count == 0) return;
			MyJumpGateCapacitorTerminal.IsLoaded = true;
			MyJumpGateCapacitorTerminal.SetupJumpGateCapacitorTerminalControls();
			MyJumpGateCapacitorTerminal.SetupJumpGateCapacitorTerminalActions();
			MyJumpGateCapacitorTerminal.SetupJumpGateCapacitorTerminalProperties();
		}
	}
}
