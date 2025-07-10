using IOTA.ModularJumpGates.CubeBlock;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.Terminal
{
	internal static class MyJumpGateRemoteLinkTerminal
	{
		private static readonly List<IMyTerminalControl> TerminalControls = new List<IMyTerminalControl>();

		public static bool IsLoaded { get; private set; } = false;
		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".JumpGateRemoteLink.";

		private static void SetupJumpGateLinkTerminalControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteLink;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "link_settings_label_lb");
				settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateLink_Label"));
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteLink;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Listbox [Remote Links]
			{
				IMyTerminalControlListbox choose_remote_link_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "LinkRemoteLink");
				choose_remote_link_lb.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateLink_TargetLink"));
				choose_remote_link_lb.SupportsMultipleBlocks = false;
				choose_remote_link_lb.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteLink;
				choose_remote_link_lb.Enabled = (block) => {
					MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block);
					return link != null && link.JumpGateGrid != null && !link.JumpGateGrid.Closed && link.IsWorking;
				};
				choose_remote_link_lb.Multiselect = false;
				choose_remote_link_lb.VisibleRowsCount = 5;
				choose_remote_link_lb.ListContent = (block0, content_list, preselect_list) => {
					MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block0);
					if (link == null || link.JumpGateGrid == null || link.JumpGateGrid.Closed || !link.IsWorking) return;
					content_list.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"-- {MyTexts.GetString("Terminal_JumpGateController_Deselect")} --"), MyStringId.NullOrEmpty, null));

					foreach (MyJumpGateRemoteLink sublink in link.GetNearbyLinks().Where((block) => block.IsWorking && block.BlockSettings.ChannelID == link.BlockSettings.ChannelID && (block.AttachedRemoteLink == null || block.AttachedRemoteLink == link) && link.IsFactionRelationValid(block.OwnerSteamID) && block.IsFactionRelationValid(link.OwnerSteamID)).OrderBy((block) => Vector3D.DistanceSquared(link.WorldMatrix.Translation, block.WorldMatrix.Translation)))
					{
						double distance = Vector3D.Distance(link.WorldMatrix.Translation, sublink.WorldMatrix.Translation);
						string name = ((sublink.TerminalBlock?.CustomName?.Length ?? 0) == 0) ? sublink.BlockID.ToString() : sublink.TerminalBlock.CustomName;
						string gridname = sublink.TerminalBlock?.CubeGrid.CustomName ?? sublink.JumpGateGrid?.PrimaryCubeGridCustomName ?? $"ID-{sublink.JumpGateGrid?.CubeGridID.ToString() ?? "N/A"}";
						string tooltip = MyTexts.GetString("Terminal_JumpGateLink_TargetLink_Tooltip").Replace("{%0}", gridname).Replace("{%1}", MyJumpGateModSession.AutoconvertMetricUnits(distance, "M", 2));
						MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{name}"), MyStringId.GetOrCompute(tooltip), sublink);
						
						if (link.AttachedRemoteLink == sublink)
						{
							content_list.Insert(0, item);
							preselect_list.Add(item);
						}
						else content_list.Add(item);
					}
				};
				choose_remote_link_lb.ItemSelected = (block, selected) => {
					if (!choose_remote_link_lb.Enabled(block)) return;
					MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block);
					MyJumpGateRemoteLink remote = link.AttachedRemoteLink;
					MyJumpGateRemoteLink target = (MyJumpGateRemoteLink) selected[0].UserData;

					if (remote == target) return;
					else if (MyNetworkInterface.IsServerLike)
					{
						link.BreakConnection(true);
						link.Connect(target);
						link.SetDirty();
						link.AttachedRemoteLink?.SetDirty();
						remote?.SetDirty();
						MyJumpGateModSession.Instance.RedrawAllTerminalControls();
					}
					else if (MyJumpGateModSession.Network.Registered)
					{
						MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
							PacketType = MyPacketTypeEnum.LINK_CONNECTION,
							TargetID = 0,
							Broadcast = false,
						};
						packet.Payload(new MyJumpGateRemoteLink.MyLinkConnectionInfo {
							ParentTerminalID = link.BlockID,
							ChildTerminalID = target?.BlockID ?? 0,
						});
						packet.Send();
					}
				};
				MyJumpGateRemoteLinkTerminal.TerminalControls.Add(choose_remote_link_lb);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_remote_link_lb);
			}

			// Slider [Link Channel]
			{
				IMyTerminalControlSlider remote_link_channel_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "LinkRemoteChannel");
				remote_link_channel_sd.Title = MyStringId.GetOrCompute($"{MyTexts.GetString("Terminal_JumpGateLink_TargetLinkChannel")}:");
				remote_link_channel_sd.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateLink_TargetLinkChannel_Tooltip"));
				remote_link_channel_sd.SupportsMultipleBlocks = true;
				remote_link_channel_sd.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateRemoteLink(block);
				remote_link_channel_sd.Enabled = (block) => {
					MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block);
					return link != null && link.JumpGateGrid != null && !link.JumpGateGrid.Closed && link.IsWorking;
				};
				remote_link_channel_sd.SetLimits(0, ushort.MaxValue);
				remote_link_channel_sd.Writer = (block, string_builder) => {
					MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block);
					if (link == null) return;
					else if (link.IsWorking) string_builder.Append(link.BlockSettings.ChannelID);
					else string_builder.Append($"- {MyTexts.GetString("GeneralText_Offline")} -");
				};
				remote_link_channel_sd.Getter = (block) => (float) (MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block)?.BlockSettings.ChannelID ?? 0);
				remote_link_channel_sd.Setter = (block, value) => {
					if (!remote_link_channel_sd.Enabled(block)) return;
					MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block);
					link.BlockSettings.ChannelID = (ushort) value;
					link.SetDirty();
				};
				MyJumpGateRemoteLinkTerminal.TerminalControls.Add(remote_link_channel_sd);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(remote_link_channel_sd);
			}

			// OnOffSwitch [Allowed Settings]
			{
				foreach (MyFactionDisplayType setting in Enum.GetValues(typeof(MyFactionDisplayType)))
				{
					MyFactionDisplayType allowed = setting;
					IMyTerminalControlOnOffSwitch do_allow_setting = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + $"LinkAllowFaction{allowed}");
					do_allow_setting.Title = MyStringId.GetOrCompute(MyTexts.GetString($"Terminal_JumpGateRemoteLink_AllowFaction{allowed}"));
					do_allow_setting.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString($"Terminal_JumpGateRemoteLink_AllowFaction{allowed}_Tooltip"));
					do_allow_setting.SupportsMultipleBlocks = true;
					do_allow_setting.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteLink;
					do_allow_setting.Enabled = do_allow_setting.Visible;
					do_allow_setting.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Yes"));
					do_allow_setting.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_No"));
					do_allow_setting.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block)?.BlockSettings.IsFactionConnectionAllowed(allowed) ?? false;
					do_allow_setting.Setter = (block, value) => {
						if (!do_allow_setting.Enabled(block)) return;
						MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block);
						link.BlockSettings.AllowFactionConnection(allowed, value);
						link.SetDirty();
					};
					MyJumpGateRemoteLinkTerminal.TerminalControls.Add(do_allow_setting);
					MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_allow_setting);
				}
			}

			// OnOffSwitch [Show Connection Effect]
			{
				IMyTerminalControlOnOffSwitch do_connection_effect = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + "ConnectionEffectOnOff");
				do_connection_effect.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateLink_DisplayConnectionEffect"));
				do_connection_effect.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateLink_DisplayConnectionEffect_Tooltip"));
				do_connection_effect.SupportsMultipleBlocks = true;
				do_connection_effect.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteLink;
				do_connection_effect.Enabled = (block) => {
					MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block);
					return link != null && link.TerminalBlock.IsFunctional && link.JumpGateGrid != null && !link.JumpGateGrid.Closed;
				};
				do_connection_effect.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_On"));
				do_connection_effect.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Off"));
				do_connection_effect.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block)?.BlockSettings?.DisplayConnectionEffect ?? false;
				do_connection_effect.Setter = (block, value) => {
					MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block);
					if (link == null || link.MarkedForClose) return;
					link.BlockSettings.DisplayConnectionEffect = value;

					if (link.AttachedRemoteLink != null)
					{
						link.AttachedRemoteLink.BlockSettings.DisplayConnectionEffect = value;
						link.AttachedRemoteLink.SetDirty();
					}

					link.SetDirty();
				};
				MyJumpGateRemoteLinkTerminal.TerminalControls.Add(do_connection_effect);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_connection_effect);
			}

			// Color [Emissive Color]
			{
				IMyTerminalControlColor effect_color_shift_cl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyUpgradeModule>(MODID_PREFIX + "LinkEffectColorShift");
				effect_color_shift_cl.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateLink_EmissiveColor"));
				effect_color_shift_cl.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_JumpGateLink_EmissiveColor_Tooltip"));
				effect_color_shift_cl.SupportsMultipleBlocks = true;
				effect_color_shift_cl.Visible = MyJumpGateModSession.IsBlockJumpGateRemoteLink;
				effect_color_shift_cl.Enabled = MyJumpGateModSession.IsBlockJumpGateRemoteLink;
				effect_color_shift_cl.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block)?.BlockSettings?.ConnectionEffectColor ?? Color.White;
				effect_color_shift_cl.Setter = (block, value) => {
					MyJumpGateRemoteLink link = MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block);
					if (link == null || link.MarkedForClose) return;
					link.BlockSettings.ConnectionEffectColor = value;

					if (link.AttachedRemoteLink != null)
					{
						link.AttachedRemoteLink.BlockSettings.ConnectionEffectColor = value;
						link.AttachedRemoteLink.SetDirty();
					}

					link.SetDirty();
				};
				MyJumpGateRemoteLinkTerminal.TerminalControls.Add(effect_color_shift_cl);
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(effect_color_shift_cl);
			}
		}

		private static void SetupJumpGateLinkTerminalActions()
		{
			
		}

		private static void SetupJumpGateLinkTerminalProperties()
		{
			
		}

		public static void Load(IMyModContext context)
		{
			if (MyJumpGateRemoteLinkTerminal.IsLoaded) return;
			MyJumpGateRemoteLinkTerminal.IsLoaded = true;
			MyJumpGateRemoteLinkTerminal.SetupJumpGateLinkTerminalControls();
			MyJumpGateRemoteLinkTerminal.SetupJumpGateLinkTerminalActions();
			MyJumpGateRemoteLinkTerminal.SetupJumpGateLinkTerminalProperties();
		}

		public static void Unload()
		{
			if (!MyJumpGateRemoteLinkTerminal.IsLoaded) return;
			MyJumpGateRemoteLinkTerminal.IsLoaded = false;
			MyJumpGateRemoteLinkTerminal.TerminalControls.Clear();
		}

		public static void UpdateRedrawControls()
		{
			if (!MyJumpGateRemoteLinkTerminal.IsLoaded) return;
			foreach (IMyTerminalControl control in MyJumpGateRemoteLinkTerminal.TerminalControls) control.UpdateVisual();
		}
	}
}
