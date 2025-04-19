using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.CubeBlock
{
	/// <summary>
	/// Game logic for jump gate drives
	/// </summary>
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "IOTA.JumpGate.JumpGateDrive.Large", "IOTA.JumpGate.JumpGateDrive.Small")]
	internal class MyJumpGateDrive : MyCubeBlockBase
	{
		#region Public Static Variables
		/// <summary>
		/// The default drive emissive color
		/// </summary>
		public static readonly Color DefaultDriveEmitterColor = new Color(62, 133, 247);
		#endregion

		#region Private Variables
		/// <summary>
		/// Sets a wattage override when greater than 0<br />
		/// The input for this block will instead be this value (and the radiator emissives enabled)
		/// </summary>
		private double SinkOverrideMW = -1;

		/// <summary>
		/// Value for brightness multiplier of radiator emissives
		/// </summary>
		private double RadiatorEmissiveRatio = 0;

		/// <summary>
		/// The power of this wattage sink override
		/// </summary>
		private double WrapperWattageSinkPower;

		/// <summary>
		/// Used for animating the drive's emissitter emissives
		/// </summary>
		private ulong[] EmitterEmissiveTick = new ulong[2] { 0, 0 };

		/// <summary>
		/// The starting color of the emitter emissives
		/// </summary>
		private Color DriveEmitterColorStart;

		/// <summary>
		/// The ending color of the emitter emissives
		/// </summary>
		private Color DriveEmitterColorEnd;

		/// <summary>
		/// The local position of this drive (relative to construct main grid) at last tick
		/// </summary>
		private Vector3D LastLocalPosition;

		/// <summary>
		/// The local rotation of this drive (relative to construct main grid) at last tick
		/// </summary>
		private Vector3D LastLocalRotation;

		/// <summary>
		/// The model dummy used to calculate block raycast position
		/// </summary>
		private IMyModelDummy RaycastDummy;
		#endregion

		#region Public Variables
		/// <summary>
		/// The stored capacitor charge in MegaWatts
		/// </summary>
		public double StoredChargeMW { get; private set; } = 0;

		/// <summary>
		/// The maximum possible distance this drive can raycast
		/// </summary>
		public double MaxRaycastDistance;

		/// <summary>
		/// The brightness for the emitter emissives
		/// </summary>
		public double EmitterEmissiveBrightness = 1;

		/// <summary>
		/// The ID of the jump gate this drive is linked to or -1 if not linked
		/// </summary>
		public long JumpGateID { get; private set; } = -1;

		/// <summary>
		/// The color of the emitter emissives
		/// </summary>
		public Color DriveEmitterColor { get; private set; } = Color.Black;

		/// <summary>
		/// The drive configuration variables for this block
		/// </summary>
		public Configuration.LocalDriveConfiguration DriveConfiguration { get; private set; }
		#endregion

		#region Constructors
		public MyJumpGateDrive() : base() { }

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		public MyJumpGateDrive(MySerializedJumpGateDrive serialized) : base(serialized)
		{
			this.FromSerialized(serialized);
		}
		#endregion

		#region "object" Methods
		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this JumpGateDrive</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		#endregion

		#region CubeBlockBase Methods
		/// <summary>
		/// Appends info to the detailed info screen
		/// </summary>
		/// <param name="info">The string builder to append to</param>
		protected override void AppendCustomInfo(StringBuilder info)
		{
			MyResourceSinkComponent sink = this.Entity.Components.Get<MyResourceSinkComponent>();

			if (sink != null && this.JumpGateGrid != null && !this.JumpGateGrid.Closed)
			{
				MyJumpGate jump_gate = this.JumpGateGrid.GetJumpGate(this.JumpGateID);
				bool jump_gate_valid = jump_gate?.IsValid() ?? false;

				double input_wattage = this.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);
				double charge_ratio = this.StoredChargeMW / this.DriveConfiguration.MaxDriveChargeMW;
				double charge_time = Math.Log((1 - charge_ratio) / 0.0005) * (this.DriveConfiguration.MaxDriveChargeMW / this.DriveConfiguration.MaxDriveChargeRateMW);

				string stored_power = Math.Round(this.StoredChargeMW, 4).ToString("#.0000");
				info.Append("\n-=-=-=( Jump Gate Drive )=-=-=-\n");
				info.Append($" Input: {Math.Round(input_wattage, 4)} MW/s\n");
				info.Append($" Required Input: {Math.Round(this.ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId), 4)} MW/s\n");
				info.Append($" Stored Power: {stored_power} MW\n");
				info.Append($" Charge: {Math.Round(charge_ratio * 100, 1)}%\n");
				info.Append($" Buffer Size: {MyJumpGateModSession.AutoconvertMetricUnits(this.DriveConfiguration.MaxDriveChargeMW * 1e6, "w", 4)}\n");
				info.Append($" Charge Efficiency: {Math.Round(this.DriveConfiguration.DriveChargeEfficiency * 100, 2)}%\n");
				if (double.IsInfinity(charge_time)) info.Append($" Charged in --:--:--\n");
				else if (double.IsNaN(charge_time)) info.Append($" Charged in 00:00:00\n");
				else info.Append($" Charged in {(int) Math.Floor(charge_time / 3600):00}:{(int) Math.Floor(charge_time % 3600 / 60d):00}:{(int) Math.Floor(charge_time % 60d):00}\n\n");
				info.Append($" Jump Gate: {jump_gate?.GetPrintableName() ?? "N/A"}\n");
				info.Append($" Jump Gate ID: {jump_gate?.JumpGateID.ToString() ?? "N/A"}\n");
				info.Append($" Current Construct: {(this.JumpGateGrid?.CubeGridID.ToString() ?? "N/A")}");
			}
		}

		protected override void Clean()
		{
			base.Clean();
			this.DriveConfiguration = null;
		}

		/// <summary>
		/// Initializes the block's game logic
		/// </summary>
		/// <param name="object_builder">This entity's object builder</param>
		public override void Init(MyObjectBuilder_EntityBase object_builder)
		{
			this.Init(object_builder, MyEntityUpdateEnum.BEFORE_NEXT_FRAME, () => {
				if (this.TerminalBlock.IsWorking && this.SinkOverrideMW > 0) return (float) this.SinkOverrideMW;
				else if (this.TerminalBlock.IsWorking)
				{
					double capacity_ratio = MathHelperD.Clamp(this.StoredChargeMW / this.DriveConfiguration.MaxDriveChargeMW, 0, 1);
					return (float) (this.DriveConfiguration.MaxDriveChargeRateMW * (1 - capacity_ratio));
				}
				else return 0f;
			}, MyJumpGateModSession.BlockComponentDataGUID);

			this.DriveConfiguration = new Configuration.LocalDriveConfiguration(this, MyJumpGateModSession.Configuration.DriveConfiguration);
			this.TerminalBlock.Synchronized = true;
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_DRIVE, this.OnNetworkBlockUpdate);
			string blockdata;

			if (this.ModStorageComponent.TryGetValue(MyJumpGateModSession.BlockComponentDataGUID, out blockdata) && blockdata.Length > 0)
			{
				try
				{
					byte[] bytes = Convert.FromBase64String(blockdata);
					this.StoredChargeMW = MathHelper.Clamp(BitConverter.ToDouble(bytes, 0), 0, this.DriveConfiguration.MaxDriveChargeMW);
					this.JumpGateID = BitConverter.ToInt64(bytes, sizeof(double));
				}
				catch (Exception e)
				{
					Logger.Error($"Failed to load block data: {this.GetType().Name}-{this.TerminalBlock.CustomName}\n{e}");
				}
			}
			else this.ModStorageComponent.Add(MyJumpGateModSession.BlockComponentDataGUID, "");

			this.MaxRaycastDistance = this.DriveConfiguration.DriveRaycastDistance;
			this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, (float) this.DriveConfiguration.MaxDriveChargeRateMW);

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet request = new MyNetworkInterface.Packet
				{
					TargetID = 0,
					Broadcast = false,
					PacketType = MyPacketTypeEnum.UPDATE_DRIVE,
				};
				request.Payload<MySerializedJumpGateDrive>(this.ToSerialized(true));
				request.Send();
			}

			Dictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();
			this.TerminalBlock.Model.GetDummies(dummies);
			this.RaycastDummy = dummies.GetValueOrDefault("camera", null);
		}

		/// <summary>
		/// MyGameLogicComponent method (Overridable)<br />
		/// Called once before first tick<br />
		/// </summary>
		public override void UpdateOnceBeforeFrame()
		{
			base.UpdateOnceBeforeFrame();
			this.TerminalBlock?.SetEmissiveParts("Emissive0", Color.Black, 0);
			this.TerminalBlock?.SetEmissiveParts("Emissive2", Color.Black, 0);
		}

		/// <summary>
		/// MyGameLogicComponent method (Overridable)<br />
		/// Called every tick after simulation<br />
		/// Updates the power requirments for this block<br />
		/// Updates the detailed info string
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();
			
			// Skip update if a projection or closed
			if (this.TerminalBlock?.CubeGrid?.Physics == null || this.IsClosed()) return;

			// Update emissives
			bool working = this.IsWorking();
			if (working) this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Lime, 1);
			else if (this.TerminalBlock.IsFunctional) this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Red, 1);
			else this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Black, 0);

			// Update local position
			if (this.LocalGameTick % 10 == 0 && this.JumpGateGrid?.CubeGrid != null && this.JumpGateGrid.FullyInitialized)
			{
				MatrixD matrix = this.JumpGateGrid.CubeGrid.WorldMatrix;
				Vector3D local_position = MyJumpGateModSession.WorldVectorToLocalVectorP(ref matrix, this.TerminalBlock.WorldMatrix.Translation);
				Vector3D local_rotation = MyJumpGateModSession.WorldVectorToLocalVectorD(ref matrix, this.TerminalBlock.WorldMatrix.Forward);
				double distance = (this.IsLargeGrid) ? 1.25 : 0.25;
				double degrees = 0.05d * Math.PI / 180d;

				if (Vector3D.Distance(local_position, this.LastLocalPosition) >= distance || Vector3D.Angle(local_rotation, this.LastLocalRotation) >= degrees)
				{
					this.JumpGateGrid.MarkGatesForUpdate();
					this.LastLocalPosition = local_position;
					this.LastLocalRotation = local_rotation;
				}
			}

			// Update stored power charge
			if (working && this.ResourceSink != null && this.SinkOverrideMW == -1 && this.StoredChargeMW < this.DriveConfiguration.MaxDriveChargeMW)
			{
				double power_draw_mw = (double) this.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) / 60d;
				this.StoredChargeMW = MathHelperD.Clamp(this.StoredChargeMW + power_draw_mw * this.DriveConfiguration.DriveChargeEfficiency, 0, this.DriveConfiguration.MaxDriveChargeMW);
			}

			// Update emissives further (based on jump gate status)
			if (working && this.JumpGateGrid != null && !this.JumpGateGrid.Closed)
			{
				MyJumpGate jump_gate = this.JumpGateGrid?.GetJumpGate(this.JumpGateID);
				bool controlled = jump_gate?.Controller != null;

				if (jump_gate != null && controlled)
				{
					this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Lime, 1);
				}
				else if (jump_gate != null)
				{
					if (this.DriveEmitterColor != Color.Black && !this.DriveEmitterCycling()) this.CycleDriveEmitter(this.DriveEmitterColor, Color.Black, 300);
					this.TerminalBlock.SetEmissiveParts("Emissive1", (MyJumpGateModSession.GameTick % 120 >= 60) ? Color.Green : Color.Black, 1);
				}
				else
				{
					if (this.DriveEmitterColor != Color.Black && !this.DriveEmitterCycling()) this.CycleDriveEmitter(this.DriveEmitterColor, Color.Black, 300);
					this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Gold, 1);
				}
			}
			else
			{
				if (this.DriveEmitterColor != Color.Black && !this.DriveEmitterCycling()) this.CycleDriveEmitter(this.DriveEmitterColor, Color.Black, 300);
				this.TerminalBlock.SetEmissiveParts("Emissive2", Color.Black, 0);
			}

			// Animate emitter emissives
			ulong cycle_tick_0 = this.EmitterEmissiveTick[0];
			ulong cycle_tick_1 = this.EmitterEmissiveTick[1];

			if (cycle_tick_0 > 0 || cycle_tick_1 > 0)
			{
				double cycle_tick_0d = cycle_tick_0;
				double cycle_tick_1d = cycle_tick_1;
				double ratio = MathHelperD.Clamp((MyJumpGateModSession.GameTick - cycle_tick_0d) / (cycle_tick_1d - cycle_tick_0d), 0, 1);
				Vector4D color_start = this.DriveEmitterColorStart.ToVector4();
				Vector4D color_end = this.DriveEmitterColorEnd.ToVector4();
				Vector4D result = (color_end - color_start) * ratio + color_start;
				this.DriveEmitterColor = new Vector4((float) result.X, (float) result.Y, (float) result.Z, (float) result.W);

				if (ratio == 1)
				{
					this.EmitterEmissiveTick[0] = 0;
					this.EmitterEmissiveTick[1] = 0;
				}
			}

			this.TerminalBlock.SetEmissiveParts("Emissive0", this.DriveEmitterColor, (float) (this.DriveEmitterColor.A / 255d * this.EmitterEmissiveBrightness));

			// Animate radiator emissive
			if (this.SinkOverrideMW != -1 && this.RadiatorEmissiveRatio < 1) this.RadiatorEmissiveRatio += 1d / 60d / 50d;
			else if (this.SinkOverrideMW == -1 && this.RadiatorEmissiveRatio > 0) this.RadiatorEmissiveRatio -= 1d / 60d / 50d;
			this.RadiatorEmissiveRatio = MathHelperD.Clamp(this.RadiatorEmissiveRatio, 0, 1);
			this.TerminalBlock.SetEmissiveParts("Emissive2", this.RadiatorEmissiveRatio == 0 ? Color.Black : new Color(250, 157, 35), (float) (this.RadiatorEmissiveRatio * 100));

			// Broadcast this update to clients if a server every 'x' game ticks
			this.CheckSendGlobalUpdate();
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Unregisters events and marks block for close
		/// </summary>
		public override void MarkForClose()
		{
			base.MarkForClose();
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_CAPACITOR, this.OnNetworkBlockUpdate);
		}

		/// <summary>
		/// Serializes block for streaming and saving
		/// </summary>
		/// <returns>false</returns>
		public override bool IsSerialized()
		{
			bool res = base.IsSerialized();
			byte[] bytes = new byte[sizeof(double) + sizeof(long)];
			Buffer.BlockCopy(BitConverter.GetBytes(this.StoredChargeMW), 0, bytes, 0, 8);
			Buffer.BlockCopy(BitConverter.GetBytes(this.JumpGateID), 0, bytes, 8, 8);
			this.ModStorageComponent[MyJumpGateModSession.BlockComponentDataGUID] = Convert.ToBase64String(bytes);
			return res;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this component recevies a block update request from server
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkBlockUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.EpochTime <= this.LastUpdateTime) return;
			MySerializedJumpGateDrive serialized = packet.Payload<MySerializedJumpGateDrive>();
			if (serialized == null || JumpGateUUID.FromGuid(serialized.UUID).GetBlock() != this.TerminalBlock.EntityId) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (serialized.IsClientRequest)
				{
					packet.Payload(this.ToSerialized(false));
					packet.Send();
				}
				else if (this.FromSerialized(serialized))
				{
					this.LastUpdateTime = packet.EpochTime;
					this.IsDirty = false;
					packet.Forward(0, true).Send();
				}
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && this.FromSerialized(serialized))
			{
				this.LastUpdateTime = packet.EpochTime;
				this.IsDirty = false;
			}
		}

		/// <summary>
		/// Server only<br />
		/// Checks if this block should be updated and if true -<br />
		/// sends an update request for this block to all clients
		/// </summary>
		private void CheckSendGlobalUpdate()
		{
			if (MyJumpGateModSession.Network.Registered && ((MyNetworkInterface.IsMultiplayerServer && MyJumpGateModSession.GameTick % MyCubeBlockBase.ForceUpdateDelay == 0) || this.IsDirty))
			{
				MyNetworkInterface.Packet update_packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_DRIVE,
					TargetID = 0,
					Broadcast = MyNetworkInterface.IsMultiplayerServer,
				};

				update_packet.Payload(this.ToSerialized(false));
				update_packet.Send();
				if (MyNetworkInterface.IsMultiplayerServer) this.LastUpdateTime = update_packet.EpochTime;
				this.IsDirty = false;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Updates this block data from a serialized drive
		/// </summary>
		/// <param name="drive">The serialized drive data</param>
		/// <returns>Whether this block was updated</returns>
		public bool FromSerialized(MySerializedJumpGateDrive drive)
		{
			if (!base.FromSerialized(drive)) return false;
			this.SinkOverrideMW = drive.SinkOverrideMW;
			this.StoredChargeMW = drive.StoredChargeMW;
			this.WrapperWattageSinkPower = drive.WattageSinkPowerUsage;
			this.JumpGateID = drive.JumpGateID;
			return true;
		}

		/// <summary>
		/// Sets an override to change this block's power usage
		/// </summary>
		/// <param name="override_mw">The new value in MW</param>
		public void SetWattageSinkOverride(double override_mw)
		{
			if (this.IsNullWrapper) return;
			else if (override_mw <= 0)
			{
				this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, (float) (this.DriveConfiguration.BaseInputWattageMW + this.DriveConfiguration.MaxDriveChargeRateMW));
				this.SinkOverrideMW = -1;
			}
			else
			{
				this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, (float) override_mw);
				this.SinkOverrideMW = override_mw;
			}
		}

		/// <summary>
		/// Attaches this drive to the specified jump gate<br />
		/// Jump gate must be null or a jump gate on the same construct as this drive
		/// </summary>
		/// <param name="jump_gate">The jump gate to attach to or null to detach</param>
		public void SetAttachedJumpGate(MyJumpGate jump_gate)
		{
			long gid = (jump_gate == null || jump_gate.Closed || jump_gate.JumpGateGrid != this.JumpGateGrid) ? -1 : jump_gate.JumpGateID;
			if (gid != this.JumpGateID) this.SetWattageSinkOverride(-1);
			this.JumpGateID = gid;
		}

		/// <summary>
		/// Animates this drive's emitter emissives' color
		/// </summary>
		/// <param name="from">The start color</param>
		/// <param name="to">The end color</param>
		/// <param name="duration">The duration in game ticks</param>
		public void CycleDriveEmitter(Color from, Color to, ushort duration)
		{
			if (from == to)
			{
				this.SetDriveEmitterColor(from);
				return;
			}

			this.DriveEmitterColorStart = from;
			this.DriveEmitterColorEnd = to;
			this.EmitterEmissiveTick[0] = MyJumpGateModSession.GameTick + 1;
			this.EmitterEmissiveTick[1] = MyJumpGateModSession.GameTick + duration + 1;
		}

		/// <summary>
		/// Sets this drive's emitter emissive color
		/// </summary>
		/// <param name="color">The new emitter color</param>
		public void SetDriveEmitterColor(Color color)
		{
			this.EmitterEmissiveTick[0] = 0;
			this.EmitterEmissiveTick[1] = 0;
			this.DriveEmitterColor = color;
		}

		/// <summary>
		/// Whether this drive's emitter emissives are being animated
		/// </summary>
		/// <returns>Animatedness</returns>
		public bool DriveEmitterCycling()
		{
			return this.EmitterEmissiveTick[0] != 0 || this.EmitterEmissiveTick[1] != 0;
		}

		/// <summary>
		/// Gets the wattage sink override
		/// </summary>
		/// <returns>The current wattage override or 0 if no override is set</returns>
		public double GetWattageSinkOverride()
		{
			return Math.Max(0, this.SinkOverrideMW);
		}

		/// <summary>
		/// Gets the amount of power being drawn by this drive's power sink
		/// </summary>
		/// <returns>The current input power in MW</returns>
		public double GetCurrentWattageSinkInput()
		{
			return (this.IsNullWrapper) ? this.WrapperWattageSinkPower : ((double) this.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) - this.DriveConfiguration.BaseInputWattageMW);
		}

		/// <summary>
		/// Drains the capacitor's charge
		/// </summary>
		/// <param name="power_mw">The power (in MegaWatts) to drain</param>
		/// <returns>The remaining power in MegaWatts</returns>
		public double DrainStoredCharge(double power_mw)
		{
			if (power_mw <= 0) return 0;
			double new_charge = MathHelperD.Clamp(this.StoredChargeMW - power_mw, 0, this.DriveConfiguration.MaxDriveChargeMW);
			power_mw -= this.StoredChargeMW - new_charge;
			this.StoredChargeMW = new_charge;
			return power_mw;
		}

		/// <summary>
		/// Gets the endpoint for a ray starting at the end of the collision mesh and ending after the specified distance
		/// </summary>
		/// <param name="distance">The distance to cast</param>
		/// <returns>A world coordinate indicating the raycast endpoint</returns>
		public Vector3D GetDriveRaycastEndpoint(double distance)
		{
			if (this.IsNullWrapper) return Vector3D.Zero;
			else if (this.RaycastDummy == null)
			{
				double collision_distance = (this.TerminalBlock.ModelCollision.BoundingBoxSizeHalf * Vector3.Abs(Base6Directions.GetIntVector(Base6Directions.Direction.Forward))).Length();
				Vector3D direction = this.TerminalBlock.WorldMatrix.GetDirectionVector(Base6Directions.Direction.Forward);
				return this.TerminalBlock.WorldMatrix.Translation + direction * (distance + collision_distance);
			}
			else
			{
				Vector3D dummypos = this.RaycastDummy.Matrix.Translation;
				Vector3D direction = this.RaycastDummy.Matrix.Forward;
				direction += (direction.Normalized() * distance);
				return MyJumpGateModSession.LocalVectorToWorldVectorP(this.TerminalBlock.WorldMatrix, dummypos) + MyJumpGateModSession.LocalVectorToWorldVectorD(this.TerminalBlock.WorldMatrix, direction);
			}
		}

		/// <summary>
		/// Gets the endpoint for a ray starting at the block's center and ending at the end of the collision mesh
		/// </summary>
		/// <returns>A world coordinate indicating the raycast start point</returns>
		public Vector3D GetDriveRaycastStartpoint()
		{
			if (this.IsNullWrapper) return Vector3D.Zero;
			else if (this.RaycastDummy == null)
			{
				double collision_distance = (this.TerminalBlock.ModelCollision.BoundingBoxSizeHalf * Vector3.Abs(Base6Directions.GetIntVector(Base6Directions.Direction.Forward))).Length();
				Vector3D direction = this.TerminalBlock.WorldMatrix.GetDirectionVector(Base6Directions.Direction.Forward);
				return this.TerminalBlock.WorldMatrix.Translation + direction;
			}
			else
			{
				Vector3D dummypos = this.RaycastDummy.Matrix.Translation;
				Vector3D direction = this.RaycastDummy.Matrix.Forward;
				return MyJumpGateModSession.LocalVectorToWorldVectorP(this.TerminalBlock.WorldMatrix, dummypos) + MyJumpGateModSession.LocalVectorToWorldVectorD(this.TerminalBlock.WorldMatrix, direction);
			}
		}

		/// <summary>
		/// Serializes this block's data
		/// </summary>
		/// <param name="as_client_request">If true, this will be an update request to server</param>
		/// <returns>The serialized drive data</returns>
		public new MySerializedJumpGateDrive ToSerialized(bool as_client_request)
		{
			if (as_client_request) return base.ToSerialized<MySerializedJumpGateDrive>(true);
			MySerializedJumpGateDrive serialized = base.ToSerialized<MySerializedJumpGateDrive>(false);
			serialized.SinkOverrideMW = this.SinkOverrideMW;
			serialized.StoredChargeMW = this.StoredChargeMW;
			serialized.WattageSinkPowerUsage = this.GetCurrentWattageSinkInput();
			serialized.JumpGateID = this.JumpGateID;
			return serialized;
		}
		#endregion
	}

	/// <summary>
	/// Class for holding serialized JumpGateDrive data
	/// </summary>
	[ProtoContract]
	internal class MySerializedJumpGateDrive : MySerializedCubeBlockBase
	{
		/// <summary>
		/// The current sink override in MegaWatts
		/// </summary>
		[ProtoMember(20)]
		public double SinkOverrideMW;

		/// <summary>
		/// The current stored charge in MegaWatts
		/// </summary>
		[ProtoMember(21)]
		public double StoredChargeMW;

		/// <summary>
		/// The current wattage override power usage of the block
		/// </summary>
		[ProtoMember(22)]
		public double WattageSinkPowerUsage;

		/// <summary>
		/// The currently attached jump gate or -1
		/// </summary>
		[ProtoMember(23)]
		public long JumpGateID;
	}
}
