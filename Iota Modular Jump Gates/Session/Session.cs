using IOTA.ModularJumpGates.Animation;
using IOTA.ModularJumpGates.Commands;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.EventController.EventComponents;
using IOTA.ModularJumpGates.ISC;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.JumpGateConstruct;
using IOTA.ModularJumpGates.ModConfiguration;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.Session
{
	public enum MySessionStatusEnum : byte { OFFLINE, LOADING, RUNNING, UNLOADING };

	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
	internal partial class MyJumpGateModSession : MySessionComponentBase
	{
		#region Public Static Variables
		/// <summary>
		/// Reference to the instance of this session component
		/// </summary>
		public static MyJumpGateModSession Instance { get; private set; } = null;
		#endregion

		#region Public Variables
		/// <summary>
		/// The current mod version (major, minor, patch)
		/// </summary>
		public Vector3I ModVersion => new Vector3I(1, 5, 1);

		/// <summary>
		/// The Mod ID string used for terminal controls
		/// </summary>
		public string ModID { get; private set; } = "IOTA.ModularJumpGatesMod";

		/// <summary>
		/// The display name used for chat messages
		/// </summary>
		public string DisplayName { get; private set; } = "[ IMJG ]";
		#endregion

		#region "MySessionComponentBase" Methods
		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called when session is unloaded<br />
		/// Saves and unloads all relevant mod data and configurations
		/// </summary>
		protected override void UnloadData()
		{
			Logger.Log("Closing...");
			this.SessionStatus = MySessionStatusEnum.UNLOADING;
			this.OnSessionUnload?.Invoke();
			base.UnloadData();
			this.ModAPIInterface.Close();
			
			MyAnimationHandler.Unload();
			MyChatCommandHandler.Close();

			this.UnloadConstructs();
			this.UnloadCubeBlocks();
			this.UnloadTerminal();
			this.UnloadNetwork();

			this.SessionUpdateTimeTicks.Clear();
			this.JumpGateAnimations.Clear();
			this.EntityWarps.Clear();

			this.ModAPIInterface = null;
			this.SessionUpdateTimeTicks = null;
			this.JumpGateAnimations = null;
			this.EntityWarps = null;
			this.GateDetonations = null;
			this.ActiveModLogFile = null;

			MyAPIGateway.Entities.OnEntityAdd -= this.OnEntityAdd;
			MyAPIGateway.Entities.OnEntityRemove -= this.OnEntityRemove;			

			JumpGatePhaseChangedEvent.Dispose();
			JumpGateStatusChangedEvent.Dispose();

			Particle.Dispose();

			this.Materials = null;
			this.ModsList = null;
			this.SessionStatus = MySessionStatusEnum.OFFLINE;

			Logger.Log("Closed.");
			Logger.Flush();
			Logger.Close();

			this.UnloadConfiguration();

			this.ModID = null;
			this.DisplayName = null;
			MyJumpGateModSession.Instance = null;
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called when session is loaded<br />
		/// Loads all relevant mod data and configurations
		/// </summary>
		public override void LoadData()
		{
			// amogst the earliest execution points, but not everything is available at this point.

			// These can be used anywhere, not just in this method/class:
			// MyAPIGateway. - main entry point for the API
			// MyDefinitionManager.Static. - reading/editing definitions
			// MyGamePruningStructure. - fast way of finding entities in an area
			// MyTransparentGeometry. and MySimpleObjectDraw. - to draw sprites (from TransparentMaterials.sbc) in world (they usually live a single tick)
			// MyVisualScriptLogicProvider. - mainly designed for VST but has its uses, use as a last resort.
			// System.Diagnostics.Stopwatch - for measuring code execution time.
			// ...and many more things, ask in #programming-modding in keen's discord for what you want to do to be pointed at the available things to use.

			MyJumpGateModSession.Instance = this;

			// Load configuration
			bool local_config_exists, global_config_exists;
			Exception local_load_err, global_load_err;
			this.Configuration = MyModConfigurationV1.Load(this, out global_config_exists, out local_config_exists, out global_load_err, out local_load_err);

			if (MyNetworkInterface.IsServerLike && !local_config_exists && MyAPIGateway.Utilities.FileExistsInWorldStorage(ModularJumpGates.Configuration.ConfigFileName, this.GetType()))
			{
				Configuration old_configuration = ModularJumpGates.Configuration.Load();

				if (old_configuration != null)
				{
					this.Configuration = MyModConfigurationV1.FromConfigurationV0(old_configuration);
					Logger.Log($"Found old mod configuration file -> UPDATED");
				}
			}

			// Load mod data
			bool log_decode_success;
			bool is_old;

			if (is_old = MyAPIGateway.Utilities.FileExistsInLocalStorage(this.ModLogSettingsFile, this.GetType()))
			{
				log_decode_success = this.UpdateLoadLogFileList();
				MyAPIGateway.Utilities.DeleteFileInLocalStorage(this.ModLogSettingsFile, this.GetType());
			}
			else
			{
				log_decode_success = this.UpdateLoadModDataFile();
			}

			// Initialize log and session
			int log_file_count = this.ModData.ModLogFiles.Count;
			Logger.Init();

			Logger.Log($"Mod Version: {this.ModVersion.X}.{this.ModVersion.Y}.{this.ModVersion.Z}");
			Logger.Log("PREINIT - Loading Data...");
			Logger.Log($"PREINIT - Mod Session Instance ID: {this.InstanceID}");

			this.SessionStatus = MySessionStatusEnum.LOADING;
			this.Network = new MyNetworkInterface(0xFFFF, this.ModContext.ModId);
			if (MyNetworkInterface.IsServerLike && !MyAPIGateway.Utilities.GetVariable($"{this.ModID}.DebugMode", out this.DebugMode)) this.DebugMode = false;
			this.UpdateOnPause = true;
			this.PurgeStoredLogFiles(this.MaxModSpecificLogFiles);
			if (is_old) Logger.Log("Upgraded old mod-specific log file data to newer mod data file");
			if (log_decode_success) Logger.Log($"Mod data loaded from local storage; Version={this.ModData.ModVersion.X}.{this.ModData.ModVersion.Y}.{this.ModData.ModVersion.Z}, Log Capacity={log_file_count}/{this.MaxModSpecificLogFiles}");
			else Logger.Warn("Failed to load mod-specific log file list. System cannot auto-delete extra log files");

			if (global_config_exists) Logger.Log("Found global mod config file");
			else Logger.Warn($"Failed to locate global mod config file; DEFAULTS_LOADED");
			if (local_config_exists) Logger.Log("Found local mod config file");
			else Logger.Warn($"Failed to locate local mod config file; DEFAULTS_LOADED");
			if (global_load_err != null) Logger.Error($"Error loading global config file:\n  ...\n[ {global_load_err.GetType().Name} ]: {global_load_err.Message}\n{global_load_err.StackTrace}\n{global_load_err.InnerException}");
			if (local_load_err != null) Logger.Error($"Error loading local config file:\n  ...\n[ {local_load_err.GetType().Name} ]: {local_load_err.Message}\n{local_load_err.StackTrace}\n{local_load_err.InnerException}");

			if (!global_config_exists || !local_config_exists)
			{
				Exception global_save_err, local_save_err;
				this.Configuration.Save(this, out global_save_err, out local_save_err);
				if (global_save_err != null) Logger.Error($"Error saving global config file:\n  ...\n[ {global_save_err.GetType().Name} ]: {global_save_err.Message}\n{global_save_err.StackTrace}\n{global_save_err.InnerException}");
				if (local_save_err != null) Logger.Error($"Error saving local config file:\n  ...\n[ {local_save_err.GetType().Name} ]: {local_save_err.Message}\n{local_save_err.StackTrace}\n{local_save_err.InnerException}");
			}

			// Networking
			if (!MyNetworkInterface.IsSingleplayer)
			{
				this.Network.Register();
				this.Network.On(MyPacketTypeEnum.UPDATE_GRIDS, this.OnNetworkGridsUpdate);
				this.Network.On(MyPacketTypeEnum.CLOSE_GRID, this.OnNetworkGridClose);
				this.Network.On(MyPacketTypeEnum.DOWNLOAD_GRID, this.OnNetworkGridDownload);
				this.Network.On(MyPacketTypeEnum.UPDATE_CONFIG, this.OnConfigurationUpdate);
				this.Network.On(MyPacketTypeEnum.CONSTRUCT_UPDATE_NOTICE, this.OnNetworkConstructUpdateNotice);
				this.Network.On(MyPacketTypeEnum.GATE_DETONATION, this.OnNetworkJumpGateDetonation);
				this.Network.On(MyPacketTypeEnum.VERSION_CHECK, this.OnNetworkVersionCheck);
				this.Network.On(MyPacketTypeEnum.UPDATE_COCKPIT, this.OnNetworkCockpitSettingsUpdate);
			}

			if (MyNetworkInterface.IsMultiplayerServer)
			{
				this.Network.On(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				this.Network.On(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
				this.Network.On(MyPacketTypeEnum.MARK_GATES_DIRTY, this.OnNetworkGridGateReconstruct);
			}

			if (MyNetworkInterface.IsDedicatedMultiplayerServer) MyInterServerCommunication.Register(0, 0, null);

			// Finalize
			this.UpdateSaveModDataFile();
			MyAPIGateway.Entities.OnEntityAdd += this.OnEntityAdd;
			MyAPIGateway.Entities.OnEntityRemove += this.OnEntityRemove;
			this.ModAPIInterface.Init();
			Logger.Log("PREINIT - Loaded.");
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called before session start<br />
		/// Updates animations and loads controls
		/// </summary>
		public override void BeforeStart()
		{
			base.BeforeStart();
			Logger.Log("INIT - Loading Data...");
			MyAnimationHandler.Load();
			this.ModsList = new MyExternalModInfo(this.ModContext);
			if (this.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient) this.RequestGridsDownload();
			if (!MyNetworkInterface.IsDedicatedMultiplayerServer) MyAPIGateway.Utilities.ShowMessage(this.DisplayName, "Initializing Constructs...");
			MyAPIGateway.TerminalControls.CustomControlGetter += this.OnTerminalSelector;
			MyChatCommandHandler.Init();
			Logger.Log("INIT - Loaded.");

			// Display startup messages
			bool is_admin = MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId);
			TextReader file_reader;
			string filename;
			string content;
			MyObjectBuilder_Checkpoint.ModItem moditem = this.ModContext.ModItem;

			if (is_admin)
			{
				filename = "Data/StartupMessage/Admin.txt";
				file_reader = (MyAPIGateway.Utilities.FileExistsInModLocation(filename, moditem)) ? MyAPIGateway.Utilities.ReadFileInModLocation(filename, moditem) : null;
				content = file_reader?.ReadToEnd();
				if (!string.IsNullOrEmpty(content)) MyAPIGateway.Utilities.ShowMessage(this.DisplayName, content);
			}

			filename = "Data/StartupMessage/General.txt";
			file_reader = (MyAPIGateway.Utilities.FileExistsInModLocation(filename, moditem)) ? MyAPIGateway.Utilities.ReadFileInModLocation(filename, moditem) : null;
			content = file_reader?.ReadToEnd();
			if (!string.IsNullOrEmpty(content)) MyAPIGateway.Utilities.ShowMessage(this.DisplayName, content);

			if (this.ModVersion.X > this.ModData.ModVersion.X
				|| (this.ModVersion.X == this.ModData.ModVersion.X && this.ModVersion.Y > this.ModData.ModVersion.Y)
				|| (this.ModVersion.X == this.ModData.ModVersion.X && this.ModVersion.Y == this.ModData.ModVersion.Y && this.ModVersion.Z > this.ModData.ModVersion.Z))
			{
				this.ModData.ModVersion = this.ModVersion;
				MyAPIGateway.Utilities.ShowMessage(this.DisplayName, $"Mod updated to version {this.ModVersion.X}.{this.ModVersion.Y}.{this.ModVersion.Z}");

				filename = "Data/StartupMessage/Update.txt";
				file_reader = (MyAPIGateway.Utilities.FileExistsInModLocation(filename, moditem)) ? MyAPIGateway.Utilities.ReadFileInModLocation(filename, moditem) : null;
				content = file_reader?.ReadToEnd();
				if (!string.IsNullOrEmpty(content)) MyAPIGateway.Utilities.ShowMessage(this.DisplayName, content);

				if (is_admin)
				{
					filename = "Data/StartupMessage/UpdateAdmin.txt";
					file_reader = (MyAPIGateway.Utilities.FileExistsInModLocation(filename, moditem)) ? MyAPIGateway.Utilities.ReadFileInModLocation(filename, moditem) : null;
					content = file_reader?.ReadToEnd();
					if (!string.IsNullOrEmpty(content)) MyAPIGateway.Utilities.ShowMessage(this.DisplayName, content);
				}
			}

			if (this.ModDataLoadError != null)
			{
				Logger.Error($"Error whilst reading mod-log file list\n  ...\n[ {this.ModDataLoadError.GetType().Name} ]: {this.ModDataLoadError.Message}\n{this.ModDataLoadError.StackTrace}\n{this.ModDataLoadError.InnerException}");
				MyAPIGateway.Utilities.ShowMessage(this.DisplayName, "An error occured loading the mod-log list.\nExisting mod log files may not be deleted automatically.\nCheck the log for more details");
				this.ModDataLoadError = null;
			}

			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.VERSION_CHECK,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Send();
			}
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called every tick before simulation<br />
		/// </summary>
		public override void UpdateBeforeSimulation()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{
				base.UpdateBeforeSimulation();
				this.FlushClosureQueues();
				this.FlushAdditionQueues();
				if (MyNetworkInterface.IsServerLike) return;

				foreach (KeyValuePair<long, byte> partial_grid in this.PartialSuspendedGridsQueue)
				{
					byte new_time = (byte) (partial_grid.Value - 1);

					if (new_time > 0)
					{
						this.PartialSuspendedGridsQueue[partial_grid.Key] = new_time;
						continue;
					}

					this.PartialSuspendedGridsQueue.Remove(partial_grid.Key);
					this.RequestGridDownload(partial_grid.Key);
				}
			}
			finally
			{
				sw.Stop();
				this.SessionUpdateTimeTicks.Enqueue(sw.Elapsed.TotalMilliseconds);
			}
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called every tick after simulation<br />
		/// Primary logic executor; ticks grids, animations, teleport requests, terminal controls, and sound emitters
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{
				this.SessionStatus = MySessionStatusEnum.RUNNING;
				++this.GameTick;
				this.GameTick = (this.GameTick >= 0xFFFFFFFFFFFFFFF0) ? 0 : this.GameTick;
				this.EntityLoadedDelayTicks -= (this.EntityLoadedDelayTicks > 0) ? (ushort) 1 : (ushort) 0;

				// Update grids
				if (MyNetworkInterface.IsStandaloneMultiplayerClient && this.GameTick == this.FirstUpdateTimeTicks)
				{
					this.RequestGridsDownload();
					Logger.Debug($"INITAL_GRID_UPDATE", 2);
				}

				// Check initialization
				if (MyNetworkInterface.IsServerLike && !this.AllSessionEntitiesLoaded) return;

				if (!this.InitializationComplete && ((MyNetworkInterface.IsServerLike && this.AllFirstTickComplete()) || (MyNetworkInterface.IsStandaloneMultiplayerClient && this.GameTick == this.FirstUpdateTimeTicks)))
				{
					if (MyNetworkInterface.IsStandaloneMultiplayerClient)
					{
						foreach (MyJumpGate jump_gate in this.GetAllJumpGates())
						{
							if (!jump_gate.MarkClosed) jump_gate.PollGateWormholeStatus();
						}
					}

					this.InitializationComplete = true;
					MyAPIGateway.Utilities.ShowMessage(this.DisplayName, "Initialization Complete!");
				}

				// Tick grids
				this.TickDistributedGrids();
				this.TickNonthreadableGrids();

				// Redraw Terminal Controls
				this.DoRedrawTerminalControls();

				bool paused = MyNetworkInterface.IsSingleplayer && MyAPIGateway.Session.GameplayFrameCounter == this.LastGameplayFrameCounter;
				this.LastGameplayFrameCounter = MyAPIGateway.Session.GameplayFrameCounter;

				if (!paused)
				{
					// Tick queued entity warps
					this.TickEntityWarps();

					// Tick queued animations
					this.TickAnimations();

					// Tick queued gate detonations
					this.TickJumpGateDetonations();
				}

				// Update Log
				if (this.GameTick % 300 == 0) Logger.Flush();
			}
			finally
			{
				sw.Stop();
				this.SessionUpdateTimeTicks[-1] += sw.Elapsed.TotalMilliseconds;
			}
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Updates terminal block scrolling for detailed info
		/// </summary>
		public override void HandleInput()
		{
			base.HandleInput();
			if (this.LastTerminalPage == MyTerminalPageEnum.ControlPanel && MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel)
				foreach (KeyValuePair<long, MyCubeBlockBase> pair in MyCubeBlockBase.Instances) pair.Value.SetScroll(0);
			this.LastTerminalPage = MyAPIGateway.Gui.GetCurrentScreen;
			if (!MyAPIGateway.Gui.IsCursorVisible || MyAPIGateway.Gui.ChatEntryVisible || MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel) return;

			MyCubeBlockBase interacted_cube_block;
			if ((interacted_cube_block = MyCubeBlockBase.Instances.GetValueOrDefault(this.InteractedBlock, null)) == null) return;

			Vector2 area = MyAPIGateway.Input.GetMouseAreaSize();
			Vector2 position = MyAPIGateway.Input.GetMousePosition();
			Vector2 percent_pos = position / area;
			if (percent_pos.X < 0.6273438f || percent_pos.Y < 0.5972222 || percent_pos.X > 0.8605469f || percent_pos.Y > 0.88125f) return;

			if (MyAPIGateway.Input.DeltaMouseScrollWheelValue() > 0) interacted_cube_block.Scroll(3);
			else if (MyAPIGateway.Input.DeltaMouseScrollWheelValue() < 0) interacted_cube_block.Scroll(-3);
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called every tick<br />
		/// Displays debug draw lines
		/// </summary>
		public override void Draw()
		{
			base.Draw();

			// Debug draw
			if (this.DebugMode && !MyNetworkInterface.IsDedicatedMultiplayerServer)
			{
				Vector4 color4;
				Color color;
				Vector3D this_position = MyAPIGateway.Session.Camera.Position;
				MyStringId line_material = this.Materials.WeaponLaser;
				List<MyJumpGate> jump_gates = new List<MyJumpGate>();
				List<MyJumpGateDrive> jump_drives = new List<MyJumpGateDrive>();

				foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
				{
					MyJumpGateConstruct jump_grid = pair.Value;
					if (jump_grid.IsSuspended || jump_grid.CubeGrid == null || !jump_grid.IsValid() || !jump_grid.AtLeastOneUpdate()) continue;
					jump_gates.AddRange(jump_grid.GetJumpGates());
					jump_drives.AddRange(jump_grid.GetAttachedJumpGateDrives());
					color4 = Color.Violet.ToVector4() * 20;
					foreach (MyJumpGate gate in jump_gates) if (gate.TrueEndpoint != null) MySimpleObjectDraw.DrawLine(gate.WorldJumpNode, gate.TrueEndpoint.Value, line_material, ref color4, 10);
					MatrixD orientation = jump_grid.GetConstructCalculatedOrienation();
					float extent = (float) jump_grid.GetCombinedAABB().HalfExtents.Max();
					MyStringId square = MyStringId.GetOrCompute("Square");
					MyTransparentGeometry.AddLineBillboard(square, Color.Red, orientation.Translation, orientation.Forward, extent, 0.1f);
					MyTransparentGeometry.AddLineBillboard(square, Color.Lime, orientation.Translation, orientation.Up, extent, 0.1f);
					MyTransparentGeometry.AddLineBillboard(square, Color.Blue, orientation.Translation, orientation.Right, extent, 0.1f);

					try
					{
						if (Vector3D.Distance(this_position, jump_grid.CubeGrid.WorldMatrix.Translation) > this.Configuration.GeneralConfiguration.DrawSyncDistance) continue;
						MatrixD grid_matrix = jump_grid.CubeGrid.WorldMatrix;
						color4 = Color.Gold.ToVector4();

						foreach (MyJumpGateDrive drive in jump_drives)
						{
							if (drive.IsClosed) continue;
							Vector3D start = drive.GetDriveRaycastStartpoint();
							Vector3D end = drive.GetDriveRaycastEndpoint(drive.MaxRaycastDistance);
							MySimpleObjectDraw.DrawLine(start, end, line_material, ref color4, 0.25f);
						}

						foreach (MyJumpGate gate in jump_gates)
						{
							if (!gate.IsValid()) continue;
							Vector3D world_node = gate.WorldJumpNode;
							MatrixD gate_matrix = gate.GetWorldMatrix(true, true);
							MatrixD gate_matrix_normalized = MatrixD.Normalize(gate_matrix);
							BoundingEllipsoidD jump_ellipse = gate.JumpEllipse;
							bool complete = gate.IsComplete();
							color = Color.Aqua;

							// Display drive intersections
							if (Vector3D.Distance(this_position, world_node) <= 100)
							{
								foreach (Vector3D node in gate.WorldDriveIntersectNodes)
								{
									BoundingBoxD node_box = BoundingBoxD.CreateFromSphere(new BoundingSphereD(MyJumpGateModSession.WorldVectorToLocalVectorP(ref grid_matrix, node), 1));
									MySimpleObjectDraw.DrawTransparentBox(ref grid_matrix, ref node_box, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.1f, null, line_material);
								}
							}

							// Display gate ellipsoid
							jump_ellipse.Draw((complete) ? Color.Lime : Color.Red, 90, 0.1f, line_material);
							BoundingEllipsoidD effective_ellipse = gate.GetEffectiveJumpEllipse();
							if (complete && effective_ellipse != jump_ellipse) effective_ellipse.Draw(Color.BlueViolet, 90, 0.1f, line_material);
							if (complete && !this.Configuration.GeneralConfiguration.LenientJumps.Value) gate.ShearEllipse.Draw(Color.Red, 90, 0.1f, line_material);
							new BoundingEllipsoidD(jump_ellipse.Radii.Max() * 2, ref jump_ellipse.WorldMatrix).Draw(Color.LightSkyBlue, 90, 0.1f, line_material);
							new BoundingEllipsoidD(jump_ellipse.Radii.Max(), ref jump_ellipse.WorldMatrix).Draw(Color.GhostWhite, 90, 0.1f, line_material);

							// Display gate ellipsoid bounds
							BoundingBoxD ellipse_aabb = new BoundingBoxD(-Vector3D.One, Vector3D.One);
							Vector3D jump_node = gate.WorldJumpNode;
							color = Color.AliceBlue;
							MySimpleObjectDraw.DrawTransparentBox(ref jump_ellipse.WorldMatrix, ref ellipse_aabb, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.1f, null, line_material);

							// Display collider sphere
							MatrixD? collider_position = gate.GetColliderMatrix();
							if (collider_position != null) new BoundingEllipsoidD(this.MaxJumpGateColliderRadius, collider_position.Value).Draw(Color.Chartreuse, 90, 0.1f, line_material);

							// Display gate normal override
							MyJumpGateControlObject control_object = gate.ControlObject;

							if (control_object != null && control_object.BlockSettings != null && control_object.BlockSettings.HasVectorNormalOverride())
							{
								color4 = Color.Magenta;
								Vector3D normal = gate.GetWorldMatrix(false, true).Forward;
								MySimpleObjectDraw.DrawLine(jump_node, jump_node + normal * 500d, line_material, ref color4, 1);
							}

							// Display jump gate endpoint aligned axes
							color4 = Color.Magenta;
							Vector3D axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(5, 0, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 1f);
							color4 = Color.Yellow;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(0, 5, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 1f);
							color4 = Color.Cyan;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(0, 0, 5));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 1f);

							// Display jump gate axes
							gate_matrix = jump_ellipse.WorldMatrix;
							color4 = Color.Red;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(7.5, 0, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 0.5f);
							color4 = Color.Green;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(0, 7.5, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 0.5f);
							color4 = Color.Blue;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(0, 0, 7.5));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 0.5f);
						}
					}
					finally
					{
						jump_gates.Clear();
						jump_drives.Clear();
					}
				}
			}

			// Cockpit HUD draw
			if (MyAPIGateway.Session.CameraController?.Entity is IMyCockpit)
			{
				IMyCockpit cockpit = (IMyCockpit) MyAPIGateway.Session.CameraController.Entity;
				MyCockpitInfo settings = this.GetCockpitTerminalSettings(cockpit);
				if (settings == null || !settings.DisplayHudMarkers) return;
				MatrixD camera = MyAPIGateway.Session.Camera.WorldMatrix;
				Vector3D camera_position = cockpit.WorldMatrix.Translation;
				MyJumpGate target = this.GetAllJumpGates()
					.Select((gate) => new MyTuple<MyJumpGate, double>(gate, Vector3D.DistanceSquared(camera_position, gate.WorldJumpNode)))
					.FirstOrDefault((pair) => {
						double radius = 2 * pair.Item1.JumpNodeRadius();
						return pair.Item2 <= radius * radius && pair.Item1.JumpGateGrid != null && (settings.DisplayForAttachedGates || !pair.Item1.JumpGateGrid.HasCubeGrid(cockpit.CubeGrid));
					}).Item1;

				if (target != null)
				{
					double extents = Math.Max(cockpit.CubeGrid.WorldAABB.Extents.Max(), 64);
					double vertex_distance_squared = extents * extents;
					float angle = (float) Vector3D.Angle(Vector3D.Up, MyAPIGateway.Session.Camera.WorldMatrix.Up);
					float size = (float) (target.JumpNodeRadius() * 0.01);

					foreach (MyTuple<Vector3D, double> pair in target.GetEffectiveJumpEllipse().GetVertices(32, 64)
						.Select((vertex) => new MyTuple<Vector3D, double>(vertex, Vector3D.DistanceSquared(vertex, camera_position)))
						.Where((pair) => pair.Item2 <= vertex_distance_squared))
					{
						float ratio = (float) (pair.Item2 / vertex_distance_squared);
						float radius = MathHelper.Lerp(0.75f, 0, ratio);
						Vector4 dot_color = Vector4.Lerp(Color.Aqua, Color.Red, ratio);
						MyTransparentGeometry.AddPointBillboard(this.Materials.WhiteDot, dot_color, pair.Item1, radius, 0);
					}

					foreach (MyJumpGateAnimation animation in this.GetGateAnimationsPlaying(target))
					{
						MyJumpGateWormholeAnimation wormhole_animation = null;
						if (animation.ActiveAnimationIndex == 3) wormhole_animation = animation.GateWormholeOpenAnimation;
						else if (animation.ActiveAnimationIndex == 4) wormhole_animation = animation.GateWormholeLoopAnimation;
						else if (animation.ActiveAnimationIndex == 5) wormhole_animation = animation.GateWormholeCloseAnimation;
						if (wormhole_animation == null) continue;

						foreach (ShapeCollider collider in wormhole_animation.GetCollidersOfType())
						{
							if (!collider.IsValid) continue;
							MatrixD collider_matrix = collider.WorldMatrix;

							switch (collider.ShapeColliderDefinition.CollisionEffectType)
							{
								case CollisionEffectTypeEnum.DAMAGE:
									MyTransparentGeometry.AddBillboardOriented(this.Materials.SpacialMarkerDamagePoint, Color.Orange, collider_matrix.Translation, camera.Left, camera.Up, size, size);
									break;
								case CollisionEffectTypeEnum.JUMP:
									MyTransparentGeometry.AddBillboardOriented(this.Materials.SpacialMarkerJumpPoint, Color.Aqua, collider_matrix.Translation, camera.Left, camera.Up, size, size);
									break;
								case CollisionEffectTypeEnum.DELETE:
									MyTransparentGeometry.AddBillboardOriented(this.Materials.SpacialMarkerDeletePoint, Color.Red, collider_matrix.Translation, camera.Left, camera.Up, size, size);
									break;
							}
						}
					}

					MyTransparentGeometry.AddBillboardOriented(this.Materials.SpacialMarkerJumpNode, Color.Aqua, target.WorldJumpNode, camera.Left, camera.Up, size, size);
				}
			}
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called after world saved<br />
		/// If singleplayer or multiplayer server: Saves configuration data to file
		/// </summary>
		public override void SaveData()
		{
			base.SaveData();
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;

			Exception global_save_err, local_save_err;
			this.Configuration.Save(this, out global_save_err, out local_save_err);
			if (global_save_err != null) Logger.Error($"Error saving global config file:\n  ...\n[ {global_save_err.GetType().Name} ]: {global_save_err.Message}\n{global_save_err.StackTrace}\n{global_save_err.InnerException}");
			if (local_save_err != null) Logger.Error($"Error saving local config file:\n  ...\n[ {local_save_err.GetType().Name} ]: {local_save_err.Message}\n{local_save_err.StackTrace}\n{local_save_err.InnerException}");

			MyAPIGateway.Utilities.SetVariable($"{this.ModID}.DebugMode", this.DebugMode);
			this.UpdateSaveModDataFile();
			List<MyCockpitInfo> cockpit_terminal_settings = new List<MyCockpitInfo>(this.CockpitBlockSettings.Count);

			foreach (KeyValuePair<IMyCockpit, MyCockpitInfo> pair in this.CockpitBlockSettings)
			{
				if (pair.Key.MarkedForClose) this.CockpitBlockSettings.Remove(pair.Key);
				else if (!pair.Value.IsDefault()) cockpit_terminal_settings.Add(pair.Value);
			}

			MyAPIGateway.Utilities.SetVariable("IOTA.CockpitSettings", Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(cockpit_terminal_settings)));
		}
		#endregion
	}
}
