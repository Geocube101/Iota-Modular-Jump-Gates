using IOTA.ModularJumpGates.Animation;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.JumpGateConstruct;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGates
{
	internal partial class MyJumpGate
	{
		#region JumpGate Classes
		/// <summary>
		/// Class holding a jump request
		/// </summary>
		[ProtoContract]
		private sealed class JumpGateInfo
		{
			public enum TypeEnum {
				JUMP_START, JUMP_FAIL, JUMP_SUCCESS,
				WORMHOLE_START, WORMHOLE_FAIL, WORMHOLE_OPEN,
				CLOSED,
				WORMHOLE_ENTITY_PREJUMP, WORMHOLE_ENTITY_JUMP_SUCCESS, WORMHOLE_ENTITY_JUMP_FAIL,
				WORMHOLE_ANTI_ENTITY_PREJUMP, WORMHOLE_ANTI_ENTITY_JUMP_SUCCESS, WORMHOLE_ANTI_ENTITY_JUMP_FAIL,
				WORMHOLE_JUMP_MOVE
			};

			#region Public Variables
			/// <summary>
			/// Whether the jump failure was a cancelled jump
			/// </summary>
			[ProtoMember(1)]
			public bool CancelOverride;

			/// <summary>
			/// The type of request
			/// </summary>
			[ProtoMember(2)]
			public TypeEnum Type;

			[ProtoMember(3)]
			public MyJumpTypeEnum JumpType;

			/// <summary>
			/// The gate's true endpoint
			/// </summary>
			[ProtoMember(4)]
			public Vector3D? TrueEndpoint = null;

			/// <summary>
			/// The JumpGateUUID of the targeted gate
			/// </summary>
			[ProtoMember(5)]
			public JumpGateUUID JumpGateID;

			/// <summary>
			/// The controller settings used to enact the jump
			/// </summary>
			[ProtoMember(6)]
			public MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings;

			/// <summary>
			/// The targeted jump gate controller settings
			/// </summary>
			[ProtoMember(7)]
			public MyJumpGateController.MyControllerBlockSettingsStruct TargetControllerSettings;

			/// <summary>
			/// The resulting message from the jump
			/// </summary>
			[ProtoMember(8)]
			public string ResultMessage;

			/// <summary>
			/// The entity batches for this jump
			/// </summary>
			[ProtoMember(9)]
			public List<string> EntityBatches;

			/// <summary>
			/// The entity warps for this jump
			/// </summary>
			[ProtoMember(10)]
			public List<string> EntityWarps;

			/// <summary>
			/// The entity ids for wormhole jumps
			/// </summary>
			[ProtoMember(11)]
			public List<long> EntityIds;
			#endregion

			#region Constructors
			/// <summary>
			/// Dummy default constructor for use with ProtoBuf
			/// </summary>
			public JumpGateInfo() { }
			#endregion
		}

		/// <summary>
		/// Class holding jump polling info
		/// </summary>
		[ProtoContract]
		private sealed class JumpGatePollingInfo
		{
			[ProtoMember(1)]
			public JumpGateUUID ThisGate;

			[ProtoMember(2)]
			public JumpGateUUID TargetGate;

			[ProtoMember(3)]
			public MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings;

			[ProtoMember(4)]
			public MyJumpGateController.MyControllerBlockSettingsStruct TargetControllerSettings;

			[ProtoMember(5)]
			public DateTime? WormholeStartTime;

			[ProtoMember(6)]
			public Vector3D TrueEndpoint;
		}

		/// <summary>
		/// Class storing information on the drive intersect planes
		/// </summary>
		private sealed class JumpDriveIntersectPlane
		{
			#region Public Variables
			/// <summary>
			/// The plane
			/// </summary>
			public PlaneD Plane;

			/// <summary>
			/// The plane's primary normal
			/// </summary>
			public Vector3D PlanePrimaryNormal;

			/// <summary>
			/// The drives used to build this plane
			/// </summary>
			public HashSet<MyJumpGateDrive> JumpGateDrives = new HashSet<MyJumpGateDrive>();

			/// <summary>
			/// The number of drivs axis aligned with this plane
			/// </summary>
			public int AlignedDrivesCount = 0;
			#endregion
		}

		/// <summary>
		/// Class holding power syphon functionality
		/// </summary>
		private sealed class GridPowerSyphon
		{
			#region Private Variables
			/// <summary>
			/// The gate syphoning power
			/// </summary>
			private readonly MyJumpGate JumpGate;

			/// <summary>
			/// The number of working drives for this gate
			/// </summary>
			private int WorkingDrivesCount;

			/// <summary>
			/// The remaining ticks in the syphon
			/// </summary>
			private ulong SyphonTimeTicks;

			/// <summary>
			/// The total amount of power to syphon in MegaWatts
			/// </summary>
			private double SyphonPower = -1;

			/// <summary>
			/// The remaining power to syphon in MegaWatts
			/// </summary>
			private double RemainingPower = -1;

			/// <summary>
			/// The callback to execute once syphon is complete
			/// </summary>
			private Action<bool> Callback;

			/// <summary>
			/// A temporary list of jump gate drives
			/// </summary>
			private readonly List<MyJumpGateDrive> JumpGateDrives = new List<MyJumpGateDrive>();
			#endregion

			#region Constructors
			/// <summary>
			/// Creates a new GridPowerSyphhon
			/// </summary>
			/// <param name="gate">The jump gate to attach to</param>
			/// <exception cref="ArgumentNullException">If the gate is null</exception>
			public GridPowerSyphon(MyJumpGate gate)
			{
				if (gate == null) throw new ArgumentNullException($"Jump gate is null");
				this.JumpGate = gate;
			}
			#endregion

			#region Public Methods
			/// <summary>
			/// Begins a power syphon from grid
			/// </summary>
			/// <param name="power_mw">The power to syphon in MegaWatts</param>
			/// <param name="ticks">The number of game ticks to syphon for</param>
			/// <param name="callback">The callback to execute once syphon is complete<br />Accepts one bool indicating whether the syphon was successfull</param>
			/// <param name="syphon_grid_only">Whether to only syphon grid power and ignore capacitors and drives</param>
			public void DoSyphonPower(double power_mw, ulong ticks, Action<bool> callback, bool syphon_grid_only = false)
			{
				MyJumpGateConstruct grid = this.JumpGate.JumpGateGrid;

				if (!this.IsValid())
				{
					callback(false);
					return;
				}
				else if (power_mw <= 0)
				{
					callback(true);
					return;
				}

				this.JumpGateDrives.AddRange(this.JumpGate.GetWorkingJumpGateDrives());
				double power_per_drive = power_mw / this.JumpGateDrives.Count;
				power_mw = (syphon_grid_only) ? power_mw : this.JumpGateDrives.Select((drive) => drive.DrainStoredCharge(power_per_drive)).Sum();

				if (power_mw <= 0)
				{
					callback(true);
					return;
				}

				power_mw = (syphon_grid_only) ? power_mw : grid.SyphonConstructCapacitorPower(power_mw);

				if (power_mw <= 0)
				{
					callback(true);
					return;
				}

				power_per_drive = power_mw / this.JumpGateDrives.Count / ticks;
				foreach (MyJumpGateDrive drive in this.JumpGateDrives) drive.SetWattageSinkOverride(power_per_drive);
				this.SyphonPower = power_mw;
				this.RemainingPower = power_mw;
				this.SyphonTimeTicks = ticks;
				this.WorkingDrivesCount = this.JumpGateDrives.Count;
				this.Callback = callback;
				this.JumpGateDrives.Clear();
			}

			/// <summary>
			/// Ticks this power syphon
			/// </summary>
			public void Tick()
			{
				if (this.SyphonPower <= 0 || !this.IsValid()) return;
				this.JumpGateDrives.AddRange(this.JumpGate.GetJumpGateDrives());
				this.WorkingDrivesCount = 0;
				double aquired_power = 0;

				foreach (MyJumpGateDrive drive in this.JumpGateDrives)
				{
					if (!drive.IsWorking) continue;
					aquired_power += drive.GetCurrentWattageSinkInput();
					++this.WorkingDrivesCount;
				}

				this.RemainingPower -= aquired_power;
				--this.SyphonTimeTicks;
				double power_per_Drive = this.RemainingPower / this.WorkingDrivesCount / this.SyphonTimeTicks;
				foreach (MyJumpGateDrive drive in this.JumpGateDrives) if (drive.IsWorking) drive.SetWattageSinkOverride(power_per_Drive);

				if (this.SyphonTimeTicks == 0)
				{
					foreach (MyJumpGateDrive drive in this.JumpGateDrives) drive.SetWattageSinkOverride(-1);
					this.Callback(this.RemainingPower <= 0);
					this.SyphonPower = -1;
				}

				this.JumpGateDrives.Clear();
			}

			/// <summary>
			/// </summary>
			/// <returns>True if this gate is not null and valid and this gate's grid is not null and valid</returns>
			public bool IsValid()
			{
				MyJumpGateConstruct grid = this.JumpGate.JumpGateGrid;
				return this.JumpGate != null && this.JumpGate.IsValid() && grid != null && grid.IsValid();
			}
			#endregion
		}

		[ProtoContract]
		private sealed class JumpGateDebugPayload
		{
			[ProtoMember(1)]
			public JumpGateUUID JumpGateUUID;
			[ProtoMember(2)]
			public byte DebugType;
			[ProtoMember(3)]
			private string F_MetaData;

			public void SetMetaData<T>(T data)
			{
				this.F_MetaData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary<T>(data));
			}

			public T GetMetaData<T>()
			{
				return MyAPIGateway.Utilities.SerializeFromBinary<T>(Convert.FromBase64String(this.F_MetaData));
			}
		}

		private sealed class JumpGateExplosionInfo
		{
			public double MaxExplosionPower;
			public long ExplosionDurationTicks;
			public long RemainingLife;

			public double CurrentExplosionPower => MathHelper.Lerp(this.MaxExplosionPower, 0, EasingFunctor.GetEaseResult((double) this.RemainingLife / this.ExplosionDurationTicks, EasingTypeEnum.EASE_OUT, EasingCurveEnum.QUADRATIC));

			public JumpGateExplosionInfo(ref MyExplosionInfo explosion)
			{
				this.ExplosionDurationTicks = (long) (explosion.LifespanMiliseconds / 1000d * 60d);
				this.MaxExplosionPower = explosion.Damage * (1d + explosion.PlayerDamage / 10d) * MathHelper.Lerp(explosion.ExplosionSphere.Radius, 0, EasingFunctor.GetEaseResult((double) this.RemainingLife / this.ExplosionDurationTicks, EasingTypeEnum.EASE_OUT, EasingCurveEnum.QUADRATIC));
				this.RemainingLife = this.ExplosionDurationTicks;
			}

			public bool Tick(long ticks = 1)
			{
				return (this.RemainingLife -= ticks) < 0;
			}
		}

		[ProtoContract]
		private sealed class JumpGateWormholeJumpInfo
		{
			[ProtoMember(1)]
			public double ExplosionBuffer;
			[ProtoMember(2)]
			public JumpGateUUID ThisGate;
			[ProtoMember(3)]
			public JumpGateUUID TargetGate;
			[ProtoMember(4)]
			public MyJumpGateController.MyControllerBlockSettingsStruct TargetControllerSettings;
		}
		#endregion
	}
}
