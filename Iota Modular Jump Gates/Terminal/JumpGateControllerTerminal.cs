using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.ProgramScripting.CubeBlock;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.Terminal
{
	internal static class MyJumpGateControllerTerminal
	{
		public static bool IsLoaded { get; private set; } = false;
		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".JumpGateController.";

		private static readonly List<MyJumpGateWaypoint> TEMP_Waypoints = new List<MyJumpGateWaypoint>();
		private static readonly ConcurrentDictionary<long, string> ControllerSearchInputs = new ConcurrentDictionary<long, string>();

		private static void SetupTerminalSetupControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ControllerSettingsLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute("Controller Settings");
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Listbox [Jump Gates]
			{
				IMyTerminalControlListbox choose_jump_gate_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerJumpGate");
				choose_jump_gate_lb.Title = MyStringId.GetOrCompute("Active Jump Gate");
				choose_jump_gate_lb.SupportsMultipleBlocks = false;
				choose_jump_gate_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				choose_jump_gate_lb.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};
				choose_jump_gate_lb.Multiselect = false;
				choose_jump_gate_lb.VisibleRowsCount = 5;
				choose_jump_gate_lb.ListContent = (block0, content_list, preselect_list) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block0);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					content_list.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-- Deselect --"), MyStringId.GetOrCompute(""), -1L));
					long selected_jump_gate_id = controller.BlockSettings.JumpGateID();

					foreach (MyJumpGate jump_gate in controller.JumpGateGrid.GetJumpGates().OrderBy((gate) => gate.JumpGateID))
					{
						if (!jump_gate.Closed && jump_gate.IsValid() && (jump_gate.Controller == null || jump_gate.Controller == controller))
						{
							MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"{jump_gate.GetPrintableName()}"), MyStringId.GetOrCompute($"Jump Gate with {jump_gate.GetJumpGateDrives().Count()} drives\nJump Gate ID: {jump_gate.JumpGateID}"), jump_gate.JumpGateID);
							content_list.Add(item);
							if (selected_jump_gate_id == jump_gate.JumpGateID) preselect_list.Add(item);
						}
					}

					if (selected_jump_gate_id != -1)
					{
						MyJumpGate jump_gate = controller.AttachedJumpGate();

						if (jump_gate == null || jump_gate.Closed)
						{
							MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("�Invalid Jump Gate�"), MyStringId.GetOrCompute($"Jump gate is not available\nJump Gate ID: {selected_jump_gate_id}"), selected_jump_gate_id);
							content_list.Add(item);
							preselect_list.Add(item);
						}
					}
				};

				choose_jump_gate_lb.ItemSelected = (block, selected) => {
					if (!choose_jump_gate_lb.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					long selected_id = (long) selected[0].UserData;
					if (selected_id == -1 && controller.BlockSettings.JumpGateID() != -1) controller.AttachedJumpGate(null);
					else controller.AttachedJumpGate(controller.JumpGateGrid.GetJumpGate(selected_id));
					controller.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_jump_gate_lb);
			}

			// TextBox [Jump Gate Name]
			{
				IMyTerminalControlTextbox jump_gate_name_tb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerJumpGateName");
				jump_gate_name_tb.Title = MyStringId.GetOrCompute("Jump Gate Name");
				jump_gate_name_tb.SupportsMultipleBlocks = true;
				jump_gate_name_tb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				jump_gate_name_tb.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && controller.JumpGateGrid.IsValid() && jump_gate != null && jump_gate.IsValid();
				};

				jump_gate_name_tb.Getter = (block) => {
					MyJumpGate jump_gate = MyJumpGateModSession.GetBlockAsJumpGateController(block)?.AttachedJumpGate();
					return new StringBuilder(jump_gate?.GetName() ?? "");
				};

				jump_gate_name_tb.Setter = (block, value) => {
					if (!jump_gate_name_tb.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (value == null || value.Length == 0) controller.BlockSettings.JumpGateName(null);
					else controller.BlockSettings.JumpGateName(value.ToString().Replace("\n", "↵"));
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(jump_gate_name_tb);
			}
		}

		private static void SetupTerminalGateControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ControllerSetupLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute("Jump Gate Setup");
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Search [Destinations]
			{
				IMyTerminalControlTextbox search_waypoint_tb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyUpgradeModule>(MODID_PREFIX + "SearchControllerWaypoint");
				search_waypoint_tb.Title = MyStringId.GetOrCompute("Search Destination Waypoints");
				search_waypoint_tb.SupportsMultipleBlocks = false;
				search_waypoint_tb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				search_waypoint_tb.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || !controller.IsWorking() || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || jump_gate == null) return false;
					else return jump_gate.IsIdle();
				};
				search_waypoint_tb.Getter = (block) => new StringBuilder(MyJumpGateControllerTerminal.ControllerSearchInputs.GetValueOrDefault(block.EntityId, ""));
				search_waypoint_tb.Setter = (block, input) => MyJumpGateControllerTerminal.ControllerSearchInputs[block.EntityId] = input.ToString();
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(search_waypoint_tb);
			}

			// Listbox [Destinations]
			{
				IMyTerminalControlListbox choose_waypoint_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerWaypoint");
				choose_waypoint_lb.Title = MyStringId.GetOrCompute("Destination Waypoints");
				choose_waypoint_lb.SupportsMultipleBlocks = false;
				choose_waypoint_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				choose_waypoint_lb.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || !controller.IsWorking() || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || jump_gate == null) return false;
					else return jump_gate.IsIdle();
				};
				choose_waypoint_lb.Multiselect = false;
				choose_waypoint_lb.VisibleRowsCount = 10;
				choose_waypoint_lb.ListContent = (block1, content_list, preselect_list) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block1);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || !controller.IsWorking() || jump_gate == null || jump_gate.Closed) return;
					controller.GetWaypointsList(MyJumpGateControllerTerminal.TEMP_Waypoints);
					MyJumpGateWaypoint selected_waypoint = controller.BlockSettings.SelectedWaypoint();
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

					content_list.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("--deselect--"), MyStringId.GetOrCompute(""), (MyJumpGateWaypoint) null));

					foreach (MyJumpGateWaypoint waypoint in MyJumpGateControllerTerminal.TEMP_Waypoints)
					{
						MyTerminalControlListBoxItem item;
						MyJumpGate destination_jump_gate;
						Vector3D? endpoint = waypoint.GetEndpoint(out destination_jump_gate);
						string name, tooltip;
						if (endpoint == null || !MyJumpGateControllerTerminal.DoesWaypointMatchSearch(controller, waypoint, jump_node, out name, out tooltip) || name == null || tooltip == null) continue;
						double distance = Vector3D.Distance(jump_node, endpoint.Value);
						if (distance < jump_gate.JumpGateConfiguration.MinimumJumpDistance || distance > jump_gate.JumpGateConfiguration.MaximumJumpDistance) continue;

						if (waypoint.WaypointType != last_waypoint_type)
						{
							last_waypoint_type = waypoint.WaypointType;

							switch (last_waypoint_type)
							{
								case MyWaypointType.JUMP_GATE:
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(" "), MyStringId.GetOrCompute(""), selected_waypoint);
									content_list.Add(item);
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("   -- Jump Gate Waypoints --"), MyStringId.GetOrCompute(""), selected_waypoint);
									content_list.Add(item);
									break;
								case MyWaypointType.BEACON:
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(" "), MyStringId.GetOrCompute(""), selected_waypoint);
									content_list.Add(item);
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("   -- Beacon Waypoints --"), MyStringId.GetOrCompute(""), selected_waypoint);
									content_list.Add(item);
									break;
								case MyWaypointType.GPS:
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(" "), MyStringId.GetOrCompute(""), selected_waypoint);
									content_list.Add(item);
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("   -- GPS Waypoints --"), MyStringId.GetOrCompute(""), selected_waypoint);
									content_list.Add(item);
									break;
								case MyWaypointType.SERVER:
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(" "), MyStringId.GetOrCompute(""), selected_waypoint);
									content_list.Add(item);
									item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("   -- Server Waypoints --"), MyStringId.GetOrCompute(""), selected_waypoint);
									content_list.Add(item);
									break;
							}
						}

						item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(tooltip), waypoint);
						if (selected_waypoint == waypoint) preselect_list.Add(item);
						content_list.Add(item);
					}

					MyJumpGateControllerTerminal.TEMP_Waypoints.Clear();
				};

				choose_waypoint_lb.ItemSelected = (block, selected) => {
					if (!choose_waypoint_lb.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGateWaypoint selected_waypoint = (MyJumpGateWaypoint) selected[0].UserData;
					controller.BlockSettings.SelectedWaypoint(selected_waypoint);
					controller.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_waypoint_lb);
			}

			// Listbox [Animations]
			{
				IMyTerminalControlListbox choose_animation_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerAnimation");
				choose_animation_lb.Title = MyStringId.GetOrCompute("Jump Gate Animation");
				choose_animation_lb.SupportsMultipleBlocks = true;
				choose_animation_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				choose_animation_lb.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || !controller.IsWorking() || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};
				choose_animation_lb.Multiselect = false;
				choose_animation_lb.VisibleRowsCount = 5;
				choose_animation_lb.ListContent = (block2, content_list, preselect_list) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block2);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					MyJumpGate jump_gate = controller.AttachedJumpGate();
					string selected_animation = controller.BlockSettings.JumpEffectAnimationName();
					string _default = "IOTA.ModularJumpGates.AnimationDef.Standard";

					foreach (string full_name in MyAnimationHandler.GetAnimationNames())
					{
						string animation_name;
						string description;
						AnimationDef animation = MyAnimationHandler.GetAnimationDef(full_name, jump_gate);

						if (animation == null)
						{
							animation_name = "-Unavailable-";
							description = $"Animation Unavailable for this jump gate";
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
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.JumpEffectAnimationName((string) selected[0].UserData);
					controller.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_animation_lb);
			}

			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Checkbox [Allow Inbound]
			{
				IMyTerminalControlCheckbox do_allow_inbound = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowInbound");
				do_allow_inbound.Title = MyStringId.GetOrCompute("Allow Inbound Jumps");
				do_allow_inbound.Tooltip = MyStringId.GetOrCompute("Whether to allow another jump gate to jump to this jump gate");
				do_allow_inbound.SupportsMultipleBlocks = true;
				do_allow_inbound.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed;
				};
				do_allow_inbound.Enabled = (block) => true;

				do_allow_inbound.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.CanBeInbound() ?? false;
				do_allow_inbound.Setter = (block, value) => {
					if (!do_allow_inbound.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.CanBeInbound(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_allow_inbound);
			}

			// Checkbox [Allow Outbound]
			{
				IMyTerminalControlCheckbox do_allow_outbound = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowOutbound");
				do_allow_outbound.Title = MyStringId.GetOrCompute("Allow Outbound Jumps");
				do_allow_outbound.Tooltip = MyStringId.GetOrCompute("Whether to allow this jump gate to jump to another jump gate");
				do_allow_outbound.SupportsMultipleBlocks = true;
				do_allow_outbound.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed;
				};
				do_allow_outbound.Enabled = (block) => true;

				do_allow_outbound.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.CanBeOutbound() ?? false;
				do_allow_outbound.Setter = (block, value) => {
					if (!do_allow_outbound.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.CanBeOutbound(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_allow_outbound);
			}
		}

		private static void SetupTerminalEntityFilterControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel beacon_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ControllerFilterSettingsLabel");
				beacon_label_lb.Label = MyStringId.GetOrCompute("Entity Filter Settings");
				beacon_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				beacon_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(beacon_label_lb);
			}

			// Listbox [Allowed Entities]
			{
				IMyTerminalControlListbox choose_allowed_entities_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>(MODID_PREFIX + "AllowedEntities");
				choose_allowed_entities_lb.Title = MyStringId.GetOrCompute("Allowed Entities");
				choose_allowed_entities_lb.SupportsMultipleBlocks = false;
				choose_allowed_entities_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				choose_allowed_entities_lb.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || !controller.IsWorking() || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return false;
					else return jump_gate != null && (jump_gate.IsIdle() || jump_gate.IsJumping());
				};
				choose_allowed_entities_lb.Multiselect = true;
				choose_allowed_entities_lb.VisibleRowsCount = 10;
				choose_allowed_entities_lb.ListContent = (block3, content_list, preselect_list) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block3);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || jump_gate == null || (!jump_gate.IsIdle() && !jump_gate.IsJumping())) return;
					List<long> blacklisted = controller.BlockSettings.GetBlacklistedEntities();
					MyTerminalControlListBoxItem item;
					if (blacklisted.Count == 0) item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-- Deselect All --"), MyStringId.GetOrCompute("Deselect all entities"), -1L);
					else item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-- Select All --"), MyStringId.GetOrCompute("Select all entities"), -2L);
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
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					List<long> whitelisted = selected.Select((item) => (long) item.UserData).ToList();
					if (whitelisted.Contains(-1)) controller.BlockSettings.SetBlacklistedEntities(controller.AttachedJumpGate().GetEntitiesInJumpSpace(false).Select((pair) => pair.Key.EntityId));
					else if (whitelisted.Contains(-2)) controller.BlockSettings.SetBlacklistedEntities(null);
					else controller.BlockSettings.SetBlacklistedEntities(controller.AttachedJumpGate().GetEntitiesInJumpSpace(false).Select((pair) => pair.Key.EntityId).Where((id) => !whitelisted.Contains(id)));
					controller.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(choose_allowed_entities_lb);
			}

			// Slider [Allowed Entity Mass]
			{
				IMyTerminalControlSlider allowed_entity_mass_min = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowedEntityMinimumMass");
				allowed_entity_mass_min.Title = MyStringId.GetOrCompute("Minimum Mass (Kg):");
				allowed_entity_mass_min.Tooltip = MyStringId.GetOrCompute("The minimum allowed mass of an entity in kilograms");
				allowed_entity_mass_min.SupportsMultipleBlocks = true;
				allowed_entity_mass_min.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				allowed_entity_mass_min.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed;
				};
				allowed_entity_mass_min.SetLimits(0f, 999e24f);

				allowed_entity_mass_min.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (controller.IsWorking()) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(controller.BlockSettings.AllowedEntityMass().Key, 4)} Kg");
					else string_builder.Append("- OFFLINE -");
				};

				allowed_entity_mass_min.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.AllowedEntityMass().Key ?? 0;
				allowed_entity_mass_min.Setter = (block, value) => {
					if (!allowed_entity_mass_min.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.AllowedEntityMass(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_entity_mass_min);

				IMyTerminalControlSlider allowed_entity_mass_max = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowedEntityMaximumMass");
				allowed_entity_mass_max.Title = MyStringId.GetOrCompute("Maximum Mass (Kg):");
				allowed_entity_mass_max.Tooltip = MyStringId.GetOrCompute("The maximum allowed mass of an entity in kilograms");
				allowed_entity_mass_max.SupportsMultipleBlocks = true;
				allowed_entity_mass_max.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				allowed_entity_mass_max.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed;
				};
				allowed_entity_mass_max.SetLimits(0f, 999e24f);

				allowed_entity_mass_max.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (controller.IsWorking()) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(controller.BlockSettings.AllowedEntityMass().Value, 4)} Kg");
					else string_builder.Append("- OFFLINE -");
				};

				allowed_entity_mass_max.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.AllowedEntityMass().Value ?? 0;
				allowed_entity_mass_max.Setter = (block, value) => {
					if (!allowed_entity_mass_max.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.AllowedEntityMass(null, value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_entity_mass_max);
			}

			// Slider [Allowed Cube Grid Size]
			{
				IMyTerminalControlSlider allowed_cubegrid_size_min = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowedCubeGridMinimumSize");
				allowed_cubegrid_size_min.Title = MyStringId.GetOrCompute("Minimum Size:");
				allowed_cubegrid_size_min.Tooltip = MyStringId.GetOrCompute("The minimum allowed size of a grid in blocks");
				allowed_cubegrid_size_min.SupportsMultipleBlocks = true;
				allowed_cubegrid_size_min.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				allowed_cubegrid_size_min.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed;
				};
				allowed_cubegrid_size_min.SetLimits(0f, uint.MaxValue);

				allowed_cubegrid_size_min.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (controller.IsWorking())
					{
						uint size = controller.BlockSettings.AllowedCubeGridSize().Key;
						string_builder.Append($"{size} Block{(size == 1 ? "" : "s")}");
					}
					else string_builder.Append("- OFFLINE -");
				};

				allowed_cubegrid_size_min.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.AllowedCubeGridSize().Key ?? 0;
				allowed_cubegrid_size_min.Setter = (block, value) => {
					if (!allowed_cubegrid_size_min.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.AllowedCubeGridSize((uint) value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_cubegrid_size_min);

				IMyTerminalControlSlider allowed_cubegrid_size_max = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowedCubeGridMaximumSize");
				allowed_cubegrid_size_max.Title = MyStringId.GetOrCompute("Maximum Size:");
				allowed_cubegrid_size_max.Tooltip = MyStringId.GetOrCompute("The maximum allowed size of a grid in blocks");
				allowed_cubegrid_size_max.SupportsMultipleBlocks = true;
				allowed_cubegrid_size_max.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				allowed_cubegrid_size_max.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed;
				};
				allowed_cubegrid_size_max.SetLimits(0f, uint.MaxValue);

				allowed_cubegrid_size_max.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (controller.IsWorking())
					{
						uint size = controller.BlockSettings.AllowedCubeGridSize().Value;
						string_builder.Append($"{size} Block{(size == 1 ? "" : "s")}");
					}
					else string_builder.Append("- OFFLINE -");
				};

				allowed_cubegrid_size_max.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.AllowedCubeGridSize().Value ?? 0;
				allowed_cubegrid_size_max.Setter = (block, value) => {
					if (!allowed_cubegrid_size_max.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.AllowedCubeGridSize(null, (uint) value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(allowed_cubegrid_size_max);
			}
		}

		private static void SetupTerminalAutoActivateControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ControllerAutoJumpSettingsLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute("Auto-Jump Settings");
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "controller_auto_activate_spacer_lb");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}

			// OnOffSwitch [Auto Activate]
			{
				IMyTerminalControlOnOffSwitch do_auto_activate = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + "CanAutoActivate");
				do_auto_activate.Title = MyStringId.GetOrCompute("Enable Auto Activation");
				do_auto_activate.Tooltip = MyStringId.GetOrCompute("Whether this gate can automatically activate");
				do_auto_activate.SupportsMultipleBlocks = true;
				do_auto_activate.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				do_auto_activate.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed;
				};
				do_auto_activate.OnText = MyStringId.GetOrCompute("On");
				do_auto_activate.OffText = MyStringId.GetOrCompute("Off");

				do_auto_activate.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.CanAutoActivate() ?? false;
				do_auto_activate.Setter = (block, value) => {
					if (!do_auto_activate.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.CanAutoActivate(value);
					controller.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_auto_activate);
			}

			// Slider [Auto Activate Mass]
			{
				IMyTerminalControlSlider autoactivate_mass_min = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerAutoActivateMinimumMass");
				autoactivate_mass_min.Title = MyStringId.GetOrCompute("Minimum Mass (Kg):");
				autoactivate_mass_min.Tooltip = MyStringId.GetOrCompute("The minimum mass above which this gate will automatically activate");
				autoactivate_mass_min.SupportsMultipleBlocks = true;
				autoactivate_mass_min.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				autoactivate_mass_min.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.BlockSettings.CanAutoActivate();
				};
				autoactivate_mass_min.SetLimits(0f, 999e24f);

				autoactivate_mass_min.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (!controller.BlockSettings.CanAutoActivate()) string_builder.Append(" - Disabled - ");
					else if (controller.IsWorking()) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(controller.BlockSettings.AutoActivateMass().Key, 4)} Kg");
					else string_builder.Append("- OFFLINE -");
				};

				autoactivate_mass_min.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.AutoActivateMass().Key ?? 0;
				autoactivate_mass_min.Setter = (block, value) => {
					if (!autoactivate_mass_min.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.AutoActivateMass(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(autoactivate_mass_min);

				IMyTerminalControlSlider autoactivate_mass_max = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerAutoActivateMaximumMass");
				autoactivate_mass_max.Title = MyStringId.GetOrCompute("Maximum Mass (Kg):");
				autoactivate_mass_max.Tooltip = MyStringId.GetOrCompute("The maximum mass below which this gate will automatically activate");
				autoactivate_mass_max.SupportsMultipleBlocks = true;
				autoactivate_mass_max.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				autoactivate_mass_max.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.BlockSettings.CanAutoActivate();
				};
				autoactivate_mass_max.SetLimits(0f, 999e24f);

				autoactivate_mass_max.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (!controller.BlockSettings.CanAutoActivate()) string_builder.Append(" - Disabled - ");
					else if (controller.IsWorking()) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(controller.BlockSettings.AutoActivateMass().Value, 4)} Kg");
					else string_builder.Append("- OFFLINE -");
				};

				autoactivate_mass_max.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.AutoActivateMass().Value ?? 0;
				autoactivate_mass_max.Setter = (block, value) => {
					if (!autoactivate_mass_max.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.AutoActivateMass(null, value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(autoactivate_mass_max);
			}

			// Slider [Auto Activate Power]
			{
				IMyTerminalControlSlider autoactivate_power_min = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerAutoActivateMinimumPower");
				autoactivate_power_min.Title = MyStringId.GetOrCompute("Minimum Power (MW):");
				autoactivate_power_min.Tooltip = MyStringId.GetOrCompute("The minimum power above which this gate will automatically activate");
				autoactivate_power_min.SupportsMultipleBlocks = true;
				autoactivate_power_min.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				autoactivate_power_min.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.BlockSettings.CanAutoActivate();
				};
				autoactivate_power_min.SetLimits(0f, 999e24f);

				autoactivate_power_min.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (!controller.BlockSettings.CanAutoActivate()) string_builder.Append(" - Disabled - ");
					else if (controller.IsWorking()) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(controller.BlockSettings.AutoActivatePower().Key, 4)} MW");
					else string_builder.Append("- OFFLINE -");
				};

				autoactivate_power_min.Getter = (block) => (float) (MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.AutoActivatePower().Key ?? 0);
				autoactivate_power_min.Setter = (block, value) => {
					if (!autoactivate_power_min.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.AutoActivatePower(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(autoactivate_power_min);

				IMyTerminalControlSlider auto_activate_power_max = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerAutoActivateMaximumPower");
				auto_activate_power_max.Title = MyStringId.GetOrCompute("Maximum Power (MW):");
				auto_activate_power_max.Tooltip = MyStringId.GetOrCompute("The maximum power below which this gate will automatically activate");
				auto_activate_power_max.SupportsMultipleBlocks = true;
				auto_activate_power_max.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				auto_activate_power_max.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.BlockSettings.CanAutoActivate();
				};
				auto_activate_power_max.SetLimits(0f, 999e24f);

				auto_activate_power_max.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (!controller.BlockSettings.CanAutoActivate()) string_builder.Append(" - Disabled - ");
					else if (controller.IsWorking()) string_builder.Append($"{MyJumpGateModSession.AutoconvertSciNotUnits(controller.BlockSettings.AutoActivatePower().Value, 4)} MW");
					else string_builder.Append("- OFFLINE -");
				};

				auto_activate_power_max.Getter = (block) => (float) (MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.AutoActivatePower().Value ?? 0);
				auto_activate_power_max.Setter = (block, value) => {
					if (!auto_activate_power_max.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.AutoActivatePower(null, value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(auto_activate_power_max);
			}

			// Slider [Auto Activate Delay]
			{
				IMyTerminalControlSlider autoactivate_delay = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerAutoActivateDelay");
				autoactivate_delay.Title = MyStringId.GetOrCompute("Activation Delay (s)");
				autoactivate_delay.Tooltip = MyStringId.GetOrCompute("The time in seconds after which the gate will activate");
				autoactivate_delay.SupportsMultipleBlocks = true;
				autoactivate_delay.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				autoactivate_delay.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.BlockSettings.CanAutoActivate();
				};
				autoactivate_delay.SetLimits(0, 60);

				autoactivate_delay.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (!controller.BlockSettings.CanAutoActivate()) string_builder.Append(" - Disabled - ");
					else if (controller.IsWorking()) string_builder.Append($"{controller.BlockSettings.AutoActivationDelay()} s");
					else string_builder.Append("- OFFLINE -");
				};

				autoactivate_delay.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.AutoActivationDelay() ?? 0;
				autoactivate_delay.Setter = (block, value) => {
					if (!autoactivate_delay.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.AutoActivationDelay(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(autoactivate_delay);
			}
		}

		private static void SetupTerminalCommLinkControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel commlink_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ControllerCommLinkLabel");
				commlink_label_lb.Label = MyStringId.GetOrCompute("Comm-Link Settings");
				commlink_label_lb.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.JumpGateGrid.HasCommLink();
				};
				commlink_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(commlink_label_lb);
			}

			// Checkbox [Allow Owned]
			{
				IMyTerminalControlCheckbox do_display_owned = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowOwned");
				do_display_owned.Title = MyStringId.GetOrCompute("Allow Owned Faction");
				do_display_owned.Tooltip = MyStringId.GetOrCompute("Whether to allow your faction to jump to this jump gate");
				do_display_owned.SupportsMultipleBlocks = true;
				do_display_owned.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.JumpGateGrid.HasCommLink();
				};
				do_display_owned.Enabled = do_display_owned.Visible;

				do_display_owned.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.CanAcceptOwned() ?? false;
				do_display_owned.Setter = (block, value) => {
					if (!do_display_owned.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.CanAcceptOwned(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_owned);
			}

			// Checkbox [Allow Friendly]
			{
				IMyTerminalControlCheckbox do_display_friendly = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowFriendly");
				do_display_friendly.Title = MyStringId.GetOrCompute("Allow Friendly Factions");
				do_display_friendly.Tooltip = MyStringId.GetOrCompute("Whether to allow friendly factions to jump to this jump gate");
				do_display_friendly.SupportsMultipleBlocks = true;
				do_display_friendly.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.JumpGateGrid.HasCommLink();
				};
				do_display_friendly.Enabled = do_display_friendly.Visible;

				do_display_friendly.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.CanAcceptFriendly() ?? false;
				do_display_friendly.Setter = (block, value) => {
					if (!do_display_friendly.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.CanAcceptFriendly(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_friendly);
			}

			// Checkbox [Allow Neutral]
			{
				IMyTerminalControlCheckbox do_display_neutral = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowNeutral");
				do_display_neutral.Title = MyStringId.GetOrCompute("Allow Neutral Factions");
				do_display_neutral.Tooltip = MyStringId.GetOrCompute("Whether to allow neutral factions to jump to this jump gate");
				do_display_neutral.SupportsMultipleBlocks = true;
				do_display_neutral.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.JumpGateGrid.HasCommLink();
				};
				do_display_neutral.Enabled = do_display_neutral.Visible;

				do_display_neutral.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.CanAcceptNeutral() ?? false;
				do_display_neutral.Setter = (block, value) => {
					if (!do_display_neutral.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.CanAcceptNeutral(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_neutral);
			}

			// Checkbox [Allow Enemy]
			{
				IMyTerminalControlCheckbox do_display_enemy = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowEnemy");
				do_display_enemy.Title = MyStringId.GetOrCompute("Allow Enemy Factions");
				do_display_enemy.Tooltip = MyStringId.GetOrCompute("Whether to allow enemy factions to jump to this jump gate");
				do_display_enemy.SupportsMultipleBlocks = true;
				do_display_enemy.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.JumpGateGrid.HasCommLink();
				};
				do_display_enemy.Enabled = do_display_enemy.Visible;

				do_display_enemy.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.CanAcceptEnemy() ?? false;
				do_display_enemy.Setter = (block, value) => {
					if (!do_display_enemy.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.CanAcceptEnemy(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_enemy);
			}

			// Checkbox [Allow Unowned]
			{
				IMyTerminalControlCheckbox do_display_unowned = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowUnowned");
				do_display_unowned.Title = MyStringId.GetOrCompute("Allow Unowned Factions");
				do_display_unowned.Tooltip = MyStringId.GetOrCompute("Whether to allow non-aligned entities to jump to this jump gate");
				do_display_unowned.SupportsMultipleBlocks = true;
				do_display_unowned.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.JumpGateGrid.HasCommLink();
				};
				do_display_unowned.Enabled = do_display_unowned.Visible;

				do_display_unowned.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.CanAcceptUnowned() ?? false;
				do_display_unowned.Setter = (block, value) => {
					if (!do_display_unowned.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.CanAcceptUnowned(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_display_unowned);
			}
		}

		private static void SetupTerminalDebugControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ControllerDebugSettingsLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute("Debug Settings");
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// OnOffSwitch [Debug Mode]
			{
				IMyTerminalControlOnOffSwitch do_debug_mode_of = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + "SessionDebugMode");
				do_debug_mode_of.Title = MyStringId.GetOrCompute("Debug Mode");
				do_debug_mode_of.Tooltip = MyStringId.GetOrCompute("Whether to enable debug draw lines");
				do_debug_mode_of.SupportsMultipleBlocks = true;
				do_debug_mode_of.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				do_debug_mode_of.Enabled = (block) => true;
				do_debug_mode_of.OnText = MyStringId.GetOrCompute("On");
				do_debug_mode_of.OffText = MyStringId.GetOrCompute("Off");

				do_debug_mode_of.Getter = (block) => MyJumpGateModSession.DebugMode;
				do_debug_mode_of.Setter = (block, value) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					MyJumpGateModSession.DebugMode = value;
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_debug_mode_of);
			}

			// Button [Rescan Entities]
			{
				IMyTerminalControlButton do_rescan_entities = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "ControllerRescanGateEntities");
				do_rescan_entities.Title = MyStringId.GetOrCompute("Rescan Entities");
				do_rescan_entities.Tooltip = MyStringId.GetOrCompute("Rescans all entities within this gate's jump space");
				do_rescan_entities.SupportsMultipleBlocks = true;
				do_rescan_entities.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				do_rescan_entities.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && !(controller.AttachedJumpGate()?.Closed ?? true);
				};
				do_rescan_entities.Action = (block) => {
					MyJumpGateController controller;
					if (!do_rescan_entities.Enabled(block)) return;
					else if ((controller = MyJumpGateModSession.GetBlockAsJumpGateController(block)) == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					MyJumpGate jump_gate = controller.AttachedJumpGate();
					jump_gate.SetColliderDirty();
				};
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_rescan_entities);
			}

			// Button [Staticify Construct]
			{
				IMyTerminalControlButton do_staticify_construct = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "ControllerStaticifyJumpGateGrid");
				do_staticify_construct.Title = MyStringId.GetOrCompute("Convert to Station");
				do_staticify_construct.Tooltip = MyStringId.GetOrCompute("Converts all grids in this construct to a station\nAll grids must have zero anglular and linear velocity");
				do_staticify_construct.SupportsMultipleBlocks = false;
				do_staticify_construct.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				do_staticify_construct.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.JumpGateGrid.GetCubeGrids().Any((grid) => !grid.IsStatic && (grid.Speed < 1e-3 || grid.Physics.AngularVelocity.Length() < 1e-3));
				};
				do_staticify_construct.Action = (block) => {
					MyJumpGateController controller;
					if (!do_staticify_construct.Enabled(block)) return;
					else if ((controller = MyJumpGateModSession.GetBlockAsJumpGateController(block)) == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (MyNetworkInterface.IsServerLike) controller.JumpGateGrid.SetConstructStaticness(true);
					else if (MyJumpGateModSession.Network.Registered)
					{
						MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
						{
							PacketType = MyPacketTypeEnum.STATICIFY_CONSTRUCT,
							Broadcast = false,
							TargetID = 0,
						};

						packet.Payload(new KeyValuePair<long, bool>(controller.JumpGateGrid.CubeGridID, true));
						packet.Send();
					}
				};
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_staticify_construct);
			}

			// Button [Unstaticify Construct]
			{
				IMyTerminalControlButton do_unstaticify_construct = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "ControllerUnstaticifyJumpGateGrid");
				do_unstaticify_construct.Title = MyStringId.GetOrCompute("Convert to Ship");
				do_unstaticify_construct.Tooltip = MyStringId.GetOrCompute("Converts all grids in this construct to a ship");
				do_unstaticify_construct.SupportsMultipleBlocks = false;
				do_unstaticify_construct.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				do_unstaticify_construct.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.JumpGateGrid.GetCubeGrids().Any((grid) => grid.IsStatic);
				};
				do_unstaticify_construct.Action = (block) => {
					MyJumpGateController controller;
					if (!do_unstaticify_construct.Enabled(block)) return;
					else if ((controller = MyJumpGateModSession.GetBlockAsJumpGateController(block)) == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (MyNetworkInterface.IsServerLike) controller.JumpGateGrid.SetConstructStaticness(false);
					else if (MyJumpGateModSession.Network.Registered)
					{
						MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
						{
							PacketType = MyPacketTypeEnum.STATICIFY_CONSTRUCT,
							Broadcast = false,
							TargetID = 0,
						};

						packet.Payload(new KeyValuePair<long, bool>(controller.JumpGateGrid.CubeGridID, false));
						packet.Send();
					}
				};
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_unstaticify_construct);
			}

			// Button [Force Grid Gate Reconstruction]
			{
				IMyTerminalControlButton do_reconstruct_grid_gates = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "ControllerRebuildGridGates");
				do_reconstruct_grid_gates.Title = MyStringId.GetOrCompute("Reconstruct Gates");
				do_reconstruct_grid_gates.Tooltip = MyStringId.GetOrCompute("Forces this construct to reconstruct all jump gates");
				do_reconstruct_grid_gates.SupportsMultipleBlocks = false;
				do_reconstruct_grid_gates.Visible = (block) => MyNetworkInterface.IsServerLike && MyJumpGateModSession.IsBlockJumpGateController(block);
				do_reconstruct_grid_gates.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && MyJumpGateModSession.DebugMode && controller.JumpGateGrid != null && !controller.JumpGateGrid.IsSuspended && !controller.JumpGateGrid.Closed;
				};
				do_reconstruct_grid_gates.Action = (block) => {
					if (!do_reconstruct_grid_gates.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.JumpGateGrid.MarkGatesForUpdate();
				};
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_reconstruct_grid_gates);
			}

			// Button [Reset Client Grids ]
			{
				IMyTerminalControlButton do_reset_client_grids = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "ControllerResetClientGrids");
				do_reset_client_grids.Title = MyStringId.GetOrCompute("Force Grids Update");
				do_reset_client_grids.Tooltip = MyStringId.GetOrCompute("Forces a download of grids from the server");
				do_reset_client_grids.SupportsMultipleBlocks = false;
				do_reset_client_grids.Visible = (block) => MyNetworkInterface.IsStandaloneMultiplayerClient && MyJumpGateModSession.IsBlockJumpGateController(block);
				do_reset_client_grids.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && MyJumpGateModSession.DebugMode;
				};
				do_reset_client_grids.Action = (block) => {
					if (!do_reset_client_grids.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.UPDATE_GRIDS,
						TargetID = 0,
						Broadcast = false,
					};
					packet.Send();
				};
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_reset_client_grids);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "controller_void_spacer_lb");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}

			// Button [Jump To Void]
			{
				IMyTerminalControlButton do_jump_bt = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "ControllerDoOutboundVoidJump");
				do_jump_bt.Title = MyStringId.GetOrCompute("Jump To Void");
				do_jump_bt.Tooltip = MyStringId.GetOrCompute("Execute a jump to the void");
				do_jump_bt.SupportsMultipleBlocks = true;
				do_jump_bt.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateController(block) && MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId);
				do_jump_bt.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId) && MyJumpGateModSession.DebugMode && MyNetworkInterface.IsServerLike && controller != null && jump_gate != null && controller.IsWorking() && jump_gate.IsComplete() && controller.BlockSettings.SelectedWaypoint() != null && controller.BlockSettings.SelectedWaypoint().HasValue() && jump_gate.IsIdle();
				};
				do_jump_bt.Action = (block) => {
					if (!do_jump_bt.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (jump_gate != null && !jump_gate.Closed && jump_gate.IsIdle()) jump_gate.JumpToVoid(controller.BlockSettings, 1e6);
					else if (jump_gate != null && !jump_gate.Closed && !jump_gate.IsIdle()) MyAPIGateway.Utilities.ShowNotification("Jump Gate is not idle", 5000, "Red");
					else MyAPIGateway.Utilities.ShowNotification("No Jump Gate Connected", 5000, "Red");
				};
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_jump_bt);
			}

			// Button [Jump From Void]
			{
				IMyTerminalControlButton do_jump_bt = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "ControllerDoInboundVoidJump");
				do_jump_bt.Title = MyStringId.GetOrCompute("Jump From Void");
				do_jump_bt.Tooltip = MyStringId.GetOrCompute("Execute a jump from the void");
				do_jump_bt.SupportsMultipleBlocks = true;
				do_jump_bt.Visible = (block) => MyJumpGateModSession.IsBlockJumpGateController(block) && MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId);
				do_jump_bt.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId) && MyJumpGateModSession.DebugMode && MyNetworkInterface.IsServerLike && controller != null && jump_gate != null && controller.IsWorking() && jump_gate.IsComplete() && controller.BlockSettings.SelectedWaypoint() != null && controller.BlockSettings.SelectedWaypoint().HasValue() && jump_gate.IsIdle();
				};
				do_jump_bt.Action = (block) => {
					if (!do_jump_bt.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					BoundingEllipsoidD ellipse = jump_gate.JumpEllipse;

					List<MyPrefabInfo> prefabs = new List<MyPrefabInfo>() {
						new MyPrefabInfo("RespawnMoonPod", Vector3D.Zero, ellipse.WorldMatrix.Up, ellipse.WorldMatrix.Forward),
					};
					List<List<IMyCubeGrid>> spawned_grids = new List<List<IMyCubeGrid>>();

					if (jump_gate != null && !jump_gate.Closed && jump_gate.IsIdle()) jump_gate.JumpFromVoid(controller.BlockSettings, prefabs, spawned_grids);
					else if (jump_gate != null && !jump_gate.Closed && !jump_gate.IsIdle()) MyAPIGateway.Utilities.ShowNotification("Jump Gate is not idle", 5000, "Red");
					else MyAPIGateway.Utilities.ShowNotification("No Jump Gate Connected", 5000, "Red");
				};
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_jump_bt);
			}
		}

		private static void SetupTerminalGateExtraControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && controller.JumpGateGrid.HasCommLink();
				};
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ControllerAdditionalSettingsLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute("Additional Settings");
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Slider [Jump Space Radius]
			{
				IMyTerminalControlSlider jump_sphere_radius_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerJumpSpaceRadius");
				jump_sphere_radius_sd.Title = MyStringId.GetOrCompute("Jump Space Radius:");
				jump_sphere_radius_sd.Tooltip = MyStringId.GetOrCompute("Radius of jump space in meters");
				jump_sphere_radius_sd.SupportsMultipleBlocks = true;
				jump_sphere_radius_sd.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				jump_sphere_radius_sd.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || !controller.IsWorking() || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};

				jump_sphere_radius_sd.SetLimits((block) => 1f, (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return 0f;
					float raycast_distance = (float) ((controller.IsLargeGrid) ? MyJumpGateModSession.Configuration.DriveConfiguration.LargeDriveRaycastDistance : MyJumpGateModSession.Configuration.DriveConfiguration.SmallDriveRaycastDistance);
					return 2f * raycast_distance;
				});

				jump_sphere_radius_sd.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (controller.IsWorking()) string_builder.Append(Math.Round(controller.BlockSettings.JumpSpaceRadius(), 4));
					else string_builder.Append("- OFFLINE -");
				};

				jump_sphere_radius_sd.Getter = (block) => (float) (MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.JumpSpaceRadius() ?? 0);
				jump_sphere_radius_sd.Setter = (block, value) => {
					if (!jump_sphere_radius_sd.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.JumpSpaceRadius(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(jump_sphere_radius_sd);
			}

			// Slider [Jump Space Depth]
			{
				IMyTerminalControlSlider jump_space_depth = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "ControllerJumpSpaceDepth");
				jump_space_depth.Title = MyStringId.GetOrCompute("Jump Space Depth Percent:");
				jump_space_depth.Tooltip = MyStringId.GetOrCompute("Percentage of lateral radius to use for ellipsoid vertial radius");
				jump_space_depth.SupportsMultipleBlocks = true;
				jump_space_depth.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				jump_space_depth.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || !controller.IsWorking() || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return false;
					else return jump_gate == null || jump_gate.IsIdle();
				};
				jump_space_depth.SetLimits(10f, 100f);

				jump_space_depth.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (controller.IsWorking()) string_builder.Append(Math.Round(controller.BlockSettings.JumpSpaceDepthPercent() * 100, 4));
					else string_builder.Append("- OFFLINE -");
				};

				jump_space_depth.Getter = (block) => (float) (MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.JumpSpaceDepthPercent() * 100 ?? 0);
				jump_space_depth.Setter = (block, value) => {
					if (!jump_space_depth.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.JumpSpaceDepthPercent(value / 100d);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(jump_space_depth);
			}

			// Color [Effect Color Shift]
			{
				IMyTerminalControlColor effect_color_shift_cl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyUpgradeModule>(MODID_PREFIX + "ControllerEffectColorShift");
				effect_color_shift_cl.Title = MyStringId.GetOrCompute("Effect Color Shift");
				effect_color_shift_cl.Tooltip = MyStringId.GetOrCompute("Shifts the jump gate effect color");
				effect_color_shift_cl.SupportsMultipleBlocks = true;
				effect_color_shift_cl.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				effect_color_shift_cl.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && jump_gate != null && jump_gate.IsComplete();
				};

				effect_color_shift_cl.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.JumpEffectAnimationColorShift() ?? Color.White;
				effect_color_shift_cl.Setter = (block, value) => {
					if (!effect_color_shift_cl.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.JumpEffectAnimationColorShift(value);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(effect_color_shift_cl);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "controller_color_spacer_lb");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed;
				};
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}

			// OnOffSwitch [Vector Normal Override]
			{
				IMyTerminalControlOnOffSwitch do_normal_vector_override_off = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>(MODID_PREFIX + "VectorNormalOverride");
				do_normal_vector_override_off.Title = MyStringId.GetOrCompute("Override Jump Gate Normal Vector");
				do_normal_vector_override_off.Tooltip = MyStringId.GetOrCompute("Whether animations use a fixed normal instead of a computed normal");
				do_normal_vector_override_off.SupportsMultipleBlocks = true;
				do_normal_vector_override_off.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				do_normal_vector_override_off.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return controller != null && controller.IsWorking() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && jump_gate != null && jump_gate.IsComplete() && jump_gate.IsIdle();
				};
				do_normal_vector_override_off.OnText = MyStringId.GetOrCompute("On");
				do_normal_vector_override_off.OffText = MyStringId.GetOrCompute("Off");

				do_normal_vector_override_off.Getter = (block) => MyJumpGateModSession.GetBlockAsJumpGateController(block)?.BlockSettings.HasVectorNormalOverride() ?? false;
				do_normal_vector_override_off.Setter = (block, value) => {
					if (!do_normal_vector_override_off.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					controller.BlockSettings.HasVectorNormalOverride(value);
					controller.SetDirty();
					MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_normal_vector_override_off);
			}

			// Label
			{
				IMyTerminalControlLabel settings_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ControllerVectorNormalOverrideLabel");
				settings_label_lb.Label = MyStringId.GetOrCompute("Jump Gate Normal Vector");
				settings_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				settings_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(settings_label_lb);
			}

			// Slider [Vector Normal X]
			{
				IMyTerminalControlSlider vector_normal_x_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "NormalVectorX");
				vector_normal_x_sd.Title = MyStringId.GetOrCompute("X:");
				vector_normal_x_sd.Tooltip = MyStringId.GetOrCompute("X component of normal in degrees");
				vector_normal_x_sd.SupportsMultipleBlocks = true;
				vector_normal_x_sd.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				vector_normal_x_sd.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return controller != null && controller.IsWorking() && controller.BlockSettings.HasVectorNormalOverride() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && jump_gate != null && !jump_gate.Closed && jump_gate.IsIdle();
				};
				vector_normal_x_sd.SetLimits(0, 360);

				vector_normal_x_sd.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else if (!controller.BlockSettings.HasVectorNormalOverride()) string_builder.Append("- DISABLED -");
					else string_builder.Append(Math.Round(controller.BlockSettings.VectorNormalOverride().Value.X * (180d / Math.PI), 4));
				};

				vector_normal_x_sd.Getter = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || !controller.BlockSettings.HasVectorNormalOverride()) return 0;
					return (float) (controller.BlockSettings.VectorNormalOverride().Value.X * (180d / Math.PI));
				};
				vector_normal_x_sd.Setter = (block, value) => {
					if (!vector_normal_x_sd.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					Vector3D normal_override = controller.BlockSettings.VectorNormalOverride().Value;
					normal_override.X = (value % 360) * (Math.PI / 180d);
					controller.BlockSettings.VectorNormalOverride(normal_override);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(vector_normal_x_sd);
			}

			// Slider [Vector Normal Y]
			{
				IMyTerminalControlSlider vector_normal_y_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "NormalVectorY");
				vector_normal_y_sd.Title = MyStringId.GetOrCompute("Y:");
				vector_normal_y_sd.Tooltip = MyStringId.GetOrCompute("Y component of normal in degrees");
				vector_normal_y_sd.SupportsMultipleBlocks = true;
				vector_normal_y_sd.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				vector_normal_y_sd.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return controller != null && controller.IsWorking() && controller.BlockSettings.HasVectorNormalOverride() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && jump_gate != null && !jump_gate.Closed && jump_gate.IsIdle();
				};
				vector_normal_y_sd.SetLimits(0, 360);

				vector_normal_y_sd.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else if (!controller.BlockSettings.HasVectorNormalOverride()) string_builder.Append("- DISABLED -");
					else string_builder.Append(Math.Round(controller.BlockSettings.VectorNormalOverride().Value.Y * (180d / Math.PI), 4));
				};

				vector_normal_y_sd.Getter = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || !controller.BlockSettings.HasVectorNormalOverride()) return 0;
					return (float) (controller.BlockSettings.VectorNormalOverride().Value.Y * (180d / Math.PI));
				};
				vector_normal_y_sd.Setter = (block, value) => {
					if (!vector_normal_y_sd.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					Vector3D normal_override = controller.BlockSettings.VectorNormalOverride().Value;
					normal_override.Y = (value % 360) * (Math.PI / 180d);
					controller.BlockSettings.VectorNormalOverride(normal_override);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(vector_normal_y_sd);
			}

			// Slider [Vector Normal Z]
			{
				IMyTerminalControlSlider vector_normal_z_sd = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>(MODID_PREFIX + "NormalVectorZ");
				vector_normal_z_sd.Title = MyStringId.GetOrCompute("Z:");
				vector_normal_z_sd.Tooltip = MyStringId.GetOrCompute("Z component of normal in degrees");
				vector_normal_z_sd.SupportsMultipleBlocks = true;
				vector_normal_z_sd.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				vector_normal_z_sd.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return controller != null && controller.IsWorking() && controller.BlockSettings.HasVectorNormalOverride() && controller.JumpGateGrid != null && !controller.JumpGateGrid.Closed && jump_gate != null && !jump_gate.Closed && jump_gate.IsIdle();
				};
				vector_normal_z_sd.SetLimits(0, 360);

				vector_normal_z_sd.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else if (!controller.BlockSettings.HasVectorNormalOverride()) string_builder.Append("- DISABLED -");
					else string_builder.Append(Math.Round(controller.BlockSettings.VectorNormalOverride().Value.Z * (180d / Math.PI), 4));
				};

				vector_normal_z_sd.Getter = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || !controller.BlockSettings.HasVectorNormalOverride()) return 0;
					return (float) (controller.BlockSettings.VectorNormalOverride().Value.Z * (180d / Math.PI));
				};
				vector_normal_z_sd.Setter = (block, value) => {
					if (!vector_normal_z_sd.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					Vector3D normal_override = controller.BlockSettings.VectorNormalOverride().Value;
					normal_override.Z = (value % 360) * (Math.PI / 180d);
					controller.BlockSettings.VectorNormalOverride(normal_override);
					controller.SetDirty();
				};

				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(vector_normal_z_sd);
			}
		}

		private static void SetupTerminalGateActionControls()
		{
			// Separator
			{
				IMyTerminalControlSeparator separator_hr = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyUpgradeModule>("");
				separator_hr.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				separator_hr.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(separator_hr);
			}

			// Label
			{
				IMyTerminalControlLabel beacon_label_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "ControllerActionsLabel");
				beacon_label_lb.Label = MyStringId.GetOrCompute("Actions");
				beacon_label_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				beacon_label_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(beacon_label_lb);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "controller_top_spacer_lb");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}

			// Button [Jump]
			{
				IMyTerminalControlButton do_jump_bt = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "ControllerDoJump");
				do_jump_bt.Title = MyStringId.GetOrCompute("Jump");
				do_jump_bt.Tooltip = MyStringId.GetOrCompute("Activate Jump Gate");
				do_jump_bt.SupportsMultipleBlocks = true;
				do_jump_bt.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				do_jump_bt.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return controller != null && jump_gate != null && controller.IsWorking() && jump_gate.IsComplete() && controller.BlockSettings.SelectedWaypoint() != null && controller.BlockSettings.SelectedWaypoint().HasValue() && jump_gate.IsIdle();
				};
				do_jump_bt.Action = (block) => {
					if (!do_jump_bt.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (jump_gate != null && !jump_gate.Closed && jump_gate.IsIdle()) jump_gate.Jump(controller.BlockSettings);
					else if (jump_gate != null && !jump_gate.Closed && !jump_gate.IsIdle()) MyAPIGateway.Utilities.ShowNotification("Jump Gate is not idle", 5000, "Red");
					else MyAPIGateway.Utilities.ShowNotification("No Jump Gate Connected", 5000, "Red");
				};
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_jump_bt);
			}

			// Button [Cancel Jump]
			{
				IMyTerminalControlButton do_jump_bt = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>(MODID_PREFIX + "ControllerNoJump");
				do_jump_bt.Title = MyStringId.GetOrCompute("Cancel Jump");
				do_jump_bt.Tooltip = MyStringId.GetOrCompute("Deactivate Jump Gate");
				do_jump_bt.SupportsMultipleBlocks = true;
				do_jump_bt.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				do_jump_bt.Enabled = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					return controller != null && jump_gate != null && controller.IsWorking() && jump_gate.IsComplete() && controller.BlockSettings.SelectedWaypoint() != null && controller.BlockSettings.SelectedWaypoint().HasValue() && jump_gate.IsJumping();
				};
				do_jump_bt.Action = (block) => {
					if (!do_jump_bt.Enabled(block)) return;
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (jump_gate != null && !jump_gate.Closed && jump_gate.IsJumping()) jump_gate.CancelJump();
					else if (jump_gate != null && !jump_gate.Closed && !jump_gate.IsJumping()) MyAPIGateway.Utilities.ShowNotification("Jump Gate is not idle", 5000, "Red");
					else MyAPIGateway.Utilities.ShowNotification("No Jump Gate Connected", 5000, "Red");
				};
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(do_jump_bt);
			}

			// Spacer
			{
				IMyTerminalControlLabel spacer_lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyUpgradeModule>(MODID_PREFIX + "controller_bottom_spacer_lb");
				spacer_lb.Label = MyStringId.GetOrCompute(" ");
				spacer_lb.Visible = MyJumpGateModSession.IsBlockJumpGateController;
				spacer_lb.SupportsMultipleBlocks = true;
				MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(spacer_lb);
			}
		}

		private static void SetupJumpGateControllerTerminalControls()
		{
			MyJumpGateControllerTerminal.SetupTerminalSetupControls();
			MyJumpGateControllerTerminal.SetupTerminalGateControls();
			MyJumpGateControllerTerminal.SetupTerminalGateActionControls();
			MyJumpGateControllerTerminal.SetupTerminalCommLinkControls();
			MyJumpGateControllerTerminal.SetupTerminalAutoActivateControls();
			MyJumpGateControllerTerminal.SetupTerminalEntityFilterControls();
			MyJumpGateControllerTerminal.SetupTerminalGateExtraControls();
			MyJumpGateControllerTerminal.SetupTerminalDebugControls();
		}

		private static void SetupJumpGateControllerTerminalActions()
		{
			// Jump
			{
				IMyTerminalAction jump_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerJumpAction");
				jump_action.Name = new StringBuilder("Jump");
				jump_action.ValidForGroups = true;
				jump_action.Icon = @"Textures\GUI\Icons\Actions\SmallShipSwitchOn.dds";
				jump_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;
				
				jump_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;

					if (jump_gate != null && !jump_gate.Closed)
					{
						if (!jump_gate.IsIdle()) MyAPIGateway.Utilities.ShowNotification("Jump Gate not Idle", 5000, "Red");
						else if (controller.BlockSettings.SelectedWaypoint() == null) MyAPIGateway.Utilities.ShowNotification("No Destination Selected", 5000, "Red");
						else jump_gate.Jump(controller.BlockSettings);
					}
					else MyAPIGateway.Utilities.ShowNotification("No Jump Gate Connected", 5000, "Red");
				};

				jump_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || jump_gate == null || controller.JumpGateGrid == null || jump_gate.Closed || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(jump_gate.Status.ToString());
				};

				jump_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(jump_action);
			}

			// No Jump
			{
				IMyTerminalAction nojump_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerNoJumpAction");
				nojump_action.Name = new StringBuilder("Cancel Jump");
				nojump_action.ValidForGroups = true;
				nojump_action.Icon = @"Textures\GUI\Icons\Actions\SmallShipSwitchOff.dds";
				nojump_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				nojump_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;

					if (jump_gate != null && !jump_gate.Closed)
					{
						if (jump_gate.Status != MyJumpGateStatus.OUTBOUND) MyAPIGateway.Utilities.ShowNotification("Jump Gate not Outbound", 5000, "Red");
						else jump_gate.CancelJump();
					}
					else MyAPIGateway.Utilities.ShowNotification("No Jump Gate Connected", 5000, "Red");
				};

				nojump_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || jump_gate == null || controller.JumpGateGrid == null || jump_gate.Closed || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(jump_gate.Status.ToString());
				};

				nojump_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(nojump_action);
			}

			// Toggle Jump
			{
				IMyTerminalAction jump_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerToggleJumpAction");
				jump_action.Name = new StringBuilder("Toggle Jump");
				jump_action.ValidForGroups = true;
				jump_action.Icon = @"Textures\GUI\Icons\Actions\SmallShipToggle.dds";
				jump_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				jump_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;

					if (jump_gate != null && !jump_gate.Closed)
					{
						if (jump_gate.IsIdle()) jump_gate.Jump(controller.BlockSettings);
						else if (jump_gate.Status == MyJumpGateStatus.OUTBOUND) jump_gate.CancelJump();
						else MyAPIGateway.Utilities.ShowNotification("Jump Gate Busy", 5000, "Red");
					}
					else MyAPIGateway.Utilities.ShowNotification("No Jump Gate Connected", 5000, "Red");
				};

				jump_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || jump_gate == null || controller.JumpGateGrid == null || jump_gate.Closed || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(jump_gate.Status.ToString());
				};

				jump_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(jump_action);
			}

			// Increase Jump Space Radius
			{
				IMyTerminalAction radius_increase_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerIncreaseJumpSpaceRadiusAction");
				radius_increase_action.Name = new StringBuilder("Increase Jump Space");
				radius_increase_action.ValidForGroups = true;
				radius_increase_action.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
				radius_increase_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;
				
				radius_increase_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking() || jump_gate == null || jump_gate.Closed || !jump_gate.IsIdle()) return;
					controller.BlockSettings.JumpSpaceRadius(controller.BlockSettings.JumpSpaceRadius() + 10);
					controller.SetDirty();
				};

				radius_increase_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.JumpSpaceRadius());
				};

				radius_increase_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(radius_increase_action);
			}

			// Decrease Jump Space Radius
			{
				IMyTerminalAction radius_decrease_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerDecreaseJumpSpaceRadiusAction");
				radius_decrease_action.Name = new StringBuilder("Decrease Jump Space");
				radius_decrease_action.ValidForGroups = true;
				radius_decrease_action.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
				radius_decrease_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				radius_decrease_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking() || jump_gate == null || jump_gate.Closed || !jump_gate.IsIdle()) return;
					controller.BlockSettings.JumpSpaceRadius(controller.BlockSettings.JumpSpaceRadius() - 10);
					controller.SetDirty();
				};

				radius_decrease_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.JumpSpaceRadius());
				};

				radius_decrease_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(radius_decrease_action);
			}

			// Increase Jump Space Depth Percent
			{
				IMyTerminalAction radius_increase_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerIncreaseJumpSpaceDepthPercentAction");
				radius_increase_action.Name = new StringBuilder("Increase Jump Space Depth");
				radius_increase_action.ValidForGroups = true;
				radius_increase_action.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
				radius_increase_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				radius_increase_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking() || jump_gate == null || jump_gate.Closed || !jump_gate.IsIdle()) return;
					controller.BlockSettings.JumpSpaceDepthPercent(controller.BlockSettings.JumpSpaceDepthPercent() + 0.1);
					controller.SetDirty();
				};

				radius_increase_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.JumpSpaceDepthPercent() * 100);
				};

				radius_increase_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(radius_increase_action);
			}

			// Decrease Jump Space Depth Percent
			{
				IMyTerminalAction radius_decrease_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerDecreaseJumpSpaceDepthPercentAction");
				radius_decrease_action.Name = new StringBuilder("Decrease Jump Space Depth");
				radius_decrease_action.ValidForGroups = true;
				radius_decrease_action.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
				radius_decrease_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				radius_decrease_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking() || jump_gate == null || jump_gate.Closed || !jump_gate.IsIdle()) return;
					controller.BlockSettings.JumpSpaceDepthPercent(controller.BlockSettings.JumpSpaceDepthPercent() - 0.1);
					controller.SetDirty();
				};

				radius_decrease_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.JumpSpaceDepthPercent() * 100);
				};

				radius_decrease_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(radius_decrease_action);
			}

			// Allow Inbound Jumps
			{
				IMyTerminalAction can_be_inbound_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowInboundAction");
				can_be_inbound_action.Name = new StringBuilder("Allow Inbound");
				can_be_inbound_action.ValidForGroups = true;
				can_be_inbound_action.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
				can_be_inbound_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				can_be_inbound_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;
					controller.BlockSettings.CanBeInbound(!controller.BlockSettings.CanBeInbound());
					controller.SetDirty();
				};

				can_be_inbound_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.CanBeInbound());
				};

				can_be_inbound_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(can_be_inbound_action);
			}

			// Allow Outbound Jumps
			{
				IMyTerminalAction can_be_outbound_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowOutboundAction");
				can_be_outbound_action.Name = new StringBuilder("Allow Outbound");
				can_be_outbound_action.ValidForGroups = true;
				can_be_outbound_action.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
				can_be_outbound_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				can_be_outbound_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;
					controller.BlockSettings.CanBeOutbound(!controller.BlockSettings.CanBeOutbound());
					controller.SetDirty();
				};

				can_be_outbound_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.CanBeInbound());
				};

				can_be_outbound_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(can_be_outbound_action);
			}

			// Allow Jumps from Unowned
			{
				IMyTerminalAction allow_unowned_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowUnownedAction");
				allow_unowned_action.Name = new StringBuilder("Allow Unowned Access");
				allow_unowned_action.ValidForGroups = true;
				allow_unowned_action.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
				allow_unowned_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				allow_unowned_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;
					controller.BlockSettings.CanAcceptUnowned(!controller.BlockSettings.CanAcceptUnowned());
					controller.SetDirty();
				};

				allow_unowned_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.CanAcceptUnowned());
				};

				allow_unowned_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(allow_unowned_action);
			}

			// Allow Jumps from Enemy
			{
				IMyTerminalAction allow_enemy_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowEnemyAction");
				allow_enemy_action.Name = new StringBuilder("Allow Enemy Access");
				allow_enemy_action.ValidForGroups = true;
				allow_enemy_action.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
				allow_enemy_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				allow_enemy_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;
					controller.BlockSettings.CanAcceptEnemy(!controller.BlockSettings.CanAcceptEnemy());
					controller.SetDirty();
				};

				allow_enemy_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.CanAcceptEnemy());
				};

				allow_enemy_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(allow_enemy_action);
			}

			// Allow Jumps from Neutral
			{
				IMyTerminalAction allow_neutral_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowNeutralAction");
				allow_neutral_action.Name = new StringBuilder("Allow Neutral Access");
				allow_neutral_action.ValidForGroups = true;
				allow_neutral_action.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
				allow_neutral_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				allow_neutral_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;
					controller.BlockSettings.CanAcceptNeutral(!controller.BlockSettings.CanAcceptNeutral());
					controller.SetDirty();
				};

				allow_neutral_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.CanAcceptNeutral());
				};

				allow_neutral_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(allow_neutral_action);
			}

			// Allow Jumps from Friendly
			{
				IMyTerminalAction allow_friendly_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowFriendlyAction");
				allow_friendly_action.Name = new StringBuilder("Allow Friendly Access");
				allow_friendly_action.ValidForGroups = true;
				allow_friendly_action.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
				allow_friendly_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				allow_friendly_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;
					controller.BlockSettings.CanAcceptFriendly(!controller.BlockSettings.CanAcceptFriendly());
					controller.SetDirty();
				};

				allow_friendly_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.CanAcceptFriendly());
				};

				allow_friendly_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(allow_friendly_action);
			}

			// Allow Jumps from Owned
			{
				IMyTerminalAction allow_owned_action = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>(MODID_PREFIX + "ControllerAllowOwnedAction");
				allow_owned_action.Name = new StringBuilder("Allow Owned Access");
				allow_owned_action.ValidForGroups = true;
				allow_owned_action.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
				allow_owned_action.Enabled = MyJumpGateModSession.IsBlockJumpGateController;

				allow_owned_action.Action = (block) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGate jump_gate = controller?.AttachedJumpGate();
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed || !controller.IsWorking()) return;
					controller.BlockSettings.CanAcceptOwned(!controller.BlockSettings.CanAcceptOwned());
					controller.SetDirty();
				};

				allow_owned_action.Writer = (block, string_builder) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					if (controller == null || controller.JumpGateGrid == null || controller.JumpGateGrid.Closed) return;
					else if (!controller.IsWorking()) string_builder.Append("- OFFLINE -");
					else string_builder.Append(controller.BlockSettings.CanAcceptOwned());
				};

				allow_owned_action.InvalidToolbarTypes = new List<MyToolbarType>() {
					MyToolbarType.Seat
				};

				MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(allow_owned_action);
			}
		}

		private static void SetupJumpGateControllerTerminalProperties()
		{
		}

		public static void Load(IMyModContext context)
		{
			if (MyJumpGateControllerTerminal.IsLoaded) return;
			MyJumpGateControllerTerminal.IsLoaded = true;
			MyJumpGateControllerTerminal.SetupJumpGateControllerTerminalControls();
			MyJumpGateControllerTerminal.SetupJumpGateControllerTerminalActions();
			MyJumpGateControllerTerminal.SetupJumpGateControllerTerminalProperties();
			MyPBJumpGateController.SetupBlockTerminal();
		}

		public static void ResetSearchInputs()
		{
			MyJumpGateControllerTerminal.ControllerSearchInputs.Clear();
		}

		public static bool DoesWaypointMatchSearch(MyJumpGateController controller, MyJumpGateWaypoint waypoint, Vector3D this_pos, out string name, out string tooltip)
		{
			name = null;
			tooltip = null;
			if (controller == null || waypoint == null) return false;
			string input = MyJumpGateControllerTerminal.ControllerSearchInputs.GetValueOrDefault(controller.BlockID, null);
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
								matched = matched && waypoint.WaypointType == MyWaypointType.JUMP_GATE && long.TryParse(parts[1], out gateid) && JumpGateUUID.FromGuid(waypoint.JumpGate).GetJumpGate() == gateid;
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
	}
}
