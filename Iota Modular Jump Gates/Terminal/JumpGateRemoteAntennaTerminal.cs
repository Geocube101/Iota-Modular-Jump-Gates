using IOTA.ModularJumpGates.CubeBlock;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.Terminal
{
	internal static class MyJumpGateRemoteAntennaTerminal
	{
		public static bool IsLoaded { get; private set; } = false;
		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".JumpGateRemoteAntenna.";

		private static void SetupJumpGateRemoteAntennaTerminalControls()
		{
			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "RemoteAntennaSettingsLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateRemoteAntenna_Label"));
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Listbox [Jump Gates]
			{
				for (byte channel_ = 0; channel_ < MyJumpGateRemoteAntenna.ChannelCount; ++channel_)
				{
					byte channel = channel_;
					IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntennaJumpGates.{channel}");
					choose_jump_gate_lb.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateRemoteAntenna_ChannelActiveJumpGate").Replace("{%0}", channel.ToString()));
					choose_jump_gate_lb.SupportsMultipleBlocks = false;
					choose_jump_gate_lb.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
					choose_jump_gate_lb.Enabled = (block) => {
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						return antenna != null && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed;
					};
					choose_jump_gate_lb.Multiselect = false;
					choose_jump_gate_lb.VisibleRowsCount = 3;
					choose_jump_gate_lb.ListContent = (block0, content_list, preselect_list) => {
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block0);
						if (antenna == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;
						content_list.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"-- {MyTexts.GetString("Terminal_JumpGateController_Deselect")} --"), MyStringId.GetOrCompute(""), -1L));

						foreach (MyJumpGate jump_gate in antenna.JumpGateGrid.GetJumpGates().OrderBy((gate) => gate.JumpGateID))
						{
							byte gate_channel = antenna.GetJumpGateInboundControlChannel(jump_gate);
							
							if (!jump_gate.Closed && jump_gate.IsValid() && (gate_channel == 0xFF || gate_channel == channel) && (jump_gate.Controller == null || jump_gate.Controller.JumpGateGrid != antenna.JumpGateGrid) && (jump_gate.RemoteAntenna == null || jump_gate.RemoteAntenna == antenna))
							{
								string tooltip = $"{MyTexts.GetString("Terminal_JumpGateController_ActiveJumpGateTooltip0").Replace("{%0}", jump_gate.GetJumpGateDrives().Count().ToString()).Replace("{%1}", jump_gate.JumpGateID.ToString())}";
								MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute(tooltip), jump_gate.JumpGateID);
								content_list.Add(item);
								if (gate_channel == channel && jump_gate.RemoteAntenna == antenna) preselect_list.Add(item);
							}
						}

						foreach (long id in antenna.RegisteredInboundControlGateIDs())
						{
							MyJumpGate jump_gate;
							if (id == -1 || (jump_gate = antenna.JumpGateGrid.GetJumpGate(id)) != null) continue;
							MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ActiveJumpGate_Invalid")), MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ActiveJumpGate_Tooltip2").Replace("{%0}", id.ToString())), -1L);
							content_list.Add(item);
							preselect_list.Add(item);
						}
					};

					choose_jump_gate_lb.ItemSelected = (block, selected) => {
						if (!choose_jump_gate_lb.Enabled(block)) return;
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						long selected_id = (long) selected[0].UserData;

						if (selected_id == -1)
						{
							antenna.SetGateForInboundControl(channel, null);
						}
						else
						{
							MyJumpGate jump_gate = antenna.JumpGateGrid.GetJumpGate(selected_id);
							byte gate_channel = antenna.GetJumpGateInboundControlChannel(jump_gate);
							if (!jump_gate.Closed && jump_gate.IsValid() && (gate_channel == 0xFF || gate_channel == channel) && (jump_gate.Controller == null || jump_gate.Controller.JumpGateGrid != antenna.JumpGateGrid) && (jump_gate.RemoteAntenna == null || jump_gate.RemoteAntenna == antenna)) antenna.SetGateForInboundControl(channel, jump_gate);
						}

						antenna.SetDirty();
						MyJumpGateModSession.Instance.RedrawAllTerminalControls();
					};

					MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_jump_gate_lb);
				}
			}

			// Slider [Antenna Range]
			{
				IMyTerminalControlSlider broadcast_radius_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "RemoteAntennaRange");
				broadcast_radius_sd.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateRemoteAntenna_BroadcastRange")}:");
				broadcast_radius_sd.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateRemoteAntenna_BroadcastRange_Tooltip"));
				broadcast_radius_sd.SupportsMultipleBlocks = true;
				broadcast_radius_sd.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				broadcast_radius_sd.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed;
				};

				broadcast_radius_sd.SetLimits(0, (float) MyJumpGateRemoteAntenna.MaxCollisionDistance);

				broadcast_radius_sd.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (antenna.IsWorking) string_builder.Append(Math.Round(antenna.BlockSettings.AntennaRange, 4));
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};

				broadcast_radius_sd.Getter = (block) => (float) (MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block)?.BlockSettings.AntennaRange ?? 0);
				broadcast_radius_sd.Setter = (block, value) => {
					if (!broadcast_radius_sd.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.AntennaRange = MathHelper.Clamp(value, 0d, 1500d);
					antenna.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(broadcast_radius_sd);
			}

			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// TextBox [Jump Gate Name]
			{
				for (byte channel_ = 0; channel_ < MyJumpGateRemoteAntenna.ChannelCount; ++channel_)
				{
					byte channel = channel_;
					IMyTerminalControlTextbox jump_gate_name_tb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntennaJumpGateName_{channel}");
					jump_gate_name_tb.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateRemoteAntenna_JumpGateName").Replace("{%0}", channel.ToString()));
					jump_gate_name_tb.SupportsMultipleBlocks = true;
					jump_gate_name_tb.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
					jump_gate_name_tb.Enabled = (block) => {
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						MyJumpGate jump_gate = antenna?.GetInboundControlGate(channel);
						return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && antenna.JumpGateGrid.IsValid() && jump_gate != null && jump_gate.IsValid();
					};

					jump_gate_name_tb.Getter = (block) => {
						MyJumpGate jump_gate = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block)?.GetInboundControlGate(channel);
						return new StringBuilder(jump_gate?.GetName() ?? "");
					};

					jump_gate_name_tb.Setter = (block, value) => {
						if (!jump_gate_name_tb.Enabled(block)) return;
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						if (value == null || value.Length == 0) antenna.BlockSettings.JumpGateNames[channel] = null;
						else antenna.BlockSettings.JumpGateNames[channel] = value.ToString().Replace("\n", "↵");
						antenna.SetDirty();
					};

					MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(jump_gate_name_tb);
				}
				;
			}

			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel allowed_settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "RemoteAntennaAllowedSettingsLabel");
				allowed_settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateRemoteAntenna_AllowedControllerSettings"));
				allowed_settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				allowed_settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_settings_label_lb);
			}

			// OnOffSwitch [Allowed Settings]
			{
				foreach (MyAllowedRemoteSettings setting in Enum.GetValues(typeof(MyAllowedRemoteSettings)))
				{
					if (setting == MyAllowedRemoteSettings.NONE || setting == MyAllowedRemoteSettings.ALL) continue;
					MyAllowedRemoteSettings allowed = setting;
					IMyTerminalControlOnOffSwitch do_allow_setting = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntennaAllowChange{allowed}");
					do_allow_setting.Title = MyStringId.GetOrCompute(MyTexts.GetString($"Terminal_JumpGateRemoteAntenna_AllowChange{allowed}"));
					do_allow_setting.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString($"Terminal_JumpGateRemoteAntenna_AllowChange{allowed}_Tooltip"));
					do_allow_setting.SupportsMultipleBlocks = true;
					do_allow_setting.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
					do_allow_setting.Enabled = do_allow_setting.Visible;
					do_allow_setting.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Yes"));
					do_allow_setting.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_No"));

					do_allow_setting.Getter = (block) => ((MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block)?.BlockSettings.AllowedRemoteSettings ?? MyAllowedRemoteSettings.NONE) & allowed) != 0;
					do_allow_setting.Setter = (block, value) => {
						if (!do_allow_setting.Enabled(block)) return;
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						antenna.BlockSettings.AllowSetting(allowed, value);
						antenna.SetDirty();
					};

					MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_allow_setting);
				}
			}
		}

		private static void SetupJumpGateRemoteAntennaTerminalActions()
		{
			{
				foreach (MyAllowedRemoteSettings setting in Enum.GetValues(typeof(MyAllowedRemoteSettings)))
				{
					if (setting == MyAllowedRemoteSettings.NONE || setting == MyAllowedRemoteSettings.ALL) continue;
					MyAllowedRemoteSettings allowed = setting;

					// Set True
					IMyTerminalAction allow_setting_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntennaSetAllow{allowed}Action");
					allow_setting_action.Name = new StringBuilder(MyTexts.GetString($"Terminal_JumpGateRemoteAntenna_EnableAllowChange{allowed}"));
					allow_setting_action.ValidForGroups = true;
					allow_setting_action.Icon = @"Textures\GUI\Icons\Actions\StationSwitchOn.dds";
					allow_setting_action.Enabled = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;

					allow_setting_action.Action = (block) => {
						if (!allow_setting_action.Enabled(block)) return;
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						antenna.BlockSettings.AllowSetting(allowed, true);
						antenna.SetDirty();
					};

					allow_setting_action.Writer = (block, string_builder) => {
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						if (antenna == null) return;
						else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
						else string_builder.Append((antenna.BlockSettings.AllowedRemoteSettings & allowed) != 0);
					};

					allow_setting_action.InvalidToolbarTypes = new List<MyToolbarType>() {
						MyToolbarType.Seat
					};

					MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(allow_setting_action);

					// Set False
					IMyTerminalAction disallow_setting_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntennaSetDisallow{allowed}Action");
					disallow_setting_action.Name = new StringBuilder(MyTexts.GetString($"Terminal_JumpGateRemoteAntenna_DisableAllowChange{allowed}"));
					disallow_setting_action.ValidForGroups = true;
					disallow_setting_action.Icon = @"Textures\GUI\Icons\Actions\StationSwitchOff.dds";
					disallow_setting_action.Enabled = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;

					disallow_setting_action.Action = (block) => {
						if (!disallow_setting_action.Enabled(block)) return;
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						antenna.BlockSettings.AllowSetting(allowed, false);
						antenna.SetDirty();
					};

					disallow_setting_action.Writer = (block, string_builder) => {
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						if (antenna == null) return;
						else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
						else string_builder.Append((antenna.BlockSettings.AllowedRemoteSettings & allowed) != 0);
					};

					disallow_setting_action.InvalidToolbarTypes = new List<MyToolbarType>() {
						MyToolbarType.Seat
					};

					MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(disallow_setting_action);

					// Toggle
					IMyTerminalAction toggle_setting_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntennaToggle{allowed}Action");
					toggle_setting_action.Name = new StringBuilder(MyTexts.GetString($"Terminal_JumpGateRemoteAntenna_ToggleAllowChange{allowed}"));
					toggle_setting_action.ValidForGroups = true;
					toggle_setting_action.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
					toggle_setting_action.Enabled = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;

					toggle_setting_action.Action = (block) => {
						if (!toggle_setting_action.Enabled(block)) return;
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						bool enabled = (antenna.BlockSettings.AllowedRemoteSettings & allowed) != 0;
						antenna.BlockSettings.AllowSetting(allowed, !enabled);
						antenna.SetDirty();
					};

					toggle_setting_action.Writer = (block, string_builder) => {
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						if (antenna == null) return;
						else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
						else string_builder.Append((antenna.BlockSettings.AllowedRemoteSettings & allowed) != 0);
					};

					toggle_setting_action.InvalidToolbarTypes = new List<MyToolbarType>() {
						MyToolbarType.Seat
					};

					MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(toggle_setting_action);
				}
			}

			// Increase range
			{
				IMyTerminalAction increase_range_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntennaIncreaseRangeAction");
				increase_range_action.Name = new StringBuilder(MyTexts.GetString("Terminal_JumpGateRemoteAntenna_IncreaseRange"));
				increase_range_action.ValidForGroups = true;
				increase_range_action.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
				increase_range_action.Enabled = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;

				increase_range_action.Action = (block) => {
					if (!increase_range_action.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.AntennaRange = MathHelper.Clamp(antenna.BlockSettings.AntennaRange + 100, 0, MyJumpGateRemoteAntenna.MaxCollisionDistance);
					antenna.SetDirty();
				};

				increase_range_action.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else string_builder.Append(antenna.BlockSettings.AntennaRange);
				};

				increase_range_action.InvalidToolbarTypes = new List<MyToolbarType>() {
						MyToolbarType.Seat
					};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(increase_range_action);
			}

			// Decrease range
			{
				IMyTerminalAction decrease_range_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntennaDecreaseRangeAction");
				decrease_range_action.Name = new StringBuilder(MyTexts.GetString("Terminal_JumpGateRemoteAntenna_DecreaseRange"));
				decrease_range_action.ValidForGroups = true;
				decrease_range_action.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
				decrease_range_action.Enabled = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;

				decrease_range_action.Action = (block) => {
					if (!decrease_range_action.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.AntennaRange = MathHelper.Clamp(antenna.BlockSettings.AntennaRange - 100, 0, MyJumpGateRemoteAntenna.MaxCollisionDistance);
					antenna.SetDirty();
				};

				decrease_range_action.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else string_builder.Append(antenna.BlockSettings.AntennaRange);
				};

				decrease_range_action.InvalidToolbarTypes = new List<MyToolbarType>() {
						MyToolbarType.Seat
					};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(decrease_range_action);
			}
		}

		private static void SetupJumpGateRemoteAntennaTerminalProperties()
		{

		}

		public static void Load(IMyModContext context)
		{
			if (MyJumpGateRemoteAntennaTerminal.IsLoaded) return;
			MyJumpGateRemoteAntennaTerminal.IsLoaded = true;
			MyJumpGateRemoteAntennaTerminal.SetupJumpGateRemoteAntennaTerminalControls();
			MyJumpGateRemoteAntennaTerminal.SetupJumpGateRemoteAntennaTerminalActions();
			MyJumpGateRemoteAntennaTerminal.SetupJumpGateRemoteAntennaTerminalProperties();
		}
	}
}
