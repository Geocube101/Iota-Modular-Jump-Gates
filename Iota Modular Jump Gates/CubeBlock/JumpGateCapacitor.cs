using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace IOTA.ModularJumpGates.CubeBlock
{
	/// <summary>
	/// Game logic for jump gate capacitors
	/// </summary>
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "IOTA.JumpGate.JumpGateCapacitor.Large", "IOTA.JumpGate.JumpGateCapacitor.Small")]
    internal class MyJumpGateCapacitor : MyCubeBlockBase
    {
        /// <summary>
        /// Stores block data for this block type
        /// </summary>
        [ProtoContract]
		public sealed class MyCapacitorBlockSettingsStruct
        {
			[ProtoMember(1)]
			public bool RechargeEnabled = true;
            [ProtoMember(2)]
            public double InternalChargeMW = 0;
            [ProtoMember(3)]
            public Color EmissiveColor = Color.White;

            public MyCapacitorBlockSettingsStruct() { }

			public void FromDictionary(Dictionary<string, object> mapping)
			{
				if (mapping == null) return;
				this.RechargeEnabled = (bool) mapping.GetValueOrDefault("RechargeEnabled", this.RechargeEnabled);
				this.InternalChargeMW = (double) mapping.GetValueOrDefault("InternalChargeMW", this.InternalChargeMW);
				this.EmissiveColor = (Color) mapping.GetValueOrDefault("EmissiveColor", this.EmissiveColor);
			}

			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object>() {
					["RechargeEnabled"] = this.RechargeEnabled,
					["InternalChargeMW"] = this.InternalChargeMW,
					["EmissiveColor"] = this.EmissiveColor,
				};
			}
        }
		
		#region Private Variables
		/// <summary>
		/// The block's resource source
		/// </summary>
		protected MyResourceSourceComponent ResourceSource { get; private set; }
		#endregion

		#region Public Variables
		/// <summary>
		/// The stored capacitor charge in MegaWatts
		/// </summary>
		public double StoredChargeMW { get; private set; } = 0;

        /// <summary>
        /// The block data for this block
        /// </summary>
        public MyCapacitorBlockSettingsStruct BlockSettings { get; private set; }

        /// <summary>
        /// The capacitor configuration variables for this block
        /// </summary>
		public Configuration.LocalCapacitorConfiguration CapacitorConfiguration { get; private set; }
		#endregion

		#region Constructors
		public MyJumpGateCapacitor() : base() { }

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		public MyJumpGateCapacitor(MySerializedJumpGateCapacitor serialized, MyJumpGateConstruct parent = null) : base(serialized, parent)
		{
			this.FromSerialized(serialized, parent);
		}
		#endregion

		#region "CubeBlockBase" Methods
		/// <summary>
		/// CubeBlockBase Method<br />
		/// Appends info to the detailed info screen
		/// </summary>
		/// <param name="info">The string builder to append to</param>
		protected override void AppendCustomInfo(StringBuilder info)
		{
			if (this.ResourceSink != null && this.BlockSettings.RechargeEnabled)
			{
				double input_wattage = this.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);
				double max_input = this.ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId);
				double input_ratio = input_wattage / max_input;
				double charge_ratio = this.StoredChargeMW / this.CapacitorConfiguration.MaxCapacitorChargeMW;
				double charge_time = Math.Log((1 - charge_ratio) / 0.0005) * (this.CapacitorConfiguration.MaxCapacitorChargeMW / this.CapacitorConfiguration.MaxCapacitorChargeRateMW * (1 / input_ratio));
				input_wattage *= this.CapacitorConfiguration.CapacitorChargeEfficiency;
				
				info.Append($"\n-=-=-=( {MyTexts.GetString("DisplayName_CubeBlock_JumpGateCapacitor")} )=-=-=-\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_Input")}: {MyJumpGateModSession.AutoconvertMetricUnits(input_wattage * 1e6, "W/s", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_RequiredInput")}: {MyJumpGateModSession.AutoconvertMetricUnits(max_input * 1e6, "W/s", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_InputRatio")}: {((double.IsNaN(input_ratio)) ? "N/A" : $"{Math.Round(MathHelperD.Clamp(input_ratio, 0, 1) * 100, 2):#.00}%")}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_StoredPower")}: {MyJumpGateModSession.AutoconvertMetricUnits(this.StoredChargeMW * 1e6, "W", 4):#.0000}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_Charge")}: {Math.Round(charge_ratio * 100, 1)}%\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_BufferSize")}: {MyJumpGateModSession.AutoconvertMetricUnits(this.CapacitorConfiguration.MaxCapacitorChargeMW * 1e6, "W", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_ChargeEfficiency")}: {Math.Round(this.CapacitorConfiguration.CapacitorChargeEfficiency * 100, 2)}%\n");
				if (double.IsInfinity(charge_time)) info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_ChargeTime")} --:--:--\n");
				else if (double.IsNaN(charge_time) || charge_time < 0) info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_ChargeTime")} 00:00:00\n");
				else info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_ChargeTime")} {(int) Math.Floor(charge_time / 3600):00}:{(int) Math.Floor(charge_time % 3600 / 60d):00}:{(int) Math.Floor(charge_time % 60d):00}\n");
			}
			else if (this.ResourceSource != null && !this.BlockSettings.RechargeEnabled)
			{
				double output_wattage = this.ResourceSource.CurrentOutputByType(MyResourceDistributorComponent.ElectricityId);
				double max_output = this.ResourceSource.MaxOutputByType(MyResourceDistributorComponent.ElectricityId);
				double output_ratio = output_wattage / max_output;
				double discharge_ratio = this.StoredChargeMW / this.CapacitorConfiguration.MaxCapacitorChargeMW;
				double discharge_time = Math.Log(discharge_ratio / 0.0005) * (this.CapacitorConfiguration.MaxCapacitorChargeMW / this.CapacitorConfiguration.MaxCapacitorChargeRateMW * (1 / output_ratio));

				info.Append($"\n-=-=-=( {MyTexts.GetString("DisplayName_CubeBlock_JumpGateCapacitor")} )=-=-=-\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_Output")}: {MyJumpGateModSession.AutoconvertMetricUnits(output_wattage * 1e6, "W/s", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_MaxOutput")}: {MyJumpGateModSession.AutoconvertMetricUnits(max_output * 1e6, "W", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_OutputRatio")}: {((double.IsNaN(output_ratio)) ? "N/A" : $"{Math.Round(MathHelperD.Clamp(output_ratio, 0, 1) * 100, 2):#.00}%")}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_StoredPower")}: {MyJumpGateModSession.AutoconvertMetricUnits(this.StoredChargeMW * 1e6, "W", 4):#.0000}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_Charge")}: {Math.Round((discharge_ratio) * 100, 1)}%\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_BufferSize")}: {MyJumpGateModSession.AutoconvertMetricUnits(this.CapacitorConfiguration.MaxCapacitorChargeMW * 1e6, "W", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_DischargeEfficiency")}: {Math.Round(this.CapacitorConfiguration.CapacitorDischargeEfficiency * 100, 2)}%\n");
				if (double.IsInfinity(discharge_time)) info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_DischargeTime")} --:--:--\n");
				else if (double.IsNaN(discharge_time) || discharge_time < 0) info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_DischargeTime")} 00:00:00\n");
				else info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateCapacitor_DischargeTime")} {(int) Math.Floor(discharge_time / 3600):00}:{(int) Math.Floor(discharge_time % 3600 / 60d):00}:{(int) Math.Floor(discharge_time % 60d):00}\n");
			}
		}

		protected override void Clean()
		{
			base.Clean();
			this.ResourceSource = null;
			this.BlockSettings = null;
			this.CapacitorConfiguration = null;
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Initializes the block's game logic
		/// </summary>
		/// <param name="object_builder">This entity's object builder</param>
		public override void Init(MyObjectBuilder_EntityBase object_builder)
		{
			this.Init(object_builder, 0, () => {
				if (this.TerminalBlock.IsWorking && this.BlockSettings.RechargeEnabled)
				{
					double capacity_ratio = MathHelperD.Clamp(this.StoredChargeMW / this.CapacitorConfiguration.MaxCapacitorChargeMW, 0, 1);
					return (float) (this.CapacitorConfiguration.MaxCapacitorChargeRateMW * (1 - capacity_ratio));
				}
				else return 0f;
			}, MyJumpGateModSession.BlockComponentDataGUID, false);
			this.CapacitorConfiguration = new Configuration.LocalCapacitorConfiguration(this, MyJumpGateModSession.Configuration.CapacitorConfiguration);
			this.TerminalBlock.Synchronized = true;
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_CAPACITOR, this.OnNetworkBlockUpdate);
			string blockdata;
			this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, (float) this.CapacitorConfiguration.MaxCapacitorChargeRateMW);

			if (this.ModStorageComponent.TryGetValue(MyJumpGateModSession.BlockComponentDataGUID, out blockdata) && blockdata.Length > 0)
			{
				try
				{
					this.BlockSettings = MyAPIGateway.Utilities.SerializeFromBinary<MyCapacitorBlockSettingsStruct>(Convert.FromBase64String(blockdata));
					this.StoredChargeMW = MathHelper.Clamp(this.BlockSettings.InternalChargeMW, 0, this.CapacitorConfiguration.MaxCapacitorChargeMW);
				}
				catch (Exception e)
				{
					this.BlockSettings = new MyCapacitorBlockSettingsStruct();
					Logger.Error($"Failed to load block data: {this.GetType().Name}-{this.TerminalBlock.CustomName}\n{e}");
				}
			}
			else
			{
				this.ModStorageComponent.Add(MyJumpGateModSession.BlockComponentDataGUID, "");
				this.BlockSettings = new MyCapacitorBlockSettingsStruct();
				if (MyAPIGateway.Session.CreativeMode) this.StoredChargeMW = this.CapacitorConfiguration.MaxCapacitorChargeMW / 2d;
			}

			this.ResourceSource = new MyResourceSourceComponent();
			MyResourceSourceInfo source_info = new MyResourceSourceInfo();
			source_info.ResourceTypeId = MyResourceDistributorComponent.ElectricityId;
			source_info.IsInfiniteCapacity = true;
			source_info.ProductionToCapacityMultiplier = 1;
			this.ResourceSource.Init(VRage.Utils.MyStringHash.GetOrCompute("Battery"), source_info);
			this.TerminalBlock.Components.Add(this.ResourceSource);
			this.ResourceSource.Enabled = false;

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet request = new MyNetworkInterface.Packet
				{
					TargetID = 0,
					Broadcast = false,
					PacketType = MyPacketTypeEnum.UPDATE_CAPACITOR,
				};
				request.Payload<MySerializedJumpGateCapacitor>(null);
				request.Send();
			}
		}

		public override void UpdateOnceBeforeFrame()
		{
			base.UpdateOnceBeforeFrame();
			if (!MyJumpGateCapacitorTerminal.IsLoaded) MyJumpGateCapacitorTerminal.Load(this.ModContext);
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Called once every tick after simulation<br />
		/// Updates power usage and emissives
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();

			// Update Emmissives
			bool is_working;
			if (this.TerminalBlock?.CubeGrid?.Physics == null || this.TerminalBlock.MarkedForClose) return;
			else if (is_working = this.IsWorking) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Lime, 1);
			else if (this.TerminalBlock.IsFunctional) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Red, 1);
			else this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Black, 1);
			double charge_ratio = this.StoredChargeMW / this.CapacitorConfiguration.MaxCapacitorChargeMW;

			// Update stored charge / resource sinks / resource sources
			if (is_working && this.BlockSettings.RechargeEnabled && this.ResourceSink != null && this.ResourceSource != null)
			{
				double power_draw_mw = (double) this.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) / 60d;
				this.StoredChargeMW = MathHelperD.Clamp(this.StoredChargeMW + power_draw_mw * this.CapacitorConfiguration.CapacitorChargeEfficiency, 0, this.CapacitorConfiguration.MaxCapacitorChargeMW);
				this.ResourceSource.Enabled = false;
			}
			else if (this.TerminalBlock.IsFunctional && this.TerminalBlock.Enabled && !this.BlockSettings.RechargeEnabled && this.ResourceSink != null && this.ResourceSource != null)
			{
				double capacity_ratio = MathHelperD.Clamp(this.StoredChargeMW / this.CapacitorConfiguration.MaxCapacitorChargeMW, 0, 1);
				float discharge_rate = (float) (this.CapacitorConfiguration.MaxCapacitorChargeRateMW * capacity_ratio);

				this.ResourceSource.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, (float) this.StoredChargeMW);
				this.ResourceSource.SetMaxOutputByType(MyResourceDistributorComponent.ElectricityId, discharge_rate);
				this.ResourceSource.Enabled = discharge_rate > 0;

				double power_draw_mw = MathHelperD.Clamp((double) this.ResourceSource.CurrentOutputByType(MyResourceDistributorComponent.ElectricityId) / 60d, 0, discharge_rate);
				this.StoredChargeMW = MathHelperD.Clamp(this.StoredChargeMW - power_draw_mw * (2 - this.CapacitorConfiguration.CapacitorDischargeEfficiency), 0, this.CapacitorConfiguration.MaxCapacitorChargeMW);
			}
			else this.ResourceSource.Enabled = false;

			if (!this.TerminalBlock.IsFunctional && this.StoredChargeMW > 0) this.StoredChargeMW = Math.Max(0, this.StoredChargeMW - 0.01d * this.CapacitorConfiguration.MaxCapacitorChargeMW / 60d);

			// Update Charge Emissives
			Random rng = new Random();

			for (byte i = 1; i <= 4; ++i)
			{
				double min = 0.25d * (i - 1);
				double max = 0.25d * i;
				double local_charge_ratio = MathHelper.Clamp((charge_ratio - min) / (max - min), 0, 1);
				Vector3 ring_color = new Vector3D(this.BlockSettings.EmissiveColor.ToVector3()) * local_charge_ratio;
				double mult = (this.TerminalBlock.IsFunctional) ? 1 : (rng.NextDouble() * 0.25d);
				this.TerminalBlock.SetEmissiveParts($"Emissive{i}", ring_color, (float) (local_charge_ratio * mult));
			}

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
			this.BlockSettings.InternalChargeMW = this.StoredChargeMW;
			this.ModStorageComponent[MyJumpGateModSession.BlockComponentDataGUID] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings));
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
			if (packet == null) return;
			MySerializedJumpGateCapacitor serialized = packet.Payload<MySerializedJumpGateCapacitor>();
			if (serialized == null || serialized.UUID.GetBlock() != this.BlockID) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1 && packet.EpochTime > this.LastUpdateTime)
			{
				if (serialized.IsClientRequest)
				{
					packet.Payload(this.ToSerialized(false));
					packet.Send();
				}
				else if (this.LastUpdateDateTimeUTC < packet.EpochDateTimeUTC && this.FromSerialized(serialized))
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
                MyNetworkInterface.Packet update_packet = new MyNetworkInterface.Packet {
                    PacketType = MyPacketTypeEnum.UPDATE_CAPACITOR,
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
		public override void ReloadConfigurations()
		{
			this.CapacitorConfiguration = new Configuration.LocalCapacitorConfiguration(this, MyJumpGateModSession.Configuration.CapacitorConfiguration);
		}

		/// <summary>
		/// Updates this block data from a serialized capacitor
		/// </summary>
		/// <param name="capacitor">The serialized capacitor data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		/// <returns>Whether this block was updated</returns>
		public bool FromSerialized(MySerializedJumpGateCapacitor capacitor, MyJumpGateConstruct parent = null)
        {
			if (!base.FromSerialized(capacitor, parent)) return false;
			else if (this.JumpGateGrid == null) return true;
			this.BlockSettings = MyAPIGateway.Utilities.SerializeFromBinary<MyCapacitorBlockSettingsStruct>(capacitor.SerializedBlockSettings);
            this.StoredChargeMW = this.BlockSettings.InternalChargeMW;
            return true;
        }

		/// <summary>
		/// Drains the capacitor's charge
		/// </summary>
		/// <param name="power_mw">The power (in MegaWatts) to drain</param>
		/// <returns>The remaining power (in MegaWatts)</returns>
		public double DrainStoredCharge(double power_mw)
        {
            if (power_mw <= 0) return 0;
            double new_charge = MathHelperD.Clamp(this.StoredChargeMW - power_mw, 0, this.CapacitorConfiguration.MaxCapacitorChargeMW);
			power_mw -= this.StoredChargeMW - new_charge;
            this.StoredChargeMW = new_charge;
            return power_mw;
        }

		/// <summary>
		/// Serializes this block's data
		/// </summary>
		/// <param name="as_client_request">If true, this will be an update request to server</param>
		/// <returns>The serialized capacitor data</returns>
		public new MySerializedJumpGateCapacitor ToSerialized(bool as_client_request)
        {
			if (as_client_request) return base.ToSerialized<MySerializedJumpGateCapacitor>(true);
			this.BlockSettings.InternalChargeMW = this.StoredChargeMW;
			MySerializedJumpGateCapacitor serialized = base.ToSerialized<MySerializedJumpGateCapacitor>(false);
			serialized.SerializedBlockSettings = MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings);
			return serialized;
        }
		#endregion
	}

	/// <summary>
	/// Class for holding serialized JumpGateCapacitor data
	/// </summary>
	[ProtoContract]
    internal class MySerializedJumpGateCapacitor : MySerializedCubeBlockBase
    {
		/// <summary>
		/// The serialized block settings as a base64 string
		/// </summary>
        [ProtoMember(20)]
		public byte[] SerializedBlockSettings;
	}
}
