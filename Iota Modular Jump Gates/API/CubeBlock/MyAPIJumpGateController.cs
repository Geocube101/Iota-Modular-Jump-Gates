using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using System.Collections.Generic;
using VRageMath;

namespace IOTA.ModularJumpGates.API.CubeBlock
{
	public class MyAPIJumpGateController : MyAPICubeBlockBase
	{
		public sealed class ControllerBlockSettingsWrapper
		{
			internal MyJumpGateController Controller;
			internal MyJumpGateController.MyControllerBlockSettingsStruct BlockSettings;

			/// <summary>
			/// Whether this controller can automatically activate the attached jump gate
			/// </summary>
			public bool CanAutoActivate { get { return this.BlockSettings.CanAutoActivate(); } set { this.BlockSettings.CanAutoActivate(value); } }

			/// <summary>
			/// Whether this controller can accept inbound connections
			/// </summary>
			public bool CanBeInbound { get { return this.BlockSettings.CanBeInbound(); } set { this.BlockSettings.CanBeInbound(value); } }

			/// <summary>
			/// Whether this controller can initiate outbound connections
			/// </summary>
			public bool CanBeOutbound { get { return this.BlockSettings.CanBeOutbound(); } set { this.BlockSettings.CanBeOutbound(value); } }

			/// <summary>
			/// Whether unowned/factionless gates may jump to this one
			/// </summary>
			public bool CanAcceptUnowned { get { return this.BlockSettings.CanAcceptUnowned(); } set { this.BlockSettings.CanAcceptUnowned(value); } }

			/// <summary>
			/// Whether enemy gates may jump to this one
			/// </summary>
			public bool CanAcceptEnemy { get { return this.BlockSettings.CanAcceptEnemy(); } set { this.BlockSettings.CanAcceptEnemy(value); } }

			/// <summary>
			/// Whether neutral gates may jump to this one
			/// </summary>
			public bool CanAcceptNeutral { get { return this.BlockSettings.CanAcceptNeutral(); } set { this.BlockSettings.CanAcceptNeutral(value); } }

			/// <summary>
			/// Whether friendly gates may jump to this one
			/// </summary>
			public bool CanAcceptFriendly { get { return this.BlockSettings.CanAcceptFriendly(); } set { this.BlockSettings.CanAcceptFriendly(value); } }

			/// <summary>
			/// Whether owned gates may jump to this one
			/// </summary>
			public bool CanAcceptOwned { get { return this.BlockSettings.CanAcceptOwned(); } set { this.BlockSettings.CanAcceptOwned(value); } }

			/// <summary>
			/// Whether this gate has a vector normal override
			/// </summary>
			public bool HasVectorNormalOverride { get { return this.BlockSettings.HasVectorNormalOverride(); } set { this.BlockSettings.HasVectorNormalOverride(value); } }

			/// <summary>
			/// A bit flag enum representing what faction relations may jump to this gate
			/// </summary>
			public MyFactionDisplayType FactionDisplayType { get { return this.BlockSettings.CanAccept(); } set { this.BlockSettings.CanAccept(value); } }

			/// <summary>
			/// The maximum allowed radius (in meters) of this gate's jump space<br />
			/// For the current jump gate radius, see MyAPIJumpGate.JumpNodeRadius()
			/// </summary>
			public double JumpSpaceRadius
			{
				get
				{
					return this.BlockSettings.JumpSpaceRadius();
				}
				set
				{
					float raycast_distance = (float) ((this.Controller.IsLargeGrid) ? MyJumpGateModSession.Configuration.DriveConfiguration.LargeDriveRaycastDistance : MyJumpGateModSession.Configuration.DriveConfiguration.SmallDriveRaycastDistance);
					this.BlockSettings.JumpSpaceRadius(MathHelper.Clamp(value, 0, 2d * raycast_distance));
				}
			}

			/// <summary>
			/// The jump gate ellipsoid depth percent<br />
			/// A value of 1 makes a perfect sphere
			/// </summary>
			public double JumpSpaceDepthPercent { get { return this.BlockSettings.JumpSpaceDepthPercent(); } set { this.BlockSettings.JumpSpaceDepthPercent(value); } }

			/// <summary>
			/// The animation name used during gate activation
			/// </summary>
			public string JumpEffectName
			{
				get
				{
					return this.BlockSettings.JumpEffectAnimationName();
				}
				set
				{
					string current_animation = this.BlockSettings.JumpEffectAnimationName();
					HashSet<string> animation_names = MyAnimationHandler.GetAnimationNames();
					this.BlockSettings.JumpEffectAnimationName(animation_names.Contains(value) ? value : current_animation);
				}
			}

			/// <summary>
			/// The name of the attached jump gate
			/// </summary>
			public string JumpGateName { get { return this.BlockSettings.JumpGateName(); } set { this.BlockSettings.JumpGateName(value); } }

			/// <summary>
			/// The normal override of the attached jump gate or null if no override set
			/// </summary>
			public Vector3D? VectorNormalOverride { get { return this.BlockSettings.VectorNormalOverride(); } set { this.BlockSettings.VectorNormalOverride(value); } }

			/// <summary>
			/// The color shift of the selected gate animation<br />
			/// Set to white for the animation's original colors
			/// </summary>
			public Color EffectColorShift { get { return this.BlockSettings.JumpEffectAnimationColorShift(); } set { this.BlockSettings.JumpEffectAnimationColorShift(value); } }

			/// <summary>
			/// The attached jump gate's current destination
			/// </summary>
			public MyJumpGateWaypoint SelectedWaypoint
			{
				get
				{
					return this.BlockSettings.SelectedWaypoint();
				}
				set
				{
					Vector3D? endpoint = value.GetEndpoint();
					MyJumpGate jump_gate = this.Controller.AttachedJumpGate();
					double max_distance = (this.Controller.IsLargeGrid) ? MyJumpGateModSession.Configuration.JumpGateConfiguration.MaximumLargeJumpDistance : MyJumpGateModSession.Configuration.JumpGateConfiguration.MaximumSmallJumpDistance;
					if (endpoint == null || jump_gate == null || Vector3D.Distance(endpoint.Value, jump_gate.WorldJumpNode) > max_distance) return;
					this.BlockSettings.SelectedWaypoint(value);
				}
			}

			/// <summary>
			/// The list of blacklisted entity IDs not allowed to jump
			/// </summary>
			public List<long> BlacklistedEntities { get { return this.BlockSettings.GetBlacklistedEntities(); } set { this.BlockSettings.SetBlacklistedEntities(value); } }

			/// <summary>
			/// The minimum allowed mass an entity must be to jump
			/// </summary>
			public float MinimumEntityMass { get { return this.BlockSettings.AllowedEntityMass().Key; } set { this.BlockSettings.AllowedEntityMass(value, null); } }

			/// <summary>
			/// The maximum allowed mass an entity must be to jump
			/// </summary>
			public float MaximumEntityMass { get { return this.BlockSettings.AllowedEntityMass().Value; } set { this.BlockSettings.AllowedEntityMass(null, value); } }

			/// <summary>
			/// The minimum blocks a grid must be to jump
			/// </summary>
			public uint MinimumCubeGridSize { get { return this.BlockSettings.AllowedCubeGridSize().Key; } set { this.BlockSettings.AllowedCubeGridSize(value, null); } }

			/// <summary>
			/// The maximum blocks a grid must be to jump
			/// </summary>
			public uint MaximumCubeGridSize { get { return this.BlockSettings.AllowedCubeGridSize().Value; } set { this.BlockSettings.AllowedCubeGridSize(null, value); } }

			/// <summary>
			/// The minimum required mass to auto-activate the attached gate
			/// </summary>
			public float MinimumAutoActivateMass { get { return this.BlockSettings.AutoActivateMass().Key; } set { this.BlockSettings.AutoActivateMass(value, null); } }

			/// <summary>
			/// The maximum required mass to auto-activate the attached gate
			/// </summary>
			public float MaximumAutoActivateMass { get { return this.BlockSettings.AutoActivateMass().Value; } set { this.BlockSettings.AutoActivateMass(null, value); } }

			/// <summary>
			/// The minimum required power draw to auto-activate the attached gate
			/// </summary>
			public double MinimumAutoActivatePower { get { return this.BlockSettings.AutoActivatePower().Key; } set { this.BlockSettings.AutoActivatePower(value, null); } }

			/// <summary>
			/// The maximum required power draw to auto-activate the attached gate
			/// </summary>
			public double MaximumAutoActivatePower { get { return this.BlockSettings.AutoActivatePower().Value; } set { this.BlockSettings.AutoActivatePower(null, value); } }

			/// <summary>
			/// The time (in seconds) after meeting the conditions for auto-activation the attached gate will activate after
			/// </summary>
			public float AutoActivateDelay { get { return this.BlockSettings.AutoActivationDelay(); } set { this.BlockSettings.AutoActivationDelay(value); } }

			/// <summary>
			/// Removes the specified entity from the blacklist
			/// </summary>
			/// <param name="entity_id">The entity ID to remove</param>
			public void RemoveBlacklistedEntity(long entity_id)
			{
				this.BlockSettings.RemoveBlacklistedEntity(entity_id);
			}

			/// <summary>
			/// Determines if the specified entity is blacklisted and cannot jump
			/// </summary>
			/// <param name="entity_id">The entity ID to check</param>
			/// <returns>Blacklist status</returns>
			public bool IsEntityBlacklisted(long entity_id)
			{
				return this.BlockSettings.IsEntityBlacklisted(entity_id);
			}

			/// <summary>
			/// Adds the specified entity to the blacklist<br />
			/// Entity cannot be jumped by this gate
			/// </summary>
			/// <param name="entity_id">The entity ID to add</param>
			public void AddBlacklistedEntity(long entity_id)
			{
				this.BlockSettings.AddBlacklistedEntity(entity_id);
			}
		}

		new internal MyJumpGateController CubeBlock;

		public ControllerBlockSettingsWrapper BlockSettings { get; internal set; }

		internal MyAPIJumpGateController(MyJumpGateController block) : base(block)
		{
			this.CubeBlock = block;
			this.BlockSettings = new ControllerBlockSettingsWrapper() { Controller = block, BlockSettings = block.BlockSettings };
		}

		#region Public API Variables
		/// <summary>
		/// IMyTextSurfaceProvider Property
		/// </summary>
		public bool UseGenericLcd => this.CubeBlock.UseGenericLcd;

		/// <summary>
		/// IMyTextSurfaceProvider Property
		/// </summary>
		public int SurfaceCount => this.CubeBlock.SurfaceCount;

		/// <summary>
		/// Gets a surface by ID
		/// </summary>
		/// <param name="surface_id">The surface ID</param>
		/// <returns>The text surface</returns>
		public Sandbox.ModAPI.Ingame.IMyTextSurface GetSurface(int surface_id)
		{
			return this.CubeBlock.GetSurface(surface_id);
		}

		/// <summary>
		/// Gets or sets this controller's attached jump gate
		/// </summary>
		public MyAPIJumpGate AttachedJumpGate
		{
			get
			{
				return MyAPISession.GetNewJumpGate(this.CubeBlock.AttachedJumpGate());
			}
			set
			{
				this.CubeBlock.AttachedJumpGate(value.JumpGate);
			}
		}
		#endregion

		#region Public API Methods
		/// <summary>
		/// Gets the list of waypoints this controller has<br />
		/// This is client dependent
		/// </summary>
		/// <param name="waypoints">The list of waypoints to populate<br />List will not be cleared</param>
		public void GetWaypointsList(List<MyJumpGateWaypoint> waypoints)
		{
			this.CubeBlock.GetWaypointsList(waypoints);
		}

		/// <summary>
		/// </summary>
		/// <param name="steam_id">The player's steam ID to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(ulong steam_id)
		{
			return this.CubeBlock.IsFactionRelationValid(steam_id);
		}

		/// <summary>
		/// </summary>
		/// <param name="steam_id">The player's identiy to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(long player_identity)
		{
			return this.CubeBlock.IsFactionRelationValid(player_identity);
		}
		#endregion
	}
}
