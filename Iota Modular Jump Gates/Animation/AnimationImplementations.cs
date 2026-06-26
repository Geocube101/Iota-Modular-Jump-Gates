using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.Animation
{
	/// <summary>
	/// Base class holding functionality for a single animation
	/// </summary>
	internal class MyAnimation
	{
		#region Protected Variables
		/// <summary>
		/// If true, animation will be cleaned automatically on end
		/// </summary>
		protected bool DoCleanOnEnd = false;

		/// <summary>
		/// If true, animation is stopped
		/// </summary>
		protected bool StopActive { get; private set; } = false;

		/// <summary>
		/// If true, animation is stopped and cleaned
		/// </summary>
		protected bool Cleaned { get; private set; } = false;

		/// <summary>
		/// The jump type of the calling jump gate
		/// </summary>
		protected MyJumpTypeEnum JumpType { get; private set; }

		/// <summary>
		/// Map mapping all jump gate drives with their associated particle effects
		/// </summary>
		protected Dictionary<MyJumpGateDrive, List<Particle>> PerDriveParticles { get; private set; } = new Dictionary<MyJumpGateDrive, List<Particle>>();

		/// <summary>
		/// Map mapping all jump gate drives with their associated particle effects
		/// </summary>
		protected Dictionary<MyJumpGateDrive, List<Particle>> PerAntiDriveParticles { get; private set; } = new Dictionary<MyJumpGateDrive, List<Particle>>();

		/// <summary>
		/// Map mapping all jump space entities with their associated particle effects
		/// </summary>
		protected Dictionary<MyEntity, List<Particle>> PerEntityParticles { get; private set; } = new Dictionary<MyEntity, List<Particle>>();

		/// <summary>
		/// List of the jump node particle effects
		/// </summary>
		protected List<Particle> NodeParticles { get; private set; } = new List<Particle>();

		/// <summary>
		/// List of the anti-node particle effects
		/// </summary>
		protected List<Particle> AntiNodeParticles { get; private set; } = new List<Particle>();

		/// <summary>
		/// List of the anti-node sound effects
		/// </summary>
		protected List<Sound> AntiNodeSounds { get; private set; } = new List<Sound>();

		/// <summary>
		/// List of the jump node sound effects
		/// </summary>
		protected List<Sound> NodeSounds { get; private set; } = new List<Sound>();

		/// <summary>
		/// List of closed entities
		/// </summary>
		protected List<MyEntity> ClosedEntities { get; private set; } = new List<MyEntity>();

		/// <summary>
		/// List of closed drives
		/// </summary>
		protected List<MyJumpGateDrive> ClosedDrives { get; private set; } = new List<MyJumpGateDrive>();

		/// <summary>
		/// The drive emitter emissive color animation or null
		/// </summary>
		protected DriveEmissiveColor DriveColor;

		/// <summary>
		/// The node physics or null
		/// </summary>
		protected NodePhysics NodePhysics;

		/// <summary>
		/// The anti-node physics or null
		/// </summary>
		protected NodePhysics AntiNodePhysics;

		/// <summary>
		/// The drive entity lock or null
		/// </summary>
		protected DriveEntityLock DriveEntityLock;
		#endregion

		#region Public Variables
		/// <summary>
		/// The current tick of this animation
		/// </summary>
		public ushort CurrentTick { get; protected set; } = 0;

		/// <summary>
		/// The parent animation that created this effect
		/// </summary>
		public MyJumpGateAnimation Parent { get; private set; }

		/// <summary>
		/// The calling jump gate
		/// </summary>
		public MyJumpGate JumpGate => this.Parent?.JumpGate;

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		public MyJumpGate TargetGate => this.Parent.TargetGate;

		/// <summary>
		/// The calling jump gate's controller settings
		/// </summary>
		public MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings => this.Parent.ControllerSettings;

		/// <summary>
		/// The target jump gate's controller settings
		/// </summary>
		public MyJumpGateController.MyControllerBlockSettingsStruct TargetControllerSettings => this.Parent.TargetControllerSettings;
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new MyAnimation
		/// </summary>
		/// <param name="parent">The parent animation</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <exception cref="ArgumentNullException">If the jump gate is null or closed</exception>
		protected MyAnimation(MyJumpGateAnimation parent, MyJumpTypeEnum jump_type)
		{
			if (parent == null) throw new ArgumentNullException("Invalid parent");
			this.Parent = parent;
			this.JumpType = jump_type;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this animation, ticking all sounds, particles, physics, and other effects
		/// </summary>
		/// <param name="caller">Entity who this animation is for</param>
		/// <param name="endpoint">The calling jump gate's targeted endpoint (may be affected by normal override)</param>
		/// <param name="anti_node">The calling jump gate's true targeted endpoint</param>
		/// <param name="jump_gate_drives">The calling jump gate's associated drives</param>
		/// <param name="target_jump_gate_drives">The targeted jump gate's associated drives</param>
		/// <param name="jump_gate_entities">The calling jump gate's jump space entities</param>
		/// <param name="world_jump_node">The calling jump gate's world jump node</param>
		public virtual void Tick(IMyPlayer caller, ref Vector3D endpoint, ref Vector3D anti_node, ref Vector3D world_jump_node, List<MyJumpGateDrive> jump_gate_drives, List<MyJumpGateDrive> target_jump_gate_drives, List<MyEntity> jump_gate_entities) { }

		/// <summary>
		/// Stops this animation
		/// </summary>
		/// <param name="full_close">Whether to clean this animation</param>
		public void Stop(bool full_close = false)
		{
			this.StopActive = true;
			this.Clean(full_close);
		}

		/// <summary>
		/// Stops all effects in this animation
		/// </summary>
		/// <param name="full_close">Whether to fully clean all effects</param>
		public virtual void Clean(bool full_close = true)
		{
			foreach (KeyValuePair<MyJumpGateDrive, List<Particle>> pair in this.PerDriveParticles)
			{
				foreach (Particle particle in pair.Value)
				{
					particle.Stop();
					if (full_close) particle.Clean();
				}
			}

			foreach (KeyValuePair<MyJumpGateDrive, List<Particle>> pair in this.PerAntiDriveParticles)
			{
				foreach (Particle particle in pair.Value)
				{
					particle.Stop();
					if (full_close) particle.Clean();
				}
			}

			foreach (KeyValuePair<MyEntity, List<Particle>> pair in this.PerEntityParticles)
			{
				foreach (Particle particle in pair.Value)
				{
					particle.Stop();
					if (full_close) particle.Clean();
				}
			}

			this.NodeParticles.ForEach((particle) => particle.Stop());
			this.AntiNodeParticles.ForEach((particle) => particle.Stop());
			this.NodeSounds.ForEach((sound) => sound.Stop());
			this.AntiNodeSounds.ForEach((sound) => sound.Stop());
			this.ClosedEntities.Clear();
			this.ClosedDrives.Clear();
			this.DriveEntityLock?.Stop();

			if (full_close)
			{
				if (this.JumpGate != null && !this.JumpGate.Closed)
					foreach (MyJumpGateDrive drive in this.JumpGate.GetJumpGateDrives()) drive.CycleDriveEmitter(drive.DriveEmitterColor, Color.Black, 300);

				this.DoCleanOnEnd = false;
				this.NodeParticles.ForEach((particle) => particle.Clean());
				this.AntiNodeParticles.ForEach((particle) => particle.Clean());
				this.NodeSounds.ForEach((sound) => sound.Clean());
				this.AntiNodeSounds.ForEach((sound) => sound.Clean());
				this.DriveColor?.Clean();
				this.DriveEntityLock?.Clean();
				this.PerDriveParticles.Clear();
				this.PerAntiDriveParticles.Clear();
				this.PerEntityParticles.Clear();
				this.NodeParticles.Clear();
				this.AntiNodeParticles.Clear();
				this.NodeSounds.Clear();
				this.AntiNodeSounds.Clear();

				this.Parent = null;
				this.DriveColor = null;
				this.NodePhysics = null;
				this.DriveEntityLock = null;
				this.ClosedEntities = null;
				this.ClosedDrives = null;
				this.PerDriveParticles = null;
				this.PerAntiDriveParticles = null;
				this.PerEntityParticles = null;
				this.NodeParticles = null;
				this.AntiNodeParticles = null;
				this.NodeSounds = null;
				this.AntiNodeSounds = null;
				this.Cleaned = true;
				Logger.Debug("Animation Cleaned!", 2);
			}
		}

		/// <summary>
		/// Marks this animation to clean on end<br />
		/// If not playing, this animtaion is cleaned immediatly
		/// </summary>
		public void CleanOnEnd()
		{
			this.DoCleanOnEnd = true;
			if (this.Stopped()) this.Clean();
		}

		/// <summary>
		/// Restarts this animaton<br />
		/// <i>Has no effect if this animation is cleaned</i>
		/// </summary>
		public void Restart()
		{
			if (this.Cleaned)
			{
				Logger.Warn("Attempt to restart fully cleaned animation");
				return;
			}

			this.StopActive = false;
			this.CurrentTick = 0;
		}

		/// <summary>
		/// Resumes this animaton<br />
		/// <i>Has no effect if this animation is cleaned</i>
		/// </summary>
		public void Resume()
		{
			if (this.Cleaned)
			{
				Logger.Warn("Attempt to restart fully cleaned animation");
				return;
			}

			this.StopActive = false;
		}

		/// <returns>True if this animation is stopped</returns>
		public virtual bool Stopped()
		{
			return this.StopActive || this.CurrentTick > this.Duration();
		}

		/// <summary>
		/// Overridable<br />
		/// Override this to add set this animation's total duration in game ticks<br />
		/// </summary>
		/// <returns>The duration of this animation in game ticks</returns>
		public virtual ushort Duration()
		{
			return 0;
		}
		#endregion
	}

	internal class MyJumpGateJumpingAnimation : MyAnimation
	{
		#region Public Variables
		/// <summary>
		/// The animation definition for the "jumping/charging" animation
		/// </summary>
		public JumpGateJumpingAnimationDef AnimationDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new MyJumpGateJumpingAnimation
		/// </summary>
		/// <param name="def">The "jumping" animation definition</param>
		/// <param name="parent">The parent animation</param>
		/// <param name="endpoint">The calling jump gate's targeted endpoint (may be affected by normal override)</param>
		/// <param name="anti_node">The calling jump gate's true targeted endpoint</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <exception cref="ArgumentNullException">If the definition or jump gate are null</exception>
		public MyJumpGateJumpingAnimation(JumpGateJumpingAnimationDef def, MyJumpGateAnimation parent, ref Vector3D endpoint, ref Vector3D anti_node, MyJumpTypeEnum jump_type) : base(parent, jump_type)
		{
			if (def == null) throw new ArgumentNullException("Definition is null");
			this.AnimationDefinition = def;
			this.DriveColor = (def.DriveEmissiveColor == null) ? null : new DriveEmissiveColor(def.DriveEmissiveColor, def.Duration, this);
			this.NodePhysics = (def.NodePhysics == null) ? null : new NodePhysics(def.NodePhysics, def.Duration, this, false);
			this.AntiNodePhysics = (def.AntiNodePhysics == null) ? null : new NodePhysics(def.AntiNodePhysics, def.Duration, this, true);
			this.DriveEntityLock = (def.DriveEntityLock == null) ? null : new DriveEntityLock(def.DriveEntityLock, def.Duration, this);

			if (def.NodeSounds != null) foreach (SoundDef sound_def in def.NodeSounds) this.NodeSounds.Add(new Sound(sound_def, def.Duration, this, false));
			if (def.AntiNodeSounds != null) foreach (SoundDef sound_def in def.AntiNodeSounds) this.AntiNodeSounds.Add(new Sound(sound_def, def.Duration, this, true));
			if (def.NodeParticles != null) foreach (ParticleDef particle_def in def.NodeParticles) this.NodeParticles.Add(new Particle(particle_def, def.Duration, this, ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle_def.ParticleOrientation), this.JumpGate.WorldJumpNode, false));
			if (def.AntiNodeParticles != null) foreach (ParticleDef particle_def in def.AntiNodeParticles) this.AntiNodeParticles.Add(new Particle(particle_def, def.Duration, this, ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, true, ref anti_node, particle_def.ParticleOrientation), anti_node, true)); ;
		}
		#endregion

		#region Public Methods
		public override void Tick(IMyPlayer caller, ref Vector3D endpoint, ref Vector3D anti_node, ref Vector3D world_jump_node, List<MyJumpGateDrive> jump_gate_drives, List<MyJumpGateDrive> target_jump_gate_drives, List<MyEntity> jump_gate_entities)
		{
			base.Tick(caller, ref endpoint, ref anti_node, ref world_jump_node, jump_gate_drives, target_jump_gate_drives, jump_gate_entities);

			if (this.CurrentTick > this.AnimationDefinition.Duration || this.StopActive)
			{
				if (this.DoCleanOnEnd) this.Clean();
				return;
			}

			Vector3D current_pos = MyAPIGateway.Session.Camera.Position;
			double distance = MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value * MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value;
			List<Particle> particles;

			if (!MyNetworkInterface.IsDedicatedMultiplayerServer && (jump_gate_entities.Contains((IMyEntity) caller.Character) || Vector3D.DistanceSquared(current_pos, world_jump_node) <= distance || Vector3D.DistanceSquared(current_pos, endpoint) <= distance))
			{
				if (this.AnimationDefinition.PerEntityParticles != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedEntities.AddRange(this.PerEntityParticles.Keys);

					foreach (MyEntity entity in jump_gate_entities)
					{
						if (this.PerEntityParticles.TryGetValue(entity, out particles)) this.ClosedEntities.Remove(entity);
						else this.PerEntityParticles.Add(entity, particles = this.AnimationDefinition.PerEntityParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, entity.WorldMatrix, entity.WorldMatrix.Translation, false)).ToList());

						foreach (Particle particle in particles)
						{
							MatrixD particle_matrix = ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle.ParticleDefinition.ParticleOrientation);
							particle_matrix.Translation = ((IMyEntity) entity).WorldVolume.Center;
							particle.Tick(this.CurrentTick, particle_matrix, jump_gate_drives, jump_gate_entities, ref endpoint, entity);
						}
					}

					foreach (MyEntity entity in this.ClosedEntities)
					{
						if (!this.PerEntityParticles.TryGetValue(entity, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerEntityParticles.Remove(entity);
					}

					this.ClosedEntities.Clear();
				}

				if (this.AnimationDefinition.PerDriveParticles != null && jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in jump_gate_drives)
					{
						if (!drive.IsWorking) continue;
						MatrixD drive_emitter_pos = drive.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerDriveParticles.TryGetValue(drive, out particles)) this.ClosedDrives.Remove(drive);
						else this.PerDriveParticles.Add(drive, particles = this.AnimationDefinition.PerDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, drive.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in particles) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						if (!this.PerDriveParticles.TryGetValue(drive, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.AnimationDefinition.PerAntiDriveParticles != null && target_jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerAntiDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in target_jump_gate_drives)
					{
						if (!drive.IsWorking) continue;
						MatrixD drive_emitter_pos = drive.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerAntiDriveParticles.TryGetValue(drive, out particles)) this.ClosedDrives.Remove(drive);
						else this.PerAntiDriveParticles.Add(drive, particles = this.AnimationDefinition.PerAntiDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, drive.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in particles) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						if (!this.PerDriveParticles.TryGetValue(drive, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID)
				{
					foreach (Particle particle in this.NodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref endpoint);
					foreach (Sound sound in this.NodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint, null);
					this.NodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveColor?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveEntityLock?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID)
				{
					List<MyJumpGateDrive> drives = (this.JumpType == MyJumpTypeEnum.INBOUND_VOID) ? jump_gate_drives : target_jump_gate_drives;
					foreach (Particle particle in this.AntiNodeParticles) particle.Tick(this.CurrentTick, null, drives, jump_gate_entities, ref anti_node);
					foreach (Sound sound in this.AntiNodeSounds) sound.Tick(this.CurrentTick, drives, jump_gate_entities, ref anti_node, null);
					this.AntiNodePhysics?.Tick(this.CurrentTick, drives, jump_gate_entities, ref anti_node);
				}
			}

			++this.CurrentTick;
		}

		public override ushort Duration()
		{
			return this.AnimationDefinition.Duration;
		}
		#endregion
	}

	internal class MyJumpGateJumpedAnimation : MyAnimation
	{
		#region Private Variables
		/// <summary>
		/// The beam pulse definition
		/// </summary>
		private BeamPulse Beam;

		/// <summary>
		/// The travel particle effects
		/// </summary>
		private List<Particle> TravelParticles = null;

		/// <summary>
		/// The travel sound effects
		/// </summary>
		private List<Sound> TravelSounds = null;

		/// <summary>
		/// A list of the attached jump gate's batch entities
		/// </summary>
		private List<MyEntity> JumpedEntities = new List<MyEntity>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The animation definition for the "jumped" animation
		/// </summary>
		public JumpGateJumpedAnimationDef AnimationDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new MyJumpGateJumpedAnimation
		/// </summary>
		/// <param name="def">The "jumped" animation definition</param>
		/// <param name="parent">The parent animation</param>
		/// <param name="endpoint">The calling jump gate's targeted endpoint (may be affected by normal override)</param>
		/// <param name="anti_node">The calling jump gate's true targeted endpoint</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <exception cref="ArgumentNullException">If the definition or jump gate are null</exception>
		public MyJumpGateJumpedAnimation(JumpGateJumpedAnimationDef def, MyJumpGateAnimation parent, ref Vector3D endpoint, ref Vector3D anti_node, MyJumpTypeEnum jump_type) : base(parent, jump_type)
		{
			if (def == null) throw new ArgumentNullException("Definition is null");
			this.AnimationDefinition = def;
			this.DriveColor = (def.DriveEmissiveColor == null) ? null : new DriveEmissiveColor(def.DriveEmissiveColor, def.Duration, this);
			this.NodePhysics = (def.NodePhysics == null) ? null : new NodePhysics(def.NodePhysics, def.Duration, this, false);
			this.AntiNodePhysics = (def.AntiNodePhysics == null) ? null : new NodePhysics(def.AntiNodePhysics, def.Duration, this, true);
			this.DriveEntityLock = (def.DriveEntityLock == null) ? null : new DriveEntityLock(def.DriveEntityLock, def.Duration, this);
			this.Beam = (def.BeamPulse == null) ? null : new BeamPulse(def.BeamPulse, def.Duration, this);

			if (def.NodeSounds != null) foreach (SoundDef sound_def in def.NodeSounds) this.NodeSounds.Add(new Sound(sound_def, def.Duration, this, false));
			if (def.AntiNodeSounds != null) foreach (SoundDef sound_def in def.AntiNodeSounds) this.AntiNodeSounds.Add(new Sound(sound_def, def.Duration, this, true));
			if (def.NodeParticles != null) foreach (ParticleDef particle_def in def.NodeParticles) this.NodeParticles.Add(new Particle(particle_def, def.Duration, this, ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle_def.ParticleOrientation), this.JumpGate.WorldJumpNode, false));
			if (def.AntiNodeParticles != null) foreach (ParticleDef particle_def in def.AntiNodeParticles) this.AntiNodeParticles.Add(new Particle(particle_def, def.Duration, this, ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, true, ref anti_node, particle_def.ParticleOrientation), anti_node, true));
		}
		#endregion

		#region Public Methods
		public bool IsPointWithinBeamPulseCylinder(ref Vector3D point, ref Vector3D gate_world_jump_node, ref Vector3D gate_world_target)
		{
			Vector3D dir = point - gate_world_jump_node;
			Vector3D normal = gate_world_target - gate_world_jump_node;
			double distance = MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value * MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value * 4;
			return Vector3D.ProjectOnPlane(ref dir, ref normal).LengthSquared() <= distance;
		}

		public override void Tick(IMyPlayer caller, ref Vector3D endpoint, ref Vector3D anti_node, ref Vector3D world_jump_node, List<MyJumpGateDrive> jump_gate_drives, List<MyJumpGateDrive> target_jump_gate_drives, List<MyEntity> jump_gate_entities)
		{
			base.Tick(caller, ref endpoint, ref anti_node, ref world_jump_node, jump_gate_drives, target_jump_gate_drives, jump_gate_entities);

			if (this.CurrentTick > this.AnimationDefinition.Duration || this.StopActive)
			{
				if (this.DoCleanOnEnd) this.Clean();
				return;
			}

			Vector3D current_pos = MyAPIGateway.Session.Camera.Position;
			double distance = MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value * MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value;
			MyEntity controller = (MyNetworkInterface.IsDedicatedMultiplayerServer) ? null : MyAPIGateway.Session.CameraController?.Entity?.GetTopMostParent();
			MyEntity parent = this.JumpGate.GetEntityBatchFromEntity(controller)?.Parent;
			List<Particle> particles;

			if (!MyNetworkInterface.IsDedicatedMultiplayerServer && parent == null && this.IsPointWithinBeamPulseCylinder(ref current_pos, ref world_jump_node, ref anti_node))
			{
				this.Beam?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint, ref world_jump_node);
			}

			if (!MyNetworkInterface.IsDedicatedMultiplayerServer && (jump_gate_entities.Contains(controller) || Vector3D.DistanceSquared(current_pos, world_jump_node) <= distance || Vector3D.DistanceSquared(current_pos, endpoint) <= distance))
			{
				if (this.AnimationDefinition.PerEntityParticles != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedEntities.AddRange(this.PerEntityParticles.Keys);

					foreach (MyEntity entity in jump_gate_entities)
					{
						if (this.PerEntityParticles.TryGetValue(entity, out particles)) this.ClosedEntities.Remove(entity);
						else this.PerEntityParticles.Add(entity, particles = this.AnimationDefinition.PerEntityParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, entity.WorldMatrix, entity.WorldMatrix.Translation, false)).ToList());

						foreach (Particle particle in particles)
						{
							MatrixD particle_matrix = ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle.ParticleDefinition.ParticleOrientation);
							particle_matrix.Translation = ((IMyEntity) entity).WorldVolume.Center;
							particle.Tick(this.CurrentTick, particle_matrix, jump_gate_drives, jump_gate_entities, ref endpoint, entity);
						}
					}

					foreach (MyEntity entity in this.ClosedEntities)
					{
						if (!this.PerEntityParticles.TryGetValue(entity, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerEntityParticles.Remove(entity);
					}

					this.ClosedEntities.Clear();
				}

				if (parent == null && this.TravelParticles != null)
				{
					foreach (Particle particle in this.TravelParticles) particle.Stop();
					this.TravelParticles.Clear();
					this.TravelParticles = null;
				}
				else if (parent != null && this.AnimationDefinition.TravelEffects != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					ushort duration = this.Beam?.BeamPulseDefinition?.Duration ?? this.AnimationDefinition.Duration;
					this.TravelParticles = this.TravelParticles ?? this.AnimationDefinition.TravelEffects.Select((particle) => new Particle(particle, duration, this, parent.WorldMatrix, parent.WorldMatrix.Translation, false)).ToList();

					foreach (Particle particle in this.TravelParticles)
					{
						MatrixD source = ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle.ParticleDefinition.ParticleOrientation);
						source.Translation = ((IMyEntity) parent).WorldVolume.Center;
						particle.Tick(this.CurrentTick, source, jump_gate_drives, this.JumpedEntities, ref endpoint, parent);
					}
				}

				if (parent == null && this.TravelSounds != null)
				{
					foreach (Sound sound in this.TravelSounds) sound.Stop();
					this.TravelSounds.Clear();
					this.TravelSounds = null;
				}
				else if (parent != null && this.AnimationDefinition.TravelSounds != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					ushort duration = this.Beam?.BeamPulseDefinition?.Duration ?? this.AnimationDefinition.Duration;
					this.TravelSounds = this.TravelSounds ?? this.AnimationDefinition.TravelSounds.Select((sound) => new Sound(sound, duration, this, false)).ToList();
					foreach (Sound sound in this.TravelSounds) sound.Tick(this.CurrentTick, jump_gate_drives, this.JumpedEntities, ref endpoint, controller);
				}



				if (this.AnimationDefinition.PerDriveParticles != null && jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in jump_gate_drives)
					{
						if (!drive.IsWorking) continue;
						MatrixD drive_emitter_pos = drive.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerDriveParticles.TryGetValue(drive, out particles)) this.ClosedDrives.Remove(drive);
						else this.PerDriveParticles.Add(drive, particles = this.AnimationDefinition.PerDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, drive.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in particles) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						if (!this.PerDriveParticles.TryGetValue(drive, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.AnimationDefinition.PerAntiDriveParticles != null && target_jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerAntiDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in target_jump_gate_drives)
					{
						if (!drive.IsWorking) continue;
						MatrixD drive_emitter_pos = drive.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerAntiDriveParticles.TryGetValue(drive, out particles)) this.ClosedDrives.Remove(drive);
						else this.PerAntiDriveParticles.Add(drive, particles = this.AnimationDefinition.PerAntiDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, drive.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in particles) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						if (!this.PerDriveParticles.TryGetValue(drive, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID)
				{
					foreach (Particle particle in this.NodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref endpoint);
					foreach (Sound sound in this.NodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint, null);
					this.NodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveColor?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveEntityLock?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID)
				{
					List<MyJumpGateDrive> drives = (this.JumpType == MyJumpTypeEnum.INBOUND_VOID) ? jump_gate_drives : target_jump_gate_drives;
					foreach (Particle particle in this.AntiNodeParticles) particle.Tick(this.CurrentTick, null, drives, jump_gate_entities, ref anti_node);
					foreach (Sound sound in this.AntiNodeSounds) sound.Tick(this.CurrentTick, drives, jump_gate_entities, ref anti_node, null);
					this.AntiNodePhysics?.Tick(this.CurrentTick, drives, jump_gate_entities, ref anti_node);
				}
			}

			++this.CurrentTick;
		}

		public override ushort Duration()
		{
			return this.AnimationDefinition.Duration;
		}

		public override void Clean(bool full_close = true)
		{
			if (this.TravelParticles != null)
			{
				foreach (Particle particle in this.TravelParticles)
				{
					if (full_close) particle.Clean();
					else particle.Stop();
				}
			}

			if (this.TravelSounds != null)
			{
				foreach (Sound sound in this.TravelSounds)
				{
					if (full_close) sound.Clean();
					else sound.Stop();
				}
			}

			this.TravelParticles?.Clear();
			this.TravelSounds?.Clear();
			this.JumpedEntities.Clear();
			this.TravelParticles = null;
			this.TravelSounds = null;

			if (full_close)
			{
				this.Beam?.Clean();
				this.Beam = null;
				this.JumpedEntities = null;
			}

			base.Clean(full_close);
		}
		#endregion
	}

	internal class MyJumpGateFailedAnimation : MyAnimation
	{
		#region Public Variables
		/// <summary>
		/// The animation definition for the "failed" animation
		/// </summary>
		public JumpGateFailedAnimationDef AnimationDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new JumpGateFailedAnimationDef
		/// </summary>
		/// <param name="def">The "failed" animation definition</param>
		/// <param name="parent">The parent animation</param>
		/// <param name="endpoint">The calling jump gate's targeted endpoint (may be affected by normal override)</param>
		/// <param name="anti_node">The calling jump gate's true targeted endpoint</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <exception cref="ArgumentNullException">If the definition or jump gate are null</exception>
		public MyJumpGateFailedAnimation(JumpGateFailedAnimationDef def, MyJumpGateAnimation parent, ref Vector3D endpoint, ref Vector3D anti_node, MyJumpTypeEnum jump_type) : base(parent, jump_type)
		{
			if (def == null) throw new ArgumentNullException("Definition is null");
			this.AnimationDefinition = def;
			this.DriveColor = (def.DriveEmissiveColor == null) ? null : new DriveEmissiveColor(def.DriveEmissiveColor, def.Duration, this);
			this.NodePhysics = (def.NodePhysics == null) ? null : new NodePhysics(def.NodePhysics, def.Duration, this, false);
			this.AntiNodePhysics = (def.AntiNodePhysics == null) ? null : new NodePhysics(def.AntiNodePhysics, def.Duration, this, true);
			this.DriveEntityLock = (def.DriveEntityLock == null) ? null : new DriveEntityLock(def.DriveEntityLock, def.Duration, this);

			if (def.NodeSounds != null) foreach (SoundDef sound_def in def.NodeSounds) this.NodeSounds.Add(new Sound(sound_def, def.Duration, this, false));
			if (def.AntiNodeSounds != null) foreach (SoundDef sound_def in def.AntiNodeSounds) this.AntiNodeSounds.Add(new Sound(sound_def, def.Duration, this, true));
			if (def.NodeParticles != null) foreach (ParticleDef particle_def in def.NodeParticles) this.NodeParticles.Add(new Particle(particle_def, def.Duration, this, ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle_def.ParticleOrientation), this.JumpGate.WorldJumpNode, false));
			if (def.AntiNodeParticles != null) foreach (ParticleDef particle_def in def.AntiNodeParticles) this.AntiNodeParticles.Add(new Particle(particle_def, def.Duration, this, ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, true, ref anti_node, particle_def.ParticleOrientation), anti_node, true)); ;
		}
		#endregion

		#region Public Methods
		public override void Tick(IMyPlayer caller, ref Vector3D endpoint, ref Vector3D anti_node, ref Vector3D world_jump_node, List<MyJumpGateDrive> jump_gate_drives, List<MyJumpGateDrive> target_jump_gate_drives, List<MyEntity> jump_gate_entities)
		{
			base.Tick(caller, ref endpoint, ref anti_node, ref world_jump_node, jump_gate_drives, target_jump_gate_drives, jump_gate_entities);

			if (this.CurrentTick > this.AnimationDefinition.Duration || this.StopActive)
			{
				if (this.DoCleanOnEnd) this.Clean();
				return;
			}

			Vector3D current_pos = MyAPIGateway.Session.Camera.Position;
			double distance = MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value * MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value;
			List<Particle> particles;

			if (!MyNetworkInterface.IsDedicatedMultiplayerServer && (jump_gate_entities.Contains((IMyEntity) caller.Character) || Vector3D.DistanceSquared(current_pos, world_jump_node) <= distance || Vector3D.DistanceSquared(current_pos, endpoint) <= distance))
			{
				if (this.AnimationDefinition.PerEntityParticles != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedEntities.AddRange(this.PerEntityParticles.Keys);

					foreach (MyEntity entity in jump_gate_entities)
					{
						if (this.PerEntityParticles.TryGetValue(entity, out particles)) this.ClosedEntities.Remove(entity);
						else this.PerEntityParticles.Add(entity, particles = this.AnimationDefinition.PerEntityParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, entity.WorldMatrix, entity.WorldMatrix.Translation, false)).ToList());

						foreach (Particle particle in particles)
						{
							MatrixD particle_matrix = ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle.ParticleDefinition.ParticleOrientation);
							particle_matrix.Translation = ((IMyEntity) entity).WorldVolume.Center;
							particle.Tick(this.CurrentTick, particle_matrix, jump_gate_drives, jump_gate_entities, ref endpoint, entity);
						}
					}

					foreach (MyEntity entity in this.ClosedEntities)
					{
						if (!this.PerEntityParticles.TryGetValue(entity, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerEntityParticles.Remove(entity);
					}

					this.ClosedEntities.Clear();
				}

				if (this.AnimationDefinition.PerDriveParticles != null && jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in jump_gate_drives)
					{
						if (!drive.IsWorking) continue;
						MatrixD drive_emitter_pos = drive.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerDriveParticles.TryGetValue(drive, out particles)) this.ClosedDrives.Remove(drive);
						else this.PerDriveParticles.Add(drive, particles = this.AnimationDefinition.PerDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, drive.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in particles) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						if (!this.PerDriveParticles.TryGetValue(drive, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.AnimationDefinition.PerAntiDriveParticles != null && target_jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerAntiDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in target_jump_gate_drives)
					{
						if (!drive.IsWorking) continue;
						MatrixD drive_emitter_pos = drive.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerAntiDriveParticles.TryGetValue(drive, out particles)) this.ClosedDrives.Remove(drive);
						else this.PerAntiDriveParticles.Add(drive, particles = this.AnimationDefinition.PerAntiDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, drive.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in particles) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						if (!this.PerDriveParticles.TryGetValue(drive, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID)
				{
					foreach (Particle particle in this.NodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref endpoint);
					foreach (Sound sound in this.NodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint, null);
					this.NodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveColor?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveEntityLock?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID)
				{
					List<MyJumpGateDrive> drives = (this.JumpType == MyJumpTypeEnum.INBOUND_VOID) ? jump_gate_drives : target_jump_gate_drives;
					foreach (Particle particle in this.AntiNodeParticles) particle.Tick(this.CurrentTick, null, drives, jump_gate_entities, ref anti_node);
					foreach (Sound sound in this.AntiNodeSounds) sound.Tick(this.CurrentTick, drives, jump_gate_entities, ref anti_node, null);
					this.AntiNodePhysics?.Tick(this.CurrentTick, drives, jump_gate_entities, ref anti_node);
				}
			}

			++this.CurrentTick;
		}

		public override ushort Duration()
		{
			return this.AnimationDefinition.Duration;
		}
		#endregion
	}

	internal class MyJumpGateWormholeAnimation : MyAnimation
	{
		#region Private Variables
		/// <summary>
		/// Whether effect can loop indefinitely
		/// </summary>
		private bool LoopEffect;

		/// <summary>
		/// A list of this gate's shape colliders
		/// </summary>
		private List<ShapeCollider> NodeShapeColliders = new List<ShapeCollider>();

		/// <summary>
		/// A list of the target gate's shape colliders
		/// </summary>
		private List<ShapeCollider> AntiNodeShapeColliders = new List<ShapeCollider>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The animation definition for the wormhole
		/// </summary>
		public JumpGateWormholeAnimationDef AnimationDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new JumpGateWormholeAnimationDef
		/// </summary>
		/// <param name="def">The wormhole animation definition</param>
		/// <param name="parent">The parent animation</param>
		/// <param name="endpoint">The calling jump gate's targeted endpoint (may be affected by normal override)</param>
		/// <param name="anti_node">The calling jump gate's true targeted endpoint</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <param name="loopable">Whether effect will be looped on end</param>
		/// <exception cref="ArgumentNullException">If the definition or jump gate are null</exception>
		public MyJumpGateWormholeAnimation(JumpGateWormholeAnimationDef def, MyJumpGateAnimation parent, ref Vector3D endpoint, ref Vector3D anti_node, MyJumpTypeEnum jump_type, bool loopable) : base(parent, jump_type)
		{
			if (def == null) throw new ArgumentNullException("Definition is null");
			this.LoopEffect = loopable;
			this.AnimationDefinition = def;
			this.DriveColor = (def.DriveEmissiveColor == null) ? null : new DriveEmissiveColor(def.DriveEmissiveColor, def.Duration, this);
			this.NodePhysics = (def.NodePhysics == null) ? null : new NodePhysics(def.NodePhysics, def.Duration, this, false);
			this.AntiNodePhysics = (def.AntiNodePhysics == null) ? null : new NodePhysics(def.AntiNodePhysics, def.Duration, this, true);
			this.DriveEntityLock = (def.DriveEntityLock == null) ? null : new DriveEntityLock(def.DriveEntityLock, def.Duration, this);

			if (def.NodeSounds != null) foreach (SoundDef sound_def in def.NodeSounds) this.NodeSounds.Add(new Sound(sound_def, def.Duration, this, false));
			if (def.AntiNodeSounds != null) foreach (SoundDef sound_def in def.AntiNodeSounds) this.AntiNodeSounds.Add(new Sound(sound_def, def.Duration, this, true));
			if (def.NodeParticles != null) foreach (ParticleDef particle_def in def.NodeParticles) this.NodeParticles.Add(new Particle(particle_def, def.Duration, this, ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle_def.ParticleOrientation), this.JumpGate.WorldJumpNode, false));
			if (def.AntiNodeParticles != null) foreach (ParticleDef particle_def in def.AntiNodeParticles) this.AntiNodeParticles.Add(new Particle(particle_def, def.Duration, this, ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, true, ref anti_node, particle_def.ParticleOrientation), anti_node, true));
			if (def.NodeShapeColliders != null) foreach (ShapeColliderDef collider_def in def.NodeShapeColliders) this.NodeShapeColliders.Add(new ShapeCollider(collider_def, def.Duration, this, false));
			if (def.AntiNodeShapeColliders != null) foreach (ShapeColliderDef collider_def in def.AntiNodeShapeColliders) this.AntiNodeShapeColliders.Add(new ShapeCollider(collider_def, def.Duration, this, true));
		}
		#endregion

		#region Public Methods
		public override void Tick(IMyPlayer caller, ref Vector3D endpoint, ref Vector3D anti_node, ref Vector3D world_jump_node, List<MyJumpGateDrive> jump_gate_drives, List<MyJumpGateDrive> target_jump_gate_drives, List<MyEntity> jump_gate_entities)
		{
			base.Tick(caller, ref endpoint, ref anti_node, ref world_jump_node, jump_gate_drives, target_jump_gate_drives, jump_gate_entities);

			if ((this.CurrentTick > this.AnimationDefinition.Duration && !this.LoopEffect) || this.StopActive)
			{
				if (this.DoCleanOnEnd) this.Clean();
				return;
			}
			else if (this.CurrentTick >= this.AnimationDefinition.Duration) this.CurrentTick = 0;

			Vector3D current_pos = MyAPIGateway.Session.Camera.Position;
			double distance = MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value * MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance.Value;
			List<Particle> particles;

			if (!MyNetworkInterface.IsDedicatedMultiplayerServer && (jump_gate_entities.Contains((IMyEntity) caller.Character) || Vector3D.DistanceSquared(current_pos, world_jump_node) <= distance || Vector3D.DistanceSquared(current_pos, endpoint) <= distance))
			{
				if (this.AnimationDefinition.PerEntityParticles != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedEntities.AddRange(this.PerEntityParticles.Keys);

					foreach (MyEntity entity in jump_gate_entities)
					{
						if (this.PerEntityParticles.TryGetValue(entity, out particles)) this.ClosedEntities.Remove(entity);
						else this.PerEntityParticles.Add(entity, particles = this.AnimationDefinition.PerEntityParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, entity.WorldMatrix, entity.WorldMatrix.Translation, false)).ToList());

						foreach (Particle particle in particles)
						{
							MatrixD particle_matrix = ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle.ParticleDefinition.ParticleOrientation);
							particle_matrix.Translation = ((IMyEntity) entity).WorldVolume.Center;
							particle.Tick(this.CurrentTick, particle_matrix, jump_gate_drives, jump_gate_entities, ref endpoint, entity);
						}
					}

					foreach (MyEntity entity in this.ClosedEntities)
					{
						if (!this.PerEntityParticles.TryGetValue(entity, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerEntityParticles.Remove(entity);
					}

					this.ClosedEntities.Clear();
				}

				if (this.AnimationDefinition.PerDriveParticles != null && jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in jump_gate_drives)
					{
						if (!drive.IsWorking) continue;
						MatrixD drive_emitter_pos = drive.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerDriveParticles.TryGetValue(drive, out particles)) this.ClosedDrives.Remove(drive);
						else this.PerDriveParticles.Add(drive, particles = this.AnimationDefinition.PerDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, drive.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in particles) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						if (!this.PerDriveParticles.TryGetValue(drive, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.AnimationDefinition.PerAntiDriveParticles != null && target_jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerAntiDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in target_jump_gate_drives)
					{
						if (!drive.IsWorking) continue;
						MatrixD drive_emitter_pos = drive.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerAntiDriveParticles.TryGetValue(drive, out particles)) this.ClosedDrives.Remove(drive);
						else this.PerAntiDriveParticles.Add(drive, particles = this.AnimationDefinition.PerAntiDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this, drive.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in particles) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						if (!this.PerDriveParticles.TryGetValue(drive, out particles)) continue;
						foreach (Particle particle in particles) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID)
				{
					foreach (Particle particle in this.NodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref endpoint);
					foreach (Sound sound in this.NodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint, null);
					this.NodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveColor?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveEntityLock?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID)
				{
					List<MyJumpGateDrive> drives = (this.JumpType == MyJumpTypeEnum.INBOUND_VOID) ? jump_gate_drives : target_jump_gate_drives;
					foreach (Particle particle in this.AntiNodeParticles) particle.Tick(this.CurrentTick, null, drives, jump_gate_entities, ref anti_node);
					foreach (Sound sound in this.AntiNodeSounds) sound.Tick(this.CurrentTick, drives, jump_gate_entities, ref anti_node, null);
					this.AntiNodePhysics?.Tick(this.CurrentTick, drives, jump_gate_entities, ref anti_node);
				}
			}

			if (MyNetworkInterface.IsServerLike && this.NodeShapeColliders != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
			{
				foreach (ShapeCollider collider in this.NodeShapeColliders) collider.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
			}

			if (MyNetworkInterface.IsServerLike && this.AntiNodeShapeColliders != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID))
			{
				foreach (ShapeCollider collider in this.AntiNodeShapeColliders) collider.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
			}

			++this.CurrentTick;
		}

		public override void Clean(bool full_close = true)
		{
			base.Clean(full_close);
			if (!full_close) return;
			this.NodeShapeColliders?.ForEach((collider) => collider.Clean());
			this.AntiNodeShapeColliders?.ForEach((collider) => collider.Clean());
			this.NodeShapeColliders = null;
			this.AntiNodeShapeColliders = null;
		}

		public override bool Stopped()
		{
			return this.StopActive || (!this.LoopEffect && this.CurrentTick > this.Duration());
		}

		public override ushort Duration()
		{
			return this.AnimationDefinition.Duration;
		}

		/// <summary>
		/// Gets all entities currently colliding with the shape colliders of this animation
		/// </summary>
		/// <param name="collider_type">The collider type to retrieve or null for all</param>
		/// <param name="parent">The gate who's colliders to retrieve or null for both</param>
		/// <returns>All matching shape collider entities</returns>
		public IEnumerable<MyEntity> GetAllColliderEntities(CollisionEffectTypeEnum? collider_type = null, MyJumpGate parent = null)
		{
			if ((parent == null || parent == this.JumpGate) && this.NodeShapeColliders != null)
			{
				foreach (ShapeCollider collider in this.NodeShapeColliders)
				{
					if (collider_type == null || collider.ShapeColliderDefinition.CollisionEffectType == collider_type)
					{
						foreach (MyEntity entity in collider.GetColliderEntities()) yield return entity;
					}
				}
			}

			if ((parent == null || parent == this.TargetGate) && this.AntiNodeShapeColliders != null)
			{
				foreach (ShapeCollider collider in this.AntiNodeShapeColliders)
				{
					if (collider_type == null || collider.ShapeColliderDefinition.CollisionEffectType == collider_type)
					{
						foreach (MyEntity entity in collider.GetColliderEntities()) yield return entity;
					}
				}
			}
		}

		/// <summary>
		/// Gets all shape colliders of this animation matching the specified type and parent gate
		/// </summary>
		/// <param name="collider_type">The collider type to retrieve or null for all</param>
		/// <param name="parent">The gate who's colliders to retrieve or null for both</param>
		/// <returns>All matching shape colliders</returns>
		public IEnumerable<ShapeCollider> GetCollidersOfType(CollisionEffectTypeEnum? collider_type = null, MyJumpGate parent = null)
		{
			if ((parent == null || parent == this.JumpGate) && this.NodeShapeColliders != null)
			{
				foreach (ShapeCollider collider in this.NodeShapeColliders)
				{
					if (collider_type == null || collider.ShapeColliderDefinition.CollisionEffectType == collider_type) yield return collider;
				}
			}

			if ((parent == null || parent == this.TargetGate) && this.AntiNodeShapeColliders != null)
			{
				foreach (ShapeCollider collider in this.AntiNodeShapeColliders)
				{
					if (collider_type == null || collider.ShapeColliderDefinition.CollisionEffectType == collider_type) yield return collider;
				}
			}
		}
		#endregion
	}
}
