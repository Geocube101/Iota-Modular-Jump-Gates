using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.ModConfiguration
{
	public sealed class MyModConfigurationV1
	{
		#region Section Schemas
		public interface IMYConfigurationSchema
		{
			void Validate();

			Dictionary<string, object> ToDictionary();
		}

		[Serializable]
		[XmlRoot("CapacitorConfiguration")]
		[ProtoContract(UseProtoMembersOnly = true)]
		public class MyModCapacitorConfiguration : IMYConfigurationSchema
		{
			#region Schema
			/// <summary>
			/// The maximum charge (in MegaWatts) a large grid capacitor can store<br />
			/// Cannot be NaN, Infinity, or less than 1<br />
			/// Defaults to 1000 MW
			/// </summary>
			[XmlElement]
			[ProtoMember(1)]
			public double? MaxLargeCapacitorChargeMW;

			/// <summary>
			/// The maximum charge (in MegaWatts) a small grid capacitor can store<br />
			/// Cannot be NaN, Infinity, or less than 1<br />
			/// Defaults to 200 MW
			/// </summary>
			[XmlElement]
			[ProtoMember(2)]
			public double? MaxSmallCapacitorChargeMW;

			/// <summary>
			/// The maximum charge rate (in MegaWatts per second) a large grid capacitor can charge at<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Should not be 0<br />
			/// Defaults to 25 MW/s
			/// </summary>
			[XmlElement]
			[ProtoMember(3)]
			public float? MaxLargeCapacitorChargeRateMW;

			/// <summary>
			/// The maximum charge rate (in MegaWatts per second) a small grid capacitor can charge at<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Should not be 0<br />
			/// Defaults to 5 MW/s
			/// </summary>
			[XmlElement]
			[ProtoMember(4)]
			public float? MaxSmallCapacitorChargeRateMW;

			/// <summary>
			/// The power transfer efficiency of large grid capacitors when charging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.95
			/// </summary>
			[XmlElement]
			[ProtoMember(5)]
			public float? LargeCapacitorChargeEfficiency;

			/// <summary>
			/// The power transfer efficiency of small grid capacitors when charging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.8
			/// </summary>
			[XmlElement]
			[ProtoMember(6)]
			public float? SmallCapacitorChargeEfficiency;

			/// <summary>
			/// The power transfer efficiency of large grid capacitors when discharging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.75
			/// </summary>
			[XmlElement]
			[ProtoMember(7)]
			public float? LargeCapacitorDischargeEfficiency;

			/// <summary>
			/// The power transfer efficiency of small grid capacitors when discharging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.5
			/// </summary>
			[XmlElement]
			[ProtoMember(8)]
			public float? SmallCapacitorDischargeEfficiency;
			#endregion

			#region Public Methods
			/// <returns>The default capacitor configuration values</returns>
			public static MyModCapacitorConfiguration Defaults()
			{
				return new MyModCapacitorConfiguration {
					MaxLargeCapacitorChargeMW = 1000d,
					MaxSmallCapacitorChargeMW = 200d,
					MaxLargeCapacitorChargeRateMW = 25f,
					MaxSmallCapacitorChargeRateMW = 5f,
					LargeCapacitorChargeEfficiency = 0.95f,
					SmallCapacitorChargeEfficiency = 0.8f,
					LargeCapacitorDischargeEfficiency = 0.75f,
					SmallCapacitorDischargeEfficiency = 0.5f,
				};
			}

			/// <summary>
			/// Loads this configuration from a dictionary
			/// </summary>
			/// <param name="map">The dictionary to read from</param>
			/// <returns>The configuration</returns>
			public static MyModCapacitorConfiguration FromDictionary(Dictionary<string, object> map)
			{
				return (map == null) ? null : new MyModCapacitorConfiguration() {
					MaxLargeCapacitorChargeMW = map.GetValueOrDefault("MaxLargeCapacitorChargeMW") as double?,
					MaxSmallCapacitorChargeMW = map.GetValueOrDefault("MaxSmallCapacitorChargeMW") as double?,
					MaxLargeCapacitorChargeRateMW = map.GetValueOrDefault("MaxLargeCapacitorChargeRateMW") as float?,
					MaxSmallCapacitorChargeRateMW = map.GetValueOrDefault("MaxSmallCapacitorChargeRateMW") as float?,
					LargeCapacitorChargeEfficiency = map.GetValueOrDefault("LargeCapacitorChargeEfficiency") as float?,
					SmallCapacitorChargeEfficiency = map.GetValueOrDefault("SmallCapacitorChargeEfficiency") as float?,
					LargeCapacitorDischargeEfficiency = map.GetValueOrDefault("LargeCapacitorDischargeEfficiency") as float?,
					SmallCapacitorDischargeEfficiency = map.GetValueOrDefault("SmallCapacitorDischargeEfficiency") as float?,
				};
			}

			/// <summary>
			/// Overlays a capacitor configuration on top of this one<br />
			/// Any null values in this configuration will be replaced by values from "config"
			/// </summary>
			/// <param name="config">The configuration to overlay</param>
			public void Overlay(MyModCapacitorConfiguration config)
			{
				if (config == null) return;
				this.MaxLargeCapacitorChargeMW = this.MaxLargeCapacitorChargeMW ?? config.MaxLargeCapacitorChargeMW;
				this.MaxSmallCapacitorChargeMW = this.MaxSmallCapacitorChargeMW ?? config.MaxSmallCapacitorChargeMW;
				this.MaxLargeCapacitorChargeRateMW = this.MaxLargeCapacitorChargeRateMW ?? config.MaxLargeCapacitorChargeRateMW;
				this.MaxSmallCapacitorChargeRateMW = this.MaxSmallCapacitorChargeRateMW ?? config.MaxSmallCapacitorChargeRateMW;
				this.LargeCapacitorChargeEfficiency = this.LargeCapacitorChargeEfficiency ?? config.LargeCapacitorChargeEfficiency;
				this.SmallCapacitorChargeEfficiency = this.SmallCapacitorChargeEfficiency ?? config.SmallCapacitorChargeEfficiency;
				this.LargeCapacitorDischargeEfficiency = this.LargeCapacitorDischargeEfficiency ?? config.LargeCapacitorDischargeEfficiency;
				this.SmallCapacitorDischargeEfficiency = this.SmallCapacitorDischargeEfficiency ?? config.SmallCapacitorDischargeEfficiency;
			}

			/// <summary>
			/// Validates this configuration<br />
			/// Any invalid values will be replaced with defaults
			/// </summary>
			public void Validate()
			{
				MyModCapacitorConfiguration defaults = MyModCapacitorConfiguration.Defaults();
				this.MaxLargeCapacitorChargeMW = MyModConfigurationV1.ValidateValue(this.MaxLargeCapacitorChargeMW, defaults.MaxLargeCapacitorChargeMW, 1);
				this.MaxSmallCapacitorChargeMW = MyModConfigurationV1.ValidateValue(this.MaxSmallCapacitorChargeMW, defaults.MaxSmallCapacitorChargeMW, 1);
				this.MaxLargeCapacitorChargeRateMW = MyModConfigurationV1.ValidateValue(this.MaxLargeCapacitorChargeRateMW, defaults.MaxLargeCapacitorChargeRateMW, 0);
				this.MaxSmallCapacitorChargeRateMW = MyModConfigurationV1.ValidateValue(this.MaxSmallCapacitorChargeRateMW, defaults.MaxSmallCapacitorChargeRateMW, 0);
				this.LargeCapacitorChargeEfficiency = MyModConfigurationV1.ValidateValue(this.LargeCapacitorChargeEfficiency, defaults.LargeCapacitorChargeEfficiency, 0, 1);
				this.SmallCapacitorChargeEfficiency = MyModConfigurationV1.ValidateValue(this.SmallCapacitorChargeEfficiency, defaults.SmallCapacitorChargeEfficiency, 0, 1);
				this.LargeCapacitorDischargeEfficiency = MyModConfigurationV1.ValidateValue(this.LargeCapacitorDischargeEfficiency, defaults.LargeCapacitorDischargeEfficiency, 0, 1);
				this.SmallCapacitorDischargeEfficiency = MyModConfigurationV1.ValidateValue(this.SmallCapacitorDischargeEfficiency, defaults.SmallCapacitorDischargeEfficiency, 0, 1);
			}

			/// <summary>
			/// Clones this configuration
			/// </summary>
			/// <returns>The cloned configuration</returns>
			public MyModCapacitorConfiguration Clone()
			{
				MyModCapacitorConfiguration clone = new MyModCapacitorConfiguration();
				clone.Overlay(this);
				return clone;
			}

			/// <summary>
			/// Saves this configuration to a dictionary
			/// </summary>
			/// <returns>The configuration as a dictionary</returns>
			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["MaxLargeCapacitorChargeMW"] = this.MaxLargeCapacitorChargeMW,
					["MaxSmallCapacitorChargeMW"] = this.MaxSmallCapacitorChargeMW,
					["MaxLargeCapacitorChargeRateMW"] = this.MaxLargeCapacitorChargeRateMW,
					["MaxSmallCapacitorChargeRateMW"] = this.MaxSmallCapacitorChargeRateMW,
					["LargeCapacitorChargeEfficiency"] = this.LargeCapacitorChargeEfficiency,
					["SmallCapacitorChargeEfficiency"] = this.SmallCapacitorChargeEfficiency,
					["LargeCapacitorDischargeEfficiency"] = this.LargeCapacitorDischargeEfficiency,
					["SmallCapacitorDischargeEfficiency"] = this.SmallCapacitorDischargeEfficiency,
				};
			}
			#endregion
		}

		[Serializable]
		[XmlRoot("DriveConfiguration")]
		[ProtoContract(UseProtoMembersOnly = true)]
		public class MyModDriveConfiguration : IMYConfigurationSchema
		{
			#region Schema
			/// <summary>
			/// The maximum distance (in meters) a large grid drive can raycast to check gate intersections<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 250 m
			/// </summary>
			[XmlElement]
			[ProtoMember(1)]
			public double? LargeDriveRaycastDistance;

			/// <summary>
			/// The maximum distance (in meters) a small grid drive can raycast to check gate intersections<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 50 m
			/// </summary>
			[XmlElement]
			[ProtoMember(2)]
			public double? SmallDriveRaycastDistance;

			/// <summary>
			/// The maximum offset (in meters) in which rays are considered to be overlapping for large drives<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 2.5 m
			/// </summary>
			[XmlElement]
			[ProtoMember(3)]
			public float? LargeDriveRaycastWidth;

			/// <summary>
			/// The maximum offset (in meters) in which rays are considered to be overlapping for small drives<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 0.5 m
			/// </summary>
			[XmlElement]
			[ProtoMember(4)]
			public float? SmallDriveRaycastWidth;

			/// <summary>
			/// The maximum charge (in MegaWatts) a large grid drive can store<br />
			/// Cannot be NaN, Infinity, or less than 1<br />
			/// Defaults to 100 MW
			/// </summary>
			[XmlElement]
			[ProtoMember(5)]
			public double? MaxLargeDriveChargeMW;

			/// <summary>
			/// The maximum charge (in MegaWatts) a small grid drive can store<br />
			/// Cannot be NaN, Infinity, or less than 1<br />
			/// Defaults to 25 MW
			/// </summary>
			[XmlElement]
			[ProtoMember(6)]
			public double? MaxSmallDriveChargeMW;

			/// <summary>
			/// The maximum charge rate (in MegaWatts per second) a large grid drive can charge at<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Should not be 0<br />
			/// Defaults to 10 MW/s
			/// </summary>
			[XmlElement]
			[ProtoMember(7)]
			public float? MaxLargeDriveChargeRateMW;

			/// <summary>
			/// The maximum charge rate (in MegaWatts per second) a small grid drive can charge at<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Should not be 0<br />
			/// Defaults to 2 MW/s
			/// </summary>
			[XmlElement]
			[ProtoMember(8)]
			public float? MaxSmallDriveChargeRateMW;

			/// <summary>
			/// The base input wattage (in MegaWatts) for large grid drives while idle
			/// Cannot be NaN or Infinity<br />
			/// Should not be 0<br />
			/// Defaults to 50<br />
			/// </summary>
			[XmlElement]
			[ProtoMember(9)]
			public float? LargeDriveIdleInputWattageMW;

			/// <summary>
			/// The base input wattage (in MegaWatts) for small grid drives while idle
			/// Cannot be NaN or Infinity<br />
			/// Should not be 0<br />
			/// Defaults to 15<br />
			/// </summary>
			[XmlElement]
			[ProtoMember(10)]
			public float? SmallDriveIdleInputWattageMW;

			/// <summary>
			/// The base input wattage (in MegaWatts) for large grid drives while active
			/// Cannot be NaN or Infinity<br />
			/// Should not be 0<br />
			/// Defaults to 75<br />
			/// </summary>
			[XmlElement]
			[ProtoMember(11)]
			public float? LargeDriveActiveInputWattageMW;

			/// <summary>
			/// The base input wattage (in MegaWatts) for small grid drives while active
			/// Cannot be NaN or Infinity<br />
			/// Should not be 0<br />
			/// Defaults to 45<br />
			/// </summary>
			[XmlElement]
			[ProtoMember(12)]
			public float? SmallDriveActiveInputWattageMW;

			/// <summary>
			/// The power transfer efficiency of large grid drives when charging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.99
			/// </summary>
			[XmlElement]
			[ProtoMember(13)]
			public float? LargeDriveChargeEfficiency;

			/// <summary>
			/// The power transfer efficiency of small grid drives when charging<br />
			/// Cannot be NaN, Infinity, negative, or higher than 1<br />
			/// Defaults to 0.95
			/// </summary>
			[XmlElement]
			[ProtoMember(14)]
			public float? SmallDriveChargeEfficiency;
			#endregion

			#region Public Methods
			/// <returns>The default drive configuration values</returns>
			public static MyModDriveConfiguration Defaults()
			{
				return new MyModDriveConfiguration {
					LargeDriveRaycastDistance = 250d,
					SmallDriveRaycastDistance = 50d,
					LargeDriveRaycastWidth = 2.5f,
					SmallDriveRaycastWidth = 0.5f,
					MaxLargeDriveChargeMW = 100d,
					MaxSmallDriveChargeMW = 25d,
					MaxLargeDriveChargeRateMW = 10f,
					MaxSmallDriveChargeRateMW = 2f,
					LargeDriveIdleInputWattageMW = 50f,
					SmallDriveIdleInputWattageMW = 15f,
					LargeDriveActiveInputWattageMW = 75f,
					SmallDriveActiveInputWattageMW = 45f,
					LargeDriveChargeEfficiency = 0.99f,
					SmallDriveChargeEfficiency = 0.95f,
				};
			}

			/// <summary>
			/// Loads this configuration from a dictionary
			/// </summary>
			/// <param name="map">The dictionary to read from</param>
			/// <returns>The configuration</returns>
			public static MyModDriveConfiguration FromDictionary(Dictionary<string, object> map)
			{
				return (map == null) ? null : new MyModDriveConfiguration() {
					LargeDriveRaycastDistance = map.GetValueOrDefault("LargeDriveRaycastDistance") as double?,
					SmallDriveRaycastDistance = map.GetValueOrDefault("SmallDriveRaycastDistance") as double?,
					LargeDriveRaycastWidth = map.GetValueOrDefault("LargeDriveRaycastWidth") as float?,
					SmallDriveRaycastWidth = map.GetValueOrDefault("SmallDriveRaycastWidth") as float?,
					MaxLargeDriveChargeMW = map.GetValueOrDefault("MaxLargeDriveChargeMW") as double?,
					MaxSmallDriveChargeMW = map.GetValueOrDefault("MaxSmallDriveChargeMW") as double?,
					MaxLargeDriveChargeRateMW = map.GetValueOrDefault("MaxLargeDriveChargeRateMW") as float?,
					MaxSmallDriveChargeRateMW = map.GetValueOrDefault("MaxSmallDriveChargeRateMW") as float?,
					LargeDriveIdleInputWattageMW = map.GetValueOrDefault("LargeDriveIdleInputWattageMW") as float?,
					SmallDriveIdleInputWattageMW = map.GetValueOrDefault("SmallDriveIdleInputWattageMW") as float?,
					LargeDriveActiveInputWattageMW = map.GetValueOrDefault("LargeDriveActiveInputWattageMW") as float?,
					SmallDriveActiveInputWattageMW = map.GetValueOrDefault("SmallDriveActiveInputWattageMW") as float?,
					LargeDriveChargeEfficiency = map.GetValueOrDefault("LargeDriveChargeEfficiency") as float?,
					SmallDriveChargeEfficiency = map.GetValueOrDefault("SmallDriveChargeEfficiency") as float?,
				};
			}

			/// <summary>
			/// Overlays a drive configuration on top of this one<br />
			/// Any null values in this configuration will be replaced by values from "config"
			/// </summary>
			/// <param name="config">The configuration to overlay</param>
			public void Overlay(MyModDriveConfiguration config)
			{
				if (config == null) return;
				this.LargeDriveRaycastDistance = this.LargeDriveRaycastDistance ?? config.LargeDriveRaycastDistance;
				this.SmallDriveRaycastDistance = this.SmallDriveRaycastDistance ?? config.SmallDriveRaycastDistance;
				this.LargeDriveRaycastWidth = this.LargeDriveRaycastWidth ?? config.LargeDriveRaycastWidth;
				this.SmallDriveRaycastWidth = this.SmallDriveRaycastWidth ?? config.SmallDriveRaycastWidth;
				this.MaxLargeDriveChargeMW = this.MaxLargeDriveChargeMW ?? config.MaxLargeDriveChargeMW;
				this.MaxSmallDriveChargeMW = this.MaxSmallDriveChargeMW ?? config.MaxSmallDriveChargeMW;
				this.MaxLargeDriveChargeRateMW = this.MaxLargeDriveChargeRateMW ?? config.MaxLargeDriveChargeRateMW;
				this.MaxSmallDriveChargeRateMW = this.MaxSmallDriveChargeRateMW ?? config.MaxSmallDriveChargeRateMW;
				this.LargeDriveIdleInputWattageMW = this.LargeDriveIdleInputWattageMW ?? config.LargeDriveIdleInputWattageMW;
				this.SmallDriveIdleInputWattageMW = this.SmallDriveIdleInputWattageMW ?? config.SmallDriveIdleInputWattageMW;
				this.LargeDriveActiveInputWattageMW = this.LargeDriveActiveInputWattageMW ?? config.LargeDriveActiveInputWattageMW;
				this.SmallDriveActiveInputWattageMW = this.SmallDriveActiveInputWattageMW ?? config.SmallDriveActiveInputWattageMW;
				this.LargeDriveChargeEfficiency = this.LargeDriveChargeEfficiency ?? config.LargeDriveChargeEfficiency;
				this.SmallDriveChargeEfficiency = this.SmallDriveChargeEfficiency ?? config.SmallDriveChargeEfficiency;
			}

			/// <summary>
			/// Validates this configuration<br />
			/// Any invalid values will be replaced with defaults
			/// </summary>
			public void Validate()
			{
				MyModDriveConfiguration defaults = MyModDriveConfiguration.Defaults();
				this.LargeDriveRaycastDistance = MyModConfigurationV1.ValidateValue(this.LargeDriveRaycastDistance, defaults.LargeDriveRaycastDistance, 0);
				this.SmallDriveRaycastDistance = MyModConfigurationV1.ValidateValue(this.SmallDriveRaycastDistance, defaults.SmallDriveRaycastDistance, 0);
				this.LargeDriveRaycastWidth = MyModConfigurationV1.ValidateValue(this.LargeDriveRaycastWidth, defaults.LargeDriveRaycastWidth, 0);
				this.SmallDriveRaycastWidth = MyModConfigurationV1.ValidateValue(this.SmallDriveRaycastWidth, defaults.SmallDriveRaycastWidth, 0);
				this.MaxLargeDriveChargeMW = MyModConfigurationV1.ValidateValue(this.MaxLargeDriveChargeMW, defaults.MaxLargeDriveChargeMW, 1);
				this.MaxSmallDriveChargeMW = MyModConfigurationV1.ValidateValue(this.MaxSmallDriveChargeMW, defaults.MaxSmallDriveChargeMW, 1);
				this.MaxLargeDriveChargeRateMW = MyModConfigurationV1.ValidateValue(this.MaxLargeDriveChargeRateMW, defaults.MaxLargeDriveChargeRateMW, 0);
				this.MaxSmallDriveChargeRateMW = MyModConfigurationV1.ValidateValue(this.MaxSmallDriveChargeRateMW, defaults.MaxSmallDriveChargeRateMW, 0);
				this.LargeDriveIdleInputWattageMW = MyModConfigurationV1.ValidateValue(this.LargeDriveIdleInputWattageMW, defaults.LargeDriveIdleInputWattageMW, 0);
				this.SmallDriveIdleInputWattageMW = MyModConfigurationV1.ValidateValue(this.SmallDriveIdleInputWattageMW, defaults.SmallDriveIdleInputWattageMW, 0);
				this.LargeDriveActiveInputWattageMW = MyModConfigurationV1.ValidateValue(this.LargeDriveActiveInputWattageMW, defaults.LargeDriveActiveInputWattageMW, 0);
				this.SmallDriveActiveInputWattageMW = MyModConfigurationV1.ValidateValue(this.SmallDriveActiveInputWattageMW, defaults.SmallDriveActiveInputWattageMW, 0);
				this.LargeDriveChargeEfficiency = MyModConfigurationV1.ValidateValue(this.LargeDriveChargeEfficiency, defaults.LargeDriveChargeEfficiency, 0, 1);
				this.SmallDriveChargeEfficiency = MyModConfigurationV1.ValidateValue(this.SmallDriveChargeEfficiency, defaults.SmallDriveChargeEfficiency, 0, 1);
			}

			/// <summary>
			/// Clones this configuration
			/// </summary>
			/// <returns>The cloned configuration</returns>
			public MyModDriveConfiguration Clone()
			{
				MyModDriveConfiguration clone = new MyModDriveConfiguration();
				clone.Overlay(this);
				return clone;
			}

			/// <summary>
			/// Saves this configuration to a dictionary
			/// </summary>
			/// <returns>The configuration as a dictionary</returns>
			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["LargeDriveRaycastDistance"] = this.LargeDriveRaycastDistance,
					["SmallDriveRaycastDistance"] = this.SmallDriveRaycastDistance,
					["LargeDriveRaycastWidth"] = this.LargeDriveRaycastWidth,
					["SmallDriveRaycastWidth"] = this.SmallDriveRaycastWidth,
					["MaxLargeDriveChargeMW"] = this.MaxLargeDriveChargeMW,
					["MaxSmallDriveChargeMW"] = this.MaxSmallDriveChargeMW,
					["MaxLargeDriveChargeRateMW"] = this.MaxLargeDriveChargeRateMW,
					["MaxSmallDriveChargeRateMW"] = this.MaxSmallDriveChargeRateMW,
					["LargeDriveIdleInputWattageMW"] = this.LargeDriveIdleInputWattageMW,
					["SmallDriveIdleInputWattageMW"] = this.SmallDriveIdleInputWattageMW,
					["LargeDriveActiveInputWattageMW"] = this.LargeDriveActiveInputWattageMW,
					["SmallDriveActiveInputWattageMW"] = this.SmallDriveActiveInputWattageMW,
					["LargeDriveChargeEfficiency"] = this.LargeDriveChargeEfficiency,
					["SmallDriveChargeEfficiency"] = this.SmallDriveChargeEfficiency,
				};
			}
			#endregion
		}

		[Serializable]
		[XmlRoot("JumpGateConfiguration")]
		[ProtoContract(UseProtoMembersOnly = true)]
		public class MyModJumpGateConfiguration : IMYConfigurationSchema
		{
			#region Sub-Schemas
			[Serializable]
			[XmlRoot("JumpGateExplosionConfiguration")]
			[ProtoContract(UseProtoMembersOnly = true)]
			public class MyModJumpGateExplosionConfiguration : IMYConfigurationSchema
			{
				#region Schema
				/// <summary>
				/// Whether jump gates explode when sufficiently damaged<br />
				/// Also controls whether the "Self Destruct" action is available<br />
				/// Defaults to false
				/// </summary>
				[XmlElement]
				[ProtoMember(1)]
				public bool? EnableGateExplosions;

				/// <summary>
				/// The multiplier used to determine a large grid jump gate's explosion size given gate power<br />
				/// Cannot be NaN, Infinite, or less than 0<br />
				/// Defaults to 1 (100%)
				/// </summary>
				[XmlElement]
				[ProtoMember(2)]
				public float? LargeGateExplosionDamageMultiplier;

				/// <summary>
				/// The multiplier used to determine a small grid jump gate's explosion size given gate power<br />
				/// Cannot be Nan, Infinite, or less than 0<br />
				/// Defaults to 1 (100%)
				/// </summary>
				[XmlElement]
				[ProtoMember(3)]
				public float? SmallGateExplosionDamageMultiplier;

				/// <summary>
				/// The percentage of large grid gate drives that must be disabled before the gate detonates<br />
				/// Cannot be NaN, Infinite, less than 0, or greater than 1<br />
				/// Defaults to 0.75 (75%)
				/// </summary>
				[XmlElement]
				[ProtoMember(4)]
				public float? LargeGateExplosionDamagePercent;

				/// <summary>
				/// The percentage of small grid gate drives that must be disabled before the gate detonates<br />
				/// Cannot be NaN, Infinite, less than 0, or greater than 1<br />
				/// Defaults to 0.75 (75%)
				/// </summary>
				[XmlElement]
				[ProtoMember(5)]
				public float? SmallGateExplosionDamagePercent;
				#endregion

				#region Public Methods
				/// <returns>The default jump gate explosion configuration values</returns>
				public static MyModJumpGateExplosionConfiguration Defaults()
				{
					return new MyModJumpGateExplosionConfiguration {
						EnableGateExplosions = false,
						LargeGateExplosionDamageMultiplier = 1f,
						SmallGateExplosionDamageMultiplier = 1f,
						LargeGateExplosionDamagePercent = 0.75f,
						SmallGateExplosionDamagePercent = 0.75f,
					};
				}

				/// <summary>
				/// Loads this configuration from a dictionary
				/// </summary>
				/// <param name="map">The dictionary to read from</param>
				/// <returns>The configuration</returns>
				public static MyModJumpGateExplosionConfiguration FromDictionary(Dictionary<string, object> map)
				{
					return (map == null) ? null : new MyModJumpGateExplosionConfiguration() {
						EnableGateExplosions = map.GetValueOrDefault("EnableGateExplosions") as bool?,
						LargeGateExplosionDamageMultiplier = map.GetValueOrDefault("LargeGateExplosionDamageMultiplier") as float?,
						SmallGateExplosionDamageMultiplier = map.GetValueOrDefault("SmallGateExplosionDamageMultiplier") as float?,
						LargeGateExplosionDamagePercent = map.GetValueOrDefault("LargeGateExplosionDamagePercent") as float?,
						SmallGateExplosionDamagePercent = map.GetValueOrDefault("SmallGateExplosionDamagePercent") as float?,
					};
				}

				/// <summary>
				/// Overlays a jump gate explision configuration on top of this one<br />
				/// Any null values in this configuration will be replaced by values from "config"
				/// </summary>
				/// <param name="config">The configuration to overlay</param>
				public void Overlay(MyModJumpGateExplosionConfiguration config)
				{
					if (config == null) return;
					this.EnableGateExplosions = this.EnableGateExplosions ?? config.EnableGateExplosions;
					this.LargeGateExplosionDamageMultiplier = this.LargeGateExplosionDamageMultiplier ?? config.LargeGateExplosionDamageMultiplier;
					this.SmallGateExplosionDamageMultiplier = this.SmallGateExplosionDamageMultiplier ?? config.SmallGateExplosionDamageMultiplier;
					this.LargeGateExplosionDamagePercent = this.LargeGateExplosionDamagePercent ?? config.LargeGateExplosionDamagePercent;
					this.SmallGateExplosionDamagePercent = this.SmallGateExplosionDamagePercent ?? config.SmallGateExplosionDamagePercent;
				}

				/// <summary>
				/// Validates this configuration<br />
				/// Any invalid values will be replaced with defaults
				/// </summary>
				public void Validate()
				{
					MyModJumpGateExplosionConfiguration defaults = MyModJumpGateExplosionConfiguration.Defaults();
					this.EnableGateExplosions = this.EnableGateExplosions ?? defaults.EnableGateExplosions ?? false;
					this.LargeGateExplosionDamageMultiplier = MyModConfigurationV1.ValidateValue(this.LargeGateExplosionDamageMultiplier, defaults.LargeGateExplosionDamageMultiplier, 0);
					this.SmallGateExplosionDamageMultiplier = MyModConfigurationV1.ValidateValue(this.SmallGateExplosionDamageMultiplier, defaults.SmallGateExplosionDamageMultiplier, 0);
					this.LargeGateExplosionDamagePercent = MyModConfigurationV1.ValidateValue(this.LargeGateExplosionDamagePercent, defaults.LargeGateExplosionDamagePercent, 0, 1);
					this.SmallGateExplosionDamagePercent = MyModConfigurationV1.ValidateValue(this.SmallGateExplosionDamagePercent, defaults.SmallGateExplosionDamagePercent, 0, 1);
				}

				/// <summary>
				/// Clones this configuration
				/// </summary>
				/// <returns>The cloned configuration</returns>
				public MyModJumpGateExplosionConfiguration Clone()
				{
					MyModJumpGateExplosionConfiguration clone = new MyModJumpGateExplosionConfiguration();
					clone.Overlay(this);
					return clone;
				}

				/// <summary>
				/// Saves this configuration to a dictionary
				/// </summary>
				/// <returns>The configuration as a dictionary</returns>
				public Dictionary<string, object> ToDictionary()
				{
					return new Dictionary<string, object> {
						["EnableGateExplosions"] = this.EnableGateExplosions,
						["LargeGateExplosionDamageMultiplier"] = this.LargeGateExplosionDamageMultiplier,
						["SmallGateExplosionDamageMultiplier"] = this.SmallGateExplosionDamageMultiplier,
						["LargeGateExplosionDamagePercent"] = this.LargeGateExplosionDamagePercent,
						["SmallGateExplosionDamagePercent"] = this.SmallGateExplosionDamagePercent,
					};
				}
				#endregion
			}

			[Serializable]
			[XmlRoot("JumpGateWormholeConfiguration")]
			[ProtoContract(UseProtoMembersOnly = true)]
			public class MyModJumpGateWormholeConfiguration
			{
				#region Schema
				/// <summary>
				/// Whether to allow large grid jump gates to be activated as wormholes for sustained jumps<br />
				/// Defaults to true
				/// </summary>
				[XmlElement]
				[ProtoMember(1)]
				public bool? AllowLargeGateWormholeActivation;

				/// <summary>
				/// Whether to allow small grid jump gates to be activated as wormholes for sustained jumps<br />
				/// Defaults to true
				/// </summary>
				[XmlElement]
				[ProtoMember(2)]
				public bool? AllowSmallGateWormholeActivation;

				/// <summary>
				/// The power multiplier a large grid jump gate applies when opening a sustained wormhole<br />
				/// Cannot be NaN, Infinite, or less than 0<br />
				/// Defaults to 1.5 (150%)
				/// </summary>
				[XmlElement]
				[ProtoMember(3)]
				public float? LargeGateWormholePowerMultiplier;

				/// <summary>
				/// The power multiplier a small grid jump gate applies when opening a sustained wormhole<br />
				/// Cannot be NaN, Infinite, or less than 0<br />
				/// Defaults to 2 (200%)
				/// </summary>
				[XmlElement]
				[ProtoMember(4)]
				public float? SmallGateWormholePowerMultiplier;

				/// <summary>
				/// The maximum amount of time a large grid jump gate can remain open as a wormhole for sustained jumps, in seconds<br />
				/// Cannot be NaN or less than 0<br />
				/// Defaults to 38 minutes (2280 seconds)
				/// </summary>
				[XmlElement]
				[ProtoMember(5)]
				public float? MaxLargeGateWormholeDurationSeconds;

				/// <summary>
				/// The maximum amount of time a small grid jump gate can remain open as a wormhole for sustained jumps, in seconds<br />
				/// Cannot be NaN or less than 0<br />
				/// Defaults to 38 minutes (2280 seconds)
				/// </summary>
				[XmlElement]
				[ProtoMember(6)]
				public float? MaxSmallGateWormholeDurationSeconds;

				/// <summary>
				/// The multiplier to apply to jump gates for every second above the time limit a gate has been open as a wormhole for sustained jumps<br />
				/// Set to 0 to disable<br />
				/// Cannot be NaN, Infinite or less than 0<br />
				/// Defaults to 0
				/// </summary>
				[XmlElement]
				[ProtoMember(7)]
				public float? OverrunPowerMultiplierPerSecond;

				/// <summary>
				/// The percent of gravity from the connected jump gate to apply to this jump gate when using sustained jumps<br />
				/// Cannot be NaN, Infinite or less than 0<br />
				/// Defaults to 0
				/// </summary>
				[XmlElement]
				[ProtoMember(8)]
				public float? GravityPassthroughMultiplier;

				/// <summary>
				/// Whether to allow large grid jump gate wormholes to "jump" when fed enough energy in the form of an explosion<br />
				/// This mimics the ability for stargates to jump<br />
				/// Defaults to false
				/// </summary>
				[XmlElement]
				[ProtoMember(9)]
				public bool? AllowLargeWormholeStargateJumps;

				/// <summary>
				/// Whether to allow small grid jump gate wormholes to "jump" when fed enough energy in the form of an explosion<br />
				/// This mimics the ability for stargates to jump<br />
				/// Defaults to false
				/// </summary>
				[XmlElement]
				[ProtoMember(10)]
				public bool? AllowSmallWormholeStargateJumps;

				/// <summary>
				/// Whether wormholes can jump to gates outside of owner gate's faction<br />
				/// Defaults to false
				/// </summary>
				[XmlElement]
				[ProtoMember(11)]
				public bool? AllowWormholeJumpsOutsideFaction;

				/// <summary>
				/// Whether wormholes can jump to standard, non-wormhole jump gates<br />
				/// Defaults to false
				/// </summary>
				[XmlElement]
				[ProtoMember(12)]
				public bool? AllowWormholeJumpsToStandardGate;

				/// <summary>
				/// The amount of meters a large grid jump gate wormhole can jump per unit of explosion power when "stargate jumping" is enabled<br />
				/// Cannot be NaN or less than 0<br />
				/// Defaults to 0.01
				/// </summary>
				[XmlElement]
				[ProtoMember(13)]
				public float? LargeGateJumpDistancePerExplosionPower;

				/// <summary>
				/// The amount of meters a small grid jump gate wormhole can jump per unit of explosion power when "stargate jumping" is enabled<br />
				/// Cannot be NaN or less than 0<br />
				/// Defaults to 0.005
				/// </summary>
				[XmlElement]
				[ProtoMember(14)]
				public float? SmallGateJumpDistancePerExplosionPower;
				#endregion

				#region Public Methods
				/// <returns>The default jump gate explosion configuration values</returns>
				public static MyModJumpGateWormholeConfiguration Defaults()
				{
					return new MyModJumpGateWormholeConfiguration {
						AllowLargeGateWormholeActivation = false,
						AllowSmallGateWormholeActivation = false,
						LargeGateWormholePowerMultiplier = 1.5f,
						SmallGateWormholePowerMultiplier = 2f,
						MaxLargeGateWormholeDurationSeconds = 2280f,
						MaxSmallGateWormholeDurationSeconds = 2280f,
						OverrunPowerMultiplierPerSecond = 0f,
						GravityPassthroughMultiplier = 0f,
						AllowLargeWormholeStargateJumps = false,
						AllowSmallWormholeStargateJumps = false,
						AllowWormholeJumpsOutsideFaction = false,
						AllowWormholeJumpsToStandardGate = false,
						LargeGateJumpDistancePerExplosionPower = 0.01f,
						SmallGateJumpDistancePerExplosionPower = 0.005f,
					};
				}

				/// <summary>
				/// Loads this configuration from a dictionary
				/// </summary>
				/// <param name="map">The dictionary to read from</param>
				/// <returns>The configuration</returns>
				public static MyModJumpGateWormholeConfiguration FromDictionary(Dictionary<string, object> map)
				{
					return (map == null) ? null : new MyModJumpGateWormholeConfiguration() {
						AllowLargeGateWormholeActivation = map.GetValueOrDefault("AllowLargeGateWormholeActivation") as bool?,
						AllowSmallGateWormholeActivation = map.GetValueOrDefault("AllowSmallGateWormholeActivation") as bool?,
						LargeGateWormholePowerMultiplier = map.GetValueOrDefault("LargeGateWormholePowerMultiplier") as float?,
						SmallGateWormholePowerMultiplier = map.GetValueOrDefault("SmallGateWormholePowerMultiplier") as float?,
						MaxLargeGateWormholeDurationSeconds = map.GetValueOrDefault("MaxLargeGateWormholeDurationSeconds") as float?,
						MaxSmallGateWormholeDurationSeconds = map.GetValueOrDefault("MaxSmallGateWormholeDurationSeconds") as float?,
						OverrunPowerMultiplierPerSecond = map.GetValueOrDefault("OverrunPowerMultiplierPerSecond") as float?,
						GravityPassthroughMultiplier = map.GetValueOrDefault("GravityPassthroughMultiplier") as float?,
						AllowLargeWormholeStargateJumps = map.GetValueOrDefault("AllowLargeWormholeStargateJumps") as bool?,
						AllowSmallWormholeStargateJumps = map.GetValueOrDefault("AllowSmallWormholeStargateJumps") as bool?,
						AllowWormholeJumpsOutsideFaction = map.GetValueOrDefault("AllowWormholeJumpsOutsideFaction") as bool?,
						AllowWormholeJumpsToStandardGate = map.GetValueOrDefault("AllowWormholeJumpsToStandardGate") as bool?,
						LargeGateJumpDistancePerExplosionPower = map.GetValueOrDefault("LargeGateJumpDistancePerExplosionPower") as float?,
						SmallGateJumpDistancePerExplosionPower = map.GetValueOrDefault("SmallGateJumpDistancePerExplosionPower") as float?,
					};
				}

				/// <summary>
				/// Overlays a jump gate explision configuration on top of this one<br />
				/// Any null values in this configuration will be replaced by values from "config"
				/// </summary>
				/// <param name="config">The configuration to overlay</param>
				public void Overlay(MyModJumpGateWormholeConfiguration config)
				{
					if (config == null) return;
					this.AllowLargeGateWormholeActivation = this.AllowLargeGateWormholeActivation ?? config.AllowLargeGateWormholeActivation;
					this.AllowSmallGateWormholeActivation = this.AllowSmallGateWormholeActivation ?? config.AllowSmallGateWormholeActivation;
					this.LargeGateWormholePowerMultiplier = this.LargeGateWormholePowerMultiplier ?? config.LargeGateWormholePowerMultiplier;
					this.SmallGateWormholePowerMultiplier = this.SmallGateWormholePowerMultiplier ?? config.SmallGateWormholePowerMultiplier;
					this.MaxLargeGateWormholeDurationSeconds = this.MaxLargeGateWormholeDurationSeconds ?? config.MaxLargeGateWormholeDurationSeconds;
					this.MaxSmallGateWormholeDurationSeconds = this.MaxSmallGateWormholeDurationSeconds ?? config.MaxSmallGateWormholeDurationSeconds;
					this.OverrunPowerMultiplierPerSecond = this.OverrunPowerMultiplierPerSecond ?? config.OverrunPowerMultiplierPerSecond;
					this.GravityPassthroughMultiplier = this.GravityPassthroughMultiplier ?? config.GravityPassthroughMultiplier;
					this.AllowLargeWormholeStargateJumps = this.AllowLargeWormholeStargateJumps ?? config.AllowLargeWormholeStargateJumps;
					this.AllowSmallWormholeStargateJumps = this.AllowSmallWormholeStargateJumps ?? config.AllowSmallWormholeStargateJumps;
					this.AllowWormholeJumpsOutsideFaction = this.AllowWormholeJumpsOutsideFaction ?? config.AllowWormholeJumpsOutsideFaction;
					this.AllowWormholeJumpsToStandardGate = this.AllowWormholeJumpsToStandardGate ?? config.AllowWormholeJumpsToStandardGate;
					this.LargeGateJumpDistancePerExplosionPower = this.LargeGateJumpDistancePerExplosionPower ?? config.LargeGateJumpDistancePerExplosionPower;
					this.SmallGateJumpDistancePerExplosionPower = this.SmallGateJumpDistancePerExplosionPower ?? config.SmallGateJumpDistancePerExplosionPower;
				}

				/// <summary>
				/// Validates this configuration<br />
				/// Any invalid values will be replaced with defaults
				/// </summary>
				public void Validate()
				{
					MyModJumpGateWormholeConfiguration defaults = MyModJumpGateWormholeConfiguration.Defaults();
					this.AllowLargeGateWormholeActivation = this.AllowLargeGateWormholeActivation ?? defaults.AllowLargeGateWormholeActivation ?? false;
					this.AllowSmallGateWormholeActivation = this.AllowSmallGateWormholeActivation ?? defaults.AllowSmallGateWormholeActivation ?? false;
					this.LargeGateWormholePowerMultiplier = MyModConfigurationV1.ValidateValue(this.LargeGateWormholePowerMultiplier, defaults.LargeGateWormholePowerMultiplier, 0);
					this.SmallGateWormholePowerMultiplier = MyModConfigurationV1.ValidateValue(this.SmallGateWormholePowerMultiplier, defaults.SmallGateWormholePowerMultiplier, 0);
					this.MaxLargeGateWormholeDurationSeconds = MyModConfigurationV1.ValidateValue(this.MaxLargeGateWormholeDurationSeconds, defaults.MaxLargeGateWormholeDurationSeconds, 0, allow_inf: true);
					this.MaxSmallGateWormholeDurationSeconds = MyModConfigurationV1.ValidateValue(this.MaxSmallGateWormholeDurationSeconds, defaults.MaxSmallGateWormholeDurationSeconds, 0, allow_inf: true);
					this.OverrunPowerMultiplierPerSecond = MyModConfigurationV1.ValidateValue(this.OverrunPowerMultiplierPerSecond, defaults.OverrunPowerMultiplierPerSecond, 0);
					this.GravityPassthroughMultiplier = MyModConfigurationV1.ValidateValue(this.GravityPassthroughMultiplier, defaults.GravityPassthroughMultiplier, 0);
					this.AllowLargeWormholeStargateJumps = this.AllowLargeWormholeStargateJumps ?? defaults.AllowLargeWormholeStargateJumps ?? false;
					this.AllowSmallWormholeStargateJumps = this.AllowSmallWormholeStargateJumps ?? defaults.AllowSmallWormholeStargateJumps ?? false;
					this.AllowWormholeJumpsOutsideFaction = this.AllowWormholeJumpsOutsideFaction ?? defaults.AllowWormholeJumpsOutsideFaction ?? false;
					this.AllowWormholeJumpsToStandardGate = this.AllowWormholeJumpsToStandardGate ?? defaults.AllowWormholeJumpsToStandardGate ?? false;
					this.LargeGateJumpDistancePerExplosionPower = MyModConfigurationV1.ValidateValue(this.LargeGateJumpDistancePerExplosionPower, defaults.LargeGateJumpDistancePerExplosionPower, 0, allow_inf: true);
					this.SmallGateJumpDistancePerExplosionPower = MyModConfigurationV1.ValidateValue(this.SmallGateJumpDistancePerExplosionPower, defaults.SmallGateJumpDistancePerExplosionPower, 0, allow_inf: true);
				}

				/// <summary>
				/// Clones this configuration
				/// </summary>
				/// <returns>The cloned configuration</returns>
				public MyModJumpGateWormholeConfiguration Clone()
				{
					MyModJumpGateWormholeConfiguration clone = new MyModJumpGateWormholeConfiguration();
					clone.Overlay(this);
					return clone;
				}

				/// <summary>
				/// Saves this configuration to a dictionary
				/// </summary>
				/// <returns>The configuration as a dictionary</returns>
				public Dictionary<string, object> ToDictionary()
				{
					return new Dictionary<string, object> {
						["AllowLargeGateWormholeActivation"] = this.AllowLargeGateWormholeActivation,
						["AllowSmallGateWormholeActivation"] = this.AllowSmallGateWormholeActivation,
						["LargeGateWormholePowerMultiplier"] = this.LargeGateWormholePowerMultiplier,
						["SmallGateWormholePowerMultiplier"] = this.SmallGateWormholePowerMultiplier,
						["MaxLargeGateWormholeDurationSeconds"] = this.MaxLargeGateWormholeDurationSeconds,
						["MaxSmallGateWormholeDurationSeconds"] = this.MaxSmallGateWormholeDurationSeconds,
						["OverrunPowerMultiplierPerSecond"] = this.OverrunPowerMultiplierPerSecond,
						["GravityPassthroughMultiplier"] = this.GravityPassthroughMultiplier,
						["AllowLargeWormholeStargateJumps"] = this.AllowLargeWormholeStargateJumps,
						["AllowSmallWormholeStargateJumps"] = this.AllowSmallWormholeStargateJumps,
						["AllowWormholeJumpsOutsideFaction"] = this.AllowWormholeJumpsOutsideFaction,
						["AllowWormholeJumpsToStandardGate"] = this.AllowWormholeJumpsToStandardGate,
						["LargeGateJumpDistancePerExplosionPower"] = this.LargeGateJumpDistancePerExplosionPower,
						["SmallGateJumpDistancePerExplosionPower"] = this.SmallGateJumpDistancePerExplosionPower,
					};
				}
				#endregion
			}
			#endregion

			#region Schema
			/// <summary>
			/// The minimum distance (in meters) below which a destination cannot be jumped to by a large grid gate<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be larger than "MaximumLargeJumpDistance"<br />
			/// Defaults to 5000 m
			/// </summary>
			[XmlElement]
			[ProtoMember(1)]
			public double? MinimumLargeJumpDistance;

			/// <summary>
			/// The minimum distance (in meters) below which a destination cannot be jumped to by a small grid gate<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be larger than "MaximumSmallJumpDistance"<br />
			/// Defaults to 2500 m
			/// </summary>
			[XmlElement]
			[ProtoMember(2)]
			public double? MinimumSmallJumpDistance;

			/// <summary>
			/// The maximum distance (in meters) above which a destination cannot be jumped to by a large grid gate<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be smaller than "MinimumLargeJumpDistance"<br />
			/// Defaults to INFINITE m
			/// </summary>
			[XmlElement]
			[ProtoMember(3)]
			public double? MaximumLargeJumpDistance;

			/// <summary>
			/// The maximum distance (in meters) above which a destination cannot be jumped to by a small grid gate<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be smaller than "MinimumSmallJumpDistance"<br />
			/// Defaults to INFINITE m
			/// </summary>
			[XmlElement]
			[ProtoMember(4)]
			public double? MaximumSmallJumpDistance;

			/// <summary>
			/// The factor by which to multiply the calculated power usage for untethered jumps for large grid gates<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be Infinity<br />
			/// Defaults to 1.05 (An extra 5%)
			/// </summary>
			[XmlElement]
			[ProtoMember(5)]
			public float? UntetheredLargeJumpEnergyCost;

			/// <summary>
			/// The factor by which to multiply the calculated power usage for untethered jumps for small grid gates<br />
			/// Cannot be NaN or less than 0<br />
			/// Should not be Infinity<br />
			/// Defaults to 1.10 (An extra 10%)
			/// </summary>
			[XmlElement]
			[ProtoMember(6)]
			public float? UntetheredSmallJumpEnergyCost;

			/// <summary>
			/// The exponent directly responsible for calculating the ratio between distance and gate size for large grid gates<br />
			/// This value is calculated using "MaxJumpGate50Distance"<br />
			/// For calculating maximum distance given gate size:<br />
			///  ... DISTANCE = ((JUMP_GATE_SIZE ^ GATE_SCALE_EXPONENT) * 1000) + MINIMUM_JUMP_DISTANCE<br />
			/// For calculating gate size given maximum distance:<br />
			///  ... JUMP_GATE_SIZE = CEIL(((DISTANCE - MINIMUM_JUMP_DISTANCE) / 1000) ^ (1 / GATE_SCALE_EXPONENT))
			/// </summary>
			[XmlIgnore]
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
			[XmlIgnore]
			[ProtoIgnore]
			public double SmallGateDistanceScaleExponent;

			/// <summary>
			/// The maximum jump gate reachable distance (in meters) for a 50 drive large grid jump gate<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 1 Tm
			/// </summary>
			[XmlElement]
			[ProtoMember(7)]
			public double? MaxLargeJumpGate50Distance;

			/// <summary>
			/// The maximum jump gate reachable distance (in meters) for a 50 drive large grid jump gate<br />
			/// Cannot be NaN, Infinity, or less than 0<br />
			/// Defaults to 1 Gm
			/// </summary>
			[XmlElement]
			[ProtoMember(8)]
			public double? MaxSmallJumpGate50Distance;

			/// <summary>
			/// The exponent 'b' used for power factor calculations: (a * x - a) ^ b + 1<br />
			/// Specifies how quickly the power factor changes given how close a large grid gate is to its reasonable distance<br />
			/// Cannot be NaN, Infinity or less than 0<br />
			/// Should not be less than 1<br />
			/// Defaults to 3
			/// </summary>
			[XmlElement]
			[ProtoMember(9)]
			public float? LargeGatePowerFactorExponent;

			/// <summary>
			/// The exponent 'b' used for power factor calculations: (a * x - a) ^ b + 1<br />
			/// Specifies how quickly the power factor changes given how close a small grid gate is to its reasonable distance<br />
			/// Cannot be NaN, Infinity or less than 0<br />
			/// Should not be less than 1<br />
			/// Defaults to 3
			/// </summary>
			[XmlElement]
			[ProtoMember(10)]
			public float? SmallGatePowerFactorExponent;

			/// <summary>
			/// The value 'a' used for power factor calculations on large grid jump gates: (a * x - a) ^ b + 1<br />
			/// Specifies the narrowness of the curve, higher values result in a narrower curve<br />
			/// Cannot be NaN or Infinity<br />
			/// Defaults to 1
			/// </summary>
			[XmlElement]
			[ProtoMember(11)]
			public float? LargeGatePowerFalloffFactor;

			/// <summary>
			/// The value 'a' used for power factor calculations on small grid jump gates: (a * x - a) ^ b + 1<br />
			/// Specifies the narrowness of the curve, higher values result in a narrower curve<br />
			/// Cannot be NaN or Infinity<br />
			/// Defaults to 1
			/// </summary>
			[XmlElement]
			[ProtoMember(12)]
			public float? SmallGatePowerFalloffFactor;

			/// <summary>
			/// The power (in KiloWatts) required for a large grid gate to jump one kilogram of mass<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0.025 Kw/Kg
			/// </summary>
			[XmlElement]
			[ProtoMember(13)]
			public float? LargeGateKilowattPerKilogram;

			/// <summary>
			/// The power (in KiloWatts) required for a small grid gate to jump one kilogram of mass<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0.075 Kw/Kg
			/// </summary>
			[XmlElement]
			[ProtoMember(14)]
			public float? SmallGateKilowattPerKilogram;

			/// <summary>
			/// The maximum distance (in kilometers) per kilometer an endpoint can be offset by if untethered for large grid gates<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0.001 Km
			/// </summary>
			[XmlElement]
			[ProtoMember(15)]
			public float? LargeGateRandomOffsetPerKilometer;

			/// <summary>
			/// The maximum distance (in kilometers) per kilometer an endpoint can be offset by if untethered for small grid gates<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0.005 Km
			/// </summary>
			[XmlElement]
			[ProtoMember(16)]
			public float? SmallGateRandomOffsetPerKilometer;

			/// <summary>
			/// The maximum distance (in KiloMeters) per unit of gravity 'g' an endpoint will be offset by if untethered for large grid gates<br />
			/// Cannot be NaN or Infinite<br />
			/// Defaults to 0.5 Km
			/// </summary>
			[XmlElement]
			[ProtoMember(17)]
			public float? LargeGateKilometerOffsetPerUnitG;

			/// <summary>
			/// The maximum distance (in KiloMeters) per unit of gravity 'g' an endpoint will be offset by if untethered for small grid gates<br />
			/// Cannot be NaN or Infinite<br />
			/// Defaults to 0.825 Km
			/// </summary>
			[XmlElement]
			[ProtoMember(18)]
			public float? SmallGateKilometerOffsetPerUnitG;

			/// <summary>
			/// If false, grids docked to the jump gate inside it's jump space will have constraints (landing gear, connectors, etc...) destroyed during jump<br />
			/// If true, grids docked will be ignored<br />
			/// Defaults to false
			/// </summary>
			[XmlElement]
			[ProtoMember(19)]
			public bool? IgnoreDockedGrids;

			/// <summary>
			/// The force (in Newtons) applied for every drive in a large grid jump gate away from the jump node when the gate is charging<br />
			/// Cannot be NaN or Infinite<br />
			/// Defaults to 1000
			/// </summary>
			[XmlElement]
			[ProtoMember(20)]
			public float? ChargingLargeDriveEffectorForceN;

			/// <summary>
			/// The force (in Newtons) applied for every drive in a large grid jump gate away from the jump node when the gate is charging<br />
			/// Cannot be NaN or Infinite<br />
			/// Defaults to 250
			/// </summary>
			[XmlElement]
			[ProtoMember(21)]
			public float? ChargingSmallDriveEffectorForceN;

			/// <summary>
			/// Whether ships jumping to an untethered destination have randomness applied to each ship or once per the entire fleet<br />
			/// Defaults to false
			/// </summary>
			[XmlElement]
			[ProtoMember(22)]
			public bool? ConfineUntetheredSpread;

			/// <summary>
			/// The radius (in meters) in which a large grid jump gate can randomly displace its jumped entities when untethered<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0 (Disabled)
			/// </summary>
			[XmlElement]
			[ProtoMember(23)]
			public float? LargeGateRandomDisplacementRadius;

			/// <summary>
			/// The radius (in meters) in which a small grid jump gate can randomly displace its jumped entities when untethered<br />
			/// Cannot be NaN, Infinite, or less than 0<br />
			/// Defaults to 0 (Disabled)
			/// </summary>
			[XmlElement]
			[ProtoMember(24)]
			public float? SmallGateRandomDisplacementRadius;

			/// <summary>
			/// The minimum percentage of a jump gate's drives that must be owned by the controller owner's faction to allow binding<br />
			/// Cannot be NaN, Infinite, less than 0, or greater than 1<br />
			/// Defaults to 0.25 (25%)
			/// </summary>
			[XmlElement]
			[ProtoMember(25)]
			public float? MinimumControlOwnerFactionRatio;

			/// <summary>
			/// The jump gate explosion configuration
			/// </summary>
			[XmlElement]
			[ProtoMember(26)]
			public MyModJumpGateExplosionConfiguration JumpGateExplosionConfiguration;

			/// <summary>
			/// The jump gate wormhole configuration
			/// </summary>
			[XmlElement]
			[ProtoMember(27)]
			public MyModJumpGateWormholeConfiguration JumpGateWormholeConfiguration;
			#endregion

			#region Public Methods
			/// <returns>The default capacitor configuration values</returns>
			public static MyModJumpGateConfiguration Defaults()
			{
				return new MyModJumpGateConfiguration {
					MinimumLargeJumpDistance = 5000d,
					MinimumSmallJumpDistance = 2500d,
					MaximumLargeJumpDistance = double.PositiveInfinity,
					MaximumSmallJumpDistance = double.PositiveInfinity,
					UntetheredLargeJumpEnergyCost = 1.05f,
					UntetheredSmallJumpEnergyCost = 1.1f,
					MaxLargeJumpGate50Distance = 1e12d,
					MaxSmallJumpGate50Distance = 1e9,
					LargeGatePowerFactorExponent = 3f,
					SmallGatePowerFactorExponent = 3f,
					LargeGatePowerFalloffFactor = 1f,
					SmallGatePowerFalloffFactor = 1f,
					LargeGateKilowattPerKilogram = 0.025f,
					SmallGateKilowattPerKilogram = 0.075f,
					LargeGateRandomOffsetPerKilometer = 0.001f,
					SmallGateRandomOffsetPerKilometer = 0.005f,
					LargeGateKilometerOffsetPerUnitG = 0.5f,
					SmallGateKilometerOffsetPerUnitG = 0.825f,
					IgnoreDockedGrids = false,
					ChargingLargeDriveEffectorForceN = 1000f,
					ChargingSmallDriveEffectorForceN = 250f,
					ConfineUntetheredSpread = false,
					LargeGateRandomDisplacementRadius = 0,
					SmallGateRandomDisplacementRadius = 0,
					MinimumControlOwnerFactionRatio = 0.25f,
					JumpGateExplosionConfiguration = MyModJumpGateExplosionConfiguration.Defaults(),
					JumpGateWormholeConfiguration = MyModJumpGateWormholeConfiguration.Defaults(),
				};
			}

			/// <summary>
			/// Loads this configuration from a dictionary
			/// </summary>
			/// <param name="map">The dictionary to read from</param>
			/// <returns>The configuration</returns>
			public static MyModJumpGateConfiguration FromDictionary(Dictionary<string, object> map)
			{
				return (map == null) ? null : new MyModJumpGateConfiguration() {
					MinimumLargeJumpDistance = map.GetValueOrDefault("MinimumLargeJumpDistance") as double?,
					MinimumSmallJumpDistance = map.GetValueOrDefault("MinimumSmallJumpDistance") as double?,
					MaximumLargeJumpDistance = map.GetValueOrDefault("MaximumLargeJumpDistance") as double?,
					MaximumSmallJumpDistance = map.GetValueOrDefault("MaximumSmallJumpDistance") as double?,
					UntetheredLargeJumpEnergyCost = map.GetValueOrDefault("UntetheredLargeJumpEnergyCost") as float?,
					UntetheredSmallJumpEnergyCost = map.GetValueOrDefault("UntetheredSmallJumpEnergyCost") as float?,
					MaxLargeJumpGate50Distance = map.GetValueOrDefault("MaxLargeJumpGate50Distance") as double?,
					MaxSmallJumpGate50Distance = map.GetValueOrDefault("MaxSmallJumpGate50Distance") as double?,
					LargeGatePowerFactorExponent = map.GetValueOrDefault("LargeGatePowerFactorExponent") as float?,
					SmallGatePowerFactorExponent = map.GetValueOrDefault("SmallGatePowerFactorExponent") as float?,
					LargeGatePowerFalloffFactor = map.GetValueOrDefault("LargeGatePowerFalloffFactor") as float?,
					SmallGatePowerFalloffFactor = map.GetValueOrDefault("SmallGatePowerFalloffFactor") as float?,
					LargeGateKilowattPerKilogram = map.GetValueOrDefault("LargeGateKilowattPerKilogram") as float?,
					SmallGateKilowattPerKilogram = map.GetValueOrDefault("SmallGateKilowattPerKilogram") as float?,
					LargeGateKilometerOffsetPerUnitG = map.GetValueOrDefault("LargeGateKilometerOffsetPerUnitG") as float?,
					SmallGateKilometerOffsetPerUnitG = map.GetValueOrDefault("SmallGateKilometerOffsetPerUnitG") as float?,
					IgnoreDockedGrids = map.GetValueOrDefault("IgnoreDockedGrids") as bool?,
					ChargingLargeDriveEffectorForceN = map.GetValueOrDefault("ChargingLargeDriveEffectorForceN") as float?,
					ChargingSmallDriveEffectorForceN = map.GetValueOrDefault("ChargingSmallDriveEffectorForceN") as float?,
					ConfineUntetheredSpread = map.GetValueOrDefault("ConfineUntetheredSpread") as bool?,
					LargeGateRandomDisplacementRadius = map.GetValueOrDefault("LargeGateRandomDisplacementRadius") as float?,
					SmallGateRandomDisplacementRadius = map.GetValueOrDefault("SmallGateRandomDisplacementRadius") as float?,
					MinimumControlOwnerFactionRatio = map.GetValueOrDefault("MinimumControlOwnerFactionRatio") as float?,
					JumpGateExplosionConfiguration = MyModJumpGateExplosionConfiguration.FromDictionary(map.GetValueOrDefault("JumpGateExplosionConfiguration") as Dictionary<string, object>),
					JumpGateWormholeConfiguration = MyModJumpGateWormholeConfiguration.FromDictionary(map.GetValueOrDefault("JumpGateWormholeConfiguration") as Dictionary<string, object>),
				};
			}

			/// <summary>
			/// Overlays a jump gate configuration on top of this one<br />
			/// Any null values in this configuration will be replaced by values from "config"
			/// </summary>
			/// <param name="config">The configuration to overlay</param>
			public void Overlay(MyModJumpGateConfiguration config)
			{
				if (config == null) return;
				this.MinimumLargeJumpDistance = this.MinimumLargeJumpDistance ?? config.MinimumLargeJumpDistance;
				this.MinimumSmallJumpDistance = this.MinimumSmallJumpDistance ?? config.MinimumSmallJumpDistance;
				this.MaximumLargeJumpDistance = this.MaximumLargeJumpDistance ?? config.MaximumLargeJumpDistance;
				this.MaximumSmallJumpDistance = this.MaximumSmallJumpDistance ?? config.MaximumSmallJumpDistance;
				this.UntetheredLargeJumpEnergyCost = this.UntetheredLargeJumpEnergyCost ?? config.UntetheredLargeJumpEnergyCost;
				this.UntetheredSmallJumpEnergyCost = this.UntetheredSmallJumpEnergyCost ?? config.UntetheredSmallJumpEnergyCost;
				this.MaxLargeJumpGate50Distance = this.MaxLargeJumpGate50Distance ?? config.MaxLargeJumpGate50Distance;
				this.MaxSmallJumpGate50Distance = this.MaxSmallJumpGate50Distance ?? config.MaxSmallJumpGate50Distance;
				this.LargeGatePowerFactorExponent = this.LargeGatePowerFactorExponent ?? config.LargeGatePowerFactorExponent;
				this.SmallGatePowerFactorExponent = this.SmallGatePowerFactorExponent ?? config.SmallGatePowerFactorExponent;
				this.LargeGatePowerFalloffFactor = this.LargeGatePowerFalloffFactor ?? config.LargeGatePowerFalloffFactor;
				this.SmallGatePowerFalloffFactor = this.SmallGatePowerFalloffFactor ?? config.SmallGatePowerFalloffFactor;
				this.LargeGateKilowattPerKilogram = this.LargeGateKilowattPerKilogram ?? config.LargeGateKilowattPerKilogram;
				this.SmallGateKilowattPerKilogram = this.SmallGateKilowattPerKilogram ?? config.SmallGateKilowattPerKilogram;
				this.LargeGateKilometerOffsetPerUnitG = this.LargeGateKilometerOffsetPerUnitG ?? config.LargeGateKilometerOffsetPerUnitG;
				this.SmallGateKilometerOffsetPerUnitG = this.SmallGateKilometerOffsetPerUnitG ?? config.SmallGateKilometerOffsetPerUnitG;
				this.IgnoreDockedGrids = this.IgnoreDockedGrids ?? config.IgnoreDockedGrids;
				this.ChargingLargeDriveEffectorForceN = this.ChargingLargeDriveEffectorForceN ?? config.ChargingLargeDriveEffectorForceN;
				this.ChargingSmallDriveEffectorForceN = this.ChargingSmallDriveEffectorForceN ?? config.ChargingSmallDriveEffectorForceN;
				this.ConfineUntetheredSpread = this.ConfineUntetheredSpread ?? config.ConfineUntetheredSpread;
				this.LargeGateRandomDisplacementRadius = this.LargeGateRandomDisplacementRadius ?? config.LargeGateRandomDisplacementRadius;
				this.SmallGateRandomDisplacementRadius = this.SmallGateRandomDisplacementRadius ?? config.SmallGateRandomDisplacementRadius;
				this.MinimumControlOwnerFactionRatio = this.MinimumControlOwnerFactionRatio ?? config.MinimumControlOwnerFactionRatio;
				if (this.JumpGateExplosionConfiguration == null) this.JumpGateExplosionConfiguration = config.JumpGateExplosionConfiguration;
				else this.JumpGateExplosionConfiguration.Overlay(config.JumpGateExplosionConfiguration);
				if (this.JumpGateWormholeConfiguration == null) this.JumpGateWormholeConfiguration = config.JumpGateWormholeConfiguration;
				else this.JumpGateWormholeConfiguration.Overlay(config.JumpGateWormholeConfiguration);
			}

			/// <summary>
			/// Validates this configuration<br />
			/// Any invalid values will be replaced with defaults
			/// </summary>
			public void Validate()
			{
				MyModJumpGateConfiguration defaults = MyModJumpGateConfiguration.Defaults();
				this.MinimumLargeJumpDistance = MyModConfigurationV1.ValidateValue(this.MinimumLargeJumpDistance, defaults.MinimumLargeJumpDistance, 0, allow_inf: true);
				this.MinimumSmallJumpDistance = MyModConfigurationV1.ValidateValue(this.MinimumSmallJumpDistance, defaults.MinimumSmallJumpDistance, 0, allow_inf: true);
				this.MaximumLargeJumpDistance = MyModConfigurationV1.ValidateValue(this.MaximumLargeJumpDistance, defaults.MaximumLargeJumpDistance, 0, allow_inf: true);
				this.MaximumSmallJumpDistance = MyModConfigurationV1.ValidateValue(this.MaximumSmallJumpDistance, defaults.MaximumSmallJumpDistance, 0, allow_inf: true);
				this.UntetheredLargeJumpEnergyCost = MyModConfigurationV1.ValidateValue(this.UntetheredLargeJumpEnergyCost, defaults.UntetheredLargeJumpEnergyCost, 0, allow_inf: true);
				this.UntetheredSmallJumpEnergyCost = MyModConfigurationV1.ValidateValue(this.UntetheredSmallJumpEnergyCost, defaults.UntetheredSmallJumpEnergyCost, 0, allow_inf: true);
				this.MaxLargeJumpGate50Distance = MyModConfigurationV1.ValidateValue(this.MaxLargeJumpGate50Distance, defaults.MaxLargeJumpGate50Distance, 0);
				this.MaxSmallJumpGate50Distance = MyModConfigurationV1.ValidateValue(this.MaxSmallJumpGate50Distance, defaults.MaxSmallJumpGate50Distance, 0);
				this.LargeGatePowerFactorExponent = MyModConfigurationV1.ValidateValue(this.LargeGatePowerFactorExponent, defaults.LargeGatePowerFactorExponent, 0);
				this.SmallGatePowerFactorExponent = MyModConfigurationV1.ValidateValue(this.SmallGatePowerFactorExponent, defaults.SmallGatePowerFactorExponent, 0);
				this.LargeGatePowerFalloffFactor = MyModConfigurationV1.ValidateValue(this.LargeGatePowerFalloffFactor, defaults.LargeGatePowerFalloffFactor);
				this.SmallGatePowerFalloffFactor = MyModConfigurationV1.ValidateValue(this.SmallGatePowerFalloffFactor, defaults.SmallGatePowerFalloffFactor);
				this.LargeGateKilowattPerKilogram = MyModConfigurationV1.ValidateValue(this.LargeGateKilowattPerKilogram, defaults.LargeGateKilowattPerKilogram, 0);
				this.SmallGateKilowattPerKilogram = MyModConfigurationV1.ValidateValue(this.SmallGateKilowattPerKilogram, defaults.SmallGateKilowattPerKilogram, 0);
				this.LargeGateRandomOffsetPerKilometer = MyModConfigurationV1.ValidateValue(this.LargeGateRandomOffsetPerKilometer, defaults.LargeGateRandomOffsetPerKilometer, 0);
				this.SmallGateRandomOffsetPerKilometer = MyModConfigurationV1.ValidateValue(this.SmallGateRandomOffsetPerKilometer, defaults.SmallGateRandomOffsetPerKilometer, 0);
				this.LargeGateKilometerOffsetPerUnitG = MyModConfigurationV1.ValidateValue(this.LargeGateKilometerOffsetPerUnitG, defaults.LargeGateKilometerOffsetPerUnitG);
				this.SmallGateKilometerOffsetPerUnitG = MyModConfigurationV1.ValidateValue(this.SmallGateKilometerOffsetPerUnitG, defaults.SmallGateKilometerOffsetPerUnitG);
				this.IgnoreDockedGrids = this.IgnoreDockedGrids ?? defaults.IgnoreDockedGrids ?? false;
				this.LargeGateRandomDisplacementRadius = MyModConfigurationV1.ValidateValue(this.LargeGateRandomDisplacementRadius, defaults.LargeGateRandomDisplacementRadius, 0);
				this.SmallGateRandomDisplacementRadius = MyModConfigurationV1.ValidateValue(this.SmallGateRandomDisplacementRadius, defaults.SmallGateRandomDisplacementRadius, 0);
				this.ConfineUntetheredSpread = this.ConfineUntetheredSpread ?? defaults.ConfineUntetheredSpread ?? false;
				this.MinimumControlOwnerFactionRatio = MyModConfigurationV1.ValidateValue(this.MinimumControlOwnerFactionRatio, defaults.MinimumControlOwnerFactionRatio, 0, 1);
				this.JumpGateExplosionConfiguration?.Validate();
				this.JumpGateWormholeConfiguration?.Validate();

				this.LargeGateDistanceScaleExponent = Math.Log((this.MaxLargeJumpGate50Distance.Value - this.MinimumLargeJumpDistance.Value) / 1000d, 50d);
				this.SmallGateDistanceScaleExponent = Math.Log((this.MaxSmallJumpGate50Distance.Value - this.MinimumSmallJumpDistance.Value) / 1000d, 50d);
			}

			/// <summary>
			/// Clones this configuration
			/// </summary>
			/// <returns>The cloned configuration</returns>
			public MyModJumpGateConfiguration Clone()
			{
				MyModJumpGateConfiguration clone = new MyModJumpGateConfiguration {
					JumpGateExplosionConfiguration = this.JumpGateExplosionConfiguration?.Clone(),
					JumpGateWormholeConfiguration = this.JumpGateWormholeConfiguration?.Clone(),
				};
				clone.Overlay(this);
				return clone;
			}

			/// <summary>
			/// Saves this configuration to a dictionary
			/// </summary>
			/// <returns>The configuration as a dictionary</returns>
			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["MinimumLargeJumpDistance"] = this.MinimumLargeJumpDistance,
					["MinimumSmallJumpDistance"] = this.MinimumSmallJumpDistance,
					["MaximumLargeJumpDistance"] = this.MaximumLargeJumpDistance,
					["MaximumSmallJumpDistance"] = this.MaximumSmallJumpDistance,
					["UntetheredLargeJumpEnergyCost"] = this.UntetheredLargeJumpEnergyCost,
					["UntetheredSmallJumpEnergyCost"] = this.UntetheredSmallJumpEnergyCost,
					["MaxLargeJumpGate50Distance"] = this.MaxLargeJumpGate50Distance,
					["MaxSmallJumpGate50Distance"] = this.MaxSmallJumpGate50Distance,
					["LargeGatePowerFactorExponent"] = this.LargeGatePowerFactorExponent,
					["SmallGatePowerFactorExponent"] = this.SmallGatePowerFactorExponent,
					["LargeGatePowerFalloffFactor"] = this.LargeGatePowerFalloffFactor,
					["SmallGatePowerFalloffFactor"] = this.SmallGatePowerFalloffFactor,
					["LargeGateKilowattPerKilogram"] = this.LargeGateKilowattPerKilogram,
					["SmallGateKilowattPerKilogram"] = this.SmallGateKilowattPerKilogram,
					["LargeGateKilometerOffsetPerUnitG"] = this.LargeGateKilometerOffsetPerUnitG,
					["SmallGateKilometerOffsetPerUnitG"] = this.SmallGateKilometerOffsetPerUnitG,
					["IgnoreDockedGrids"] = this.IgnoreDockedGrids,
					["ChargingLargeDriveEffectorForceN"] = this.ChargingLargeDriveEffectorForceN,
					["ChargingSmallDriveEffectorForceN"] = this.ChargingSmallDriveEffectorForceN,
					["ConfineUntetheredSpread"] = this.ConfineUntetheredSpread,
					["LargeGateRandomDisplacementRadius"] = this.LargeGateRandomDisplacementRadius,
					["SmallGateRandomDisplacementRadius"] = this.SmallGateRandomDisplacementRadius,
					["MinimumControlOwnerFactionRatio"] = this.MinimumControlOwnerFactionRatio,
					["JumpGateExplosionConfiguration"] = this.JumpGateExplosionConfiguration?.ToDictionary(),
					["JumpGateWormholeConfiguration"] = this.JumpGateWormholeConfiguration?.ToDictionary(),
				};
			}
			#endregion
		}

		[Serializable]
		[XmlRoot("ConstructConfiguration")]
		[ProtoContract(UseProtoMembersOnly = true)]
		public class MyModConstructConfiguration : IMYConfigurationSchema
		{
			#region Schema
			/// <summary>
			/// If true, gates can only see other gates connected via antennas or part of the same construct<br />
			/// Defaults to true
			/// </summary>
			[XmlElement]
			[ProtoMember(1)]
			public bool? RequireGridCommLink;

			/// <summary>
			/// The maximum number of jump gates a single grid group can have<br />
			/// Cannot be less than 0<br />
			/// "0" will be treated as infinite<br />
			/// Defaults to 3 if server otherwise 0
			/// </summary>
			[XmlElement]
			[ProtoMember(2)]
			public uint? MaxTotalGatesPerConstruct;

			/// <summary>
			/// The maximum number of large grid jump gates a single grid group can have<br />
			/// Cannot be less than 0<br />
			/// "0" will be treated as infinite<br />
			/// Defaults to 0
			/// </summary>
			[XmlElement]
			[ProtoMember(3)]
			public uint? MaxLargeGatesPerConstruct;

			/// <summary>
			/// The maximum number of small grid jump gates a single grid group can have<br />
			/// Cannot be less than 0<br />
			/// "0" will be treated as infinite<br />
			/// Defaults to 0
			/// </summary>
			[XmlElement]
			[ProtoMember(4)]
			public uint? MaxSmallGatesPerConstruct;
			#endregion

			#region Public Methods
			/// <returns>The default construct configuration values</returns>
			public static MyModConstructConfiguration Defaults()
			{
				return new MyModConstructConfiguration {
					RequireGridCommLink = true,
					MaxTotalGatesPerConstruct = MyNetworkInterface.IsMultiplayerServer ? 3u : 0u,
					MaxLargeGatesPerConstruct = 0,
					MaxSmallGatesPerConstruct = 0,
				};
			}

			/// <summary>
			/// Loads this configuration from a dictionary
			/// </summary>
			/// <param name="map">The dictionary to read from</param>
			/// <returns>The configuration</returns>
			public static MyModConstructConfiguration FromDictionary(Dictionary<string, object> map)
			{
				return (map == null) ? null : new MyModConstructConfiguration() {
					RequireGridCommLink = map.GetValueOrDefault("RequireGridCommLink") as bool?,
					MaxTotalGatesPerConstruct = map.GetValueOrDefault("MaxTotalGatesPerConstruct") as uint?,
					MaxLargeGatesPerConstruct = map.GetValueOrDefault("MaxLargeGatesPerConstruct") as uint?,
					MaxSmallGatesPerConstruct = map.GetValueOrDefault("MaxSmallGatesPerConstruct") as uint?,
				};
			}

			/// <summary>
			/// Overlays a construct configuration on top of this one<br />
			/// Any null values in this configuration will be replaced by values from "config"
			/// </summary>
			/// <param name="config">The configuration to overlay</param>
			public void Overlay(MyModConstructConfiguration config)
			{
				if (config == null) return;
				this.RequireGridCommLink = this.RequireGridCommLink ?? config.RequireGridCommLink;
				this.MaxTotalGatesPerConstruct = this.MaxTotalGatesPerConstruct ?? config.MaxTotalGatesPerConstruct;
				this.MaxLargeGatesPerConstruct = this.MaxLargeGatesPerConstruct ?? config.MaxLargeGatesPerConstruct;
				this.MaxSmallGatesPerConstruct = this.MaxSmallGatesPerConstruct ?? config.MaxSmallGatesPerConstruct;
			}

			/// <summary>
			/// Clones this configuration
			/// </summary>
			/// <returns>The cloned configuration</returns>
			public MyModConstructConfiguration Clone()
			{
				MyModConstructConfiguration clone = new MyModConstructConfiguration();
				clone.Overlay(this);
				return clone;
			}

			/// <summary>
			/// Validates this configuration<br />
			/// Any invalid values will be replaced with defaults
			/// </summary>
			public void Validate()
			{
				MyModConstructConfiguration defaults = MyModConstructConfiguration.Defaults();
				this.Overlay(defaults);
			}

			/// <summary>
			/// Saves this configuration to a dictionary
			/// </summary>
			/// <returns>The configuration as a dictionary</returns>
			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["RequireGridCommLink"] = this.RequireGridCommLink,
					["MaxTotalGatesPerConstruct"] = this.MaxTotalGatesPerConstruct,
					["MaxLargeGatesPerConstruct"] = this.MaxLargeGatesPerConstruct,
					["MaxSmallGatesPerConstruct"] = this.MaxSmallGatesPerConstruct,
				};
			}
			#endregion
		}

		[Serializable]
		[XmlRoot("GeneralConfiguration")]
		[ProtoContract(UseProtoMembersOnly = true)]
		public class MyModGeneralConfiguration : IMYConfigurationSchema
		{
			#region Schema
			/// <summary>
			/// Sets the verbosity level of debug information in the log file<br />
			/// Accepts value between 0 and 5<br />
			/// Defaults to 5
			/// </summary>
			[XmlElement]
			[ProtoMember(1)]
			public byte? DebugLogVerbosity;

			/// <summary>
			/// The maximum distance (in meters) in which clients will be shown debug draw and gate particles<br />
			/// Cannot be NaN or less than 500<br />
			/// Defaults to 5000 m
			/// </summary>
			[XmlElement]
			[ProtoMember(2)]
			public double? DrawSyncDistance;

			/// <summary>
			/// If false, grids partially outside the jump sphere will be sheared<br />
			/// Defaults to true
			/// </summary>
			[XmlElement]
			[ProtoMember(3)]
			public bool? LenientJumps;

			/// <summary>
			/// If true, highlights blocks that will be sheared by the jump gate ellipsoid
			/// Defaults to true
			/// </summary>
			[XmlElement]
			[ProtoMember(4)]
			public bool? HighlightShearBlocks;

			/// <summary>
			/// If false, untethered jumps will have randomness applied to endpoint<br />
			/// SEE: GeneralConfiguration/RandomOffsetPerKilometer<br />
			/// Defaults to false
			/// </summary>
			[XmlElement]
			[ProtoMember(5)]
			public bool? SafeJumps;

			/// <summary>
			/// The number of concurrent threads to use for construct updates<br />
			/// Defaults to 8 if dedicated server, otherwise 3
			/// </summary>
			[XmlElement]
			[ProtoMember(6)]
			public byte? ConcurrentGridUpdateThreads;

			/// <summary>
			/// If true, allows jump gates to pull power directly from the reactor if instantaneous power is insufficient<br />
			/// Defaults to true
			/// </summary>
			[XmlElement]
			[ProtoMember(7)]
			public bool? SyphonReactorPower;

			/// <summary>
			/// If true, hidden/disabled GPS markers will be shown as a destination on the jump gate controller/antenna<br />
			/// Defaults to false
			/// </summary>
			[XmlElement]
			[ProtoMember(8)]
			public bool? ShowHiddenGPSMarkers;

			/// <summary>
			/// Whether to allow admins to bypass the wormhole maximum duration<br />
			/// Defaults to true
			/// </summary>
			[XmlElement]
			[ProtoMember(9)]
			public bool? AllowAdminWormholeDurationBypass;

			/// <summary>
			/// The maximum number of total jumps that are allowed at a time<br />
			/// Cannot be less than 0<br />
			/// "0" will be treated as infinite<br />
			/// Defaults to 50 if server otherwise 0
			/// </summary>
			[XmlElement]
			[ProtoMember(10)]
			public uint? MaxConcurrentJumps;
			#endregion

			#region Public Methods
			/// <returns>The default general configuration values</returns>
			public static MyModGeneralConfiguration Defaults()
			{
				return new MyModGeneralConfiguration {
					DebugLogVerbosity = 5,
					DrawSyncDistance = 5000f,
					LenientJumps = true,
					HighlightShearBlocks = true,
					SafeJumps = false,
					ConcurrentGridUpdateThreads = (byte) (MyNetworkInterface.IsDedicatedMultiplayerServer ? 8 : 3),
					SyphonReactorPower = true,
					ShowHiddenGPSMarkers = false,
					AllowAdminWormholeDurationBypass = true,
					MaxConcurrentJumps = MyNetworkInterface.IsMultiplayerServer ? 50u : 0u,
				};
			}

			/// <summary>
			/// Loads this configuration from a dictionary
			/// </summary>
			/// <param name="map">The dictionary to read from</param>
			/// <returns>The configuration</returns>
			public static MyModGeneralConfiguration FromDictionary(Dictionary<string, object> map)
			{
				return (map == null) ? null : new MyModGeneralConfiguration()
				{
					DebugLogVerbosity = map.GetValueOrDefault("DebugLogVerbosity") as byte?,
					DrawSyncDistance = map.GetValueOrDefault("DrawSyncDistance") as float?,
					LenientJumps = map.GetValueOrDefault("LenientJumps") as bool?,
					HighlightShearBlocks = map.GetValueOrDefault("HighlightShearBlocks") as bool?,
					SafeJumps = map.GetValueOrDefault("SafeJumps") as bool?,
					ConcurrentGridUpdateThreads = map.GetValueOrDefault("ConcurrentGridUpdateThreads") as byte?,
					SyphonReactorPower = map.GetValueOrDefault("SyphonReactorPower") as bool?,
					ShowHiddenGPSMarkers = map.GetValueOrDefault("ShowHiddenGPSMarkers") as bool?,
					AllowAdminWormholeDurationBypass = map.GetValueOrDefault("AllowAdminWormholeDurationBypass") as bool?,
					MaxConcurrentJumps = map.GetValueOrDefault("MaxConcurrentJumps") as uint?,
				};
			}

			/// <summary>
			/// Overlays a construct configuration on top of this one<br />
			/// Any null values in this configuration will be replaced by values from "config"
			/// </summary>
			/// <param name="config">The configuration to overlay</param>
			public void Overlay(MyModGeneralConfiguration config)
			{
				if (config == null) return;
				this.DebugLogVerbosity = this.DebugLogVerbosity ?? config.DebugLogVerbosity;
				this.DrawSyncDistance = this.DrawSyncDistance ?? config.DrawSyncDistance;
				this.LenientJumps = this.LenientJumps ?? config.LenientJumps;
				this.HighlightShearBlocks = this.HighlightShearBlocks ?? config.HighlightShearBlocks;
				this.SafeJumps = this.SafeJumps ?? config.SafeJumps;
				this.ConcurrentGridUpdateThreads = this.ConcurrentGridUpdateThreads ?? config.ConcurrentGridUpdateThreads;
				this.SyphonReactorPower = this.SyphonReactorPower ?? config.SyphonReactorPower;
				this.ShowHiddenGPSMarkers = this.ShowHiddenGPSMarkers ?? config.ShowHiddenGPSMarkers;
				this.AllowAdminWormholeDurationBypass = this.AllowAdminWormholeDurationBypass ?? config.AllowAdminWormholeDurationBypass;
				this.MaxConcurrentJumps = this.MaxConcurrentJumps ?? config.MaxConcurrentJumps;
			}

			/// <summary>
			/// Validates this configuration<br />
			/// Any invalid values will be replaced with defaults
			/// </summary>
			public void Validate()
			{
				MyModGeneralConfiguration defaults = MyModGeneralConfiguration.Defaults();
				this.DebugLogVerbosity = MyModConfigurationV1.ValidateValue(this.DebugLogVerbosity, defaults.DebugLogVerbosity, 0, 5);
				this.DrawSyncDistance = MyModConfigurationV1.ValidateValue(this.DrawSyncDistance, defaults.DrawSyncDistance, 500, allow_inf: true);
				this.LenientJumps = this.LenientJumps ?? defaults.LenientJumps ?? true;
				this.HighlightShearBlocks = this.HighlightShearBlocks ?? defaults.HighlightShearBlocks ?? true;
				this.SafeJumps = this.SafeJumps ?? defaults.SafeJumps ?? false;
				this.ConcurrentGridUpdateThreads = MyModConfigurationV1.ValidateValue(this.ConcurrentGridUpdateThreads, defaults.ConcurrentGridUpdateThreads, 1);
				this.SyphonReactorPower = this.SyphonReactorPower ?? defaults.SyphonReactorPower ?? true;
				this.ShowHiddenGPSMarkers = this.ShowHiddenGPSMarkers ?? defaults.ShowHiddenGPSMarkers ?? false;
				this.AllowAdminWormholeDurationBypass = this.AllowAdminWormholeDurationBypass ?? defaults.AllowAdminWormholeDurationBypass ?? true;
				this.MaxConcurrentJumps = MyModConfigurationV1.ValidateValue(this.MaxConcurrentJumps, defaults.MaxConcurrentJumps);
			}

			/// <summary>
			/// Clones this configuration
			/// </summary>
			/// <returns>The cloned configuration</returns>
			public MyModGeneralConfiguration Clone()
			{
				MyModGeneralConfiguration clone = new MyModGeneralConfiguration();
				clone.Overlay(this);
				return clone;
			}

			/// <summary>
			/// Saves this configuration to a dictionary
			/// </summary>
			/// <returns>The configuration as a dictionary</returns>
			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["DebugLogVerbosity"] = this.DebugLogVerbosity,
					["DrawSyncDistance"] = this.DrawSyncDistance,
					["LenientJumps"] = this.LenientJumps,
					["HighlightShearBlocks"] = this.HighlightShearBlocks,
					["SafeJumps"] = this.SafeJumps,
					["ConcurrentGridUpdateThreads"] = this.ConcurrentGridUpdateThreads,
					["SyphonReactorPower"] = this.SyphonReactorPower,
					["ShowHiddenGPSMarkers"] = this.ShowHiddenGPSMarkers,
					["AllowAdminWormholeDurationBypass"] = this.AllowAdminWormholeDurationBypass,
					["MaxConcurrentJumps"] = this.MaxConcurrentJumps,
				};
			}
			#endregion
		}

		[Serializable]
		[XmlRoot("ModSettings")]
		[ProtoContract(UseProtoMembersOnly = true)]
		public class MyModSettings : IMYConfigurationSchema
		{
			#region Schema
			/// <summary>
			/// Maximum number of mod-specific log files that can be store before deleting<br />
			/// Cannot be less than 1<br />
			/// Defaults to 25
			/// </summary>
			[XmlElement]
			[ProtoMember(1)]
			public uint MaxStoredModSpecificLogFiles;
			#endregion

			#region Public Methods
			/// <returns>The default mod settings</returns>
			public static MyModSettings Defaults()
			{
				return new MyModSettings {
					MaxStoredModSpecificLogFiles = 25,
				};
			}

			/// <summary>
			/// Loads this configuration from a dictionary
			/// </summary>
			/// <param name="map">The dictionary to read from</param>
			/// <returns>The configuration</returns>
			public static MyModSettings FromDictionary(Dictionary<string, object> map)
			{
				MyModSettings defaults = MyModSettings.Defaults(); 
				return (map == null) ? defaults : new MyModSettings() {
					MaxStoredModSpecificLogFiles = (map.GetValueOrDefault("MaxStoredModSpecificLogFiles") as uint?) ?? defaults.MaxStoredModSpecificLogFiles,
				};
			}

			/// <summary>
			/// Validates this configuration<br />
			/// Any invalid values will be replaced with defaults
			/// </summary>
			public void Validate()
			{
				MyModSettings defaults = MyModSettings.Defaults();
				this.MaxStoredModSpecificLogFiles = MyModConfigurationV1.ValidateValue(this.MaxStoredModSpecificLogFiles, defaults.MaxStoredModSpecificLogFiles, 1);
			}

			/// <summary>
			/// Clones this configuration
			/// </summary>
			/// <returns>The cloned configuration</returns>
			public MyModSettings Clone()
			{
				return new MyModSettings {
					MaxStoredModSpecificLogFiles = this.MaxStoredModSpecificLogFiles,
				};
			}

			/// <summary>
			/// Saves this configuration to a dictionary
			/// </summary>
			/// <returns>The configuration as a dictionary</returns>
			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["MaxStoredModSpecificLogFiles"] = this.MaxStoredModSpecificLogFiles,
				};
			}
			#endregion
		}
		#endregion

		#region Package Schemas
		[Serializable]
		[XmlRoot("LocalConfiguration")]
		[ProtoContract(UseProtoMembersOnly = true)]
		public sealed class MyLocalModConfiguration : IMYConfigurationSchema
		{
			#region Schema
			/// <summary>
			/// The world-local capacitor configuration
			/// </summary>
			[XmlElement]
			[ProtoMember(1)]
			public MyModCapacitorConfiguration CapacitorConfiguration;

			/// <summary>
			/// The world-local drive configuration
			/// </summary>
			[XmlElement]
			[ProtoMember(2)]
			public MyModDriveConfiguration DriveConfiguration;

			/// <summary>
			/// The world-local jump gate configuration
			/// </summary>
			[XmlElement]
			[ProtoMember(3)]
			public MyModJumpGateConfiguration JumpGateConfiguration;

			/// <summary>
			/// The world-local construct configuration
			/// </summary>
			[XmlElement]
			[ProtoMember(4)]
			public MyModConstructConfiguration ConstructConfiguration;

			/// <summary>
			/// The world-local general configuration
			/// </summary>
			[XmlElement]
			[ProtoMember(5)]
			public MyModGeneralConfiguration GeneralConfiguration;
			#endregion

			#region Public Methods
			/// <returns>The default local mod configuration values</returns>
			public static MyLocalModConfiguration Defaults()
			{
				return new MyLocalModConfiguration {
					CapacitorConfiguration = MyModCapacitorConfiguration.Defaults(),
					DriveConfiguration = MyModDriveConfiguration.Defaults(),
					JumpGateConfiguration = MyModJumpGateConfiguration.Defaults(),
					ConstructConfiguration = MyModConstructConfiguration.Defaults(),
					GeneralConfiguration = MyModGeneralConfiguration.Defaults(),
				};
			}

			/// <summary>
			/// Loads this configuration from a dictionary
			/// </summary>
			/// <param name="map">The dictionary to read from</param>
			/// <returns>The configuration</returns>
			public static MyLocalModConfiguration FromDictionary(Dictionary<string, object> map)
			{
				return (map == null) ? null : new MyLocalModConfiguration() {
					CapacitorConfiguration = MyModCapacitorConfiguration.FromDictionary(map.GetValueOrDefault("CapacitorConfiguration") as Dictionary<string, object>),
					DriveConfiguration = MyModDriveConfiguration.FromDictionary(map.GetValueOrDefault("DriveConfiguration") as Dictionary<string, object>),
					JumpGateConfiguration = MyModJumpGateConfiguration.FromDictionary(map.GetValueOrDefault("JumpGateConfiguration") as Dictionary<string, object>),
					ConstructConfiguration = MyModConstructConfiguration.FromDictionary(map.GetValueOrDefault("ConstructConfiguration") as Dictionary<string, object>),
					GeneralConfiguration = MyModGeneralConfiguration.FromDictionary(map.GetValueOrDefault("GeneralConfiguration") as Dictionary<string, object>),
				};
			}

			/// <summary>
			/// Overlays a local mod configuration on top of this one<br />
			/// Any null values in this configuration will be replaced by values from "config"
			/// </summary>
			/// <param name="config">The configuration to overlay</param>
			public void Overlay(MyLocalModConfiguration config)
			{
				if (config == null) return;

				if (this.CapacitorConfiguration == null) this.CapacitorConfiguration = config.CapacitorConfiguration;
				else this.CapacitorConfiguration.Overlay(config.CapacitorConfiguration);

				if (this.DriveConfiguration == null) this.DriveConfiguration = config.DriveConfiguration;
				else this.DriveConfiguration.Overlay(config.DriveConfiguration);

				if (this.JumpGateConfiguration == null) this.JumpGateConfiguration = config.JumpGateConfiguration;
				else this.JumpGateConfiguration.Overlay(config.JumpGateConfiguration);

				if (this.ConstructConfiguration == null) this.ConstructConfiguration = config.ConstructConfiguration;
				else this.ConstructConfiguration.Overlay(config.ConstructConfiguration);

				if (this.GeneralConfiguration == null) this.GeneralConfiguration = config.GeneralConfiguration;
				else this.GeneralConfiguration.Overlay(config.GeneralConfiguration);
			}

			/// <summary>
			/// Validates this configuration<br />
			/// Any invalid values will be replaced with defaults
			/// </summary>
			public void Validate()
			{
				MyLocalModConfiguration defaults = MyLocalModConfiguration.Defaults();
				this.CapacitorConfiguration = this.CapacitorConfiguration ?? defaults.CapacitorConfiguration;
				this.DriveConfiguration = this.DriveConfiguration ?? defaults.DriveConfiguration;
				this.JumpGateConfiguration = this.JumpGateConfiguration ?? defaults.JumpGateConfiguration;
				this.ConstructConfiguration = this.ConstructConfiguration ?? defaults.ConstructConfiguration;
				this.GeneralConfiguration = this.GeneralConfiguration ?? defaults.GeneralConfiguration;
				this.CapacitorConfiguration.Validate();
				this.DriveConfiguration.Validate();
				this.JumpGateConfiguration.Validate();
				this.ConstructConfiguration.Validate();
				this.GeneralConfiguration.Validate();
			}

			/// <summary>
			/// Clones this configuration
			/// </summary>
			/// <returns>The cloned configuration</returns>
			public MyLocalModConfiguration Clone()
			{
				return new MyLocalModConfiguration {
					CapacitorConfiguration = this.CapacitorConfiguration?.Clone(),
					DriveConfiguration = this.DriveConfiguration?.Clone(),
					JumpGateConfiguration = this.JumpGateConfiguration?.Clone(),
					ConstructConfiguration = this.ConstructConfiguration?.Clone(),
					GeneralConfiguration = this.GeneralConfiguration?.Clone(),
				};
			}

			/// <summary>
			/// Saves this configuration to a dictionary
			/// </summary>
			/// <returns>The configuration as a dictionary</returns>
			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["CapacitorConfiguration"] = this.CapacitorConfiguration?.ToDictionary(),
					["DriveConfiguration"] = this.DriveConfiguration?.ToDictionary(),
					["JumpGateConfiguration"] = this.JumpGateConfiguration?.ToDictionary(),
					["ConstructConfiguration"] = this.ConstructConfiguration?.ToDictionary(),
					["GeneralConfiguration"] = this.GeneralConfiguration?.ToDictionary(),
				};
			}
			#endregion
		}

		[Serializable]
		[XmlRoot("GlobalConfiguration")]
		[ProtoContract(UseProtoMembersOnly = true)]
		public sealed class MyGlobalModConfiguration : IMYConfigurationSchema
		{
			#region Schema
			/// <summary>
			/// Global mod configuration to override local world configuration with
			/// </summary>
			[XmlElement]
			[ProtoMember(1)]
			public MyLocalModConfiguration LocalModConfiguration;

			/// <summary>
			/// Global mod settings
			/// </summary>
			[XmlElement]
			[ProtoMember(2)]
			public MyModSettings ModSettings;
			#endregion

			#region Public Methods
			/// <returns>The default global mod configuration values</returns>
			public static MyGlobalModConfiguration Defaults()
			{
				return new MyGlobalModConfiguration {
					LocalModConfiguration = new MyLocalModConfiguration {
						CapacitorConfiguration = new MyModCapacitorConfiguration(),
						DriveConfiguration = new MyModDriveConfiguration(),
						JumpGateConfiguration = new MyModJumpGateConfiguration(),
						ConstructConfiguration = new MyModConstructConfiguration(),
						GeneralConfiguration = new MyModGeneralConfiguration(),
					},
					ModSettings = MyModSettings.Defaults(),
				};
			}

			/// <summary>
			/// Loads this configuration from a dictionary
			/// </summary>
			/// <param name="map">The dictionary to read from</param>
			/// <returns>The configuration</returns>
			public static MyGlobalModConfiguration FromDictionary(Dictionary<string, object> map)
			{
				return (map == null) ? null : new MyGlobalModConfiguration() {
					LocalModConfiguration = MyLocalModConfiguration.FromDictionary(map.GetValueOrDefault("LocalModConfiguration") as Dictionary<string, object>),
					ModSettings = MyModSettings.FromDictionary(map.GetValueOrDefault("ModSettings") as Dictionary<string, object>),
				};
			}

			/// <summary>
			/// Validates this configuration<br />
			/// Any invalid values will be replaced with defaults
			/// </summary>
			public void Validate()
			{
				MyGlobalModConfiguration defaults = MyGlobalModConfiguration.Defaults();
				this.LocalModConfiguration = this.LocalModConfiguration ?? defaults.LocalModConfiguration;
				this.ModSettings = this.ModSettings ?? defaults.ModSettings;
				this.LocalModConfiguration.Validate();
				this.ModSettings.Validate();
			}

			/// <summary>
			/// Clones this configuration
			/// </summary>
			/// <returns>The cloned configuration</returns>
			public MyGlobalModConfiguration Clone()
			{
				return new MyGlobalModConfiguration {
					LocalModConfiguration = this.LocalModConfiguration?.Clone(),
					ModSettings = this.ModSettings?.Clone(),
				};
			}

			/// <summary>
			/// Saves this configuration to a dictionary
			/// </summary>
			/// <returns>The configuration as a dictionary</returns>
			public Dictionary<string, object> ToDictionary()
			{
				return new Dictionary<string, object> {
					["LocalModConfiguration"] = this.LocalModConfiguration?.ToDictionary(),
					["ModSettings"] = this.ModSettings.ToDictionary(),
				};
			}
			#endregion
		}
		#endregion

		#region Local Configurations
		public sealed class MyLocalCapacitorConfiguration
		{
			/// <summary>
			/// The maximum possible charge (in Mega-Watts) this capacitor can hold
			/// </summary>
			public readonly double MaxCapacitorChargeMW;

			/// <summary>
			/// The maximum possible charge rate (in Mega-Watts per second) this capacitor can charge at
			/// </summary>
			public readonly float MaxCapacitorChargeRateMW;

			/// <summary>
			/// The power transfer efficiency of this capacitor when charging
			/// </summary>
			public readonly float CapacitorChargeEfficiency;

			/// <summary>
			/// The power transfer efficiency of this capacitor when discharging
			/// </summary>
			public readonly float CapacitorDischargeEfficiency;

			internal MyLocalCapacitorConfiguration(MyModCapacitorConfiguration configuration, MyCubeSize grid_size)
			{
				switch (grid_size)
				{
					case MyCubeSize.Small:
						this.MaxCapacitorChargeMW = configuration.MaxSmallCapacitorChargeMW.Value;
						this.MaxCapacitorChargeRateMW = configuration.MaxSmallCapacitorChargeRateMW.Value;
						this.CapacitorChargeEfficiency = configuration.SmallCapacitorChargeEfficiency.Value;
						this.CapacitorDischargeEfficiency = configuration.SmallCapacitorDischargeEfficiency.Value;
						break;
					case MyCubeSize.Large:
						this.MaxCapacitorChargeMW = configuration.MaxLargeCapacitorChargeMW.Value;
						this.MaxCapacitorChargeRateMW = configuration.MaxLargeCapacitorChargeRateMW.Value;
						this.CapacitorChargeEfficiency = configuration.LargeCapacitorChargeEfficiency.Value;
						this.CapacitorDischargeEfficiency = configuration.LargeCapacitorDischargeEfficiency.Value;
						break;
					default:
						throw new InvalidOperationException("Illegal grid size during configuration");
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

		public sealed class MyLocalDriveConfiguration
		{
			/// <summary>
			/// The maximum distance (in meters) a this drive can raycast to check gate intersections
			/// </summary>
			public readonly double DriveRaycastDistance;

			/// <summary>
			/// The maximum offset (in meters) in which rays are considered to be overlapping for this drive
			/// </summary>
			public readonly float DriveRaycastWidth;

			/// <summary>
			/// The maximum charge (in Mega-Watts) a this drive can store
			/// </summary>
			public readonly double MaxDriveChargeMW;

			/// <summary>
			/// The maximum charge rate (in Mega-Watts per second) a this drive can charge at
			/// </summary>
			public readonly float MaxDriveChargeRateMW;

			/// <summary>
			/// The base input wattage (in Mega-Watts) for this drive while idle
			/// </summary>
			public readonly float BaseIdleInputWattageMW;

			/// <summary>
			/// The base input wattage (in Mega-Watts) for this drive while active
			/// </summary>
			public readonly float BaseActiveInputWattageMW;

			/// <summary>
			/// The power transfer efficiency of this drive when charging
			/// </summary>
			public readonly float DriveChargeEfficiency;

			internal MyLocalDriveConfiguration(MyModDriveConfiguration configuration, MyCubeSize grid_size)
			{
				switch (grid_size)
				{
					case MyCubeSize.Small:
						this.DriveRaycastDistance = configuration.SmallDriveRaycastDistance.Value;
						this.DriveRaycastWidth = configuration.SmallDriveRaycastWidth.Value;
						this.MaxDriveChargeMW = configuration.MaxSmallDriveChargeMW.Value;
						this.MaxDriveChargeRateMW = configuration.MaxSmallDriveChargeRateMW.Value;
						this.BaseIdleInputWattageMW = configuration.SmallDriveIdleInputWattageMW.Value;
						this.BaseActiveInputWattageMW = configuration.SmallDriveActiveInputWattageMW.Value;
						this.DriveChargeEfficiency = configuration.SmallDriveChargeEfficiency.Value;
						break;
					case MyCubeSize.Large:
						this.DriveRaycastDistance = configuration.LargeDriveRaycastDistance.Value;
						this.DriveRaycastWidth = configuration.LargeDriveRaycastWidth.Value;
						this.MaxDriveChargeMW = configuration.MaxLargeDriveChargeMW.Value;
						this.MaxDriveChargeRateMW = configuration.MaxLargeDriveChargeRateMW.Value;
						this.BaseIdleInputWattageMW = configuration.LargeDriveIdleInputWattageMW.Value;
						this.BaseActiveInputWattageMW = configuration.LargeDriveActiveInputWattageMW.Value;
						this.DriveChargeEfficiency = configuration.LargeDriveChargeEfficiency.Value;
						break;
					default:
						throw new InvalidOperationException("Illegal grid size during configuration");
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

		public sealed class MyLocalJumpGateConfiguration
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
			public readonly float UntetheredJumpEnergyCost;

			/// <summary>
			/// The maximum jump gate reachable distance (in Kilo-Meters) for a 50 drive jump gate matching this gate's grid size
			/// </summary>
			public readonly double MaxJumpGate50Distance;

			/// <summary>
			/// The exponent 'b' used for power factor calculations: (a * x - a) ^ b + 1<br />
			/// Specifies how quickly the power factor changes given how close this gate is to its reasonable distance
			/// </summary>
			public readonly float GatePowerFactorExponent;

			/// <summary>
			/// The value 'a' used for power factor calculations for this jump gate: (a * x - a) ^ b + 1<br />
			/// Specifies the narrowness of the curve, higher values result in a narrower curve
			/// </summary>
			public readonly float GatePowerFalloffFactor;

			/// <summary>
			/// The power (in Kilo-Watts) required for this gate to jump one kilogram of mass
			/// </summary>
			public readonly float GateKilowattPerKilogram;

			/// <summary>
			/// The maximum distance (in Kilo-Meters) per kilometer an endpoint can be offset by if untethered for this gate
			/// </summary>
			public readonly float GateRandomOffsetPerKilometer;

			/// <summary>
			/// The maximum distance (in Kilo-Meters) per unit of gravity 'g' an endpoint will be offset by if untethered for this gate
			/// </summary>
			public readonly float GateKilometerOffsetPerUnitG;

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
			/// Whether ships jumping to an untethered destination have randomness applied to each ship or once per the entire fleet
			/// </summary>
			public readonly bool ConfineUntetheredSpread;

			/// <summary>
			/// The multiplier used to determine this jump gate's explosion size given gate power
			/// </summary>
			public readonly float ExplosionDamageMultiplier;

			/// <summary>
			/// The percentage of gate drives that must be disabled before the gate detonates
			/// </summary>
			public readonly float ExplosionDamagePercent;

			/// <summary>
			/// The radius (in meters) in which this jump gate can randomly displace its jumped entities when untethered
			/// </summary>
			public readonly float GateRandomDisplacementRadius;

			/// <summary>
			/// The minimum percentage of this jump gate's drives that must be owned by the controller owner's faction to allow binding
			/// </summary>
			public readonly float MinimumControlOwnerFactionRatio;

			/// <summary>
			/// The power multiplier when opening a sustained wormhole
			/// </summary>
			public readonly float GateWormholePowerMultiplier = 1.5f;

			/// <summary>
			/// Whether this jump gate can be activated as wormholes for sustained jumps
			/// </summary>
			public readonly bool AllowWormholeActivation = true;

			/// <summary>
			/// The maximum amount of time this jump gate can remain open as a wormhole for sustained jumps, in seconds
			/// </summary>
			public readonly float MaxWormholeDurationSeconds = 2280;

			/// <summary>
			/// The multiplier to apply to jump gates for every second above the time limit a gate has been open as a wormhole for sustained jumps
			/// </summary>
			public readonly float OverrunPowerMultiplierPerSecond = 0;

			/// <summary>
			/// The percent of gravity from the connected jump gate to apply to this jump gate when using sustained jumps
			/// </summary>
			public readonly float GravityPassthroughMultiplier = 0;

			/// <summary>
			/// Whether to allow the jump gate wormholes to "jump" when fed enough energy in the form of an explosion<br />
			/// This mimics the ability for stargates to jump
			/// </summary>
			public readonly bool AllowWormholeStargateJumps = false;

			/// <summary>
			/// Whether wormholes can jump to gates outside of owner gate's faction
			/// </summary>
			public readonly bool AllowWormholeJumpsOutsideFaction = false;

			/// <summary>
			/// Whether wormholes can jump to standard, non-wormhole jump gates
			/// </summary>
			public readonly bool AllowWormholeJumpsToStandardGate = false;

			/// <summary>
			/// The amount of meters a jump gate wormhole can jump per unit of explosion power when "stargate jumping" is enabled
			/// </summary>
			public readonly float JumpDistancePerExplosionPower = 1;

			internal MyLocalJumpGateConfiguration(MyModJumpGateConfiguration configuration, MyCubeSize grid_size)
			{
				switch (grid_size)
				{
					case MyCubeSize.Small:
						this.MinimumJumpDistance = configuration.MinimumSmallJumpDistance.Value;
						this.MaximumJumpDistance = configuration.MaximumSmallJumpDistance.Value;
						this.UntetheredJumpEnergyCost = configuration.UntetheredSmallJumpEnergyCost.Value;
						this.MaxJumpGate50Distance = configuration.MaxSmallJumpGate50Distance.Value;
						this.GatePowerFactorExponent = configuration.SmallGatePowerFactorExponent.Value;
						this.GatePowerFalloffFactor = configuration.SmallGatePowerFalloffFactor.Value;
						this.GateKilowattPerKilogram = configuration.SmallGateKilowattPerKilogram.Value;
						this.GateRandomOffsetPerKilometer = configuration.SmallGateRandomOffsetPerKilometer.Value;
						this.GateKilometerOffsetPerUnitG = configuration.SmallGateKilometerOffsetPerUnitG.Value;
						this.GateDistanceScaleExponent = configuration.SmallGateDistanceScaleExponent;
						this.ChargingDriveEffectorForceN = configuration.ChargingSmallDriveEffectorForceN.Value;
						this.ExplosionDamageMultiplier = configuration.JumpGateExplosionConfiguration.SmallGateExplosionDamageMultiplier.Value;
						this.ExplosionDamagePercent = configuration.JumpGateExplosionConfiguration.SmallGateExplosionDamagePercent.Value;
						this.GateRandomDisplacementRadius = configuration.SmallGateRandomDisplacementRadius.Value;
						this.GateWormholePowerMultiplier = configuration.JumpGateWormholeConfiguration.SmallGateWormholePowerMultiplier.Value;
						this.AllowWormholeActivation = configuration.JumpGateWormholeConfiguration.AllowSmallGateWormholeActivation.Value;
						this.MaxWormholeDurationSeconds = configuration.JumpGateWormholeConfiguration.MaxSmallGateWormholeDurationSeconds.Value;
						this.JumpDistancePerExplosionPower = configuration.JumpGateWormholeConfiguration.SmallGateJumpDistancePerExplosionPower.Value;
						this.AllowWormholeStargateJumps = configuration.JumpGateWormholeConfiguration.AllowSmallWormholeStargateJumps.Value;
						break;
					case MyCubeSize.Large:
						this.MinimumJumpDistance = configuration.MinimumLargeJumpDistance.Value;
						this.MaximumJumpDistance = configuration.MaximumLargeJumpDistance.Value;
						this.UntetheredJumpEnergyCost = configuration.UntetheredLargeJumpEnergyCost.Value;
						this.MaxJumpGate50Distance = configuration.MaxLargeJumpGate50Distance.Value;
						this.GatePowerFactorExponent = configuration.LargeGatePowerFactorExponent.Value;
						this.GatePowerFalloffFactor = configuration.LargeGatePowerFalloffFactor.Value;
						this.GateKilowattPerKilogram = configuration.LargeGateKilowattPerKilogram.Value;
						this.GateRandomOffsetPerKilometer = configuration.LargeGateRandomOffsetPerKilometer.Value;
						this.GateKilometerOffsetPerUnitG = configuration.LargeGateKilometerOffsetPerUnitG.Value;
						this.GateDistanceScaleExponent = configuration.LargeGateDistanceScaleExponent;
						this.ChargingDriveEffectorForceN = configuration.ChargingLargeDriveEffectorForceN.Value;
						this.ExplosionDamageMultiplier = configuration.JumpGateExplosionConfiguration.LargeGateExplosionDamageMultiplier.Value;
						this.ExplosionDamagePercent = configuration.JumpGateExplosionConfiguration.LargeGateExplosionDamagePercent.Value;
						this.GateRandomDisplacementRadius = configuration.LargeGateRandomDisplacementRadius.Value;
						this.GateWormholePowerMultiplier = configuration.JumpGateWormholeConfiguration.LargeGateWormholePowerMultiplier.Value;
						this.AllowWormholeActivation = configuration.JumpGateWormholeConfiguration.AllowLargeGateWormholeActivation.Value;
						this.MaxWormholeDurationSeconds = configuration.JumpGateWormholeConfiguration.MaxLargeGateWormholeDurationSeconds.Value;
						this.JumpDistancePerExplosionPower = configuration.JumpGateWormholeConfiguration.LargeGateJumpDistancePerExplosionPower.Value;
						this.AllowWormholeStargateJumps = configuration.JumpGateWormholeConfiguration.AllowLargeWormholeStargateJumps.Value;
						break;
					default:
						throw new InvalidOperationException("Illegal grid size during configuration");
				}

				this.IgnoreDockedGrids = configuration.IgnoreDockedGrids.Value;
				this.ConfineUntetheredSpread = configuration.ConfineUntetheredSpread.Value;
				this.MinimumControlOwnerFactionRatio = configuration.MinimumControlOwnerFactionRatio.Value;
				this.OverrunPowerMultiplierPerSecond = configuration.JumpGateWormholeConfiguration.OverrunPowerMultiplierPerSecond.Value;
				this.GravityPassthroughMultiplier = configuration.JumpGateWormholeConfiguration.GravityPassthroughMultiplier.Value;
				this.AllowWormholeJumpsOutsideFaction = configuration.JumpGateWormholeConfiguration.AllowWormholeJumpsOutsideFaction.Value;
				this.AllowWormholeJumpsToStandardGate = configuration.JumpGateWormholeConfiguration.AllowWormholeJumpsToStandardGate.Value;
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
					["ConfineUntetheredSpread"] = this.ConfineUntetheredSpread,
					["ExplosionDamageMultiplier"] = this.ExplosionDamageMultiplier,
					["ExplosionDamagePercent"] = this.ExplosionDamagePercent,
					["GateRandomDisplacementRadius"] = this.GateRandomDisplacementRadius,
					["MinimumControlOwnerFactionRatio"] = this.MinimumControlOwnerFactionRatio,
					["GateWormholePowerMultiplier"] = this.GateWormholePowerMultiplier,
					["AllowWormholeActivation"] = this.AllowWormholeActivation,
					["MaxWormholeDurationSeconds"] = this.MaxWormholeDurationSeconds,
					["OverrunPowerMultiplierPerSecond"] = this.OverrunPowerMultiplierPerSecond,
					["GravityPassthroughMultiplier"] = this.GravityPassthroughMultiplier,
					["AllowWormholeStargateJumps"] = this.AllowWormholeStargateJumps,
					["AllowWormholeJumpsOutsideFaction"] = this.AllowWormholeJumpsOutsideFaction,
					["AllowWormholeJumpsToStandardGate"] = this.AllowWormholeJumpsToStandardGate,
					["JumpDistancePerExplosionPower"] = this.JumpDistancePerExplosionPower,
				};
			}
		}
		#endregion

		#region Public Static Variables
		/// <summary>
		/// The file name of the world-local mod configuration file
		/// </summary>
		public static string ModLocalConfigurationFile => "LocalModConfigurationV1.xml";

		/// <summary>
		/// The file name of the global mod configuraton file
		/// </summary>
		public static string ModGlobalConfigurationFile => "GlobalModConfigurationV1.xml";

		/// <summary>
		/// The variable name used to sync server/client config values
		/// </summary>
		public static string ConfigVariableName => $"{MyJumpGateModSession.Instance.ModID}.ServerModConfig";
		#endregion

		#region Private Variables
		/// <summary>
		/// The local mod configuration as loaded from file
		/// </summary>
		private MyLocalModConfiguration LocalModConfiguration;

		/// <summary>
		/// The global mod configuration as loaded from file
		/// </summary>
		private MyGlobalModConfiguration GlobalModConfiguration;

		/// <summary>
		/// The large grid capacitor configuration
		/// </summary>
		private MyLocalCapacitorConfiguration LargeGridCapacitorConfiguration;

		/// <summary>
		/// The small grid capacitor configuration
		/// </summary>
		private MyLocalCapacitorConfiguration SmallGridCapacitorConfiguration;

		/// <summary>
		/// The large grid drive configuration
		/// </summary>
		private MyLocalDriveConfiguration LargeGridDriveConfiguration;

		/// <summary>
		/// The small grid drive configuration
		/// </summary>
		private MyLocalDriveConfiguration SmallGridDriveConfiguration;

		/// <summary>
		/// The large grid jump gate configuration
		/// </summary>
		private MyLocalJumpGateConfiguration LargeGridJumpGateConfiguration;

		/// <summary>
		/// The small grid jump gate configuration
		/// </summary>
		private MyLocalJumpGateConfiguration SmallGridJumpGateConfiguration;
		#endregion

		#region Public Variables
		/// <summary>
		/// The finalized capacitor configuration as loaded from files
		/// </summary>
		public MyModCapacitorConfiguration CapacitorConfiguration { get; private set; }

		/// <summary>
		/// The finalized drive configuration as loaded from files
		/// </summary>
		public MyModDriveConfiguration DriveConfiguration { get; private set; }

		/// <summary>
		/// The finalized jump gate configuration as loaded from files
		/// </summary>
		public MyModJumpGateConfiguration JumpGateConfiguration { get; private set; }

		/// <summary>
		/// The finalized construct configuration as loaded from files
		/// </summary>
		public MyModConstructConfiguration ConstructConfiguration { get; private set; }

		/// <summary>
		/// The finalized general configuration as loaded from files
		/// </summary>
		public MyModGeneralConfiguration GeneralConfiguration { get; private set; }

		/// <summary>
		/// The finlized mod settings as loaded from file
		/// </summary>
		public MyModSettings ModSettings { get; private set; }
		#endregion

		#region Private Static Methods
		/// <summary>
		/// Checks if the specified byte value is valid
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <param name="default_">The default value if the provided value is invalid</param>
		/// <param name="min">The minimum allowed value or null if no minimum</param>
		/// <param name="max">The maximum allowed value or null if no maximum</param>
		/// <param name="allow_nan">Whether to allow a NaN value</param>
		/// <param name="allow_inf">Whether to allow an infinite value</param>
		/// <returns>The specified value, clamped within min and max if applicable, or the default if not valid</returns>
		private static byte ValidateValue(byte? value, byte? default_, byte? min = null, byte? max = null)
		{
			byte result = value ?? default_ ?? default(byte);
			if (min != null) value = Math.Max(min.Value, result);
			if (max != null) value = Math.Min(max.Value, result);
			return result;
		}

		/// <summary>
		/// Checks if the specified uint value is valid
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <param name="default_">The default value if the provided value is invalid</param>
		/// <param name="min">The minimum allowed value or null if no minimum</param>
		/// <param name="max">The maximum allowed value or null if no maximum</param>
		/// <param name="allow_nan">Whether to allow a NaN value</param>
		/// <param name="allow_inf">Whether to allow an infinite value</param>
		/// <returns>The specified value, clamped within min and max if applicable, or the default if not valid</returns>
		private static uint ValidateValue(uint? value, uint? default_, uint? min = null, uint? max = null)
		{
			uint result = value ?? default_ ?? default(uint);
			if (min != null) value = Math.Max(min.Value, result);
			if (max != null) value = Math.Min(max.Value, result);
			return result;
		}

		/// <summary>
		/// Checks if the specified float value is valid
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <param name="default_">The default value if the provided value is invalid</param>
		/// <param name="min">The minimum allowed value or null if no minimum</param>
		/// <param name="max">The maximum allowed value or null if no maximum</param>
		/// <param name="allow_nan">Whether to allow a NaN value</param>
		/// <param name="allow_inf">Whether to allow an infinite value</param>
		/// <returns>The specified value, clamped within min and max if applicable, or the default if not valid</returns>
		private static float ValidateValue(float? value, float? default_, float? min = null, float? max = null, bool allow_nan = false, bool allow_inf = false)
		{
			float result = value ?? default_ ?? default(float);
			if (!allow_nan && float.IsNaN(result) || !allow_inf && float.IsInfinity(result)) value = default_;
			if (min != null) value = Math.Max(min.Value, result);
			if (max != null) value = Math.Min(max.Value, result);
			return result;
		}

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
		private static double ValidateValue(double? value, double? default_, double? min = null, double? max = null, bool allow_nan = false, bool allow_inf = false)
		{
			double result = value ?? default_ ?? default(double);
			if (!allow_nan && double.IsNaN(result) || !allow_inf && double.IsInfinity(result)) value = default_;
			if (min != null) value = Math.Max(min.Value, result);
			if (max != null) value = Math.Min(max.Value, result);
			return result;
		}
		#endregion

		#region Internal Static Methods
		/// <summary>
		/// Removes the config variable from SBC
		/// </summary>
		internal static void Dispose()
		{
			MyAPIGateway.Utilities.RemoveVariable(MyModConfigurationV1.ConfigVariableName);
		}

		/// <summary>
		/// Loads the finalized mod configuration from local and world storage
		/// </summary>
		/// <param name="instance">The session instance</param>
		/// <param name="global_file_exists">Whether the global file exists</param>
		/// <param name="local_file_exists">Whether the local file exists</param>
		/// <param name="global_load_err">The global file loading error</param>
		/// <param name="local_load_err">The local file loading error</param>
		/// <returns>The finalized configuration</returns>
		internal static MyModConfigurationV1 Load(MyJumpGateModSession instance, out bool global_file_exists, out bool local_file_exists, out Exception global_load_err, out Exception local_load_err)
		{
			string filename;
			global_load_err = null;
			local_load_err = null;
			TextReader reader = null;
			MyGlobalModConfiguration global_config;
			MyLocalModConfiguration local_config;

			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				string encoded;

				if (!(global_file_exists = local_file_exists = MyAPIGateway.Utilities.GetVariable(MyModConfigurationV1.ConfigVariableName, out encoded)))
				{
					Logger.Critical($"Failed to locate server config variable");
					throw new NullReferenceException("Server mod configuration does not exist");
				}

				try
				{
					MyGlobalModConfiguration packed_config = MyAPIGateway.Utilities.SerializeFromBinary<MyGlobalModConfiguration>(Convert.FromBase64String(encoded));
					Logger.Log("Mod-local server config loaded");
					return new MyModConfigurationV1 {
						LocalModConfiguration = packed_config.LocalModConfiguration,
						GlobalModConfiguration = packed_config,
						CapacitorConfiguration = packed_config.LocalModConfiguration.CapacitorConfiguration,
						DriveConfiguration = packed_config.LocalModConfiguration.DriveConfiguration,
						JumpGateConfiguration = packed_config.LocalModConfiguration.JumpGateConfiguration,
						ConstructConfiguration = packed_config.LocalModConfiguration.ConstructConfiguration,
						GeneralConfiguration = packed_config.LocalModConfiguration.GeneralConfiguration,
						ModSettings = packed_config.ModSettings,
					};
				}
				catch (Exception err)
				{
					Logger.Critical($"Failed to load mod-local config file from server\n\t...\n{err}");
					throw err;
				}
			}

			if (global_file_exists = MyAPIGateway.Utilities.FileExistsInLocalStorage(filename = MyModConfigurationV1.ModGlobalConfigurationFile, instance.GetType()))
			{
				try
				{
					reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(filename, instance.GetType());
					global_config = MyAPIGateway.Utilities.SerializeFromXML<MyGlobalModConfiguration>(reader.ReadToEnd());
					global_config = global_config ?? MyGlobalModConfiguration.Defaults();
				}
				catch (Exception err)
				{
					global_load_err = err;
					global_config = MyGlobalModConfiguration.Defaults();
				}
				finally
				{
					reader?.Close();
				}
			}
			else global_config = MyGlobalModConfiguration.Defaults();

			if (local_file_exists = MyAPIGateway.Utilities.FileExistsInWorldStorage(filename = MyModConfigurationV1.ModLocalConfigurationFile, instance.GetType()))
			{
				try
				{
					reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(filename, instance.GetType());
					local_config = MyAPIGateway.Utilities.SerializeFromXML<MyLocalModConfiguration>(reader.ReadToEnd());
					local_config = local_config ?? MyLocalModConfiguration.Defaults();
				}
				catch (Exception err)
				{
					local_load_err = err;
					local_config = MyLocalModConfiguration.Defaults();
				}
				finally
				{
					reader?.Close();
				}
			}
			else local_config = MyLocalModConfiguration.Defaults();

			MyLocalModConfiguration final_local_config = global_config.LocalModConfiguration.Clone();
			final_local_config.Overlay(local_config);
			MyModConfigurationV1 config = new MyModConfigurationV1 {
				LocalModConfiguration = local_config,
				GlobalModConfiguration = global_config,
				CapacitorConfiguration = final_local_config.CapacitorConfiguration,
				DriveConfiguration = final_local_config.DriveConfiguration,
				JumpGateConfiguration = final_local_config.JumpGateConfiguration,
				ConstructConfiguration = final_local_config.ConstructConfiguration,
				GeneralConfiguration = final_local_config.GeneralConfiguration,
				ModSettings = global_config.ModSettings,
			};
			config.Validate();
			MyAPIGateway.Utilities.SetVariable(MyModConfigurationV1.ConfigVariableName, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(new MyGlobalModConfiguration {
				LocalModConfiguration = local_config,
				ModSettings = config.ModSettings,
			})));
			return config;
		}

		internal static MyModConfigurationV1 FromConfigurationV0(Configuration configuration)
		{
			if (configuration == null || MyNetworkInterface.IsStandaloneMultiplayerClient) return null;

			MyLocalModConfiguration local_config = new MyLocalModConfiguration {
				CapacitorConfiguration = new MyModCapacitorConfiguration {
					MaxLargeCapacitorChargeMW = configuration.CapacitorConfiguration.MaxLargeCapacitorChargeMW,
					MaxSmallCapacitorChargeMW = configuration.CapacitorConfiguration.MaxSmallCapacitorChargeMW,
					MaxLargeCapacitorChargeRateMW = (float) configuration.CapacitorConfiguration.MaxLargeCapacitorChargeRateMW,
					MaxSmallCapacitorChargeRateMW = (float) configuration.CapacitorConfiguration.MaxSmallCapacitorChargeRateMW,
					LargeCapacitorChargeEfficiency = (float) configuration.CapacitorConfiguration.LargeCapacitorChargeEfficiency,
					SmallCapacitorChargeEfficiency = (float) configuration.CapacitorConfiguration.SmallCapacitorChargeEfficiency,
					LargeCapacitorDischargeEfficiency = (float) configuration.CapacitorConfiguration.LargeCapacitorDischargeEfficiency,
					SmallCapacitorDischargeEfficiency = (float) configuration.CapacitorConfiguration.SmallCapacitorDischargeEfficiency,
				},
				DriveConfiguration = new MyModDriveConfiguration {
					LargeDriveRaycastDistance = configuration.DriveConfiguration.LargeDriveRaycastDistance,
					SmallDriveRaycastDistance = configuration.DriveConfiguration.SmallDriveRaycastDistance,
					LargeDriveRaycastWidth = (float) configuration.DriveConfiguration.LargeDriveRaycastWidth,
					SmallDriveRaycastWidth = (float) configuration.DriveConfiguration.SmallDriveRaycastWidth,
					MaxLargeDriveChargeMW = configuration.DriveConfiguration.MaxLargeDriveChargeMW,
					MaxSmallDriveChargeMW = configuration.DriveConfiguration.MaxSmallDriveChargeMW,
					MaxLargeDriveChargeRateMW = (float) configuration.DriveConfiguration.MaxLargeDriveChargeRateMW,
					MaxSmallDriveChargeRateMW = (float) configuration.DriveConfiguration.MaxSmallDriveChargeRateMW,
					LargeDriveIdleInputWattageMW = (float) configuration.DriveConfiguration.LargeDriveIdleInputWattageMW,
					SmallDriveIdleInputWattageMW = (float) configuration.DriveConfiguration.SmallDriveIdleInputWattageMW,
					LargeDriveActiveInputWattageMW = (float) configuration.DriveConfiguration.LargeDriveActiveInputWattageMW,
					SmallDriveActiveInputWattageMW = (float) configuration.DriveConfiguration.SmallDriveActiveInputWattageMW,
					LargeDriveChargeEfficiency = (float) configuration.DriveConfiguration.LargeDriveChargeEfficiency,
					SmallDriveChargeEfficiency = (float) configuration.DriveConfiguration.SmallDriveChargeEfficiency,
				},
				JumpGateConfiguration = new MyModJumpGateConfiguration {
					MinimumLargeJumpDistance = configuration.JumpGateConfiguration.MinimumLargeJumpDistance,
					MinimumSmallJumpDistance = configuration.JumpGateConfiguration.MinimumSmallJumpDistance,
					MaximumLargeJumpDistance = configuration.JumpGateConfiguration.MaximumLargeJumpDistance,
					MaximumSmallJumpDistance = configuration.JumpGateConfiguration.MaximumSmallJumpDistance,
					UntetheredLargeJumpEnergyCost = (float) configuration.JumpGateConfiguration.UntetheredLargeJumpEnergyCost,
					UntetheredSmallJumpEnergyCost = (float) configuration.JumpGateConfiguration.UntetheredSmallJumpEnergyCost,
					MaxLargeJumpGate50Distance = configuration.JumpGateConfiguration.MaxLargeJumpGate50Distance,
					MaxSmallJumpGate50Distance = configuration.JumpGateConfiguration.MaxSmallJumpGate50Distance,
					LargeGatePowerFactorExponent = (float) configuration.JumpGateConfiguration.LargeGatePowerFactorExponent,
					SmallGatePowerFactorExponent = (float) configuration.JumpGateConfiguration.SmallGatePowerFactorExponent,
					LargeGatePowerFalloffFactor = (float) configuration.JumpGateConfiguration.LargeGatePowerFalloffFactor,
					SmallGatePowerFalloffFactor = (float) configuration.JumpGateConfiguration.SmallGatePowerFalloffFactor,
					LargeGateKilowattPerKilogram = (float) configuration.JumpGateConfiguration.LargeGateKilowattPerKilogram,
					SmallGateKilowattPerKilogram = (float) configuration.JumpGateConfiguration.SmallGateKilowattPerKilogram,
					LargeGateRandomOffsetPerKilometer = (float) configuration.JumpGateConfiguration.LargeGateRandomOffsetPerKilometer,
					SmallGateRandomOffsetPerKilometer = (float) configuration.JumpGateConfiguration.SmallGateRandomOffsetPerKilometer,
					LargeGateKilometerOffsetPerUnitG = (float) configuration.JumpGateConfiguration.LargeGateKilometerOffsetPerUnitG,
					SmallGateKilometerOffsetPerUnitG = (float) configuration.JumpGateConfiguration.SmallGateKilometerOffsetPerUnitG,
					LargeGateRandomDisplacementRadius = configuration.JumpGateConfiguration.LargeGateRandomDisplacementRadius,
					SmallGateRandomDisplacementRadius = configuration.JumpGateConfiguration.SmallGateRandomDisplacementRadius,
					MinimumControlOwnerFactionRatio = configuration.JumpGateConfiguration.MinimumControlOwnerFactionRatio,
					ConfineUntetheredSpread = configuration.JumpGateConfiguration.ConfineUntetheredSpread,
					IgnoreDockedGrids = configuration.JumpGateConfiguration.IgnoreDockedGrids,
					ChargingLargeDriveEffectorForceN = (float) configuration.JumpGateConfiguration.ChargingLargeDriveEffectorForceN,
					ChargingSmallDriveEffectorForceN = (float) configuration.JumpGateConfiguration.ChargingSmallDriveEffectorForceN,
					JumpGateExplosionConfiguration = new MyModJumpGateConfiguration.MyModJumpGateExplosionConfiguration {
						EnableGateExplosions = configuration.JumpGateConfiguration.EnableGateExplosions,
						LargeGateExplosionDamageMultiplier = configuration.JumpGateConfiguration.LargeGateExplosionDamageMultiplier,
						SmallGateExplosionDamageMultiplier = configuration.JumpGateConfiguration.SmallGateExplosionDamageMultiplier,
						LargeGateExplosionDamagePercent = configuration.JumpGateConfiguration.LargeGateExplosionDamagePercent,
						SmallGateExplosionDamagePercent = configuration.JumpGateConfiguration.SmallGateExplosionDamagePercent,
					},
					JumpGateWormholeConfiguration = new MyModJumpGateConfiguration.MyModJumpGateWormholeConfiguration {
						AllowLargeGateWormholeActivation = configuration.JumpGateConfiguration.AllowLargeGateWormholeActivation,
						AllowSmallGateWormholeActivation = configuration.JumpGateConfiguration.AllowSmallGateWormholeActivation,
						LargeGateWormholePowerMultiplier = configuration.JumpGateConfiguration.LargeGateWormholePowerMultiplier,
						SmallGateWormholePowerMultiplier = configuration.JumpGateConfiguration.SmallGateWormholePowerMultiplier,
						MaxLargeGateWormholeDurationSeconds = configuration.JumpGateConfiguration.MaxLargeGateWormholeDurationSeconds,
						MaxSmallGateWormholeDurationSeconds = configuration.JumpGateConfiguration.MaxSmallGateWormholeDurationSeconds,
						AllowLargeWormholeStargateJumps = null,
						AllowSmallWormholeStargateJumps = null,
						AllowWormholeJumpsOutsideFaction = null,
						AllowWormholeJumpsToStandardGate = null,
						OverrunPowerMultiplierPerSecond = null,
						LargeGateJumpDistancePerExplosionPower = null,
						SmallGateJumpDistancePerExplosionPower = null,
						GravityPassthroughMultiplier = null,
					},
				},
				ConstructConfiguration = new MyModConstructConfiguration {
					RequireGridCommLink = configuration.ConstructConfiguration.RequireGridCommLink,
					MaxTotalGatesPerConstruct = configuration.ConstructConfiguration.MaxTotalGatesPerConstruct,
					MaxLargeGatesPerConstruct = configuration.ConstructConfiguration.MaxLargeGatesPerConstruct,
					MaxSmallGatesPerConstruct = configuration.ConstructConfiguration.MaxSmallGatesPerConstruct,
				},
				GeneralConfiguration = new MyModGeneralConfiguration {
					DebugLogVerbosity = configuration.GeneralConfiguration.DebugLogVerbosity,
					DrawSyncDistance = configuration.GeneralConfiguration.DrawSyncDistance,
					LenientJumps = configuration.GeneralConfiguration.LenientJumps,
					HighlightShearBlocks = configuration.GeneralConfiguration.HighlightShearBlocks,
					SafeJumps = configuration.GeneralConfiguration.SafeJumps,
					ConcurrentGridUpdateThreads = configuration.GeneralConfiguration.ConcurrentGridUpdateThreads,
					SyphonReactorPower = configuration.GeneralConfiguration.SyphonReactorPower,
					ShowHiddenGPSMarkers = configuration.GeneralConfiguration.ShowHiddenGPSMarkers,
					AllowAdminWormholeDurationBypass = configuration.GeneralConfiguration.AllowAdminWormholeDurationBypass,
				},
			};

			MyGlobalModConfiguration global_config = new MyGlobalModConfiguration {
				LocalModConfiguration = new MyLocalModConfiguration(),
				ModSettings = new MyModSettings {
					MaxStoredModSpecificLogFiles = configuration.GeneralConfiguration.MaxStoredModSpecificLogFiles,
				},
			};

			MyModConfigurationV1 config = new MyModConfigurationV1 {
				LocalModConfiguration = local_config,
				GlobalModConfiguration = global_config,
				CapacitorConfiguration = local_config.CapacitorConfiguration,
				DriveConfiguration = local_config.DriveConfiguration,
				JumpGateConfiguration = local_config.JumpGateConfiguration,
				ConstructConfiguration = local_config.ConstructConfiguration,
				GeneralConfiguration = local_config.GeneralConfiguration,
				ModSettings = global_config.ModSettings,
			};

			config.Validate();

			if (MyNetworkInterface.IsMultiplayerServer)
			{
				MyAPIGateway.Utilities.SetVariable(MyModConfigurationV1.ConfigVariableName, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(new MyGlobalModConfiguration {
					LocalModConfiguration = config.LocalModConfiguration,
					ModSettings = config.ModSettings,
				})));
			}

			return config;
		}

		/// <summary>
		/// Loads the configuration from a dictionary
		/// </summary>
		/// <param name="map">The dictionary to read from</param>
		/// <returns>The loaded configuration values</returns>
		internal static MyModConfigurationV1 FromDictionary(Dictionary<string, object> map)
		{
			MyLocalModConfiguration local = MyLocalModConfiguration.FromDictionary(map);

			return new MyModConfigurationV1 {
				CapacitorConfiguration = local?.CapacitorConfiguration,
				DriveConfiguration = local?.DriveConfiguration,
				JumpGateConfiguration = local?.JumpGateConfiguration,
				ConstructConfiguration = local?.ConstructConfiguration,
				GeneralConfiguration = local?.GeneralConfiguration,
				ModSettings = MyModSettings.FromDictionary(map.GetValueOrDefault("ModSettings") as Dictionary<string, object>),
			};
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Saves this mod configuration to world storage<br />
		/// Mod settings will be saved to local storage
		/// </summary>
		/// <param name="instance">The session instance</param>
		/// <param name="global_save_err">The global file saving error</param>
		/// <param name="local_save_err">The local file saving error</param>
		internal void Save(MyJumpGateModSession instance, out Exception global_save_err, out Exception local_save_err)
		{
			global_save_err = null;
			local_save_err = null;
			TextWriter writer = null;
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;

			try
			{
				MyLocalModConfiguration local = new MyLocalModConfiguration {
					CapacitorConfiguration = this.CapacitorConfiguration,
					DriveConfiguration = this.DriveConfiguration,
					JumpGateConfiguration = this.JumpGateConfiguration,
					ConstructConfiguration= this.ConstructConfiguration,
					GeneralConfiguration = this.GeneralConfiguration,
				};
				writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(MyModConfigurationV1.ModLocalConfigurationFile, instance.GetType());
				writer.Write(MyAPIGateway.Utilities.SerializeToXML(local));
			}
			catch (Exception err)
			{
				local_save_err = err;
			}
			finally
			{
				writer?.Close();
			}

			try
			{
				MyGlobalModConfiguration global = new MyGlobalModConfiguration {
					LocalModConfiguration = this.GlobalModConfiguration.LocalModConfiguration,
					ModSettings = this.ModSettings,
				};
				writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(MyModConfigurationV1.ModGlobalConfigurationFile, instance.GetType());
				writer.Write(MyAPIGateway.Utilities.SerializeToXML(global));
			}
			catch (Exception err)
			{
				global_save_err = err;
			}
			finally
			{
				writer?.Close();
			}
		}

		/// <summary>
		/// Validates all configuration values
		/// </summary>
		internal void Validate()
		{
			this.CapacitorConfiguration.Validate();
			this.DriveConfiguration.Validate();
			this.JumpGateConfiguration.Validate();
			this.ConstructConfiguration.Validate();
			this.GeneralConfiguration.Validate();
			this.ModSettings.Validate();
		}

		/// <summary>
		/// Updates this configuration from the specified local configuration and mod settings
		/// </summary>
		/// <param name="local_configuration">The new local configuration</param>
		/// <param name="mod_settings">The new mod settings</param>
		internal void Update(MyLocalModConfiguration local_configuration, MyModSettings mod_settings)
		{
			if (local_configuration != null)
			{
				local_configuration.Overlay(MyLocalModConfiguration.Defaults());
				local_configuration.Validate();
				this.LocalModConfiguration = local_configuration;
				this.CapacitorConfiguration = local_configuration.CapacitorConfiguration;
				this.DriveConfiguration = local_configuration.DriveConfiguration;
				this.JumpGateConfiguration = local_configuration.JumpGateConfiguration;
				this.ConstructConfiguration = local_configuration.ConstructConfiguration;
				this.GeneralConfiguration = local_configuration.GeneralConfiguration;
				this.SmallGridCapacitorConfiguration = null;
				this.LargeGridCapacitorConfiguration = null;
				this.SmallGridDriveConfiguration = null;
				this.LargeGridDriveConfiguration = null;
				this.SmallGridJumpGateConfiguration = null;
				this.LargeGridJumpGateConfiguration = null;
			}

			if (mod_settings != null)
			{
				mod_settings.Validate();
				this.ModSettings = mod_settings;
			}

			if (MyNetworkInterface.IsMultiplayerServer)
			{
				MyAPIGateway.Utilities.SetVariable(MyModConfigurationV1.ConfigVariableName, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(new MyGlobalModConfiguration {
					LocalModConfiguration = this.LocalModConfiguration,
					ModSettings = this.ModSettings,
				})));
			}
		}

		/// <summary>
		/// Converts this configuration to a dictionary
		/// </summary>
		/// <returns>The dictionary</returns>
		internal Dictionary<string, object> ToDictionary()
		{
			return new Dictionary<string, object> {
				["CapacitorConfiguration"] = this.CapacitorConfiguration.ToDictionary(),
				["DriveConfiguration"] = this.DriveConfiguration.ToDictionary(),
				["JumpGateConfiguration"] = this.JumpGateConfiguration.ToDictionary(),
				["ConstructConfiguration"] = this.ConstructConfiguration.ToDictionary(),
				["GeneralConfiguration"] = this.GeneralConfiguration.ToDictionary(),
				["ModSettings"] = this.ModSettings.ToDictionary(),
			};
		}

		/// <summary>
		/// Gets the grid-size specific configuration for a capacitor
		/// </summary>
		/// <param name="capacitor">The jump gate capacitor block</param>
		/// <returns>The local configuration reference</returns>
		internal MyLocalCapacitorConfiguration GetCapacitorConfigurationForBlock(MyJumpGateCapacitor capacitor)
		{
			if (capacitor == null) return null;
			else if (capacitor.IsLargeGrid && this.LargeGridCapacitorConfiguration != null) return this.LargeGridCapacitorConfiguration;
			else if (capacitor.IsLargeGrid) return this.LargeGridCapacitorConfiguration = new MyLocalCapacitorConfiguration(this.CapacitorConfiguration, MyCubeSize.Large);
			else if (capacitor.IsSmallGrid && this.SmallGridCapacitorConfiguration != null) return this.SmallGridCapacitorConfiguration;
			else if (capacitor.IsSmallGrid) return this.SmallGridCapacitorConfiguration = new MyLocalCapacitorConfiguration(this.CapacitorConfiguration, MyCubeSize.Small);
			else throw new InvalidOperationException("Illegal grid size retrieving configuration");
		}

		/// <summary>
		/// Gets the grid-size specific configuration for a capacitor
		/// </summary>
		/// <param name="capacitor">The jump gate capacitor block</param>
		/// <returns>The local configuration reference</returns>
		internal MyLocalCapacitorConfiguration GetCapacitorConfigurationForBlock(IMyCubeBlock capacitor)
		{
			if (capacitor == null || capacitor.CubeGrid == null) return null;
			else if (capacitor.CubeGrid.GridSizeEnum == MyCubeSize.Large && this.LargeGridCapacitorConfiguration != null) return this.LargeGridCapacitorConfiguration;
			else if (capacitor.CubeGrid.GridSizeEnum == MyCubeSize.Large) return this.LargeGridCapacitorConfiguration = new MyLocalCapacitorConfiguration(this.CapacitorConfiguration, MyCubeSize.Large);
			else if (capacitor.CubeGrid.GridSizeEnum == MyCubeSize.Small && this.SmallGridCapacitorConfiguration != null) return this.SmallGridCapacitorConfiguration;
			else if (capacitor.CubeGrid.GridSizeEnum == MyCubeSize.Small) return this.SmallGridCapacitorConfiguration = new MyLocalCapacitorConfiguration(this.CapacitorConfiguration, MyCubeSize.Small);
			else throw new InvalidOperationException("Illegal grid size retrieving configuration");
		}

		/// <summary>
		/// Gets the grid-size specific configuration for a drive
		/// </summary>
		/// <param name="capacitor">The jump gate drive block</param>
		/// <returns>The local configuration reference</returns>
		internal MyLocalDriveConfiguration GetDriveConfigurationForBlock(MyJumpGateDrive drive)
		{
			if (drive == null) return null;
			else if (drive.IsLargeGrid && this.LargeGridDriveConfiguration != null) return this.LargeGridDriveConfiguration;
			else if (drive.IsLargeGrid) return this.LargeGridDriveConfiguration = new MyLocalDriveConfiguration(this.DriveConfiguration, MyCubeSize.Large);
			else if (drive.IsSmallGrid && this.SmallGridDriveConfiguration != null) return this.SmallGridDriveConfiguration;
			else if (drive.IsSmallGrid) return this.SmallGridDriveConfiguration = new MyLocalDriveConfiguration(this.DriveConfiguration, MyCubeSize.Small);
			else throw new InvalidOperationException("Illegal grid size retrieving configuration");
		}

		/// <summary>
		/// Gets the grid-size specific configuration for a drive
		/// </summary>
		/// <param name="capacitor">The jump gate drive block</param>
		/// <returns>The local configuration reference</returns>
		internal MyLocalDriveConfiguration GetDriveConfigurationForBlock(IMyCubeBlock drive)
		{
			if (drive == null) return null;
			else if (drive.CubeGrid.GridSizeEnum == MyCubeSize.Large && this.LargeGridDriveConfiguration != null) return this.LargeGridDriveConfiguration;
			else if (drive.CubeGrid.GridSizeEnum == MyCubeSize.Large) return this.LargeGridDriveConfiguration = new MyLocalDriveConfiguration(this.DriveConfiguration, MyCubeSize.Large);
			else if (drive.CubeGrid.GridSizeEnum == MyCubeSize.Small && this.SmallGridDriveConfiguration != null) return this.SmallGridDriveConfiguration;
			else if (drive.CubeGrid.GridSizeEnum == MyCubeSize.Small) return this.SmallGridDriveConfiguration = new MyLocalDriveConfiguration(this.DriveConfiguration, MyCubeSize.Small);
			else throw new InvalidOperationException("Illegal grid size retrieving configuration");
		}

		/// <summary>
		/// Gets the grid-size specific configuration for a jump gate
		/// </summary>
		/// <param name="capacitor">The jump gate</param>
		/// <returns>The local configuration reference</returns>
		internal MyLocalJumpGateConfiguration GetJumpGateConfigurationForGate(MyJumpGate jump_gate)
		{
			if (jump_gate == null) return null;
			else if (jump_gate.IsLargeGrid() && this.LargeGridJumpGateConfiguration != null) return this.LargeGridJumpGateConfiguration;
			else if (jump_gate.IsLargeGrid()) return this.LargeGridJumpGateConfiguration = new MyLocalJumpGateConfiguration(this.JumpGateConfiguration, MyCubeSize.Large);
			else if (jump_gate.IsSmallGrid() && this.SmallGridJumpGateConfiguration != null) return this.SmallGridJumpGateConfiguration;
			else if (jump_gate.IsSmallGrid()) return this.SmallGridJumpGateConfiguration = new MyLocalJumpGateConfiguration(this.JumpGateConfiguration, MyCubeSize.Small);
			else throw new InvalidOperationException("Illegal grid size retrieving configuration");
		}
		#endregion
	}
}
