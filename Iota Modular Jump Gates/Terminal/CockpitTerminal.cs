using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;

namespace IOTA.ModularJumpGates.Terminal
{
	internal static class MyCockpitTerminal
	{
		private static List<IMyTerminalControl> TerminalControls = new List<IMyTerminalControl>();

		public static bool IsLoaded { get; private set; } = false;
		public static string MODID_PREFIX { get; private set; } = MyJumpGateModSession.Instance.ModID + ".ShipCockpit.";

		private static void SetupJumpGateBlockTerminalControls()
		{
			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyCockpit>(MODID_PREFIX + "CockpitSettingsLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_ShipCockpit_Label"));
				settings_label_lb.Visible = (block) => block is IMyCockpit;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyCockpitTerminal.TerminalControls.Add(settings_label_lb);
				MyAPIGateway.TerminalControls.AddControl<IMyCockpit>(settings_label_lb);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyCockpit>(MODID_PREFIX + "ShipCockpitSettingsTopSpacer");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = (block) => block is IMyCockpit;
				spacer_lb.SupportsMultipleBlocks = true;
				MyCockpitTerminal.TerminalControls.Add(spacer_lb);
				MyAPIGateway.TerminalControls.AddControl<IMyCockpit>(spacer_lb);
			}

			// OnOffSwitch [Show HUD Markers]
			{
				IMyTerminalControlOnOffSwitch do_show_hud_markers = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyCockpit>(MODID_PREFIX + "DoShowHudMarkers");
				do_show_hud_markers.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_ShipCockpit_ShowHudMarkers"));
				do_show_hud_markers.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_ShipCockpit_ShowHudMarkers_Tooltip"));
				do_show_hud_markers.SupportsMultipleBlocks = true;
				do_show_hud_markers.Visible = (block) => block is IMyCockpit;
				do_show_hud_markers.Enabled = (block) => block.IsFunctional;
				do_show_hud_markers.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Yes"));
				do_show_hud_markers.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_No"));
				do_show_hud_markers.Getter = (block) => MyJumpGateModSession.Instance.GetCockpitTerminalSettings(block as IMyCockpit)?.DisplayHudMarkers ?? false;
				do_show_hud_markers.Setter = (block, value) => {
					if (!do_show_hud_markers.Enabled(block)) return;
					IMyCockpit cockpit = (IMyCockpit) block;
					MyJumpGateModSession.MyCockpitInfo settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit);
					settings.DisplayHudMarkers = value;
					MyJumpGateModSession.Instance.UpdateCockpitTerminalSettings(cockpit, settings);
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyCockpitTerminal.TerminalControls.Add(do_show_hud_markers);
				MyAPIGateway.TerminalControls.AddControl<IMyCockpit>(do_show_hud_markers);
			}

			// OnOffSwitch [Show Markers for Self]
			{
				IMyTerminalControlOnOffSwitch do_show_hud_markers_for_self = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyCockpit>(MODID_PREFIX + "DoShowHudMarkersForSelf");
				do_show_hud_markers_for_self.Title = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_ShipCockpit_ShowHudMarkersForSelf"));
				do_show_hud_markers_for_self.Tooltip = MyStringId.GetOrCompute(MyTexts.GetString("Terminal_ShipCockpit_ShowHudMarkersForSelf_Tooltip"));
				do_show_hud_markers_for_self.SupportsMultipleBlocks = true;
				do_show_hud_markers_for_self.Visible = (block) => block is IMyCockpit;
				do_show_hud_markers_for_self.Enabled = (block) => block.IsFunctional;
				do_show_hud_markers_for_self.OnText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_Yes"));
				do_show_hud_markers_for_self.OffText = MyStringId.GetOrCompute(MyTexts.GetString("GeneralText_No"));
				do_show_hud_markers_for_self.Getter = (block) => MyJumpGateModSession.Instance.GetCockpitTerminalSettings(block as IMyCockpit)?.DisplayForAttachedGates ?? false;
				do_show_hud_markers_for_self.Setter = (block, value) => {
					if (!do_show_hud_markers_for_self.Enabled(block)) return;
					IMyCockpit cockpit = (IMyCockpit) block;
					MyJumpGateModSession.MyCockpitInfo settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit);
					settings.DisplayForAttachedGates = value;
					MyJumpGateModSession.Instance.UpdateCockpitTerminalSettings(cockpit, settings);
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				MyCockpitTerminal.TerminalControls.Add(do_show_hud_markers_for_self);
				MyAPIGateway.TerminalControls.AddControl<IMyCockpit>(do_show_hud_markers_for_self);
			}
		}

		private static void SetupJumpGateBlockTerminalActions()
		{
			// Enable Show HUD Markers
			{
				IMyTerminalAction show_hud_markers_action = MyAPIGateway.TerminalControls.CreateAction<IMyCockpit>(MODID_PREFIX + "EnableDisplayHudMarkersAction");
				show_hud_markers_action.Name = new StringBuilder(MyTexts.GetString("Terminal_ShipCockpitAction_EnableHudMarkers"));
				show_hud_markers_action.ValidForGroups = true;
				show_hud_markers_action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
				show_hud_markers_action.Enabled = (block) => block is IMyCockpit;
				show_hud_markers_action.Action = (block) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null || settings.DisplayHudMarkers) return;
					settings.DisplayHudMarkers = true;
					MyJumpGateModSession.Instance.UpdateCockpitTerminalSettings(cockpit, settings);
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				show_hud_markers_action.Writer = (block, string_builder) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null) return;
					else string_builder.Append(MyTexts.GetString((settings.DisplayHudMarkers) ? "GeneralText_EnabledLC" : "GeneralText_DisabledLC"));
				};
				show_hud_markers_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat,
				};
				MyAPIGateway.TerminalControls.AddAction<IMyCockpit>(show_hud_markers_action);
			}

			// Disable Show HUD Markers
			{
				IMyTerminalAction show_hud_markers_action = MyAPIGateway.TerminalControls.CreateAction<IMyCockpit>(MODID_PREFIX + "DisableDisplayHudMarkersAction");
				show_hud_markers_action.Name = new StringBuilder(MyTexts.GetString("Terminal_ShipCockpitAction_DisableHudMarkers"));
				show_hud_markers_action.ValidForGroups = true;
				show_hud_markers_action.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
				show_hud_markers_action.Enabled = (block) => block is IMyCockpit;
				show_hud_markers_action.Action = (block) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null || !settings.DisplayHudMarkers) return;
					settings.DisplayHudMarkers = false;
					MyJumpGateModSession.Instance.UpdateCockpitTerminalSettings(cockpit, settings);
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				show_hud_markers_action.Writer = (block, string_builder) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null) return;
					else string_builder.Append(MyTexts.GetString((settings.DisplayHudMarkers) ? "GeneralText_EnabledLC" : "GeneralText_DisabledLC"));
				};
				show_hud_markers_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat,
				};
				MyAPIGateway.TerminalControls.AddAction<IMyCockpit>(show_hud_markers_action);
			}

			// Toggle Show HUD Markers
			{
				IMyTerminalAction show_hud_markers_action = MyAPIGateway.TerminalControls.CreateAction<IMyCockpit>(MODID_PREFIX + "ToggleDisplayHudMarkersAction");
				show_hud_markers_action.Name = new StringBuilder(MyTexts.GetString("Terminal_ShipCockpitAction_ToggleHudMarkers"));
				show_hud_markers_action.ValidForGroups = true;
				show_hud_markers_action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
				show_hud_markers_action.Enabled = (block) => block is IMyCockpit;
				show_hud_markers_action.Action = (block) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null) return;
					settings.DisplayHudMarkers = !settings.DisplayHudMarkers;
					MyJumpGateModSession.Instance.UpdateCockpitTerminalSettings(cockpit, settings);
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				show_hud_markers_action.Writer = (block, string_builder) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null) return;
					else string_builder.Append(MyTexts.GetString((settings.DisplayHudMarkers) ? "GeneralText_EnabledLC" : "GeneralText_DisabledLC"));
				};
				show_hud_markers_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat,
				};
				MyAPIGateway.TerminalControls.AddAction<IMyCockpit>(show_hud_markers_action);
			}

			// Enable Show HUD Markers For Self
			{
				IMyTerminalAction show_hud_markers_action = MyAPIGateway.TerminalControls.CreateAction<IMyCockpit>(MODID_PREFIX + "EnableDisplayHudMarkersForSelfAction");
				show_hud_markers_action.Name = new StringBuilder(MyTexts.GetString("Terminal_ShipCockpitAction_EnableHudMarkersForSelf"));
				show_hud_markers_action.ValidForGroups = true;
				show_hud_markers_action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
				show_hud_markers_action.Enabled = (block) => block is IMyCockpit;
				show_hud_markers_action.Action = (block) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null || settings.DisplayForAttachedGates) return;
					settings.DisplayForAttachedGates = true;
					MyJumpGateModSession.Instance.UpdateCockpitTerminalSettings(cockpit, settings);
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				show_hud_markers_action.Writer = (block, string_builder) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null) return;
					else string_builder.Append(MyTexts.GetString((settings.DisplayForAttachedGates) ? "GeneralText_EnabledLC" : "GeneralText_DisabledLC"));
				};
				show_hud_markers_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat,
				};
				MyAPIGateway.TerminalControls.AddAction<IMyCockpit>(show_hud_markers_action);
			}

			// Disable Show HUD Markers For Self
			{
				IMyTerminalAction show_hud_markers_action = MyAPIGateway.TerminalControls.CreateAction<IMyCockpit>(MODID_PREFIX + "DisableDisplayHudMarkersForSelfAction");
				show_hud_markers_action.Name = new StringBuilder(MyTexts.GetString("Terminal_ShipCockpitAction_DisableHudMarkersForSelf"));
				show_hud_markers_action.ValidForGroups = true;
				show_hud_markers_action.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
				show_hud_markers_action.Enabled = (block) => block is IMyCockpit;
				show_hud_markers_action.Action = (block) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null || !settings.DisplayForAttachedGates) return;
					settings.DisplayForAttachedGates = false;
					MyJumpGateModSession.Instance.UpdateCockpitTerminalSettings(cockpit, settings);
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				show_hud_markers_action.Writer = (block, string_builder) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null) return;
					else string_builder.Append(MyTexts.GetString((settings.DisplayForAttachedGates) ? "GeneralText_EnabledLC" : "GeneralText_DisabledLC"));
				};
				show_hud_markers_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat,
				};
				MyAPIGateway.TerminalControls.AddAction<IMyCockpit>(show_hud_markers_action);
			}

			// Show HUD Markers For Self
			{
				IMyTerminalAction show_hud_markers_action = MyAPIGateway.TerminalControls.CreateAction<IMyCockpit>(MODID_PREFIX + "ToggleDisplayHudMarkersForSelfAction");
				show_hud_markers_action.Name = new StringBuilder(MyTexts.GetString("Terminal_ShipCockpitAction_ToggleHudMarkersForSelf"));
				show_hud_markers_action.ValidForGroups = true;
				show_hud_markers_action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
				show_hud_markers_action.Enabled = (block) => block is IMyCockpit;
				show_hud_markers_action.Action = (block) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null) return;
					settings.DisplayForAttachedGates = !settings.DisplayForAttachedGates;
					MyJumpGateModSession.Instance.UpdateCockpitTerminalSettings(cockpit, settings);
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};
				show_hud_markers_action.Writer = (block, string_builder) => {
					IMyCockpit cockpit = block as IMyCockpit;
					MyJumpGateModSession.MyCockpitInfo settings;
					if (cockpit == null || cockpit.MarkedForClose || (settings = MyJumpGateModSession.Instance.GetCockpitTerminalSettings(cockpit)) == null) return;
					else string_builder.Append(MyTexts.GetString((settings.DisplayForAttachedGates) ? "GeneralText_EnabledLC" : "GeneralText_DisabledLC"));
				};
				show_hud_markers_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat,
				};
				MyAPIGateway.TerminalControls.AddAction<IMyCockpit>(show_hud_markers_action);
			}
		}

		private static void SetupJumpGateBlockTerminalProperties()
		{
			
		}

		public static void Load()
		{
			if (MyCockpitTerminal.IsLoaded) return;
			MyCockpitTerminal.IsLoaded = true;
			MyCockpitTerminal.SetupJumpGateBlockTerminalControls();
			MyCockpitTerminal.SetupJumpGateBlockTerminalActions();
			MyCockpitTerminal.SetupJumpGateBlockTerminalProperties();
		}

		public static void Unload()
		{
			if (!MyCockpitTerminal.IsLoaded) return;
			MyCockpitTerminal.IsLoaded = false;
			MyCockpitTerminal.MODID_PREFIX = null;
		}

		public static void UpdateRedrawControls()
		{
			if (!MyCockpitTerminal.IsLoaded) return;
			foreach (IMyTerminalControl control in MyCockpitTerminal.TerminalControls) control.UpdateVisual();
		}
	}
}
