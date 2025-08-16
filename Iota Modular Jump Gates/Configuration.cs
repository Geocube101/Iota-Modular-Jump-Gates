using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRageMath;

namespace IOTA.ModularJumpGates
{
	/// <summary>
	/// Class for holding all mod configuration data
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class Configuration
	{
		#region Configuration Schemas
		[ProtoContract(UseProtoMembersOnly = true)]
		public sealed class CapacitorConfigurationSchema
		{
			/// <summary>
			/// The maximum charge (in MegaWatts) a large grid capacitor can store<br />
			/// Cannot be NaN, Infinity, or less than 1<br />
			/// Defaults to 1000 MW
			/// </summary>
			[ProtoMember(1, IsRequired = true)]
			public double MaxLargeCapacitorChargeMW = 1000d;

			/// <summary>
			/// The maximum charge (in MegaWatts) a small grid capacitor can store<br />
			/// Cannot be NaN, Infinity, or less than 1<br />
			/// Defaults to 200 MW
			/// </summary>
			[ProtoMember(2, IsRequired = true)]
			public double MaxSmallCapacitorChargeMW = 200d;

			/// <summary>
			/// The maximum charge rate (in MegaWatts per second) a large grid capacitor can charge at<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Should not be 0<br />
			/// Defaults to 12.5 MW/s
			/// </summary>
			[ProtoMember(3, IsRequired = true)]
			public double MaxLargeCapacitorChargeRateMW = 25d;

			/// <summary>
			/// The maximum charge rate (in MegaWatts per second) a small grid capacitor can charge at<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Should not be 0<br />
			/// Defaults to 2.5 MW/s
			/// </summary>
			[ProtoMember(4, IsRequired = true)]
			public double MaxSmallCapacitorChargeRateMW = 5d;

			/// <summary>
			/// The power transfer efficiency of large grid capacitors when charging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.95
			/// </summary>
			[ProtoMember(5, IsRequired = true)]
			public double LargeCapacitorChargeEfficiency = 0.95;

			/// <summary>
			/// The power transfer efficiency of small grid capacitors when charging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.8
			/// </summary>
			[ProtoMember(6, IsRequired = true)]
			public double SmallCapacitorChargeEfficiency = 0.8;

			/// <summary>
			/// The power transfer efficiency of large grid capacitors when discharging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.75
			/// </summary>
			[ProtoMember(7, IsRequired = true)]
			public double LargeCapacitorDischargeEfficiency = 0.75;

			/// <summary>
			/// The power transfer efficiency of small grid capacitors when discharging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.5
			/// </summary>
			[ProtoMember(8, IsRequired = true)]
			public double SmallCapacitorDischargeEfficiency = 0.5;

			/// <summary>
			/// Validates all values
			/// </summary>
			internal void Validate()
			{
				CapacitorConfigurationSchema defaults = new CapacitorConfigurationSchema();
				this.MaxLargeCapacitorChargeMW = ValidateDoubleValue(this.MaxLargeCapacitorChargeMW, defaults.MaxLargeCapacitorChargeMW, 1);
				this.MaxSmallCapacitorChargeMW = ValidateDoubleValue(this.MaxSmallCapacitorChargeMW, defaults.MaxSmallCapacitorChargeMW, 1);
				this.MaxLargeCapacitorChargeRateMW = ValidateDoubleValue(this.MaxLargeCapacitorChargeRateMW, defaults.MaxLargeCapacitorChargeRateMW, 0);
				this.MaxSmallCapacitorChargeRateMW = ValidateDoubleValue(this.MaxSmallCapacitorChargeRateMW, defaults.MaxSmallCapacitorChargeRateMW, 0);
				this.LargeCapacitorChargeEfficiency = ValidateDoubleValue(this.LargeCapacitorChargeEfficiency, defaults.LargeCapacitorChargeEfficiency, 0, 1);
				this.SmallCapacitorChargeEfficiency = ValidateDoubleValue(this.SmallCapacitorChargeEfficiency, defaults.SmallCapacitorChargeEfficiency, 0, 1);
				this.LargeCapacitorDischargeEfficiency = ValidateDoubleValue(this.LargeCapacitorDischargeEfficiency, defaults.LargeCapacitorDischargeEfficiency, 0, 1);
				this.SmallCapacitorDischargeEfficiency = ValidateDoubleValue(this.SmallCapacitorDischargeEfficiency, defaults.SmallCapacitorDischargeEfficiency, 0, 1);
			}
		}

		[ProtoContract(UseProtoMembersOnly = true)]
		public sealed class DriveConfigurationSchema
		{
			/// <summary>
			/// The maximum distance (in meters) a large grid drive can raycast to check gate intersections<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 250 m
			/// </summary>
			[ProtoMember(1, IsRequired = true)]
			public double LargeDriveRaycastDistance = 250d;

			/// <summary>
			/// The maximum distance (in meters) a small grid drive can raycast to check gate intersections<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 50 m
			/// </summary>
			[ProtoMember(2, IsRequired = true)]
			public double SmallDriveRaycastDistance = 50d;

			/// <summary>
			/// The maximum offset (in meters) in which rays are considered to be overlapping for large drives<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 2.5 m
			/// </summary>
			[ProtoMember(3, IsRequired = true)]
			public double LargeDriveRaycastWidth = 2.5d;

			/// <summary>
			/// The maximum offset (in meters) in which rays are considered to be overlapping for small drives<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 0.5 m
			/// </summary>
			[ProtoMember(4, IsRequired = true)]
			public double SmallDriveRaycastWidth = 0.5d;

			/// <summary>
			/// The maximum charge (in MegaWatts) a large grid drive can store<br />
			/// Cannot be NaN, Infinity, or less than 1<br />
			/// Defaults to 100 MW
			/// </summary>
			[ProtoMember(5, IsRequired = true)]
			public double MaxLargeDriveChargeMW = 100;

			/// <summary>
			/// The maximum charge (in MegaWatts) a small grid drive can store<br />
			/// Cannot be NaN, Infinity, or less than 1<br />
			/// Defaults to 25 MW
			/// </summary>
			[ProtoMember(6, IsRequired = true)]
			public double MaxSmallDriveChargeMW = 25d;

			/// <summary>
			/// The maximum charge rate (in MegaWatts per second) a large grid drive can charge at<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Should not be 0<br />
			/// Defaults to 10 MW/s
			/// </summary>
			[ProtoMember(7, IsRequired = true)]
			public double MaxLargeDriveChargeRateMW = 10d;

			/// <summary>
			/// The maximum charge rate (in MegaWatts per second) a small grid drive can charge at<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Should not be 0<br />
			/// Defaults to 2 MW/s
			/// </summary>
			[ProtoMember(8, IsRequired = true)]
			public double MaxSmallDriveChargeRateMW = 2d;

			/// <summary>
			/// The base input wattage (in MegaWatts) for large grid drives while idle
			/// Cannot be NaN or Infinity<br />
			/// Should not be 0<br />
			/// Defaults to 100<br />
			/// </summary>
			[ProtoMember(9, IsRequired = true)]
			public double LargeDriveIdleInputWattageMW = 50d;

			/// <summary>
			/// The base input wattage (in MegaWatts) for small grid drives while idle
			/// Cannot be NaN or Infinity<br />
			/// Should not be 0<br />
			/// Defaults to 25<br />
			/// </summary>
			[ProtoMember(10, IsRequired = true)]
			public double SmallDriveIdleInputWattageMW = 15d;

			/// <summary>
			/// The base input wattage (in MegaWatts) for large grid drives while active
			/// Cannot be NaN or Infinity<br />
			/// Should not be 0<br />
			/// Defaults to 100<br />
			/// </summary>
			[ProtoMember(11, IsRequired = true)]
			public double LargeDriveActiveInputWattageMW = 75d;

			/// <summary>
			/// The base input wattage (in MegaWatts) for small grid drives while active
			/// Cannot be NaN or Infinity<br />
			/// Should not be 0<br />
			/// Defaults to 25<br />
			/// </summary>
			[ProtoMember(12, IsRequired = true)]
			public double SmallDriveActiveInputWattageMW = 45d;

			/// <summary>
			/// The power transfer efficiency of large grid drives when charging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.99
			/// </summary>
			[ProtoMember(13, IsRequired = true)]
			public double LargeDriveChargeEfficiency = 0.99;

			/// <summary>
			/// The power transfer efficiency of small grid drives when charging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.95
			/// </summary>
			[ProtoMember(14, IsRequired = true)]
			public double SmallDriveChargeEfficiency = 0.95;

			/// <summary>
			/// Validates all values
			/// </summary>
			internal void Validate()
			{
				DriveConfigurationSchema defaults = new DriveConfigurationSchema();
				this.LargeDriveRaycastDistance = ValidateDoubleValue(this.LargeDriveRaycastDistance, defaults.LargeDriveRaycastDistance, 0);
				this.SmallDriveRaycastDistance = ValidateDoubleValue(this.SmallDriveRaycastDistance, defaults.SmallDriveRaycastDistance, 0);
				this.LargeDriveRaycastWidth = ValidateDoubleValue(this.LargeDriveRaycastWidth, defaults.LargeDriveRaycastWidth, 0);
				this.SmallDriveRaycastWidth = ValidateDoubleValue(this.SmallDriveRaycastWidth, defaults.SmallDriveRaycastWidth, 0);
				this.MaxLargeDriveChargeMW = ValidateDoubleValue(this.MaxLargeDriveChargeMW, defaults.MaxLargeDriveChargeMW, 1);
				this.MaxSmallDriveChargeMW = ValidateDoubleValue(this.MaxSmallDriveChargeMW, defaults.MaxSmallDriveChargeMW, 1);
				this.MaxLargeDriveChargeRateMW = ValidateDoubleValue(this.MaxLargeDriveChargeRateMW, defaults.MaxLargeDriveChargeRateMW, 0);
				this.MaxSmallDriveChargeRateMW = ValidateDoubleValue(this.MaxSmallDriveChargeRateMW, defaults.MaxSmallDriveChargeRateMW, 0);
				this.LargeDriveIdleInputWattageMW = ValidateDoubleValue(this.LargeDriveIdleInputWattageMW, defaults.LargeDriveIdleInputWattageMW, 0);
				this.SmallDriveIdleInputWattageMW = ValidateDoubleValue(this.SmallDriveIdleInputWattageMW, defaults.SmallDriveIdleInputWattageMW, 0);
				this.LargeDriveActiveInputWattageMW = ValidateDoubleValue(this.LargeDriveActiveInputWattageMW, defaults.LargeDriveActiveInputWattageMW, 0);
				this.SmallDriveActiveInputWattageMW = ValidateDoubleValue(this.SmallDriveActiveInputWattageMW, defaults.SmallDriveActiveInputWattageMW, 0);
				this.LargeDriveChargeEfficiency = ValidateDoubleValue(this.LargeDriveChargeEfficiency, defaults.LargeDriveChargeEfficiency, 0, 1);
				this.SmallDriveChargeEfficiency = ValidateDoubleValue(this.SmallDriveChargeEfficiency, defaults.SmallDriveChargeEfficiency, 0, 1);
			}
		}

		[ProtoContract(UseProtoMembersOnly = true)]
		public sealed class JumpGateConfigurationSchema
		{
			/// <summary>
			/// The minimum distance (in meters) below which a destination cannot be jumped to by a large grid gate<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be larger than "MaximumLargeJumpDistance"<br />
			/// Defaults to 5000 m
			/// </summary>
			[ProtoMember(1, IsRequired = true)]
			public double MinimumLargeJumpDistance = 5000d;

			/// <summary>
			/// The minimum distance (in meters) below which a destination cannot be jumped to by a small grid gate<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be larger than "MaximumSmallJumpDistance"<br />
			/// Defaults to 2500 m
			/// </summary>
			[ProtoMember(2, IsRequired = true)]
			public double MinimumSmallJumpDistance = 2500d;

			/// <summary>
			/// The maximum distance (in meters) above which a destination cannot be jumped to by a large grid gate<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be smaller than "MinimumLargeJumpDistance"<br />
			/// Defaults to INFINITE m
			/// </summary>
			[ProtoMember(3, IsRequired = true)]
			public double MaximumLargeJumpDistance = double.PositiveInfinity;

			/// <summary>
			/// The maximum distance (in meters) above which a destination cannot be jumped to by a small grid gate<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be smaller than "MinimumSmallJumpDistance"<br />
			/// Defaults to INFINITE m
			/// </summary>
			[ProtoMember(4, IsRequired = true)]
			public double MaximumSmallJumpDistance = double.PositiveInfinity;

			/// <summary>
			/// The factor by which to multiply the calculated power usage for untethered jumps for large grid gates<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be Infinity<br />
			/// Defaults to 1.05 (An extra 5%)
			/// </summary>
			[ProtoMember(5, IsRequired = true)]
			public double UntetheredLargeJumpEnergyCost = 1.05d;

			/// <summary>
			/// The factor by which to multiply the calculated power usage for untethered jumps for small grid gates<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be Infinity<br />
			/// Defaults to 1.10 (An extra 10%)
			/// </summary>
			[ProtoMember(6, IsRequired = true)]
			public double UntetheredSmallJumpEnergyCost = 1.1d;

			/// <summary>
			/// The exponent directly responsible for calculating the ratio between distance and gate size for large grid gates<br />
			/// This value is calculated using "MaxJumpGate50Distance"<br />
			/// For calculating maximum distance given gate size:<br />
			///  ... DISTANCE = ((JUMP_GATE_SIZE ^ GATE_SCALE_EXPONENT) * 1000) + MINIMUM_JUMP_DISTANCE<br />
			/// For calculating gate size given maximum distance:<br />
			///  ... JUMP_GATE_SIZE = CEIL(((DISTANCE - MINIMUM_JUMP_DISTANCE) / 1000) ^ (1 / GATE_SCALE_EXPONENT))
			/// </summary>
			[ProtoIgnore]
			public double LargeGateDistanceScaleExponent;

			/// <summary>
			/// The exponent directly responsible for calculating the ratio between distance and gate size for small grid gates<br />
			/// This value is calculated using "MaxJumpGate50Distance"<br />
			/// For calculating maximum distance given gate size:<br />
			///  ... DISTANCE = ((JUMP_GATE_SIZE ^ GATE_SCALE_EXPONENT) * 1000) + MINIMUM_JUMP_DISTANCE<br />
			/// For calculating gate size given maximum distance:<br />
			///  ... JUMP_GATE_SIZE = CEIL(((DISTANCE - MINIMUM_JUMP_DISTANCE) / 1000) ^ (1 / GATE_SCALE_EXPONENT))
			/// </summary>
			[ProtoIgnore]
			public double SmallGateDistanceScaleExponent;

			/// <summary>
			/// The maximum jump gate reachable distance (in meters) for a 50 drive large grid jump gate<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 100 Mm
			/// </summary>
			[ProtoMember(7, IsRequired = true)]
			public double MaxLargeJumpGate50Distance = 100000000;

			/// <summary>
			/// The maximum jump gate reachable distance (in meters) for a 50 drive large grid jump gate<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 20 Mm
			/// </summary>
			[ProtoMember(8, IsRequired = true)]
			public double MaxSmallJumpGate50Distance = 20000000;

			/// <summary>
			/// The exponent 'b' used for power factor calculations: (a * x - a) ^ b + 1<br />
			/// Specifies how quickly the power factor changes given how close a large grid gate is to its reasonable distance<br />
			/// Cannot be NaN, Infinity or less than 0<br />
			/// Should not be less than 1<br />
			/// Defaults to 3
			/// </summary>
			[ProtoMember(9, IsRequired = true)]
			public double LargeGatePowerFactorExponent = 3d;

			/// <summary>
			/// The exponent 'b' used for power factor calculations: (a * x - a) ^ b + 1<br />
			/// Specifies how quickly the power factor changes given how close a small grid gate is to its reasonable distance<br />
			/// Cannot be NaN, Infinity or less than 0<br />
			/// Should not be less than 1<br />
			/// Defaults to 3
			/// </summary>
			[ProtoMember(10, IsRequired = true)]
			public double SmallGatePowerFactorExponent = 3d;

			/// <summary>
			/// The value 'a' used for power factor calculations on large grid jump gates: (a * x - a) ^ b + 1<br />
			/// Specifies the narrowness of the curve, higher values result in a narrower curve<br />
			/// Cannot be NaN or Infinity<br />
			/// Defaults to 1
			/// </summary>
			[ProtoMember(11, IsRequired = true)]
			public double LargeGatePowerFalloffFactor = 1d;

			/// <summary>
			/// The value 'a' used for power factor calculations on small grid jump gates: (a * x - a) ^ b + 1<br />
			/// Specifies the narrowness of the curve, higher values result in a narrower curve<br />
			/// Cannot be NaN or Infinity<br />
			/// Defaults to 1
			/// </summary>
			[ProtoMember(12, IsRequired = true)]
			public double SmallGatePowerFalloffFactor = 1d;

			/// <summary>
			/// The power (in KiloWatts) required for a large grid gate to jump one kilogram of mass<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0.1 Kw/Kg
			/// </summary>
			[ProtoMember(13, IsRequired = true)]
			public double LargeGateKilowattPerKilogram = 0.1d;

			/// <summary>
			/// The power (in KiloWatts) required for a small grid gate to jump one kilogram of mass<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0.25 Kw/Kg
			/// </summary>
			[ProtoMember(14, IsRequired = true)]
			public double SmallGateKilowattPerKilogram = 0.25d;

			/// <summary>
			/// The maximum distance (in kilometers) per kilometer an endpoint can be offset by if untethered for large grid gates<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0.01 Km
			/// </summary>
			[ProtoMember(15, IsRequired = true)]
			public double LargeGateRandomOffsetPerKilometer = 0.01d;

			/// <summary>
			/// The maximum distance (in kilometers) per kilometer an endpoint can be offset by if untethered for small grid gates<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0.05 Km
			/// </summary>
			[ProtoMember(16, IsRequired = true)]
			public double SmallGateRandomOffsetPerKilometer = 0.05d;

			/// <summary>
			/// The maximum distance (in KiloMeters) per unit of gravity 'g' an endpoint will be offset by if untethered for large grid gates<br />
			/// Cannot be NaN or Infinite<br />
			/// Defaults to 5 Km
			/// </summary>
			[ProtoMember(17, IsRequired = true)]
			public double LargeGateKilometerOffsetPerUnitG = 0.5d;

			/// <summary>
			/// The maximum distance (in KiloMeters) per unit of gravity 'g' an endpoint will be offset by if untethered for small grid gates<br />
			/// Cannot be NaN or Infinite<br />
			/// Defaults to 10 Km
			/// </summary>
			[ProtoMember(18, IsRequired = true)]
			public double SmallGateKilometerOffsetPerUnitG = 0.825d;

			/// <summary>
			/// If false, grids docked to the jump gate inside it's jump space will have constraints (landing gear, connectors, etc...) destroyed during jump<br />
			/// If true, grids docked will be ignored<br />
			/// Defaults to false
			/// </summary>
			[ProtoMember(19, IsRequired = true)]
			public bool IgnoreDockedGrids = false;

			/// <summary>
			/// The force (in Newtons) applied for every drive in a large grid jump gate away from the jump node when the gate is charging<br />
			/// Cannot be NaN or Infinite<br />
			/// Defaults to 1000
			/// </summary>
			[ProtoMember(20, IsRequired = true)]
			public double ChargingLargeDriveEffectorForceN = 1000;

			/// <summary>
			/// The force (in Newtons) applied for every drive in a large grid jump gate away from the jump node when the gate is charging<br />
			/// Cannot be NaN or Infinite<br />
			/// Defaults to 250
			/// </summary>
			[ProtoMember(21, IsRequired = true)]
			public double ChargingSmallDriveEffectorForceN = 250;

			/// <summary>
			/// The maximum number of total jumps that are allowed at a time<br />
			/// Cannot be less than 0<br />
			/// "0" will be treated as infinite<br />
			/// Defaults to 50 if server otherwise 0
			/// </summary>
			[ProtoMember(22, IsRequired = true)]
			public uint MaxConcurrentJumps = MyNetworkInterface.IsMultiplayerServer ? 50u : 0u;

			/// <summary>
			/// Whether ships jumping to an untethered destination have randomness applied to each ship or once per the entire fleet
			/// </summary>
			[ProtoMember(23, IsRequired = true)]
			public bool ConfineUntetheredSpread = false;

			/// <summary>
			/// Validates all values
			/// </summary>
			internal void Validate()
			{
				JumpGateConfigurationSchema defaults = new JumpGateConfigurationSchema();
				this.MinimumLargeJumpDistance = ValidateDoubleValue(this.MinimumLargeJumpDistance, defaults.MinimumLargeJumpDistance, 0, allow_inf: true);
				this.MinimumSmallJumpDistance = ValidateDoubleValue(this.MinimumSmallJumpDistance, defaults.MinimumSmallJumpDistance, 0, allow_inf: true);
				this.MaximumLargeJumpDistance = ValidateDoubleValue(this.MaximumLargeJumpDistance, defaults.MaximumLargeJumpDistance, 0, allow_inf: true);
				this.MaximumSmallJumpDistance = ValidateDoubleValue(this.MaximumSmallJumpDistance, defaults.MaximumSmallJumpDistance, 0, allow_inf: true);
				this.UntetheredLargeJumpEnergyCost = ValidateDoubleValue(this.UntetheredLargeJumpEnergyCost, defaults.UntetheredLargeJumpEnergyCost, 0, allow_inf: true);
				this.UntetheredSmallJumpEnergyCost = ValidateDoubleValue(this.UntetheredSmallJumpEnergyCost, defaults.UntetheredSmallJumpEnergyCost, 0, allow_inf: true);
				this.MaxLargeJumpGate50Distance = ValidateDoubleValue(this.MaxLargeJumpGate50Distance, defaults.MaxLargeJumpGate50Distance, 0);
				this.MaxSmallJumpGate50Distance = ValidateDoubleValue(this.MaxSmallJumpGate50Distance, defaults.MaxSmallJumpGate50Distance, 0);
				this.LargeGatePowerFactorExponent = ValidateDoubleValue(this.LargeGatePowerFactorExponent, defaults.LargeGatePowerFactorExponent, 0);
				this.SmallGatePowerFactorExponent = ValidateDoubleValue(this.SmallGatePowerFactorExponent, defaults.SmallGatePowerFactorExponent, 0);
				this.LargeGatePowerFalloffFactor = ValidateDoubleValue(this.LargeGatePowerFalloffFactor, defaults.LargeGatePowerFalloffFactor);
				this.SmallGatePowerFalloffFactor = ValidateDoubleValue(this.SmallGatePowerFalloffFactor, defaults.SmallGatePowerFalloffFactor);
				this.LargeGateKilowattPerKilogram = ValidateDoubleValue(this.LargeGateKilowattPerKilogram, defaults.LargeGateKilowattPerKilogram, 0);
				this.SmallGateKilowattPerKilogram = ValidateDoubleValue(this.SmallGateKilowattPerKilogram, defaults.SmallGateKilowattPerKilogram, 0);
				this.LargeGateRandomOffsetPerKilometer = ValidateDoubleValue(this.LargeGateRandomOffsetPerKilometer, defaults.LargeGateRandomOffsetPerKilometer, 0);
				this.SmallGateRandomOffsetPerKilometer = ValidateDoubleValue(this.SmallGateRandomOffsetPerKilometer, defaults.SmallGateRandomOffsetPerKilometer, 0);
				this.LargeGateKilometerOffsetPerUnitG = ValidateDoubleValue(this.LargeGateKilometerOffsetPerUnitG, defaults.LargeGateKilometerOffsetPerUnitG);
				this.SmallGateKilometerOffsetPerUnitG = ValidateDoubleValue(this.SmallGateKilometerOffsetPerUnitG, defaults.SmallGateKilometerOffsetPerUnitG);

				if (this.LargeGateKilometerOffsetPerUnitG == 5) this.LargeGateKilometerOffsetPerUnitG = 0.5;
				if (this.SmallGateKilometerOffsetPerUnitG == 10) this.LargeGateKilometerOffsetPerUnitG = 0.825;

				this.LargeGateDistanceScaleExponent = Math.Log((this.MaxLargeJumpGate50Distance - this.MinimumLargeJumpDistance) / 1000d, 50d);
				this.SmallGateDistanceScaleExponent = Math.Log((this.MaxSmallJumpGate50Distance - this.MinimumSmallJumpDistance) / 1000d, 50d);
			}
		}

		[ProtoContract(UseProtoMembersOnly = true)]
		public sealed class ConstructConfigurationSchema
		{
			/// <summary>
			/// If true, gates can only see other gates connected via antennas or part of the same construct<br />
			/// Defaults to true
			/// </summary>
			[ProtoMember(1, IsRequired = true)]
			public bool RequireGridCommLink = true;

			/// <summary>
			/// The maximum number of jump gates a single grid group can have<br />
			/// Cannot be less than 0<br />
			/// "0" will be treated as infinite<br />
			/// Defaults to 3 if server otherwise 0
			/// </summary>
			[ProtoMember(2, IsRequired = true)]
			public uint MaxTotalGatesPerConstruct = MyNetworkInterface.IsMultiplayerServer ? 3u : 0u;

			/// <summary>
			/// The maximum number of large grid jump gates a single grid group can have<br />
			/// Cannot be less than 0<br />
			/// "0" will be treated as infinite<br />
			/// Defaults to 0
			/// </summary>
			[ProtoMember(3, IsRequired = true)]
			public uint MaxLargeGatesPerConstruct = 0;

			/// <summary>
			/// The maximum number of small grid jump gates a single grid group can have<br />
			/// Cannot be less than 0<br />
			/// "0" will be treated as infinite<br />
			/// Defaults to 0
			/// </summary>
			[ProtoMember(4, IsRequired = true)]
			public uint MaxSmallGatesPerConstruct = 0;

			/// <summary>
			/// Validates all values
			/// </summary>
			internal void Validate() { }
		}

		[ProtoContract(UseProtoMembersOnly = true)]
		public sealed class GeneralConfigurationSchema
		{
			/// <summary>
			/// Sets the verbosity level of debug information in the log file<br />
			/// Accepts value between 0 and 5<br />
			/// Defaults to 3
			/// </summary>
			[ProtoMember(1, IsRequired = true)]
			public byte DebugLogVerbosity = 5;

			/// <summary>
			/// The maximum distance (in meters) in which clients will be shown debug draw and gate particles<br />
			/// Cannot be NaN or less than 500<br />
			/// Defaults to 5000 m
			/// </summary>
			[ProtoMember(2, IsRequired = true)]
			public double DrawSyncDistance = 5000d;

			/// <summary>
			/// If false, grids partially outside the jump sphere will be sheared<br />
			/// Defaults to true
			/// </summary>
			[ProtoMember(3, IsRequired = true)]
			public bool LenientJumps = true;

			/// <summary>
			/// If true, highlights blocks that will be sheared by the jump gate ellipsoid
			/// Defaults to true
			/// </summary>
			[ProtoMember(4, IsRequired = true)]
			public bool HighlightShearBlocks = true;

			/// <summary>
			/// If false, untethered jumps will have randomness applied to endpoint<br />
			/// SEE: GeneralConfiguration/RandomOffsetPerKilometer<br />
			/// Defaults to false
			/// </summary>
			[ProtoMember(5, IsRequired = true)]
			public bool SafeJumps = false;

			/// <summary>
			/// The number of concurrent threads to use for construct updates<br />
			/// Defaults to 3 if dedicated server, otherwise 1
			/// </summary>
			[ProtoMember(6, IsRequired = true)]
			public byte ConcurrentGridUpdateThreads = (byte) (MyNetworkInterface.IsDedicatedMultiplayerServer ? 3 : 1);

			/// <summary>
			/// If true, allows jump gates to pull power directly from the reactor if instantaneous power is insufficient<br />
			/// Defaults to true
			/// </summary>
			[ProtoMember(7, IsRequired = true)]
			public bool SyphonReactorPower = true;

			/// <summary>
			/// Validates all values
			/// </summary>
			internal void Validate()
			{
				GeneralConfigurationSchema defaults = new GeneralConfigurationSchema();
				this.DebugLogVerbosity =  MathHelper.Clamp(this.DebugLogVerbosity, (byte) 0, (byte) 5);
				this.DrawSyncDistance = ValidateDoubleValue(this.DrawSyncDistance, defaults.DrawSyncDistance, 500, allow_inf: true);
				this.ConcurrentGridUpdateThreads = (byte) MathHelper.Max(1, (int) this.ConcurrentGridUpdateThreads);
			}
		}
		#endregion

		#region Local Block Configurations
		public sealed class LocalCapacitorConfiguration
		{
			/// <summary>
			/// The maximum possible charge (in Mega-Watts) this capacitor can hold
			/// </summary>
			public readonly double MaxCapacitorChargeMW;

			/// <summary>
			/// The maximum possible charge rate (in Mega-Watts per second) this capacitor can charge at
			/// </summary>
			public readonly double MaxCapacitorChargeRateMW;

			/// <summary>
			/// The power transfer efficiency of this capacitor when charging
			/// </summary>
			public readonly double CapacitorChargeEfficiency;

			/// <summary>
			/// The power transfer efficiency of this capacitor when discharging
			/// </summary>
			public readonly double CapacitorDischargeEfficiency;

			internal LocalCapacitorConfiguration(MyCubeBlockBase block, CapacitorConfigurationSchema config)
			{
				if (block == null) throw new ArgumentException("The specified block is null");
				else if (block.IsLargeGrid)
				{
					this.MaxCapacitorChargeMW = config.MaxLargeCapacitorChargeMW;
					this.MaxCapacitorChargeRateMW = config.MaxLargeCapacitorChargeRateMW;
					this.CapacitorChargeEfficiency = config.LargeCapacitorChargeEfficiency;
					this.CapacitorDischargeEfficiency = config.LargeCapacitorDischargeEfficiency;
				}
				else
				{
					this.MaxCapacitorChargeMW = config.MaxSmallCapacitorChargeMW;
					this.MaxCapacitorChargeRateMW = config.MaxSmallCapacitorChargeRateMW;
					this.CapacitorChargeEfficiency = config.SmallCapacitorChargeEfficiency;
					this.CapacitorDischargeEfficiency = config.SmallCapacitorDischargeEfficiency;
				}
			}

			internal Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["MaxCapacitorChargeMW"] = this.MaxCapacitorChargeMW,
					["MaxCapacitorChargeRateMW"] = this.MaxCapacitorChargeRateMW,
					["CapacitorChargeEfficiency"] = this.CapacitorChargeEfficiency,
					["CapacitorDischargeEfficiency"] = this.CapacitorDischargeEfficiency,
				};
			}
		}

		public sealed class LocalDriveConfiguration
		{
			/// <summary>
			/// The maximum distance (in meters) a this drive can raycast to check gate intersections
			/// </summary>
			public readonly double DriveRaycastDistance;

			/// <summary>
			/// The maximum offset (in meters) in which rays are considered to be overlapping for this drive
			/// </summary>
			public readonly double DriveRaycastWidth;

			/// <summary>
			/// The maximum charge (in Mega-Watts) a this drive can store
			/// </summary>
			public readonly double MaxDriveChargeMW;

			/// <summary>
			/// The maximum charge rate (in Mega-Watts per second) a this drive can charge at
			/// </summary>
			public readonly double MaxDriveChargeRateMW;

			/// <summary>
			/// The base input wattage (in Mega-Watts) for this drive while idle
			/// </summary>
			public readonly double BaseIdleInputWattageMW;

			/// <summary>
			/// The base input wattage (in Mega-Watts) for this drive while active
			/// </summary>
			public readonly double BaseActiveInputWattageMW;

			/// <summary>
			/// The power transfer efficiency of this drive when charging
			/// </summary>
			public readonly double DriveChargeEfficiency;

			internal LocalDriveConfiguration(MyCubeBlockBase block, DriveConfigurationSchema config)
			{
				if (block == null) throw new ArgumentException("The specified block is null");
				else if (block.IsLargeGrid)
				{
					this.DriveRaycastDistance = config.LargeDriveRaycastDistance;
					this.DriveRaycastWidth = config.LargeDriveRaycastWidth;
					this.MaxDriveChargeMW = config.MaxLargeDriveChargeMW;
					this.MaxDriveChargeRateMW = config.MaxLargeDriveChargeRateMW;
					this.BaseIdleInputWattageMW = config.LargeDriveIdleInputWattageMW;
					this.BaseActiveInputWattageMW = config.LargeDriveActiveInputWattageMW;
					this.DriveChargeEfficiency = config.LargeDriveChargeEfficiency;
				}
				else
				{
					this.DriveRaycastDistance = config.SmallDriveRaycastDistance;
					this.DriveRaycastWidth = config.SmallDriveRaycastWidth;
					this.MaxDriveChargeMW = config.MaxSmallDriveChargeMW;
					this.MaxDriveChargeRateMW = config.MaxSmallDriveChargeRateMW;
					this.BaseIdleInputWattageMW = config.SmallDriveIdleInputWattageMW;
					this.BaseActiveInputWattageMW = config.SmallDriveActiveInputWattageMW;
					this.DriveChargeEfficiency = config.SmallDriveChargeEfficiency;
				}
			}

			internal Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["DriveRaycastDistance"] = this.DriveRaycastDistance,
					["DriveRaycastWidth"] = this.DriveRaycastWidth,
					["MaxDriveChargeMW"] = this.MaxDriveChargeMW,
					["MaxDriveChargeRateMW"] = this.MaxDriveChargeRateMW,
					["BaseIdleInputWattageMW"] = this.BaseIdleInputWattageMW,
					["BaseActiveInputWattageMW"] = this.BaseActiveInputWattageMW,
					["DriveChargeEfficiency"] = this.DriveChargeEfficiency,
				};
			}
		}

		public sealed class LocalJumpGateConfiguration
		{
			/// <summary>
			/// The minimum distance (in meters) below which a destination cannot be jumped to by this gate
			/// </summary>
			public readonly double MinimumJumpDistance;

			/// <summary>
			/// The maximum distance (in meters) above which a destination cannot be jumped to by this gate
			/// </summary>
			public readonly double MaximumJumpDistance;

			/// <summary>
			/// The factor by which to multiply the calculated power usage for untethered jumps for this gates
			/// </summary>
			public readonly double UntetheredJumpEnergyCost;

			/// <summary>
			/// The maximum jump gate reachable distance (in Kilo-Meters) for a 50 drive jump gate matching this gate's grid size
			/// </summary>
			public readonly double MaxJumpGate50Distance;

			/// <summary>
			/// The exponent 'b' used for power factor calculations: (a * x - a) ^ b + 1<br />
			/// Specifies how quickly the power factor changes given how close this gate is to its reasonable distance
			/// </summary>
			public readonly double GatePowerFactorExponent;

			/// <summary>
			/// The value 'a' used for power factor calculations for this jump gate: (a * x - a) ^ b + 1<br />
			/// Specifies the narrowness of the curve, higher values result in a narrower curve
			/// </summary>
			public readonly double GatePowerFalloffFactor;

			/// <summary>
			/// The power (in Kilo-Watts) required for this gate to jump one kilogram of mass
			/// </summary>
			public readonly double GateKilowattPerKilogram;

			/// <summary>
			/// The maximum distance (in Kilo-Meters) per kilometer an endpoint can be offset by if untethered for this gate
			/// </summary>
			public readonly double GateRandomOffsetPerKilometer;

			/// <summary>
			/// The maximum distance (in Kilo-Meters) per unit of gravity 'g' an endpoint will be offset by if untethered for this gate
			/// </summary>
			public readonly double GateKilometerOffsetPerUnitG;

			/// <summary>
			/// The exponent directly responsible for calculating the ratio between distance and gate size for this gate<br />
			/// This value is calculated using "MaxJumpGate50Distance"<br />
			/// For calculating maximum distance given gate size:<br />
			///  ... DISTANCE = ((JUMP_GATE_SIZE ^ GATE_SCALE_EXPONENT) * 1000) + MINIMUM_JUMP_DISTANCE<br />
			/// For calculating gate size given maximum distance:<br />
			///  ... JUMP_GATE_SIZE = CEIL(((DISTANCE - MINIMUM_JUMP_DISTANCE) / 1000) ^ (1 / GATE_SCALE_EXPONENT))
			/// </summary>
			public readonly double GateDistanceScaleExponent;

			/// <summary>
			/// If false, grids docked to this gate inside it's jump space will have constraints (landing gear, connectors, etc...) destroyed during jump<br />
			/// If true, grids docked will be ignored
			/// </summary>
			public readonly bool IgnoreDockedGrids;

			/// <summary>
			/// The force (in Newtons) applied for every drive in this gate away from the jump node when the gate is charging
			/// </summary>
			public readonly double ChargingDriveEffectorForceN;

			/// <summary>
			/// The maximum number of total jumps that are allowed at a time
			/// </summary>
			public readonly uint MaxConcurrentJumps;

			/// <summary>
			/// Whether ships jumping to an untethered destination have randomness applied to each ship or once per the entire fleet
			/// </summary>
			public readonly bool ConfineUntetheredSpread;

			internal LocalJumpGateConfiguration(MyJumpGate jump_gate, JumpGateConfigurationSchema config)
			{
				if (jump_gate == null) throw new ArgumentException("The specified jump gate is null");
				else if (jump_gate.IsLargeGrid())
				{
					this.MinimumJumpDistance = config.MinimumLargeJumpDistance;
					this.MaximumJumpDistance = config.MaximumLargeJumpDistance;
					this.UntetheredJumpEnergyCost = config.UntetheredLargeJumpEnergyCost;
					this.MaxJumpGate50Distance = config.MaxLargeJumpGate50Distance;
					this.GatePowerFactorExponent = config.LargeGatePowerFactorExponent;
					this.GatePowerFalloffFactor = config.LargeGatePowerFalloffFactor;
					this.GateKilowattPerKilogram = config.LargeGateKilowattPerKilogram;
					this.GateRandomOffsetPerKilometer = config.LargeGateRandomOffsetPerKilometer;
					this.GateKilometerOffsetPerUnitG = config.LargeGateKilometerOffsetPerUnitG;
					this.GateDistanceScaleExponent = config.LargeGateDistanceScaleExponent;
					this.ChargingDriveEffectorForceN = config.ChargingLargeDriveEffectorForceN;
				}
				else
				{
					this.MinimumJumpDistance = config.MinimumSmallJumpDistance;
					this.MaximumJumpDistance = config.MaximumSmallJumpDistance;
					this.UntetheredJumpEnergyCost = config.UntetheredSmallJumpEnergyCost;
					this.MaxJumpGate50Distance = config.MaxSmallJumpGate50Distance;
					this.GatePowerFactorExponent = config.SmallGatePowerFactorExponent;
					this.GatePowerFalloffFactor = config.SmallGatePowerFalloffFactor;
					this.GateKilowattPerKilogram = config.SmallGateKilowattPerKilogram;
					this.GateRandomOffsetPerKilometer = config.SmallGateRandomOffsetPerKilometer;
					this.GateKilometerOffsetPerUnitG = config.SmallGateKilometerOffsetPerUnitG;
					this.GateDistanceScaleExponent = config.SmallGateDistanceScaleExponent;
					this.ChargingDriveEffectorForceN = config.ChargingSmallDriveEffectorForceN;
				}

				this.IgnoreDockedGrids = config.IgnoreDockedGrids;
				this.MaxConcurrentJumps = config.MaxConcurrentJumps;
				this.ConfineUntetheredSpread = config.ConfineUntetheredSpread;
			}

			internal Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["MinimumJumpDistance"] = this.MinimumJumpDistance,
					["MaximumJumpDistance"] = this.MaximumJumpDistance,
					["UntetheredJumpEnergyCost"] = this.UntetheredJumpEnergyCost,
					["MaxJumpGate50Distance"] = this.MaxJumpGate50Distance,
					["GatePowerFactorExponent"] = this.GatePowerFactorExponent,
					["GatePowerFalloffFactor"] = this.GatePowerFalloffFactor,
					["GateKilowattPerKilogram"] = this.GateKilowattPerKilogram,
					["GateRandomOffsetPerKilometer"] = this.GateRandomOffsetPerKilometer,
					["GateKilometerOffsetPerUnitG"] = this.GateKilometerOffsetPerUnitG,
					["GateDistanceScaleExponent"] = this.GateDistanceScaleExponent,
					["IgnoreDockedGrids"] = this.IgnoreDockedGrids,
					["ChargingDriveEffectorForceN"] = this.ChargingDriveEffectorForceN,
					["MaxConcurrentJumps"] = this.MaxConcurrentJumps,
					["ConfineUntetheredSpread"] = this.ConfineUntetheredSpread,
				};
			}
		}
		#endregion

		#region Private Static Variables
		/// <summary>
		/// The name of the local configuration file
		/// </summary>
		private static string ConfigFileName => "Config.xml";

		/// <summary>
		/// The variable name used for MyAPIGateway.Utilities.GetVariable and MyAPIGateway.Utilities.SetVariable
		/// </summary>
		private static string ConfigVariableName => $"{MyJumpGateModSession.MODID}.ServerConfig";
		#endregion

		#region Public Variables
		/// <summary>
		/// The global capacitor configuration
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		public CapacitorConfigurationSchema CapacitorConfiguration = null;

		/// <summary>
		/// The global capacitor configuration
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		public DriveConfigurationSchema DriveConfiguration = null;

		/// <summary>
		/// The global jump gate configuration
		/// </summary>
		[ProtoMember(3, IsRequired = true)]
		public JumpGateConfigurationSchema JumpGateConfiguration = null;

		/// <summary>
		/// The global jump gate grid configuration
		/// </summary>
		[ProtoMember(4, IsRequired = true)]
		public ConstructConfigurationSchema ConstructConfiguration = null;

		/// <summary>
		/// The global session configuration
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public GeneralConfigurationSchema GeneralConfiguration = null;
		#endregion

		#region Private Static Methods
		/// <summary>
		/// Checks if the specified double value is valid
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <param name="default_">The default value if the provided value is invalid</param>
		/// <param name="min">The minimum allowed value or null if no minimum</param>
		/// <param name="max">The maximum allowed value or null if no maximum</param>
		/// <param name="allow_nan">Whether to allow a NaN value</param>
		/// <param name="allow_inf">Whether to allow an infinite value</param>
		/// <returns>The specified value, clamped within min and max if applicable, or the default if not valid</returns>
		private static double ValidateDoubleValue(double value, double default_, double? min = null, double? max = null, bool allow_nan = false, bool allow_inf = false)
		{
			if (!allow_nan && double.IsNaN(value) || !allow_inf && double.IsInfinity(value)) value = default_;
			if (min != null) value = Math.Max(min.Value, value);
			if (max != null) value = Math.Min(max.Value, value);
			return value;
		}
		#endregion

		#region Internal Static Methods
		/// <summary>
		/// </summary>
		/// <returns>The default configuration data</returns>
		internal static Configuration Defaults()
		{
			Configuration defaults = new Configuration {
				CapacitorConfiguration = new CapacitorConfigurationSchema(),
				DriveConfiguration = new DriveConfigurationSchema(),
				JumpGateConfiguration = new JumpGateConfigurationSchema(),
				ConstructConfiguration = new ConstructConfigurationSchema(),
				GeneralConfiguration = new GeneralConfigurationSchema(),
			};
			defaults.Validate();
			return defaults;
		}

		/// <summary>
		/// Loads configuration from file or server<br />
		/// If Server or Singleplayer: Loads configuration from file<br />
		/// If Client: Loads configuration from Sandbox file, sent from server on session load
		/// </summary>
		/// <returns>The loaded configuration data</returns>
		internal static Configuration Load()
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				string encoded;

				if (!MyAPIGateway.Utilities.GetVariable(ConfigVariableName, out encoded))
				{
					Logger.Error($"Failed to locate server config variable");
					return Defaults();
				}

				try
				{
					Configuration config = MyAPIGateway.Utilities.SerializeFromBinary<Configuration>(Convert.FromBase64String(encoded));
					config.Validate();
					Logger.Log("Mod-local server config loaded");
					return config;
				}
				catch (Exception e)
				{
					Logger.Error($"Failed to load mod-local config file\n\t...\n{e}");
					return Defaults();
				}
			}
			else
			{
				Configuration configuration = Defaults();

				if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(ConfigFileName, MyJumpGateModSession.Instance.GetType())) Logger.Error($"Failed to locate local config file");
				else
				{
					TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(ConfigFileName, MyJumpGateModSession.Instance.GetType());

					try
					{
						configuration = MyAPIGateway.Utilities.SerializeFromXML<Configuration>(reader.ReadToEnd());
						configuration.Validate();
						Logger.Log("Mod-local config loaded");
					}
					catch (Exception e)
					{
						Logger.Error($"Failed to load mod-local config file\n\t...\n{e}");
					}
					finally
					{
						reader.Close();
					}
				}

				MyAPIGateway.Utilities.SetVariable(ConfigVariableName, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(configuration)));
				return configuration;
			}
		}
		#endregion

		#region Constructors
		public Configuration() { }
		#endregion

		#region Internal Methods
		/// <summary>
		/// Validates all data, fixing any invalid values
		/// </summary>
		internal void Validate()
		{
			this.CapacitorConfiguration.Validate();
			this.DriveConfiguration.Validate();
			this.JumpGateConfiguration.Validate();
			this.ConstructConfiguration.Validate();
			this.GeneralConfiguration.Validate();
		}

		/// <summary>
		/// Saves the data to file<br />
		/// Does nothing if not server or not singleplayer
		/// </summary>
		internal void Save()
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(ConfigFileName, MyJumpGateModSession.Instance.GetType());

			try
			{
				writer.Write(MyAPIGateway.Utilities.SerializeToXML(this));
				Logger.Log("Mod-local config saved");
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to save mod-local config file\n\t...\n{e}");
			}
			finally
			{
				writer.Close();
			}
		}
		#endregion
	}
}
