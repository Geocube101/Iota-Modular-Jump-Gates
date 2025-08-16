using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.Terminal
{
	internal static class MyJumpGateRemoteAntennaTerminal
	{
		private static List<IMyTerminalControl> TerminalControls = new List<IMyTerminalControl>();
		private static Dictionary<IMyTerminalControlOnOffSwitch, bool> SectionSwitches = new Dictionary<IMyTerminalControlOnOffSwitch, bool>();

		public static bool IsLoaded { get; private set; } = false;
		public static string MODID_PREFIX { get; private set; } = MyJumpGateModSession.MODID + ".JumpGateRemoteAntenna.";

		private static ConcurrentDictionary<long, string> AntennaSearchInputs = new ConcurrentDictionary<long, string>();

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
						MyJumpGate old_gate = antenna.GetInboundControlGate(channel);
						long selected_id = (long) selected[0].UserData;

						if (selected_id == -1)
						{
							antenna.SetGateForInboundControl(channel, null);
						}
						else
						{
							MyJumpGate jump_gate = antenna.JumpGateGrid.GetJumpGate(selected_id);
							byte gate_channel = antenna.GetJumpGateInboundControlChannel(jump_gate);

							if (jump_gate != null && !jump_gate.Closed && jump_gate.IsValid() && (gate_channel == 0xFF || gate_channel == channel) && (jump_gate.Controller == null || jump_gate.Controller.JumpGateGrid != antenna.JumpGateGrid) && (jump_gate.RemoteAntenna == null || jump_gate.RemoteAntenna == antenna))
							{
								antenna.SetGateForInboundControl(channel, jump_gate);
								jump_gate.SetDirty();
							}
						}

						antenna.SetDirty();
						old_gate?.SetDirty();
						MyJumpGateModSession.Instance.RedrawAllTerminalControls();
					};
					MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(choose_jump_gate_lb);
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
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(broadcast_radius_sd);
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
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						MyJumpGate jump_gate = antenna?.GetInboundControlGate(channel);
						return new StringBuilder(jump_gate?.GetName() ?? antenna?.GetJumpGateName(channel) ?? "");
					};
					jump_gate_name_tb.Setter = (block, value) => {
						if (!jump_gate_name_tb.Enabled(block)) return;
						MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
						if (value == null || value.Length == 0) antenna.BlockSettings.JumpGateNames[channel] = "";
						else antenna.BlockSettings.JumpGateNames[channel] = value.ToString().Replace("\n", "↵");
						antenna.SetDirty();
					};
					MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(jump_gate_name_tb);
					MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(jump_gate_name_tb);
				}
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
					MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_allow_setting);
					MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_allow_setting);
				}
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
				IMyTerminalControlLabel allowed_settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "RemoteAntennaControllerSettingsLabel");
				allowed_settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateRemoteAntenna_BaseControllerSettings"));
				allowed_settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				allowed_settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_settings_label_lb);
			}

			// Listbox [Terminal Channel]
			{
				IMyTerminalControlListbox choose_channel_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntennaTerminalChannel");
				choose_channel_lb.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateRemoteAntenna_ActiveTerminalChannel"));
				choose_channel_lb.SupportsMultipleBlocks = true;
				choose_channel_lb.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				choose_channel_lb.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed;
				};
				choose_channel_lb.Multiselect = false;
				choose_channel_lb.VisibleRowsCount = 3;
				choose_channel_lb.ListContent = (block1, content_list, preselect_list) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block1);
					if (antenna == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;

					for (byte channel = 0; channel < MyJumpGateRemoteAntenna.ChannelCount; ++channel)
					{
						string listing = MyTexts.GetString("Terminal_JumpGateRemoteAntenna_ActiveTerminalChannelListing").Replace("{%0}", channel.ToString());
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(listing), MyStringId.NullOrEmpty, channel);
						content_list.Add(item);
						if (channel == antenna.CurrentTerminalChannel) preselect_list.Add(item);
					}
				};
				choose_channel_lb.ItemSelected = (block, selected) => {
					if (!choose_channel_lb.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.CurrentTerminalChannel = MathHelper.Clamp((byte) selected[0].UserData, (byte) 0, (byte) (MyJumpGateRemoteAntenna.ChannelCount - 1));
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(choose_channel_lb);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_channel_lb);
			}
		}

		private static void SetupTerminalGateControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			IMyTerminalControlOnOffSwitch section_switch = MyJumpGateRemoteAntennaTerminal.GenerateSectionSwitch("JumpGate", true);

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "AntennaSetupLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_JumpGateSetup"));
				settings_label_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Search [Destinations]
			{
				IMyTerminalControlTextbox search_waypoint_tb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyUpgradeModule>(MODID_PREFIX + "SearchAntennaWaypoint");
				search_waypoint_tb.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_SearchDestinationWaypoints"));
				search_waypoint_tb.SupportsMultipleBlocks = false;
				search_waypoint_tb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				search_waypoint_tb.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null || !antenna.IsWorking || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return false;
					else return antenna.RegisteredInboundControlGates().All((jump_gate) => jump_gate.IsIdle());
				};
				search_waypoint_tb.Getter = (block) => new StringBuilder(MyJumpGateRemoteAntennaTerminal.AntennaSearchInputs.GetValueOrDefault(block.EntityId, ""));
				search_waypoint_tb.Setter = (block, input) => MyJumpGateRemoteAntennaTerminal.AntennaSearchInputs[block.EntityId] = input.ToString();
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(search_waypoint_tb);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(search_waypoint_tb);
			}

			// Listbox [Destinations]
			{
				IMyTerminalControlListbox choose_waypoint_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaWaypoint");
				choose_waypoint_lb.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_DestinationWaypoints"));
				choose_waypoint_lb.SupportsMultipleBlocks = false;
				choose_waypoint_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				choose_waypoint_lb.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (antenna == null || !antenna.IsWorking || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};
				choose_waypoint_lb.Multiselect = false;
				choose_waypoint_lb.VisibleRowsCount = 10;
				choose_waypoint_lb.ListContent = (block1, content_list, preselect_list) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block1);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (antenna == null || !antenna.IsWorking || jump_gate == null || jump_gate.Closed) return;
					MyJumpGateWaypoint selected_waypoint = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].SelectedWaypoint();
					Vector3D jump_node = jump_gate.WorldJumpNode;
					MyWaypointType last_waypoint_type = MyWaypointType.NONE;

					if (selected_waypoint != null)
					{
						MyTerminalControlListBoxItem item;
						MyJumpGate destination_jump_gate;
						Vector3D? endpoint = selected_waypoint.GetEndpoint(out destination_jump_gate);

						if (endpoint != null)
						{
							string name, tooltip;
							selected_waypoint.GetNameAndTooltip(ref jump_node, out name, out tooltip);
							item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(tooltip), selected_waypoint);
							preselect_list.Add(item);
							content_list.Add(item);
						}
					}

					content_list.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"--{MyTexts.GetString("Terminal_JumpGateController_Deselect")}--"), MyStringId.NullOrEmpty, (MyJumpGateWaypoint) null));

					foreach (MyJumpGateWaypoint waypoint in antenna.GetWaypointsList(antenna.CurrentTerminalChannel))
					{
						MyTerminalControlListBoxItem item;
						Vector3D? endpoint = waypoint.GetEndpoint();
						string name, tooltip;
						if (endpoint == null || !MyJumpGateRemoteAntennaTerminal.DoesWaypointMatchSearch(antenna, waypoint, jump_node, out name, out tooltip) || name == null || tooltip == null) continue;
						double distance = Vector3D.Distance(jump_node, endpoint.Value);
						if (distance < jump_gate.JumpGateConfiguration.MinimumJumpDistance || distance > jump_gate.JumpGateConfiguration.MaximumJumpDistance) continue;

						if (waypoint.WaypointType != last_waypoint_type)
						{
							last_waypoint_type = waypoint.WaypointType;

							switch (last_waypoint_type)
							{
								case MyWaypointType.JUMP_GATE:
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(" "), MyStringId.NullOrEmpty, selected_waypoint);
									content_list.Add(item);
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"   -- {MyTexts.GetString("Terminal_JumpGateController_JumpGateWaypointsList")} --"), MyStringId.NullOrEmpty, selected_waypoint);
									content_list.Add(item);
									break;
								case MyWaypointType.BEACON:
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(" "), MyStringId.NullOrEmpty, selected_waypoint);
									content_list.Add(item);
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"   -- {MyTexts.GetString("Terminal_JumpGateController_BeaconWaypointsList")} --"), MyStringId.NullOrEmpty, selected_waypoint);
									content_list.Add(item);
									break;
								case MyWaypointType.GPS:
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(" "), MyStringId.NullOrEmpty, selected_waypoint);
									content_list.Add(item);
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"   -- {MyTexts.GetString("Terminal_JumpGateController_GPSWaypointsList")} --"), MyStringId.NullOrEmpty, selected_waypoint);
									content_list.Add(item);
									break;
								case MyWaypointType.SERVER:
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(" "), MyStringId.NullOrEmpty, selected_waypoint);
									content_list.Add(item);
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"   -- {MyTexts.GetString("Terminal_JumpGateController_ServerWaypointsList")} --"), MyStringId.NullOrEmpty, selected_waypoint);
									content_list.Add(item);
									break;
							}
						}

						item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(tooltip), waypoint);
						if (selected_waypoint == waypoint) preselect_list.Add(item);
						content_list.Add(item);
					}
				};
				choose_waypoint_lb.ItemSelected = (block, selected) => {
					if (!choose_waypoint_lb.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGateWaypoint selected_waypoint = (MyJumpGateWaypoint) selected[0].UserData;
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].SelectedWaypoint(selected_waypoint);
					antenna.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(choose_waypoint_lb);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_waypoint_lb);
			}

			// Listbox [Animations]
			{
				IMyTerminalControlListbox choose_animation_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaAnimation");
				choose_animation_lb.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_JumpGateAnimation"));
				choose_animation_lb.SupportsMultipleBlocks = true;
				choose_animation_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				choose_animation_lb.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (antenna == null || !antenna.IsWorking || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};
				choose_animation_lb.Multiselect = false;
				choose_animation_lb.VisibleRowsCount = 5;
				choose_animation_lb.ListContent = (block2, content_list, preselect_list) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block2);
					if (antenna == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;
					MyJumpGate jump_gate = antenna.GetInboundControlGate(antenna.CurrentTerminalChannel);
					string selected_animation = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpEffectAnimationName();
					string _default = "IOTA.ModularJumpGates.AnimationDef.Standard";

					foreach (string full_name in MyAnimationHandler.GetAnimationNames())
					{
						string animation_name;
						string description;
						AnimationDef animation = MyAnimationHandler.GetAnimationDef(full_name, jump_gate);

						if (animation == null)
						{
							animation_name = $"-{MyTexts.GetString("GeneralText_Unavailable")}-";
							description = MyTexts.GetString("Terminal_JumpGateController_JumpGateAnimationUnavailable");
						}
						else
						{
							animation_name = animation.AnimationName;
							description = full_name;
							if (animation.Description != null && animation.Description.Length > 0) description += $":\n{animation.Description}";
						}

						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(animation_name), MyStringId.GetOrCompute(description), full_name);
						content_list.Add(item);
						if (selected_animation == full_name) preselect_list.Add(item);
						else if ((selected_animation == null || selected_animation.Length == 0) && full_name == _default) preselect_list.Add(item);
					}
				};
				choose_animation_lb.ItemSelected = (block, selected) => {
					if (!choose_animation_lb.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpEffectAnimationName((string) selected[0].UserData);
					antenna.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(choose_animation_lb);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_animation_lb);
			}

			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Checkbox [Allow Inbound]
			{
				IMyTerminalControlCheckbox do_allow_inbound = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowInbound");
				do_allow_inbound.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowInbound"));
				do_allow_inbound.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowInbound_Tooltip"));
				do_allow_inbound.SupportsMultipleBlocks = true;
				do_allow_inbound.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_allow_inbound.Enabled = (block) => true;
				do_allow_inbound.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanBeInbound() ?? false;
				};
				do_allow_inbound.Setter = (block, value) => {
					if (!do_allow_inbound.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanBeInbound(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_allow_inbound);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_allow_inbound);
			}

			// Checkbox [Allow Outbound]
			{
				IMyTerminalControlCheckbox do_allow_outbound = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowOutbound");
				do_allow_outbound.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowOutbound"));
				do_allow_outbound.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowOutbound_Tooltip"));
				do_allow_outbound.SupportsMultipleBlocks = true;
				do_allow_outbound.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_allow_outbound.Enabled = (block) => true;
				do_allow_outbound.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanBeOutbound() ?? false;
				};
				do_allow_outbound.Setter = (block, value) => {
					if (!do_allow_outbound.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanBeOutbound(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_allow_outbound);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_allow_outbound);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "JumpGateSectionBottomSpacer");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}
		}

		private static void SetupTerminalEntityFilterControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			IMyTerminalControlOnOffSwitch section_switch = MyJumpGateRemoteAntennaTerminal.GenerateSectionSwitch("EntityFilter", false);

			// Label
			{
				IMyTerminalControlLabel beacon_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "AntennaFilterSettingsLabel");
				beacon_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_EntityFilterSettings"));
				beacon_label_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				beacon_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(beacon_label_lb);
			}

			// Listbox [Allowed Entities]
			{
				IMyTerminalControlListbox choose_allowed_entities_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "AllowedEntities");
				choose_allowed_entities_lb.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowedEntities"));
				choose_allowed_entities_lb.SupportsMultipleBlocks = false;
				choose_allowed_entities_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				choose_allowed_entities_lb.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (antenna == null || !antenna.IsWorking || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return false;
					else return jump_gate != null && (jump_gate.IsIdle() || jump_gate.IsJumping());
				};
				choose_allowed_entities_lb.Multiselect = true;
				choose_allowed_entities_lb.VisibleRowsCount = 10;
				choose_allowed_entities_lb.ListContent = (block3, content_list, preselect_list) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block3);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (antenna == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed || jump_gate == null || (!jump_gate.IsIdle() && !jump_gate.IsJumping())) return;
					List<long> blacklisted = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].GetBlacklistedEntities();
					MyTerminalControlListBoxItem item;
					if (blacklisted.Count == 0) item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"-- {MyTexts.GetString("Terminal_JumpGateController_DeselectAll")} --"), MyStringId.NullOrEmpty, -1L);
					else item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"-- {MyTexts.GetString("Terminal_JumpGateController_SelectAll")} --"), MyStringId.NullOrEmpty, -2L);
					content_list.Add(item);

					foreach (KeyValuePair<MyEntity, float> pair in jump_gate.GetEntitiesInJumpSpace())
					{
						string entity_name = pair.Key.DisplayName ?? $"E:{pair.Key.EntityId}";
						if (entity_name.Length > 21) entity_name = $"{entity_name.Substring(0, 22)}...";
						string marker = (jump_gate.IsEntityValidForJumpSpace(pair.Key)) ? "[+] " : "[-] ";
						item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(marker + entity_name), MyStringId.GetOrCompute(pair.Key.EntityId.ToString()), pair.Key.EntityId);
						content_list.Add(item);
						if (!blacklisted.Contains(pair.Key.EntityId)) preselect_list.Add(item);
					}
				};
				choose_allowed_entities_lb.ItemSelected = (block, selected) => {
					if (!choose_allowed_entities_lb.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					List<long> whitelisted = selected.Select((item) => (long) item.UserData).ToList();
					MyJumpGateController.MyControllerBlockSettingsStruct block_settings = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel];
					MyJumpGate jump_gate = antenna.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (whitelisted.Contains(-1)) block_settings.SetBlacklistedEntities(jump_gate.GetEntitiesInJumpSpace(false).Select((pair) => pair.Key.EntityId));
					else if (whitelisted.Contains(-2)) block_settings.SetBlacklistedEntities(null);
					else block_settings.SetBlacklistedEntities(jump_gate.GetEntitiesInJumpSpace(false).Select((pair) => pair.Key.EntityId).Where((id) => !whitelisted.Contains(id)));
					antenna.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(choose_allowed_entities_lb);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_allowed_entities_lb);
			}

			// Slider [Allowed Entity Mass]
			{
				IMyTerminalControlSlider allowed_entity_mass_min = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowedEntityMinimumMass");
				allowed_entity_mass_min.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_MinimumMass")} (Kg):");
				allowed_entity_mass_min.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_FilterMinimumMass_Tooltip"));
				allowed_entity_mass_min.SupportsMultipleBlocks = true;
				allowed_entity_mass_min.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				allowed_entity_mass_min.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed;
				};
				allowed_entity_mass_min.SetLimits(0f, 999e24f);
				allowed_entity_mass_min.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (antenna.IsWorking) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedEntityMass().Key, 4)} Kg");
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				allowed_entity_mass_min.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedEntityMass().Key ?? 0;
				};
				allowed_entity_mass_min.Setter = (block, value) => {
					if (!allowed_entity_mass_min.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedEntityMass(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(allowed_entity_mass_min);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_entity_mass_min);

				IMyTerminalControlSlider allowed_entity_mass_max = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowedEntityMaximumMass");
				allowed_entity_mass_max.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_MaximumMass")} (Kg):");
				allowed_entity_mass_max.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_FilterMaximumMass_Tooltip"));
				allowed_entity_mass_max.SupportsMultipleBlocks = true;
				allowed_entity_mass_max.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				allowed_entity_mass_max.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed;
				};
				allowed_entity_mass_max.SetLimits(0f, 999e24f);
				allowed_entity_mass_max.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (antenna.IsWorking) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedEntityMass().Value, 4)} Kg");
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				allowed_entity_mass_max.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedEntityMass().Value ?? 0;
				};
				allowed_entity_mass_max.Setter = (block, value) => {
					if (!allowed_entity_mass_max.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedEntityMass(null, value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(allowed_entity_mass_max);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_entity_mass_max);
			}

			// Slider [Allowed Cube Grid Size]
			{
				IMyTerminalControlSlider allowed_cubegrid_size_min = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowedCubeGridMinimumSize");
				allowed_cubegrid_size_min.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_MinimumSize")}:");
				allowed_cubegrid_size_min.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_MinimumSize_Tooltip"));
				allowed_cubegrid_size_min.SupportsMultipleBlocks = true;
				allowed_cubegrid_size_min.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				allowed_cubegrid_size_min.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed;
				};
				allowed_cubegrid_size_min.SetLimits(0f, uint.MaxValue);
				allowed_cubegrid_size_min.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (antenna.IsWorking)
					{
						uint size = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedCubeGridSize().Key;
						string blockstring = MyTexts.GetString((size == 1) ? "Terminal_JumpGateController_BlockSingular" : "Terminal_JumpGateController_BlockPlural");
						string_builder.Append($"{size} {blockstring}");
					}
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				allowed_cubegrid_size_min.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedCubeGridSize().Key ?? 0;
				};
				allowed_cubegrid_size_min.Setter = (block, value) => {
					if (!allowed_cubegrid_size_min.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedCubeGridSize((uint) value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(allowed_cubegrid_size_min);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_cubegrid_size_min);

				IMyTerminalControlSlider allowed_cubegrid_size_max = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowedCubeGridMaximumSize");
				allowed_cubegrid_size_max.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_MaximumSize")}:");
				allowed_cubegrid_size_max.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_MaximumSize_Tooltip"));
				allowed_cubegrid_size_max.SupportsMultipleBlocks = true;
				allowed_cubegrid_size_max.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				allowed_cubegrid_size_max.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed;
				};
				allowed_cubegrid_size_max.SetLimits(0f, uint.MaxValue);
				allowed_cubegrid_size_max.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (antenna.IsWorking)
					{
						uint size = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedCubeGridSize().Value;
						string blockstring = MyTexts.GetString((size == 1) ? "Terminal_JumpGateController_BlockSingular" : "Terminal_JumpGateController_BlockPlural");
						string_builder.Append($"{size} {blockstring}");
					}
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				allowed_cubegrid_size_max.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedCubeGridSize().Value ?? 0;
				};
				allowed_cubegrid_size_max.Setter = (block, value) => {
					if (!allowed_cubegrid_size_max.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AllowedCubeGridSize(null, (uint) value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(allowed_cubegrid_size_max);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_cubegrid_size_max);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "EntityFilterSectionBottomSpacer");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}
		}

		private static void SetupTerminalAutoActivateControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			IMyTerminalControlOnOffSwitch section_switch = MyJumpGateRemoteAntennaTerminal.GenerateSectionSwitch("AutoJump", false);

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "AntennaAutoJumpSettingsLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AutoJumpSettings"));
				settings_label_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// OnOffSwitch [Auto Activate]
			{
				IMyTerminalControlOnOffSwitch do_auto_activate = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + "CanAutoActivate");
				do_auto_activate.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_EnableAutoActivation"));
				do_auto_activate.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_EnableAutoActivation_Tooltip"));
				do_auto_activate.SupportsMultipleBlocks = true;
				do_auto_activate.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_auto_activate.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed;
				};
				do_auto_activate.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_On"));
				do_auto_activate.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Off"));
				do_auto_activate.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate() ?? false;
				};
				do_auto_activate.Setter = (block, value) => {
					if (!do_auto_activate.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate(value);
					antenna.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_auto_activate);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_auto_activate);
			}

			// Slider [Auto Activate Mass]
			{
				IMyTerminalControlSlider autoactivate_mass_min = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaAutoActivateMinimumMass");
				autoactivate_mass_min.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_MinimumMass")} (Kg):");
				autoactivate_mass_min.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AutoActivateMinimumMass_Tooltip"));
				autoactivate_mass_min.SupportsMultipleBlocks = true;
				autoactivate_mass_min.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				autoactivate_mass_min.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate();
				};
				autoactivate_mass_min.SetLimits(0f, 999e24f);
				autoactivate_mass_min.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate()) string_builder.Append($"- {MyTexts.GetString("GeneralText_Disabled")} -");
					else if (antenna.IsWorking) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivateMass().Key, 4)} Kg");
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				autoactivate_mass_min.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivateMass().Key ?? 0;
				};
				autoactivate_mass_min.Setter = (block, value) => {
					if (!autoactivate_mass_min.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivateMass(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(autoactivate_mass_min);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(autoactivate_mass_min);

				IMyTerminalControlSlider autoactivate_mass_max = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaAutoActivateMaximumMass");
				autoactivate_mass_max.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_MaximumMass")} (Kg):");
				autoactivate_mass_max.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AutoActivateMaximumMass_Tooltip"));
				autoactivate_mass_max.SupportsMultipleBlocks = true;
				autoactivate_mass_max.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				autoactivate_mass_max.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate();
				};
				autoactivate_mass_max.SetLimits(0f, 999e24f);
				autoactivate_mass_max.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate()) string_builder.Append($"- {MyTexts.GetString("GeneralText_Disabled")} -");
					else if (antenna.IsWorking) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivateMass().Value, 4)} Kg");
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				autoactivate_mass_max.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivateMass().Value ?? 0;
				};
				autoactivate_mass_max.Setter = (block, value) => {
					if (!autoactivate_mass_max.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivateMass(null, value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(autoactivate_mass_max);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(autoactivate_mass_max);
			}

			// Slider [Auto Activate Power]
			{
				IMyTerminalControlSlider autoactivate_power_min = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaAutoActivateMinimumPower");
				autoactivate_power_min.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_MinimumPower")} (MW):");
				autoactivate_power_min.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_MinimumPower_Tooltip"));
				autoactivate_power_min.SupportsMultipleBlocks = true;
				autoactivate_power_min.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				autoactivate_power_min.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate();
				};
				autoactivate_power_min.SetLimits(0f, 999e24f);
				autoactivate_power_min.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate()) string_builder.Append($"- {MyTexts.GetString("GeneralText_Disabled")} -");
					else if (antenna.IsWorking) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivatePower().Key, 4)} MW");
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				autoactivate_power_min.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return (float) (antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivatePower().Key ?? 0);
				};
				autoactivate_power_min.Setter = (block, value) => {
					if (!autoactivate_power_min.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivatePower(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(autoactivate_power_min);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(autoactivate_power_min);

				IMyTerminalControlSlider auto_activate_power_max = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaAutoActivateMaximumPower");
				auto_activate_power_max.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_MaximumPower")} (MW):");
				auto_activate_power_max.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_MaximumPower_Tooltip"));
				auto_activate_power_max.SupportsMultipleBlocks = true;
				auto_activate_power_max.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				auto_activate_power_max.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate();
				};
				auto_activate_power_max.SetLimits(0f, 999e24f);
				auto_activate_power_max.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate()) string_builder.Append($"- {MyTexts.GetString("GeneralText_Disabled")} -");
					else if (antenna.IsWorking) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivatePower().Value, 4)} MW");
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				auto_activate_power_max.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return (float) (antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivateMass().Value ?? 0);
				};
				auto_activate_power_max.Setter = (block, value) => {
					if (!auto_activate_power_max.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivatePower(null, value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(auto_activate_power_max);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(auto_activate_power_max);
			}

			// Slider [Auto Activate Delay]
			{
				IMyTerminalControlSlider autoactivate_delay = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaAutoActivateDelay");
				autoactivate_delay.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_ActivationDelay")} (s)");
				autoactivate_delay.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ActivationDelay_Tooltip"));
				autoactivate_delay.SupportsMultipleBlocks = true;
				autoactivate_delay.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				autoactivate_delay.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate();
				};
				autoactivate_delay.SetLimits(0, 60);
				autoactivate_delay.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAutoActivate()) string_builder.Append($"- {MyTexts.GetString("GeneralText_Disabled")} -");
					else if (antenna.IsWorking) string_builder.Append($"{antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivationDelay()} s");
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				autoactivate_delay.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivationDelay() ?? 0;
				};
				autoactivate_delay.Setter = (block, value) => {
					if (!autoactivate_delay.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].AutoActivationDelay(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(autoactivate_delay);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(autoactivate_delay);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "AutoJumpSectionBottomSpacer");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}
		}

		private static void SetupTerminalCommLinkControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			IMyTerminalControlOnOffSwitch section_switch = MyJumpGateRemoteAntennaTerminal.GenerateSectionSwitch("CommLink", false);

			// Label
			{
				IMyTerminalControlLabel commlink_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "AntennaCommLinkLabel");
				commlink_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_CommLinkSettings"));
				commlink_label_lb.Visible = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.HasCommLink() && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				};
				commlink_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(commlink_label_lb);
			}

			// Checkbox [Allow Owned]
			{
				IMyTerminalControlCheckbox do_display_owned = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowOwned");
				do_display_owned.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowOwned"));
				do_display_owned.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowOwned_Tooltip"));
				do_display_owned.SupportsMultipleBlocks = true;
				do_display_owned.Visible = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.HasCommLink() && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				};
				do_display_owned.Enabled = (block) => true;
				do_display_owned.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptOwned() ?? false;
				};
				do_display_owned.Setter = (block, value) => {
					if (!do_display_owned.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptOwned(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_display_owned);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_owned);
			}

			// Checkbox [Allow Friendly]
			{
				IMyTerminalControlCheckbox do_display_friendly = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowFriendly");
				do_display_friendly.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowFriendly"));
				do_display_friendly.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowFriendly_Tooltip"));
				do_display_friendly.SupportsMultipleBlocks = true;
				do_display_friendly.Visible = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.HasCommLink() && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				};
				do_display_friendly.Enabled = (block) => true;
				do_display_friendly.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptFriendly() ?? false;
				};
				do_display_friendly.Setter = (block, value) => {
					if (!do_display_friendly.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptFriendly(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_display_friendly);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_friendly);
			}

			// Checkbox [Allow Neutral]
			{
				IMyTerminalControlCheckbox do_display_neutral = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowNeutral");
				do_display_neutral.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowNeutral"));
				do_display_neutral.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowNeutral_Tooltip"));
				do_display_neutral.SupportsMultipleBlocks = true;
				do_display_neutral.Visible = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.HasCommLink() && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				};
				do_display_neutral.Enabled = (block) => true;
				do_display_neutral.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptNeutral() ?? false;
				};
				do_display_neutral.Setter = (block, value) => {
					if (!do_display_neutral.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptNeutral(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_display_neutral);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_neutral);
			}

			// Checkbox [Allow Enemy]
			{
				IMyTerminalControlCheckbox do_display_enemy = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowEnemy");
				do_display_enemy.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowEnemy"));
				do_display_enemy.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowEnemy_Tooltip"));
				do_display_enemy.SupportsMultipleBlocks = true;
				do_display_enemy.Visible = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.HasCommLink() && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				};
				do_display_enemy.Enabled = (block) => true;
				do_display_enemy.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptEnemy() ?? false;
				};
				do_display_enemy.Setter = (block, value) => {
					if (!do_display_enemy.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptEnemy(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_display_enemy);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_enemy);
			}

			// Checkbox [Allow Unowned]
			{
				IMyTerminalControlCheckbox do_display_unowned = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaAllowUnowned");
				do_display_unowned.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowUnowned"));
				do_display_unowned.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AllowUnowned_Tooltip"));
				do_display_unowned.SupportsMultipleBlocks = true;
				do_display_unowned.Visible = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.HasCommLink() && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				};
				do_display_unowned.Enabled = (block) => true;
				do_display_unowned.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptUnowned() ?? false;
				};
				do_display_unowned.Setter = (block, value) => {
					if (!do_display_unowned.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].CanAcceptUnowned(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_display_unowned);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_unowned);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "CommLinkSectionBottomSpacer");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}
		}

		private static void SetupTerminalDebugControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			IMyTerminalControlOnOffSwitch section_switch = MyJumpGateRemoteAntennaTerminal.GenerateSectionSwitch("Debug", false);

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "AntennaDebugSettingsLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_DebugSettings"));
				settings_label_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// OnOffSwitch [Debug Mode]
			{
				IMyTerminalControlOnOffSwitch do_debug_mode_of = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + "SessionDebugMode");
				do_debug_mode_of.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_DebugMode"));
				do_debug_mode_of.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_DebugMode_Tooltip"));
				do_debug_mode_of.SupportsMultipleBlocks = true;
				do_debug_mode_of.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_debug_mode_of.Enabled = (block) => true;
				do_debug_mode_of.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_On"));
				do_debug_mode_of.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Off"));
				do_debug_mode_of.Getter = (block) => MyJumpGateModSession.Instance.DebugMode;
				do_debug_mode_of.Setter = (block, value) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					MyJumpGateModSession.Instance.DebugMode = value;
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_debug_mode_of);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_debug_mode_of);
			}

			// Button [Rescan Entities]
			{
				IMyTerminalControlButton do_rescan_entities = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "AntennaRescanGateEntities");
				do_rescan_entities.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_DebugRescanEntities"));
				do_rescan_entities.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_DebugRescanEntities_Tooltip"));
				do_rescan_entities.SupportsMultipleBlocks = true;
				do_rescan_entities.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_rescan_entities.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					return antenna != null && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && jump_gate != null && !jump_gate.Closed && (jump_gate.Phase == MyJumpGatePhase.IDLE || jump_gate.Phase == MyJumpGatePhase.CHARGING);
				};
				do_rescan_entities.Action = (block) => {
					MyJumpGateRemoteAntenna antenna;
					if (!do_rescan_entities.Enabled(block)) return;
					else if ((antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block)) == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;
					MyJumpGate jump_gate = antenna.GetInboundControlGate(antenna.CurrentTerminalChannel);
					jump_gate?.SetColliderDirtyMP();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_rescan_entities);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_rescan_entities);
			}

			// Button [Staticify Construct]
			{
				IMyTerminalControlButton do_staticify_construct = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "AntennaStaticifyJumpGateGrid");
				do_staticify_construct.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ConvertToStation"));
				do_staticify_construct.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ConvertToStation_Tooltip"));
				do_staticify_construct.SupportsMultipleBlocks = false;
				do_staticify_construct.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_staticify_construct.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.GetCubeGrids().Any((grid) => !grid.IsStatic && grid.Physics != null && grid.LinearVelocity.Length() < 1e-3 && grid.Physics.AngularVelocity.Length() < 1e-3);
				};
				do_staticify_construct.Action = (block) => {
					MyJumpGateRemoteAntenna antenna;
					if (!do_staticify_construct.Enabled(block)) return;
					else if ((antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block)) == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;
					else if (MyNetworkInterface.IsServerLike) antenna.JumpGateGrid.SetConstructStaticness(true);
					else if (MyJumpGateModSession.Network.Registered)
					{
						MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
							PacketType = MyPacketTypeEnum.STATICIFY_CONSTRUCT,
							Broadcast = false,
							TargetID = 0,
						};

						packet.Payload(new KeyValuePair<long, bool>(antenna.JumpGateGrid.CubeGridID, true));
						packet.Send();
					}
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_staticify_construct);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_staticify_construct);
			}

			// Button [Unstaticify Construct]
			{
				IMyTerminalControlButton do_unstaticify_construct = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "AntennaUnstaticifyJumpGateGrid");
				do_unstaticify_construct.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ConvertToShip"));
				do_unstaticify_construct.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ConvertToShip_Tooltip"));
				do_unstaticify_construct.SupportsMultipleBlocks = false;
				do_unstaticify_construct.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_unstaticify_construct.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.GetCubeGrids().Any((grid) => grid.IsStatic);
				};
				do_unstaticify_construct.Action = (block) => {
					MyJumpGateRemoteAntenna antenna;
					if (!do_unstaticify_construct.Enabled(block)) return;
					else if ((antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block)) == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;
					else if (MyNetworkInterface.IsServerLike) antenna.JumpGateGrid.SetConstructStaticness(false);
					else if (MyJumpGateModSession.Network.Registered)
					{
						MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
							PacketType = MyPacketTypeEnum.STATICIFY_CONSTRUCT,
							Broadcast = false,
							TargetID = 0,
						};

						packet.Payload(new KeyValuePair<long, bool>(antenna.JumpGateGrid.CubeGridID, false));
						packet.Send();
					}
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_unstaticify_construct);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_unstaticify_construct);
			}

			// Button [Force Grid Gate Reconstruction]
			{
				IMyTerminalControlButton do_reconstruct_grid_gates = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "AntennaRebuildGridGates");
				do_reconstruct_grid_gates.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ReconstructGates"));
				do_reconstruct_grid_gates.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ReconstructGates_Tooltip"));
				do_reconstruct_grid_gates.SupportsMultipleBlocks = false;
				do_reconstruct_grid_gates.Visible = (block) => MyNetworkInterface.IsServerLike && MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_reconstruct_grid_gates.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && MyJumpGateModSession.Instance.DebugMode && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.IsSuspended && !antenna.JumpGateGrid.Closed;
				};
				do_reconstruct_grid_gates.Action = (block) => {
					if (!do_reconstruct_grid_gates.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.JumpGateGrid.MarkGatesForUpdate();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_reconstruct_grid_gates);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_reconstruct_grid_gates);
			}

			// Button [Reset Client Grids ]
			{
				IMyTerminalControlButton do_reset_client_grids = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "AntennaResetClientGrids");
				do_reset_client_grids.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ForceGridsUpdate"));
				do_reset_client_grids.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_ForceGridsUpdate_Tooltip"));
				do_reset_client_grids.SupportsMultipleBlocks = false;
				do_reset_client_grids.Visible = (block) => MyNetworkInterface.IsStandaloneMultiplayerClient && MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_reset_client_grids.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && MyJumpGateModSession.Instance.DebugMode;
				};
				do_reset_client_grids.Action = (block) => {
					if (!do_reset_client_grids.Enabled(block)) return;
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
						PacketType = MyPacketTypeEnum.UPDATE_GRIDS,
						TargetID = 0,
						Broadcast = false,
					};
					packet.Send();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_reset_client_grids);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_reset_client_grids);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "DebugSectionBottomSpacer");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}
		}

		private static void SetupTerminalGateExtraControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.HasCommLink();
				};
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			IMyTerminalControlOnOffSwitch section_switch = MyJumpGateRemoteAntennaTerminal.GenerateSectionSwitch("Extra", false);

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "AntennaAdditionalSettingsLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_AdditionalSettings"));
				settings_label_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Slider [Jump Space Radius]
			{
				IMyTerminalControlSlider jump_sphere_radius_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaJumpSpaceRadius");
				jump_sphere_radius_sd.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_JumpSpaceRadius")}:");
				jump_sphere_radius_sd.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_JumpSpaceRadius_Tooltip"));
				jump_sphere_radius_sd.SupportsMultipleBlocks = true;
				jump_sphere_radius_sd.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				jump_sphere_radius_sd.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (antenna == null || !antenna.IsWorking || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};
				jump_sphere_radius_sd.SetLimits((block) => 1f, (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return 0f;
					float raycast_distance = (float) ((antenna.IsLargeGrid) ? MyJumpGateModSession.Configuration.DriveConfiguration.LargeDriveRaycastDistance : MyJumpGateModSession.Configuration.DriveConfiguration.SmallDriveRaycastDistance);
					return 2f * raycast_distance;
				});
				jump_sphere_radius_sd.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (antenna.IsWorking) string_builder.Append(Math.Round(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpSpaceRadius(), 4));
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				jump_sphere_radius_sd.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return (float) (antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpSpaceRadius() ?? 0);
				};
				jump_sphere_radius_sd.Setter = (block, value) => {
					if (!jump_sphere_radius_sd.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpSpaceRadius(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(jump_sphere_radius_sd);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(jump_sphere_radius_sd);
			}

			// Slider [Jump Space Depth]
			{
				IMyTerminalControlSlider jump_space_depth = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "AntennaJumpSpaceDepth");
				jump_space_depth.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_JumpSpaceDepthPercent")}:");
				jump_space_depth.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_JumpSpaceDepthPercent_Tooltip"));
				jump_space_depth.SupportsMultipleBlocks = true;
				jump_space_depth.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				jump_space_depth.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (antenna == null || !antenna.IsWorking || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};
				jump_space_depth.SetLimits(10f, 100f);
				jump_space_depth.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (antenna.IsWorking) string_builder.Append(Math.Round(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpSpaceDepthPercent() * 100, 4));
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				jump_space_depth.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return (float) (antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpSpaceDepthPercent() * 100 ?? 0);
				};
				jump_space_depth.Setter = (block, value) => {
					if (!jump_space_depth.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpSpaceDepthPercent(value / 100d);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(jump_space_depth);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(jump_space_depth);
			}

			// Listbox [Jump Space Fit Type]
			{
				IMyTerminalControlListbox jump_space_fit_type = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "AntennaJumpSpaceFitType");
				jump_space_fit_type.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_JumpSpaceFitType")}:");
				jump_space_fit_type.Multiselect = false;
				jump_space_fit_type.SupportsMultipleBlocks = true;
				jump_space_fit_type.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				jump_space_fit_type.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (antenna == null || !antenna.IsWorking || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};
				jump_space_fit_type.VisibleRowsCount = 3;
				jump_space_fit_type.ListContent = (block, content_list, preselect_list) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;
					MyJumpSpaceFitType selected_fit = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpSpaceFitType();

					foreach (MyJumpSpaceFitType fit_type in Enum.GetValues(typeof(MyJumpSpaceFitType)))
					{
						string enum_name = Enum.GetName(typeof(MyJumpSpaceFitType), fit_type);
						string name = MyTexts.GetString($"JumpSpaceFitType_{enum_name}");
						string tooltip = MyTexts.GetString($"Terminal_JumpGateController_JumpSpaceFitType_{enum_name}_Tooltip");
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(tooltip), fit_type);
						content_list.Add(item);
						if (selected_fit == fit_type) preselect_list.Add(item);
					}
				};
				jump_space_fit_type.ItemSelected = (block, selected) => {
					if (!jump_space_fit_type.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna.GetInboundControlGate(antenna.CurrentTerminalChannel);
					MyJumpSpaceFitType data = (MyJumpSpaceFitType) selected[0].UserData;
					MyJumpSpaceFitType old_data = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpSpaceFitType();
					if (data == old_data) return;
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpSpaceFitType(data);
					antenna.SetDirty();
					jump_gate.SetJumpSpaceEllipsoidDirty();
					jump_gate.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(jump_space_fit_type);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(jump_space_fit_type);
			}

			// Listbox [Gravity Alignment Type]
			{
				IMyTerminalControlListbox gravity_alignment_type = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerGravityAlignmentType");
				gravity_alignment_type.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateController_GravityAlignmentType")}:");
				gravity_alignment_type.Multiselect = false;
				gravity_alignment_type.SupportsMultipleBlocks = true;
				gravity_alignment_type.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				gravity_alignment_type.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					if (antenna == null || !antenna.IsWorking || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};
				gravity_alignment_type.VisibleRowsCount = 4;
				gravity_alignment_type.ListContent = (block, content_list, preselect_list) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;
					MyGravityAlignmentType selected_alignment = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].GravityAlignmentType();

					foreach (MyGravityAlignmentType alignment in Enum.GetValues(typeof(MyGravityAlignmentType)))
					{
						string enum_name = Enum.GetName(typeof(MyGravityAlignmentType), alignment);
						string name = MyTexts.GetString($"GravityAlignmentType_{enum_name}");
						string tooltip = MyTexts.GetString($"Terminal_JumpGateController_GravityAlignmentType_{enum_name}_Tooltip");
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(tooltip), alignment);
						content_list.Add(item);
						if (selected_alignment == alignment) preselect_list.Add(item);
					}
				};
				gravity_alignment_type.ItemSelected = (block, selected) => {
					if (!gravity_alignment_type.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyGravityAlignmentType data = (MyGravityAlignmentType) selected[0].UserData;
					MyGravityAlignmentType old_data = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].GravityAlignmentType();
					if (data == old_data) return;
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].GravityAlignmentType(data);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(gravity_alignment_type);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(gravity_alignment_type);
			}

			// Color [Effect Color Shift]
			{
				IMyTerminalControlColor effect_color_shift_cl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyUpgradeModule>(MODID_PREFIX + "AntennaEffectColorShift");
				effect_color_shift_cl.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_EffectColorShift"));
				effect_color_shift_cl.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_EffectColorShift_Tooltip"));
				effect_color_shift_cl.SupportsMultipleBlocks = true;
				effect_color_shift_cl.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				effect_color_shift_cl.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && (jump_gate == null || jump_gate.IsComplete());
				};
				effect_color_shift_cl.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpEffectAnimationColorShift() ?? Color.White;
				};
				effect_color_shift_cl.Setter = (block, value) => {
					if (!effect_color_shift_cl.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].JumpEffectAnimationColorShift(value);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(effect_color_shift_cl);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(effect_color_shift_cl);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "controller_color_spacer_lb");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				};
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}

			// OnOffSwitch [Vector Normal Override]
			{
				IMyTerminalControlOnOffSwitch do_normal_vector_override_off = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + "VectorNormalOverride");
				do_normal_vector_override_off.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_VectorNormalOverride"));
				do_normal_vector_override_off.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_VectorNormalOverride_Tooltip"));
				do_normal_vector_override_off.SupportsMultipleBlocks = true;
				do_normal_vector_override_off.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				do_normal_vector_override_off.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					return antenna != null && antenna.IsWorking && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && (jump_gate == null || (jump_gate.IsComplete() && jump_gate.IsIdle()));
				};
				do_normal_vector_override_off.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_On"));
				do_normal_vector_override_off.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Off"));
				do_normal_vector_override_off.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					return antenna?.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride() ?? false;
				};
				do_normal_vector_override_off.Setter = (block, value) => {
					if (!do_normal_vector_override_off.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride(value);
					antenna.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(do_normal_vector_override_off);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_normal_vector_override_off);
			}

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "AntennaVectorNormalOverrideLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_NormalVector"));
				settings_label_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Slider [Vector Normal X]
			{
				IMyTerminalControlSlider vector_normal_x_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "NormalVectorX");
				vector_normal_x_sd.Title = MyStringId.GetOrCompute("X:");
				vector_normal_x_sd.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_NormalVectorComponent_Tooltip").Replace("{%0}", "X"));
				vector_normal_x_sd.SupportsMultipleBlocks = true;
				vector_normal_x_sd.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				vector_normal_x_sd.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					return antenna != null && antenna.IsWorking && antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride() && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && (jump_gate == null || (!jump_gate.Closed && jump_gate.IsIdle()));
				};
				vector_normal_x_sd.SetLimits(0, 360);
				vector_normal_x_sd.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else if (!antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride()) string_builder.Append("- DISABLED -");
					else string_builder.Append(Math.Round(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride().Value.X * (180d / Math.PI), 4));
				};
				vector_normal_x_sd.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null || !antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride()) return 0;
					return (float) (antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride().Value.X * (180d / Math.PI));
				};
				vector_normal_x_sd.Setter = (block, value) => {
					if (!vector_normal_x_sd.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					Vector3D normal_override = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride().Value;
					normal_override.X = (value % 360) * (Math.PI / 180d);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride(normal_override);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(vector_normal_x_sd);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(vector_normal_x_sd);
			}

			// Slider [Vector Normal Y]
			{
				IMyTerminalControlSlider vector_normal_y_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "NormalVectorY");
				vector_normal_y_sd.Title = MyStringId.GetOrCompute("Y:");
				vector_normal_y_sd.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_NormalVectorComponent_Tooltip").Replace("{%0}", "Y"));
				vector_normal_y_sd.SupportsMultipleBlocks = true;
				vector_normal_y_sd.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				vector_normal_y_sd.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					return antenna != null && antenna.IsWorking && antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride() && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && (jump_gate == null || (!jump_gate.Closed && jump_gate.IsIdle()));
				};
				vector_normal_y_sd.SetLimits(0, 360);
				vector_normal_y_sd.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else if (!antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride()) string_builder.Append("- DISABLED -");
					else string_builder.Append(Math.Round(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride().Value.Y * (180d / Math.PI), 4));
				};
				vector_normal_y_sd.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null || !antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride()) return 0;
					return (float) (antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride().Value.Y * (180d / Math.PI));
				};
				vector_normal_y_sd.Setter = (block, value) => {
					if (!vector_normal_y_sd.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					Vector3D normal_override = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride().Value;
					normal_override.Y = (value % 360) * (Math.PI / 180d);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride(normal_override);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(vector_normal_y_sd);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(vector_normal_y_sd);
			}

			// Slider [Vector Normal Z]
			{
				IMyTerminalControlSlider vector_normal_z_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "NormalVectorZ");
				vector_normal_z_sd.Title = MyStringId.GetOrCompute("Z:");
				vector_normal_z_sd.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateController_NormalVectorComponent_Tooltip").Replace("{%0}", "Z"));
				vector_normal_z_sd.SupportsMultipleBlocks = true;
				vector_normal_z_sd.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				vector_normal_z_sd.Enabled = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					MyJumpGate jump_gate = antenna?.GetInboundControlGate(antenna.CurrentTerminalChannel);
					return antenna != null && antenna.IsWorking && antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride() && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && (jump_gate == null || (!jump_gate.Closed && jump_gate.IsIdle()));
				};
				vector_normal_z_sd.SetLimits(0, 360);
				vector_normal_z_sd.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null) return;
					else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else if (!antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride()) string_builder.Append("- DISABLED -");
					else string_builder.Append(Math.Round(antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride().Value.Z * (180d / Math.PI), 4));
				};
				vector_normal_z_sd.Getter = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null || !antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].HasVectorNormalOverride()) return 0;
					return (float) (antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride().Value.Z * (180d / Math.PI));
				};
				vector_normal_z_sd.Setter = (block, value) => {
					if (!vector_normal_z_sd.Enabled(block)) return;
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					Vector3D normal_override = antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride().Value;
					normal_override.Z = (value % 360) * (Math.PI / 180d);
					antenna.BlockSettings.BaseControllerSettings[antenna.CurrentTerminalChannel].VectorNormalOverride(normal_override);
					antenna.SetDirty();
				};
				MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(vector_normal_z_sd);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(vector_normal_z_sd);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ExtraSectionBottomSpacer");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}
		}

		private static void SetupJumpGateRemoteAntennaTerminalActions()
		{
			// Allowed Settings
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

			// Staticify
			{
				IMyTerminalAction jump_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "AntennaStaticifyJumpGateGridAction");
				jump_action.Name = new StringBuilder(MyTexts.GetString("Terminal_JumpGateController_ConvertToStation"));
				jump_action.ValidForGroups = true;
				jump_action.Icon = @"Textures\GUI\Icons\Actions\StationSwitchOn.dds";
				jump_action.Enabled = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;

				jump_action.Action = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed || !antenna.JumpGateGrid.GetCubeGrids().Any((grid) => !grid.IsStatic && grid.Physics != null && grid.LinearVelocity.Length() < 1e-3 && grid.Physics.AngularVelocity.Length() < 1e-3)) return;
					else if (MyNetworkInterface.IsServerLike) antenna.JumpGateGrid.SetConstructStaticness(true);
					else if (MyJumpGateModSession.Network.Registered)
					{
						MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
							PacketType = MyPacketTypeEnum.STATICIFY_CONSTRUCT,
							Broadcast = false,
							TargetID = 0,
						};
						packet.Payload(new KeyValuePair<long, bool>(antenna.JumpGateGrid.CubeGridID, true));
						packet.Send();
					}
				};

				jump_action.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;
					else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else
					{
						int count = 0;
						int total = 0;

						foreach (IMyCubeGrid grid in antenna.JumpGateGrid.GetCubeGrids())
						{
							++total;
							count += (grid.IsStatic) ? 1 : 0;
						}

						string_builder.Append($"{count}/{total}");
					}
				};

				jump_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(jump_action);
			}

			// Unstaticify
			{
				IMyTerminalAction jump_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "AntennaUnstaticifyJumpGateGridAction");
				jump_action.Name = new StringBuilder(MyTexts.GetString("Terminal_JumpGateController_ConvertToShip"));
				jump_action.ValidForGroups = true;
				jump_action.Icon = @"Textures\GUI\Icons\Actions\StationSwitchOff.dds";
				jump_action.Enabled = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;

				jump_action.Action = (block) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					bool enabled = antenna != null && antenna.JumpGateGrid != null && !antenna.JumpGateGrid.Closed && antenna.JumpGateGrid.GetCubeGrids().Any((grid) => grid.IsStatic);
					if (!enabled) return;
					else if (MyNetworkInterface.IsServerLike) antenna.JumpGateGrid.SetConstructStaticness(false);
					else if (MyJumpGateModSession.Network.Registered)
					{
						MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
						{
							PacketType = MyPacketTypeEnum.STATICIFY_CONSTRUCT,
							Broadcast = false,
							TargetID = 0,
						};

						packet.Payload(new KeyValuePair<long, bool>(antenna.JumpGateGrid.CubeGridID, false));
						packet.Send();
					}
				};

				jump_action.Writer = (block, string_builder) => {
					MyJumpGateRemoteAntenna antenna = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block);
					if (antenna == null || antenna.JumpGateGrid == null || antenna.JumpGateGrid.Closed) return;
					else if (!antenna.IsWorking) string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
					else
					{
						int count = 0;
						int total = 0;

						foreach (IMyCubeGrid grid in antenna.JumpGateGrid.GetCubeGrids())
						{
							++total;
							count += (grid.IsStatic) ? 1 : 0;
						}

						string_builder.Append($"{count}/{total}");
					}
				};

				jump_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(jump_action);
			}
		}

		private static void SetupJumpGateRemoteAntennaTerminalProperties()
		{

		}

		private static IMyTerminalControlOnOffSwitch GenerateSectionSwitch(string name, bool default_enabled)
		{
			// Section Switch
			IMyTerminalControlOnOffSwitch section_switch = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + $"{name}SectionSwitch");
			section_switch.Title = MyStringId.GetOrCompute(MyTexts.GetString($"Terminal_JumpGateController_{name}SectionSwitch"));
			section_switch.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString($"Terminal_JumpGateController_{name}SectionSwitch_Tooltip"));
			section_switch.SupportsMultipleBlocks = true;
			section_switch.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteAntenna;
			section_switch.Enabled = (block) => true;
			section_switch.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_On"));
			section_switch.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Off"));
			section_switch.Getter = (block) => MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
			section_switch.Setter = (block, value) => {
				if (!MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block)) return;
				MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch] = value;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				IMyEntity interacting = MyAPIGateway.Gui.InteractedEntity;
				MyAPIGateway.Gui.ChangeInteractedEntity(interacting, false);
			};
			//MyJumpGateRemoteAntennaTerminal.TerminalControls.Add(section_switch);
			MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch] = true;

			// Spacer
			IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + $"RemoteAntenna{name}SectionSwitchSpacer");
			spacer_lb.Label = MyStringId.GetOrCompute(" ");
			spacer_lb.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block) && MyJumpGateRemoteAntennaTerminal.SectionSwitches[section_switch];
			spacer_lb.SupportsMultipleBlocks = true;

			//MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			//MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(section_switch);
			return section_switch;
		}

		public static void Load(IMyModContext context)
		{
			if (MyJumpGateRemoteAntennaTerminal.IsLoaded) return;
			MyJumpGateRemoteAntennaTerminal.IsLoaded = true;
			MyJumpGateRemoteAntennaTerminal.SetupJumpGateRemoteAntennaTerminalControls();
			MyJumpGateRemoteAntennaTerminal.SetupJumpGateRemoteAntennaTerminalActions();
			MyJumpGateRemoteAntennaTerminal.SetupJumpGateRemoteAntennaTerminalProperties();

			MyJumpGateRemoteAntennaTerminal.SetupTerminalGateControls();
			MyJumpGateRemoteAntennaTerminal.SetupTerminalCommLinkControls();
			MyJumpGateRemoteAntennaTerminal.SetupTerminalAutoActivateControls();
			MyJumpGateRemoteAntennaTerminal.SetupTerminalEntityFilterControls();
			MyJumpGateRemoteAntennaTerminal.SetupTerminalGateExtraControls();
			MyJumpGateRemoteAntennaTerminal.SetupTerminalDebugControls();
		}

		public static void Unload()
		{
			if (!MyJumpGateRemoteAntennaTerminal.IsLoaded) return;
			MyJumpGateRemoteAntennaTerminal.IsLoaded = false;
			MyJumpGateRemoteAntennaTerminal.TerminalControls.Clear();
			MyJumpGateRemoteAntennaTerminal.SectionSwitches.Clear();
			MyJumpGateRemoteAntennaTerminal.AntennaSearchInputs.Clear();
			MyJumpGateRemoteAntennaTerminal.TerminalControls = null;
			MyJumpGateRemoteAntennaTerminal.SectionSwitches = null;
			MyJumpGateRemoteAntennaTerminal.AntennaSearchInputs = null;
			MyJumpGateRemoteAntennaTerminal.MODID_PREFIX = null;
		}

		public static void ResetSearchInputs()
		{
			MyJumpGateRemoteAntennaTerminal.AntennaSearchInputs.Clear();
		}

		public static bool DoesWaypointMatchSearch(MyJumpGateRemoteAntenna antenna, MyJumpGateWaypoint waypoint, Vector3D this_pos, out string name, out string tooltip)
		{
			name = null;
			tooltip = null;
			if (antenna == null || waypoint == null) return false;
			string input = MyJumpGateRemoteAntennaTerminal.AntennaSearchInputs.GetValueOrDefault(antenna.BlockID, null);
			waypoint.GetNameAndTooltip(ref this_pos, out name, out tooltip);
			Vector3D? null_point;
			MyJumpGate destination_jump_gate;
			if (input == null || input.Length == 0) return true;
			else if ((null_point = waypoint.GetEndpoint(out destination_jump_gate)) == null) return false;
			double distance = Vector3D.Distance(this_pos, null_point.Value);
			bool matched = true;

			foreach (string filter in input.ToLowerInvariant().Split(new char[] { ';' }))
			{
				if (filter.StartsWith("#"))
				{
					string[] parts = filter.Split(new char[] { '=' }, 2);
					if (parts.Length < 2) matched = matched && true;
					else
					{
						switch (parts[0])
						{
							case "#type":
							{
								MyWaypointType waypoint_type;
								matched = matched && Enum.TryParse<MyWaypointType>(parts[1], true, out waypoint_type) && waypoint.WaypointType == waypoint_type;
								break;
							}
							case "#distance":
							{
								string[] ranges = parts[1].Split(new string[] { ".." }, StringSplitOptions.None);
								Logger.Log(string.Join(" / ", ranges));
								if (ranges.Length == 0) continue;
								else if (ranges.Length == 1)
								{
									double range;
									matched = matched && double.TryParse(ranges[0], out range) && Math.Abs(range - distance) <= 1;
								}
								else if (ranges.Length >= 2 && ranges[0].Length == 0)
								{
									double range;
									matched = matched && double.TryParse(ranges[ranges.Length - 1], out range) && distance <= range;
								}
								else if (ranges.Length >= 2 && ranges[ranges.Length - 1].Length == 0)
								{
									double range;
									matched = matched && double.TryParse(ranges[0], out range) && distance >= range;
								}
								else if (ranges.Length >= 2)
								{
									double min_range, max_range;
									matched = matched && double.TryParse(ranges[0], out min_range) && double.TryParse(ranges[ranges.Length - 1], out max_range) && min_range <= distance && distance <= max_range;
								}

								break;
							}
							case "#jumpgate":
							case "#gate":
							{
								long gateid;
								matched = matched && waypoint.WaypointType == MyWaypointType.JUMP_GATE && long.TryParse(parts[1], out gateid) && waypoint.JumpGate.GetJumpGate() == gateid;
								break;
							}
							case "#gridname":
							{
								string gridname = null;
								if (waypoint.WaypointType == MyWaypointType.JUMP_GATE) gridname = destination_jump_gate?.JumpGateGrid?.PrimaryCubeGridCustomName;
								else if (waypoint.WaypointType == MyWaypointType.BEACON) gridname = waypoint.Beacon.CubeGridCustomName;
								else matched = false;
								matched = matched && gridname != null && gridname.ToLowerInvariant().Contains(parts[1]);
								break;
							}
						}
					}
				}
				else matched = matched && name.ToLowerInvariant().Contains(filter);
			}

			return matched;
		}

		public static void UpdateRedrawControls()
		{
			if (!MyJumpGateRemoteAntennaTerminal.IsLoaded) return;
			foreach (IMyTerminalControl control in MyJumpGateRemoteAntennaTerminal.TerminalControls) control.UpdateVisual();
		}
	}
}
