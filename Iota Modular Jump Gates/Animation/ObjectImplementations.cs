using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Extensions;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.Animation
{
	internal class AnimatableObject
	{
		#region Protected Variables
		/// <summary>
		/// Whether this particle should be spawned at the gate's anti-node
		/// </summary>
		protected readonly bool IsAntiNode;

		/// <summary>
		/// The duration of this particle effect in game ticks
		/// </summary>
		protected readonly ushort Duration;

		/// <summary>
		/// The parent animation that created this object
		/// </summary>
		protected MyAnimation Parent { get; private set; }

		/// <summary>
		/// The calling jump gate
		/// </summary>
		protected MyJumpGate JumpGate => this.Parent?.JumpGate;

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		protected MyJumpGate TargetGate => this.Parent?.TargetGate;

		/// <summary>
		/// The calling jump gate's controller settings
		/// </summary>
		protected MyJumpGateController.MyControllerBlockSettingsStruct SourceGateSettings => this.Parent?.ControllerSettings;

		/// <summary>
		/// The targeted jump gate's controller settings or null
		/// </summary>
		protected MyJumpGateController.MyControllerBlockSettingsStruct TargetGateSettings => this.Parent?.TargetControllerSettings;
		#endregion

		#region Constructors
		protected AnimatableObject(ushort duration, bool is_anti_node, MyAnimation parent)
		{
			if (parent == null) throw new ArgumentNullException("Parent cannot be null");
			this.Duration = duration;
			this.IsAntiNode = is_anti_node;
			this.Parent = parent;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Stops this effect fully<br />
		/// Effect is cleaned and cannot be replayed
		/// </summary>
		public virtual void Clean()
		{
			this.Parent = null;
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for particle definitions
	/// </summary>
	internal sealed class Particle : AnimatableObject, IEquatable<Particle>
	{
		private sealed class TransientParticle
		{
			public MyParticleEffect ParticleEffect;
			public Vector3D ParticleRotation;

			public TransientParticle(MyParticleEffect effect, Vector3D rotation)
			{
				this.ParticleEffect = effect;
				this.ParticleRotation = rotation;
			}
		}

		#region Private Static Variables
		/// <summary>
		/// The number of active effects to play at a given time
		/// </summary>
		private static ushort RenderQueueLength => 128;

		/// <summary>
		/// The next particle ID
		/// </summary>
		private static int NextParticleID = 0;

		/// <summary>
		/// Master list of active particle effects on this client
		/// </summary>
		private static List<Particle> ActiveParticlesQueue = new List<Particle>();

		/// <summary>
		/// Master map storing transient particles
		/// </summary>
		private static Dictionary<MyJumpGate, Dictionary<byte, TransientParticle>> TransientParticles = new Dictionary<MyJumpGate, Dictionary<byte, TransientParticle>>();
		#endregion

		#region Private Static Methods
		/// <summary>
		/// Adds a particle from the active queue
		/// </summary>
		/// <param name="particle">The particle ro add</param>
		private static void QueueParticleForRender(Particle particle)
		{
			lock (Particle.ActiveParticlesQueue)
			{
				if (particle == null || Particle.ActiveParticlesQueue.Contains(particle)) return;
				particle.IsPlayableInQueue = false;
				Particle.ActiveParticlesQueue.Add(particle);
			}
		}

		/// <summary>
		/// Removes a particle from the active queue
		/// </summary>
		/// <param name="particle">The particle ro remove</param>
		private static void DequeueParticleForRender(Particle particle)
		{
			lock (Particle.ActiveParticlesQueue)
			{
				if (particle == null) return;
				Particle.ActiveParticlesQueue.Remove(particle);
				particle.IsPlayableInQueue = false;
			}
		}

		private static bool IsEffectWithinPlayDistance(MyParticleEffect effect, ref MatrixD new_particle_matrix)
		{
			try
			{
				if (effect == null) return false;
				else if (effect.DistanceMax == 0) return true;
				else return Vector3D.DistanceSquared(new_particle_matrix.Translation, MyAPIGateway.Session.Camera.Position) <= effect.DistanceMax * effect.DistanceMax;
			}
			catch (NullReferenceException)
			{
				return false;
			}
		}
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Updates the state of all active particles
		/// </summary>
		public static void Render()
		{
			if (MyNetworkInterface.IsDedicatedMultiplayerServer) return;
			uint index = 0;
			Vector3D camera_pos = MyAPIGateway.Session.Camera.Position;

			lock (Particle.ActiveParticlesQueue)
			{
				foreach (Particle particle in Particle.ActiveParticlesQueue.OrderBy((particle) => Vector3D.DistanceSquared(camera_pos, particle.EffectPosition)))
				{
					particle.IsPlayableInQueue = index++ < Particle.RenderQueueLength;
				}
			}
		}

		/// <summary>
		/// Cleans the static particle manager
		/// </summary>
		public static void Dispose()
		{
			Particle.ActiveParticlesQueue?.Clear();
			Particle.TransientParticles?.Clear();
			Particle.ActiveParticlesQueue = null;
			Particle.TransientParticles = null;
		}
		#endregion

		#region Private Variables
		/// <summary>
		/// Whether this particle effect is in the queue and should be played
		/// </summary>
		private bool IsPlayableInQueue = true;

		/// <summary>
		/// Whether this particle is stopped
		/// </summary>
		private bool Stopped = true;

		/// <summary>
		/// This particle's ID
		/// </summary>
		private readonly int ParticleID;

		/// <summary>
		/// The current effect's position
		/// </summary>
		private Vector3D EffectPosition;

		/// <summary>
		/// The last particle rotations
		/// </summary>
		private List<Vector3D> ParticleRotations = null;

		/// <summary>
		/// The particle effects
		/// </summary>
		private List<MyParticleEffect> ParticleEffects = null;

		/// <summary>
		/// The transient IDs
		/// </summary>
		private List<byte> ParticleTransientIDs = null;
		#endregion

		#region Public Variables
		/// <summary>
		/// The particle definition
		/// </summary>
		public ParticleDef ParticleDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new particle effect
		/// </summary>
		/// <param name="def">The particle definition</param>
		/// <param name="animation_duration">The parent animation duration in game ticks</param>
		/// <param name="parent">The parent animation</param>
		/// <param name="matrix">The particle orientation matrix</param>
		/// <param name="position">The particle effect position</param>
		/// <param name="anti_node">Whether this particle should spawn at the anti-node</param>
		/// <exception cref="ArgumentNullException">If the particle definition is null</exception>
		public Particle(ParticleDef def, ushort animation_duration, MyAnimation parent, MatrixD matrix, Vector3D position, bool anti_node) : base((def.Duration == 0) ? animation_duration : def.Duration, anti_node, parent)
		{
			if (def == null) throw new ArgumentNullException("ParticleDef cannot be null");
			this.ParticleID = Particle.NextParticleID++;
			this.ParticleDefinition = def;
			matrix.Translation = position;
			this.ParticleEffects = new List<MyParticleEffect>();
			this.ParticleTransientIDs = new List<byte>();
			this.ParticleRotations = new List<Vector3D>();
			if (this.ParticleDefinition.ParticleNames == null) return;

			Dictionary<byte, TransientParticle> transient_particles = Particle.TransientParticles.GetValueOrDefault(this.JumpGate, null);

			for (int i = 0; i < this.ParticleDefinition.ParticleNames.Length; ++i)
			{
				string particle_name = this.ParticleDefinition.ParticleNames[i];
				byte transient_id = (this.ParticleDefinition.TransientIDs != null && i < this.ParticleDefinition.TransientIDs.Length) ? this.ParticleDefinition.TransientIDs[i] : (byte) 0;
				MyParticleEffect effect;

				if (transient_id != 0 && transient_particles != null && transient_particles.ContainsKey(transient_id))
				{
					TransientParticle transient = transient_particles[transient_id];
					transient.ParticleEffect.StopEmitting();
					transient.ParticleEffect.StopLights();
					this.ParticleEffects.Add(transient.ParticleEffect);
					this.ParticleTransientIDs.Add(transient_id);
					this.ParticleRotations.Add(transient.ParticleRotation);
				}
				else if (MyParticlesManager.TryCreateParticleEffect(particle_name, ref matrix, ref position, uint.MaxValue, out effect))
				{
					effect.Autodelete = false;
					effect.StopEmitting();
					effect.StopLights();
					this.ParticleEffects.Add(effect);
					this.ParticleTransientIDs.Add(transient_id);
					this.ParticleRotations.Add(Vector3D.Zero);
				}
				else
				{
					this.ParticleEffects.Add(null);
					this.ParticleTransientIDs.Add(transient_id);
					this.ParticleRotations.Add(Vector3D.Zero);
					Logger.Error($"Error creating particle effect: {particle_name}/{transient_id}; ENABLED={MyParticlesManager.Enabled}, IS_ANTI_NODE={anti_node}");
				}
			}

			Particle.QueueParticleForRender(this);
		}
		#endregion

		#region "object" Methods
		public override bool Equals(object obj)
		{
			return obj != null && obj is Particle && this.Equals((Particle) obj);
		}

		public bool Equals(Particle particle)
		{
			return particle != null && this.ParticleID == particle.ParticleID;
		}

		public override int GetHashCode()
		{
			return this.ParticleID;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this particle effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="source">The matrix defining a particle's position or null to calculate</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		/// <param name="this_entity">This entity or null if not bound to an entity</param>
		public void Tick(ushort current_tick, MatrixD? source, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, MyEntity this_entity = null, Vector3D? entity_lock_pos = null)
		{
			MatrixD base_matrix = (this.JumpGate == null) ? MatrixD.Identity : (source ?? ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, this.IsAntiNode, ref endpoint, this.ParticleDefinition?.ParticleOrientation));
			this.EffectPosition = base_matrix.Translation;

			if (this.ParticleEffects == null || this.ParticleEffects.Count == 0) return;
			else if (this.IsPlayableInQueue && current_tick >= this.ParticleDefinition.StartTime && current_tick < this.ParticleDefinition.StartTime + this.Duration)
			{
				this.Stopped = false;
				ushort local_tick = (ushort) (current_tick - this.ParticleDefinition.StartTime);
				Vector3D rotations_per_second;
				Vector3D offset = this.ParticleDefinition.ParticleOffset;
				Vector4D rps, off;
				AnimationExpression.ExpressionArguments arguments = new AnimationExpression.ExpressionArguments(local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, entity_lock_pos ?? this_entity?.WorldMatrix.Translation, this_entity);

				float birth_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleBirthAnimation, arguments, 1);
				float color_intensity_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleColorIntensityAnimation, arguments, 1);
				float fade_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleFadeAnimation, arguments, 1);
				float life_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleLifeAnimation, arguments, 1);
				float radius_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleRadiusAnimation, arguments, 1);
				float scale_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleScaleAnimation, arguments, 1);
				float velocity_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleVelocityAnimation, arguments, 1);

				Vector4D color = AttributeAnimationDef.GetAnimatedVectorValue(this.ParticleDefinition.Animations?.ParticleColorAnimation, arguments, Vector4D.One);
				color *= this.SourceGateSettings?.JumpEffectAnimationColorShift().ToVector4D() ?? Vector4D.One;
				Vector4 mapped_color = new Vector4(
					(float) Math.Round(color.X, 4),
					(float) Math.Round(color.Y, 4),
					(float) Math.Round(color.Z, 4),
					(float) Math.Round(color.W, 4)
				);

				rps = AttributeAnimationDef.GetAnimatedVectorValue(this.ParticleDefinition.Animations?.ParticleRotationSpeedAnimation, arguments, Vector4D.Zero);
				rotations_per_second = new Vector3D(rps.X, rps.Y, rps.Z) * 360d / 60d * (Math.PI / 180d);

				off = AttributeAnimationDef.GetAnimatedVectorValue(this.ParticleDefinition.Animations?.ParticleOffsetAnimation, arguments, new Vector4D(offset, 0));
				offset = new Vector3D(off.X, off.Y, off.Z);

				for (int i = 0; i < this.ParticleEffects.Count; ++i)
				{
					MyParticleEffect effect = this.ParticleEffects[i];
					Vector3D rotation = this.ParticleRotations[i] + rotations_per_second;
					MatrixD particle_matrix = MatrixD.CreateFromYawPitchRoll(rotation.Y, rotation.Z, rotation.X) * base_matrix;
					particle_matrix.Translation += MyJumpGateModSession.LocalVectorToWorldVectorD(ref particle_matrix, offset);
					bool in_bounds = Particle.IsEffectWithinPlayDistance(effect, ref particle_matrix);
					bool dirtify = this.ParticleDefinition.DirtifyEffect;

					try
					{
						if ((effect == null || effect.IsStopped) && local_tick % 15 == 0)
						{
							string effect_name = this.ParticleDefinition.ParticleNames[i];
							Vector3D position = particle_matrix.Translation;

							if (MyParticlesManager.TryCreateParticleEffect(effect_name, ref particle_matrix, ref position, uint.MaxValue, out effect))
							{
								effect.Autodelete = false;
								effect.SetElapsedTime(local_tick / 60f);
								this.ParticleEffects[i] = effect;
							}
						}
						else if (effect != null && !effect.IsStopped && effect.IsEmittingStopped && in_bounds)
						{
							effect.SetElapsedTime(local_tick / 60f);
							effect.Play();
							dirtify = true;
						}
						else if (effect != null && !effect.IsStopped && !effect.IsEmittingStopped && !in_bounds)
						{
							effect.StopEmitting();
							effect.StopLights();
							continue;
						}

						if (effect == null || effect.IsStopped || effect.IsEmittingStopped) continue;
						this.ParticleRotations[i] = rotation;
						effect.UserBirthMultiplier = birth_mp;
						effect.UserColorIntensityMultiplier = color_intensity_mp;
						effect.UserFadeMultiplier = fade_mp;
						effect.UserLifeMultiplier = life_mp;
						effect.UserRadiusMultiplier = radius_mp;
						effect.UserScale = scale_mp;
						effect.UserVelocityMultiplier = velocity_mp;
						effect.WorldMatrix = particle_matrix;
						effect.UserColorMultiplier = mapped_color;
						if (dirtify) effect.SetDirty();
					}
					catch (NullReferenceException)
					{

					}
				}
			}
			else if (current_tick > this.ParticleDefinition.StartTime + this.Duration)
			{
				this.Stop();
				if (this.ParticleDefinition.CleanOnEffectEnd) this.Clean();
			}
			else if (!this.IsPlayableInQueue && !this.Stopped) this.Stop();
		}

		public override void Clean()
		{
			if (this.ParticleEffects == null) return;
			foreach (MyParticleEffect effect in this.ParticleEffects) effect?.Stop();
			if (Particle.TransientParticles.ContainsKey(this.JumpGate))
				foreach (KeyValuePair<byte, TransientParticle> effect in Particle.TransientParticles[this.JumpGate])
					if (!effect.Value.ParticleEffect.IsStopped)
						effect.Value.ParticleEffect.Stop();
			Particle.TransientParticles.Remove(this.JumpGate);
			this.ParticleEffects.Clear();
			this.ParticleTransientIDs.Clear();
			this.ParticleRotations.Clear();
			this.ParticleRotations = null;
			this.ParticleTransientIDs = null;
			this.ParticleEffects = null;
			this.ParticleDefinition = null;
			this.Stopped = true;
			base.Clean();
			Particle.DequeueParticleForRender(this);
		}

		/// <summary>
		/// Stops this effect temporarily
		/// </summary>
		public void Stop()
		{
			if (this.ParticleEffects == null || this.Stopped) return;
			Dictionary<byte, TransientParticle> transient_particles = Particle.TransientParticles.GetValueOrNew(this.JumpGate);

			for (int i = 0; i < this.ParticleEffects.Count; ++i)
			{
				MyParticleEffect effect = this.ParticleEffects[i];
				if (effect == null) continue;
				byte transient_id = this.ParticleTransientIDs[i];

				if (effect.Loop) effect.Stop();
				else
				{
					effect.StopEmitting();
					effect.StopLights();
				}

				if (transient_id == 0) continue;
				TransientParticle old_effect = transient_particles.GetValueOrDefault(transient_id, null);
				if (old_effect != null && old_effect.ParticleEffect != effect) old_effect.ParticleEffect.Stop();
				transient_particles[transient_id] = new TransientParticle(effect, this.ParticleRotations[i]);
			}

			this.Stopped = true;
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for sound definitions
	/// </summary>
	internal sealed class Sound : AnimatableObject
	{
		#region Private Variables
		/// <summary>
		/// The gate's assigned sound emitter ID
		/// </summary>
		private List<ulong> SoundIDs = null;

		/// <summary>
		/// The sound emitters for non-gate sounds
		/// </summary>
		private List<MyEntity3DSoundEmitter> SoundEmitters = null;
		#endregion

		#region Public Variables
		/// <summary>
		/// The sound definition
		/// </summary>
		public SoundDef SoundDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new sound effect
		/// </summary>
		/// <param name="def">The sound definition</param>
		/// <param name="parent">The parent animation</param>
		/// <param name="anti_node">Whether this sound should play at the anti-node</param>
		/// <exception cref="ArgumentNullException">If the sound definition or jump gate is null</exception>
		public Sound(SoundDef def, ushort animation_duration, MyAnimation parent, bool anti_node) : base((def.Duration == 0) ? animation_duration : def.Duration, anti_node, parent)
		{
			if (def == null) throw new ArgumentNullException("SoundDef cannot be null");
			this.SoundDefinition = def;
			this.SoundIDs = new List<ulong>();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this sound effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, MyEntity source)
		{
			if (this.JumpGate == null || this.JumpGate.Closed) return;

			if (current_tick >= this.SoundDefinition.StartTime && current_tick < this.SoundDefinition.StartTime + this.Duration)
			{
				bool is_start = current_tick == this.SoundDefinition.StartTime;
				ushort local_tick = (ushort) (current_tick - this.SoundDefinition.StartTime);

				if (is_start && source != null)
				{
					if (this.SoundEmitters == null) this.SoundEmitters = new List<MyEntity3DSoundEmitter>();
					else
					{
						foreach (MyEntity3DSoundEmitter emitter in this.SoundEmitters) emitter.StopSound(true);
						this.SoundEmitters.Clear();
					}

					foreach (string sound_name in this.SoundDefinition.SoundNames)
					{
						MyEntity3DSoundEmitter emitter = new MyEntity3DSoundEmitter(source);
						emitter.PlaySound(new MySoundPair(sound_name), true, alwaysHearOnRealistic: true, force3D: true);
						this.SoundEmitters.Add(emitter);
					}
				}
				else if (is_start && this.IsAntiNode)
				{
					foreach (string sound_name in this.SoundDefinition.SoundNames)
					{
						ulong? id = this.JumpGate.PlaySound(sound_name, pos: endpoint);
						if (id != null) this.SoundIDs.Add(id.Value);
						else Logger.Error($"Failed to spawn 3D sound '{sound_name}' for jump gate {JumpGateUUID.FromJumpGate(this.JumpGate)}");
					}
				}
				else if (is_start)
				{
					foreach (string sound_name in this.SoundDefinition.SoundNames)
					{
						ulong? id = this.JumpGate.PlaySound(sound_name);
						if (id != null) this.SoundIDs.Add(id.Value);
						else Logger.Error($"Failed to spawn 3D sound '{sound_name}' for jump gate {JumpGateUUID.FromJumpGate(this.JumpGate)}");
					}
				}

				float volume = this.SoundDefinition.Volume;
				float? distance = this.SoundDefinition.Distance;
				AnimationExpression.ExpressionArguments arguments = new AnimationExpression.ExpressionArguments(local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, source?.WorldMatrix.Translation, source);
				volume = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.SoundDefinition.Animations?.SoundVolumeAnimation, arguments, volume);
				if (this.SoundDefinition.Animations?.SoundDistanceAnimation != null) distance = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.SoundDefinition.Animations?.SoundDistanceAnimation, arguments);

				foreach (ulong sound_id in this.SoundIDs)
				{
					this.JumpGate.SetSoundVolume(sound_id, volume);
					this.JumpGate.SetSoundDistance(sound_id, distance);
					this.JumpGate.SetSoundPosition(sound_id, (this.IsAntiNode) ? (this.TargetGate?.WorldJumpNode ?? endpoint) : this.JumpGate.WorldJumpNode);
				}

				if (this.SoundEmitters != null)
				{
					foreach (MyEntity3DSoundEmitter emitter in this.SoundEmitters)
					{
						emitter.VolumeMultiplier = volume;
						emitter.CustomMaxDistance = distance;
						emitter.SetPosition(source?.WorldMatrix.Translation);
					}
				}
			}
			else if (current_tick > this.SoundDefinition.StartTime + this.Duration) this.Stop();
		}

		public override void Clean()
		{
			this.Stop();
			base.Clean();
			this.SoundIDs = null;
			this.SoundDefinition = null;
			this.SoundEmitters = null;
		}

		/// <summary>
		/// Stops this sound temporarily
		/// </summary>
		public void Stop()
		{
			foreach (ulong? sound_id in this.SoundIDs) this.JumpGate.StopSound(sound_id);
			if (this.SoundEmitters != null) foreach (MyEntity3DSoundEmitter emitter in this.SoundEmitters) emitter.StopSound(true);
			this.SoundIDs.Clear();
			this.SoundEmitters?.Clear();
			this.SoundEmitters = null;
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for beam pulse definitions
	/// </summary>
	internal sealed class BeamPulse : AnimatableObject
	{
		#region Private Variables
		/// <summary>
		/// The beam material
		/// </summary>
		private MyStringId? BeamMaterial = null;

		/// <summary>
		/// The flash point particles
		/// </summary>
		private Particle[] FlashPointParticles = null;
		#endregion

		#region Public Variables
		/// <summary>
		/// The beam pulse definition
		/// </summary>
		public BeamPulseDef BeamPulseDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new Beam Pulse effect
		/// </summary>
		/// <param name="def">The beam pulse definition</param>
		/// <param name="animation_duration">The parent animation duration in game ticks</param>
		/// <param name="parent">The parent animation</param>
		/// <exception cref="ArgumentNullException">If the beam pulse definition is null</exception>
		public BeamPulse(BeamPulseDef def, ushort animation_duration, MyAnimation parent) : base((def.Duration == 0) ? animation_duration : def.Duration, false, parent)
		{
			if (def == null) throw new ArgumentNullException("BeamPulseDef cannot be null");
			this.BeamPulseDefinition = def;
			this.FlashPointParticles = def.FlashPointParticles?.Select((particle) => new Particle(particle, animation_duration, parent, MatrixD.Identity, this.JumpGate.WorldJumpNode, false)).ToArray();
			if (this.BeamPulseDefinition.Material != null) this.BeamMaterial = MyStringId.GetOrCompute(this.BeamPulseDefinition.Material);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this beam pulse effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		/// <param name="jump_node">The calling gate's world jump node</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, ref Vector3D jump_node)
		{
			if (!this.JumpGate.Closed && current_tick >= this.BeamPulseDefinition.StartTime && current_tick < this.BeamPulseDefinition.StartTime + this.Duration && this.Duration > 0)
			{
				ushort local_tick = (ushort) (current_tick - this.BeamPulseDefinition.StartTime);
				double tick_ratio = (this.BeamPulseDefinition.TravelTime == 0) ? 1 : MathHelper.Clamp(((double) local_tick) / this.BeamPulseDefinition.TravelTime, 0, 1);
				AnimationExpression.ExpressionArguments arguments = new AnimationExpression.ExpressionArguments(local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null);
				double frequency = Math.Max(0, AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.BeamFrequencyAnimation, arguments, this.BeamPulseDefinition.BeamFrequency));
				double beam_length = AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.ParticleLifeAnimation, arguments, this.BeamPulseDefinition.BeamLength);
				double duty_cycle = MathHelper.Clamp(AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.BeamDutyCycleAnimation, arguments, this.BeamPulseDefinition.BeamDutyCycle), 0, 1);
				double offset = AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.BeamOffsetAnimation, arguments, this.BeamPulseDefinition.BeamOffset);

				Vector3D beam_dir = endpoint - jump_node;
				Vector3D beam_dir_n = beam_dir.Normalized();
				Vector3D offset_vec = beam_dir_n * offset;
				beam_dir -= offset_vec;
				beam_length = (beam_length < 0) ? (beam_dir.Length() * tick_ratio) : beam_length;
				Vector3D beam_end = jump_node + beam_dir * tick_ratio;
				Vector3D beam_start = ((Vector3D.Distance(beam_end, jump_node) <= beam_length) ? jump_node : (beam_end - beam_dir_n * beam_length)) + offset_vec;

				double beam_width;
				Vector4 beam_color;

				if (this.FlashPointParticles != null)
				{
					MatrixD flash_matrix = MatrixD.Identity;
					flash_matrix.Translation = beam_end;
					foreach (Particle particle in this.FlashPointParticles) particle.Tick(current_tick, flash_matrix, drives, entities, ref endpoint, null);
				}

				if (frequency == 0)
				{
					arguments.SetThis(beam_end);
					beam_width = Math.Abs(AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.ParticleRadiusAnimation, arguments, this.BeamPulseDefinition.BeamWidth));
					beam_color = AttributeAnimationDef.GetAnimatedVectorValue(this.BeamPulseDefinition.Animations?.ParticleColorAnimation, arguments, this.BeamPulseDefinition.BeamColor.ToVector4D()) * new Vector4D(new Vector3D(Math.Abs(this.BeamPulseDefinition.BeamBrightness)), 1) * (this.SourceGateSettings?.JumpEffectAnimationColorShift().ToVector4D() ?? Vector4D.One);
					MySimpleObjectDraw.DrawLine(beam_start, beam_end, this.BeamMaterial, ref beam_color, (float) beam_width);
					return;
				}

				double beam_dir_length = beam_dir.Length();
				double waveform = (beam_dir_length - (beam_dir_length - beam_length)) / frequency;
				double w0 = waveform * duty_cycle;
				double w1 = waveform - w0;
				Vector3D delta = beam_dir_n * waveform;
				Vector3D on_delta = beam_dir_n * w0;
				beam_dir_length = 0;

				for (double i = 0; i < frequency; ++i)
				{
					arguments.SetThis(beam_start);
					beam_width = Math.Abs(AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.ParticleRadiusAnimation, arguments, this.BeamPulseDefinition.BeamWidth));
					beam_color = AttributeAnimationDef.GetAnimatedVectorValue(this.BeamPulseDefinition.Animations?.ParticleColorAnimation, arguments, this.BeamPulseDefinition.BeamColor.ToVector4D()) * new Vector4D(new Vector3D(Math.Abs(this.BeamPulseDefinition.BeamBrightness)), 1) * (this.SourceGateSettings?.JumpEffectAnimationColorShift().ToVector4D() ?? Vector4D.One);
					beam_dir_length += waveform;
					beam_end = beam_start + ((beam_dir_length > beam_length) ? (beam_dir_n * (waveform - (beam_dir_length - beam_length))) : on_delta);
					MySimpleObjectDraw.DrawLine(beam_start, beam_end, this.BeamMaterial, ref beam_color, (float) beam_width);
					beam_start += delta;
				}
			}
		}

		public override void Clean()
		{
			base.Clean();
			this.BeamPulseDefinition = null;
			this.BeamMaterial = null;
			if (this.FlashPointParticles == null) return;
			foreach (Particle particle in this.FlashPointParticles) particle.Clean();
			this.FlashPointParticles = null;
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for drive emitter emissive color animations
	/// </summary>
	internal sealed class DriveEmissiveColor : AnimatableObject
	{
		#region Private Variables
		/// <summary>
		/// Map mapping all drives to animate with their initial emitter emissive colors
		/// </summary>
		private readonly Dictionary<long, Color> InitialDriveColors = new Dictionary<long, Color>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The emitter emissive color definition
		/// </summary>
		public DriveEmissiveColorDef DriveEmissiveColorDef { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new emitter emissive color effect
		/// </summary>
		/// <param name="def">The emitter emissive color definition</param>
		/// <param name="animation_duration">The parent animation duration in game ticks</param>
		/// <param name="parent">The parent animation</param>
		/// <exception cref="ArgumentNullException">If the emitter emissive color definition is null</exception>
		public DriveEmissiveColor(DriveEmissiveColorDef def, ushort animation_duration, MyAnimation parent) : base((def.Duration == 0) ? animation_duration : def.Duration, false, parent)
		{
			if (def == null) throw new ArgumentNullException("DriveEmissiveColorDef cannot be null");
			this.DriveEmissiveColorDef = def;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this emitter emissive color effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint)
		{
			if (!this.JumpGate.Closed && current_tick >= this.DriveEmissiveColorDef.StartTime && current_tick < this.DriveEmissiveColorDef.StartTime + this.Duration && this.Duration > 0)
			{
				double tick = current_tick - this.DriveEmissiveColorDef.StartTime;
				ushort local_tick = (ushort) (current_tick - this.DriveEmissiveColorDef.StartTime);
				double tick_ratio = MathHelperD.Clamp((this.Duration == 0) ? tick : (tick / this.Duration), 0, 1);
				List<MyJumpGateDrive> working_drives = drives.Where((drive) => drive != null && drive.IsWorking).ToList();

				foreach (MyJumpGateDrive drive in working_drives)
				{
					if (!this.InitialDriveColors.ContainsKey(drive.BlockID)) this.InitialDriveColors.Add(drive.BlockID, drive.DriveEmitterColor);
					double brightness = this.DriveEmissiveColorDef.Brightness;
					Vector4D color = this.DriveEmissiveColorDef.EmissiveColor.ToVector4D() * (this.SourceGateSettings?.JumpEffectAnimationColorShift().ToVector4D() ?? Vector4D.One);
					AnimationExpression.ExpressionArguments arguments = new AnimationExpression.ExpressionArguments(local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null);
					color = AttributeAnimationDef.GetAnimatedVectorValue(this.DriveEmissiveColorDef.Animations?.ParticleColorAnimation, arguments, color);
					brightness = AttributeAnimationDef.GetAnimatedDoubleValue(this.DriveEmissiveColorDef.Animations?.ParticleColorIntensityAnimation, arguments, brightness);
					Vector4D start = this.InitialDriveColors[drive.BlockID].ToVector4();
					Vector4D result = (color - start) * tick_ratio + start;
					drive.EmitterEmissiveBrightness = brightness;
					drive.SetDriveEmitterColor(new Vector4((float) result.X, (float) result.Y, (float) result.Z, (float) result.W));
				}
			}
		}

		public override void Clean()
		{
			base.Clean();
			this.DriveEmissiveColorDef = null;
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for node physics definitions
	/// </summary>
	internal sealed class NodePhysics : AnimatableObject
	{
		#region Public Variables
		/// <summary>
		/// The node physics definition
		/// </summary>
		public NodePhysicsDef NodePhysicsDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new node physics effect
		/// </summary>
		/// <param name="def">The node physics definition</param>
		/// <param name="animation_duration">The parent animation duration in game ticks</param>
		/// <param name="parent">The parent animation</param>
		/// <param name="anti_node">Whether node physics should be placed at the anti-node</param>
		/// <exception cref="ArgumentNullException">If the node physics definition is null</exception>
		public NodePhysics(NodePhysicsDef def, ushort animation_duration, MyAnimation parent, bool anti_node) : base((def.Duration == 0) ? animation_duration : def.Duration, anti_node, parent)
		{
			if (def == null) throw new ArgumentNullException("NodePhysicsDef cannot be null");
			this.NodePhysicsDefinition = def;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this node physics effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint)
		{
			if (!this.JumpGate.Closed && current_tick >= this.NodePhysicsDefinition.StartTime && current_tick < this.NodePhysicsDefinition.StartTime + this.Duration && this.Duration > 0)
			{
				ushort local_tick = (ushort) (current_tick - this.NodePhysicsDefinition.StartTime);
				Vector3D jump_node = this.JumpGate.WorldJumpNode;
				double attractor_force = this.NodePhysicsDefinition.AttractorForce;
				double attractor_force_falloff = this.NodePhysicsDefinition.AttractorForceFalloff;
				Vector4D force_offset = new Vector4D(this.NodePhysicsDefinition.ForceOffset, 0);
				double max_speed = this.NodePhysicsDefinition.MaxSpeed;
				Vector4D torque = new Vector4D(this.NodePhysicsDefinition.AttractorTorque, 0);
				AnimationExpression.ExpressionArguments arguments = new AnimationExpression.ExpressionArguments(local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null);

				attractor_force = AttributeAnimationDef.GetAnimatedDoubleValue(this.NodePhysicsDefinition.Animations?.PhysicsForceAnimation, arguments, attractor_force);
				attractor_force_falloff = AttributeAnimationDef.GetAnimatedDoubleValue(this.NodePhysicsDefinition.Animations?.PhysicsForceFalloffAnimation, arguments, attractor_force_falloff);
				max_speed = AttributeAnimationDef.GetAnimatedDoubleValue(this.NodePhysicsDefinition.Animations?.PhysicsForceMaxSpeedAnimation, arguments, max_speed);

				force_offset = AttributeAnimationDef.GetAnimatedVectorValue(this.NodePhysicsDefinition.Animations?.PhysicsForceOffsetAnimation, arguments, force_offset);
				torque = AttributeAnimationDef.GetAnimatedVectorValue(this.NodePhysicsDefinition.Animations?.PhysicsForceTorqueAnimation, arguments, torque);

				if (attractor_force != 0)
				{
					Vector3D offset = new Vector3D(force_offset.X, force_offset.Y, force_offset.Z);
					Vector3D? _torque = null;
					float? speed = null;
					if (max_speed > 0) speed = (float) max_speed;
					if (torque != Vector4D.Zero) _torque = new Vector3D(torque.X, torque.Y, torque.Z);
					double max_distance = this.JumpGate.JumpEllipse.Radii.Max();

					foreach (MyEntity entity in entities)
					{
						if (entity.GetPhysicsBody() == null) continue;
						Vector3D force_dir = ((((this.IsAntiNode) ? endpoint : jump_node) - entity.WorldMatrix.Translation).Normalized() + offset) * attractor_force;
						Vector3D mass_center;

						try
						{
							mass_center = entity.GetPhysicsBody().CenterOfMassWorld;
						}
						catch (NullReferenceException)
						{
							mass_center = entity.WorldMatrix.Translation;
						}

						double distance = Vector3D.Distance(mass_center, jump_node);
						double ratio = Math.Pow(1 - MathHelperD.Clamp(distance / max_distance, 0, 1), attractor_force_falloff);
						entity.GetPhysicsBody().AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force_dir * ratio, mass_center, _torque, speed);
					}
				}
			}
		}

		public override void Clean()
		{
			base.Clean();
			this.NodePhysicsDefinition = null;
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for drive entity lock definitions
	/// </summary>
	internal sealed class DriveEntityLock : AnimatableObject
	{
		private sealed class DriveEntityLockInfo
		{
			public bool Stopped { get; private set; } = false;

			public EasingCurveEnum EasingCurve;

			public EasingTypeEnum EasingType;

			public ushort EntityLockTime { get; private set; }

			public ushort ParentDuration { get; private set; }

			public float RatioModifier { get; private set; }

			public Vector3D InitialRotation { get; private set; }

			public MyJumpGateDrive JumpGateDrive { get; private set; }

			public MyAnimation Parent { get; private set; }

			public Dictionary<MyEntity, Particle[]> LockedEntityParticles { get; private set; } = new Dictionary<MyEntity, Particle[]>();

			public DriveEntityLockInfo(ushort lock_time, ushort parent_duration, DriveEntityLockDef def, MyJumpGateDrive drive, MyAnimation parent)
			{
				this.EasingCurve = def.LockDelayEasingCurve;
				this.EasingType = def.LockDelayEasingType;
				this.EntityLockTime = lock_time;
				this.ParentDuration = parent_duration;
				this.RatioModifier = MathHelper.Clamp(def.RatioModifier, -1f, 1f);
				this.InitialRotation = def.InitialRotation * (Math.PI / 180d);
				this.JumpGateDrive = drive;
				this.Parent = parent;
			}

			public void Tick(ushort local_tick, ParticleDef[] particle_effects, List<MyEntity> locked_entities, List<MyJumpGateDrive> gate_drives, ref Vector3D endpoint)
			{
				if (particle_effects == null) return;
				this.Stopped = false;

				foreach (MyEntity entity in locked_entities)
				{
					if (this.LockedEntityParticles.ContainsKey(entity)) continue;
					Particle[] effects = new Particle[particle_effects.Length];

					for (int i = 0; i < particle_effects.Length; ++i)
					{
						MatrixD matrix = this.JumpGateDrive.WorldMatrix;
						effects[i] = new Particle(particle_effects[i], this.ParentDuration, this.Parent, matrix, matrix.Translation, false);
					}

					this.LockedEntityParticles[entity] = effects;
				}

				double entity_lock_ratio = (this.EntityLockTime == 0) ? 1 : MathHelper.Clamp((double) local_tick / this.EntityLockTime, 0, 1);
				entity_lock_ratio = EasingFunctor.GetEaseResult(entity_lock_ratio, this.EasingType, this.EasingCurve);
				MatrixD drive_matrix = this.JumpGateDrive.WorldMatrix;
				drive_matrix.Translation = this.JumpGateDrive.GetDriveRaycastStartpoint();
				MatrixD rotated_drive_matrix = MatrixD.CreateFromYawPitchRoll(this.InitialRotation.Y, this.InitialRotation.X, this.InitialRotation.Z) * drive_matrix;
				List<MyEntity> removed_entities = new List<MyEntity>();
				if (this.RatioModifier > 0) entity_lock_ratio *= this.RatioModifier;
				else if (this.RatioModifier < 0) entity_lock_ratio = 1 - (entity_lock_ratio * -this.RatioModifier);

				foreach (KeyValuePair<MyEntity, Particle[]> pair in this.LockedEntityParticles)
				{
					if (!locked_entities.Contains(pair.Key))
					{
						foreach (Particle particle in pair.Value) particle.Clean();
						removed_entities.Add(pair.Key);
						continue;
					}

					Vector3D dir = (pair.Key.WorldMatrix.Translation - drive_matrix.Translation);
					MatrixD entity_drive_matrix = MatrixD.CreateWorld(drive_matrix.Translation, dir, dir.Cross(dir + Vector3D.One));
					MatrixD.Lerp(ref rotated_drive_matrix, ref entity_drive_matrix, entity_lock_ratio, out entity_drive_matrix);
					foreach (Particle particle in pair.Value) particle.Tick(local_tick, MatrixD.Normalize(entity_drive_matrix), gate_drives, locked_entities, ref endpoint, pair.Key, drive_matrix.Translation);
				}

				foreach (MyEntity entity in removed_entities) this.LockedEntityParticles.Remove(entity);
			}

			public void Clean()
			{
				foreach (KeyValuePair<MyEntity, Particle[]> pair in this.LockedEntityParticles) foreach (Particle particle in pair.Value) particle.Clean();
				this.LockedEntityParticles?.Clear();
				this.LockedEntityParticles = null;
				this.JumpGateDrive = null;
				this.Stopped = true;
				this.Parent = null;
			}

			public void Stop()
			{
				if (this.Stopped) return;
				foreach (KeyValuePair<MyEntity, Particle[]> pair in this.LockedEntityParticles) foreach (Particle particle in pair.Value) particle.Stop();
				this.LockedEntityParticles?.Clear();
				this.Stopped = true;
			}
		}

		#region Private Variables
		private List<MyEntity> LockedEntities = new List<MyEntity>();

		private Dictionary<MyJumpGateDrive, DriveEntityLockInfo> DriveInfo = new Dictionary<MyJumpGateDrive, DriveEntityLockInfo>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The drive entity lock definition
		/// </summary>
		public DriveEntityLockDef DriveEntityLockDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new drive entity lock effect
		/// </summary>
		/// <param name="def">The drive entity lock definition</param>
		/// <param name="animation_duration">The parent animation duration in game ticks</param>
		/// <param name="parent">The parent animation</param>
		/// <exception cref="ArgumentNullException">If the drive entity lock definition is null</exception>
		public DriveEntityLock(DriveEntityLockDef def, ushort animation_duration, MyAnimation parent) : base((def.Duration == 0) ? animation_duration : def.Duration, false, parent)
		{
			if (def == null) throw new ArgumentNullException("DriveEntityLockDef cannot be null");
			this.DriveEntityLockDefinition = def;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this node physics effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint)
		{
			if (!this.JumpGate.Closed && current_tick >= this.DriveEntityLockDefinition.StartTime && current_tick < this.DriveEntityLockDefinition.StartTime + this.Duration && this.Duration > 0 && this.DriveEntityLockDefinition.EntityLockParticles != null)
			{
				ushort local_tick = (ushort) (current_tick - this.DriveEntityLockDefinition.StartTime);
				Vector3D jump_node = this.JumpGate.WorldJumpNode;
				double farthest = 0;
				double shortest = double.MaxValue;
				Random prng = new Random();

				foreach (MyJumpGateDrive drive in drives)
				{
					double distance = Vector3D.Distance(drive.WorldMatrix.Translation, jump_node);
					farthest = Math.Max(farthest, distance);
					shortest = Math.Min(shortest, distance);
				}

				foreach (MyJumpGateDrive drive in drives)
				{
					if (this.DriveInfo.ContainsKey(drive)) continue;
					double distance = Vector3D.Distance(drive.WorldMatrix.Translation, jump_node);
					float ratio = 0.5f;

					switch (this.DriveEntityLockDefinition.LockDelayShift)
					{
						case LockDelayShiftEnum.NEAREST:
							ratio = (float) MathHelper.Clamp((distance - shortest) / (farthest - shortest), 0, 1);
							break;
						case LockDelayShiftEnum.FARTHEST:
							ratio = (float) (1 - MathHelper.Clamp((distance - shortest) / (farthest - shortest), 0, 1));
							break;
						case LockDelayShiftEnum.RANDOM:
							ratio = (float) prng.NextDouble();
							break;
					}

					ushort lock_time = (ushort) Math.Round((this.DriveEntityLockDefinition.MaxLockTime - this.DriveEntityLockDefinition.MinLockTime) * ratio + this.DriveEntityLockDefinition.MinLockTime);
					this.DriveInfo[drive] = new DriveEntityLockInfo(lock_time, this.Duration, this.DriveEntityLockDefinition, drive, this.Parent);
				}

				this.LockedEntities.Clear();
				if (entities.Count == 0) return;
				int ncount = (this.DriveEntityLockDefinition.MaxEntityLockCount == 0) ? entities.Count : this.DriveEntityLockDefinition.MaxEntityLockCount;
				List<MyJumpGateDrive> closed_drives = new List<MyJumpGateDrive>();

				switch (this.DriveEntityLockDefinition.EntityLockType)
				{
					case EntityLockTypeEnum.LOCK_NEAREST:
						this.LockedEntities.AddRange(entities.OrderBy((entity) => Vector3D.DistanceSquared(entity.WorldMatrix.Translation, jump_node)).Take(ncount));
						break;
					case EntityLockTypeEnum.LOCK_FARTHEST:
						this.LockedEntities.AddRange(entities.OrderByDescending((entity) => Vector3D.DistanceSquared(entity.WorldMatrix.Translation, jump_node)).Take(ncount));
						break;
					case EntityLockTypeEnum.LOCK_RANDOM:
						if (local_tick != 0) break;
						for (int i = 0; i < ncount; ++i) this.LockedEntities.Add(entities.GetRandomItemFromList());
						break;
				}

				foreach (KeyValuePair<MyJumpGateDrive, DriveEntityLockInfo> pair in this.DriveInfo)
				{
					if (!drives.Contains(pair.Key))
					{
						pair.Value.Clean();
						closed_drives.Add(pair.Key);
						continue;
					}

					pair.Value.Tick(local_tick, this.DriveEntityLockDefinition.EntityLockParticles, this.LockedEntities, drives, ref endpoint);
				}

				foreach (MyJumpGateDrive drive in closed_drives) this.DriveInfo.Remove(drive);
			}
		}

		/// <summary>
		/// Stops this effect temporarily
		/// </summary>
		public void Stop()
		{
			if (this.DriveInfo == null || this.DriveInfo.Count == 0) return;
			foreach (KeyValuePair<MyJumpGateDrive, DriveEntityLockInfo> pair in this.DriveInfo) pair.Value.Stop();
			this.DriveInfo.Clear();
			this.LockedEntities?.Clear();
		}

		public override void Clean()
		{
			base.Clean();
			foreach (KeyValuePair<MyJumpGateDrive, DriveEntityLockInfo> pair in this.DriveInfo) pair.Value.Clean();
			this.DriveInfo?.Clear();
			this.LockedEntities?.Clear();
			this.DriveInfo = null;
			this.LockedEntities = null;
			this.DriveEntityLockDefinition = null;
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for shape collider definitions
	/// </summary>
	internal sealed class ShapeCollider : AnimatableObject
	{
		#region Private Static Variables
		/// <summary>
		/// List of all shape collider physical collider entities
		/// </summary>
		private static List<MyEntity> PhysicalColliders = new List<MyEntity>();
		#endregion

		#region Private Variables
		/// <summary>
		/// Mutext used to access raw collider entities
		/// </summary>
		private readonly object ColliderMutex = new object();

		/// <summary>
		/// The last calculated collider inner cutout percent
		/// </summary>
		private Vector3 LastCalculatedInnerCutoutPercent = Vector3.Zero;

		/// <summary>
		/// The shape collider's physical collider
		/// </summary>
		private MyEntity PhysicalCollider;

		/// <summary>
		/// The list of raw entities inside this collider
		/// </summary>
		private List<MyEntity> RawColliderEntities = new List<MyEntity>();

		/// <summary>
		/// The list of entities inside this collider
		/// </summary>
		private List<MyEntity> ColliderEntities = new List<MyEntity>();

		/// <summary>
		/// Temporary list of grid blocks and their world positions
		/// </summary>
		private List<KeyValuePair<IMySlimBlock, Vector3D>> GridBlocks = new List<KeyValuePair<IMySlimBlock, Vector3D>>();
		#endregion

		#region Public Variables
		/// <summary>
		/// True if shape collider is valid
		/// </summary>
		public bool IsValid => this.PhysicalCollider?.Physics != null && this.RawColliderEntities != null && !this.PhysicalCollider.MarkedForClose;

		/// <summary>
		/// This collider's world matrix or null
		/// </summary>
		public MatrixD WorldMatrix => this.PhysicalCollider.WorldMatrix;

		/// <summary>
		/// The sound definition
		/// </summary>
		public ShapeColliderDef ShapeColliderDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new shape collider
		/// </summary>
		/// <param name="def">The shape collider definition</param>
		/// <param name="parent">The parent animation</param>
		/// <param name="anti_node">Whether this collider should be placed at the anti-node</param>
		/// <exception cref="ArgumentNullException">If the shape collider definition or jump gate is null</exception>
		public ShapeCollider(ShapeColliderDef def, ushort animation_duration, MyAnimation parent, bool anti_node) : base((def.Duration == 0) ? animation_duration : def.Duration, anti_node, parent)
		{
			if (def == null) throw new ArgumentNullException("ShapeColliderDef cannot be null");
			this.ShapeColliderDefinition = def;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback for entity intersecting physical collider
		/// </summary>
		/// <param name="ientity">The entity intersecting surface</param>
		/// <param name="is_entering">Whether entity is entering or leaving</param>
		private void OnEntityCollision(IMyEntity ientity, bool is_entering)
		{
			if (this.RawColliderEntities == null) return;

			lock (this.ColliderMutex)
			{
				MyEntity entity = ientity as MyEntity;
				if (entity == null || entity == this.PhysicalCollider || ShapeCollider.PhysicalColliders.Contains(entity) || this.JumpGate.JumpGateGrid.IsEntityPartOfConstruct(entity)) return;
				else if (is_entering && !this.RawColliderEntities.Contains(entity)) this.RawColliderEntities.Add(entity);
				else if (!is_entering) this.RawColliderEntities.Remove(entity);
			}
		}

		/// <summary>
		/// Updates the physical collider with new extents and world matrix
		/// </summary>
		/// <param name="world_matrix">The new collider world matrix</param>
		private void UpdatePhysicalCollider(ref MatrixD world_matrix, float multiplier)
		{
			Vector3D scale = (this.PhysicalCollider == null) ? Vector3D.Zero : this.PhysicalCollider.WorldMatrix.Scale;
			//Logger.Debug($"COLLIDER_UPDATE COLLIDER={this.PhysicalCollider?.DisplayName}, TYPE={this.ShapeColliderDefinition?.CollisionEffectType}, EXTENTS={this.PhysicalCollider?.WorldMatrix.Scale}, NEW_EXTENTS={world_matrix.Scale}, SCALED_EXTENTS={extended_matrix.Scale}, SUB={((this.PhysicalCollider == null) ? Vector3D.NegativeInfinity : (world_matrix.Scale - this.PhysicalCollider.WorldMatrix.Scale))}, CHECK={this.PhysicalCollider != null && (world_matrix.Scale - this.PhysicalCollider.WorldMatrix.Scale).Max() <= 0}", 4);

			if (this.PhysicalCollider != null && (world_matrix.Scale - scale).Max() <= 1e-3) {
				MatrixD sub_scalar, matrix;
				MatrixD.CreateScale(ref scale, out sub_scalar);
				matrix = sub_scalar * MatrixD.Normalize(world_matrix);
				this.PhysicalCollider.PositionComp.Scale = null;
				this.PhysicalCollider.PositionComp.SetWorldMatrix(ref matrix, skipTeleportCheck: true, forceUpdate: true);
				return;
			}

			MatrixD scalar = MatrixD.CreateScale(multiplier);
			MatrixD extended_matrix = scalar * world_matrix;
			string gate_name = this.JumpGate.GetPrintableName();
			this.PhysicalCollider?.Close();
			this.PhysicalCollider = new MyEntity()
			{
				EntityId = 0,
				Save = false,
				Flags = EntityFlags.IsNotGamePrunningStructureObject,
			};
			this.PhysicalCollider.Init(new StringBuilder($"JumpGateShapeCollider_{this.JumpGate.JumpGateGrid.CubeGridID}_{this.JumpGate.JumpGateID}"), null, null, 1f);
			this.PhysicalCollider.OnClosing += (entity) => Logger.Debug($"CLOSED_SHAPE_COLLIDER COLLIDER={entity.DisplayName}, TYPE={this.ShapeColliderDefinition?.CollisionEffectType}, EXTENTS={entity.WorldMatrix.Scale}", 4);
			this.PhysicalCollider.PositionComp.Scale = null;
			this.PhysicalCollider.PositionComp.SetWorldMatrix(ref extended_matrix, skipTeleportCheck: true, forceUpdate: true);
			PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForDetector(this.PhysicalCollider, this.OnEntityCollision, MatrixD.Identity, Vector3.Zero, RigidBodyFlag.RBF_KINEMATIC, 15, true);
			MyAPIGateway.Physics.CreateBoxPhysics(settings, extended_matrix.Scale, 0);
			MyAPIGateway.Entities.AddEntity(this.PhysicalCollider);
			ShapeCollider.PhysicalColliders.Add(this.PhysicalCollider);
			Logger.Debug($"CREATED_SHAPE_COLLIDER COLLIDER={this.PhysicalCollider.DisplayName}, TYPE={this.ShapeColliderDefinition.CollisionEffectType}, EXTENTS={this.PhysicalCollider.WorldMatrix.Scale}, OLD_EXTENS={scale}", 4);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this sound effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint)
		{
			if (this.JumpGate == null || this.JumpGate.Closed || this.TargetGate == null || this.TargetGate.Closed || this.ColliderEntities == null || this.RawColliderEntities == null || this.ShapeColliderDefinition.CollisionShape == CollisionShapeEnum.NONE || this.ShapeColliderDefinition.CollisionEffectType == CollisionEffectTypeEnum.NONE) return;

			if (current_tick >= this.ShapeColliderDefinition.StartTime && current_tick < this.ShapeColliderDefinition.StartTime + this.Duration)
			{
				const float extent_multiplier = 1.5f;
				const float extent_multiplier_inv = 1 / extent_multiplier;
				ushort local_tick = (ushort) (current_tick - this.ShapeColliderDefinition.StartTime);

				if (local_tick % (ushort) this.ShapeColliderDefinition.EffectArguments[0] == 0)
				{
					this.ColliderEntities.Clear();
					MyJumpGate gate = (this.IsAntiNode) ? this.TargetGate : this.JumpGate;
					AnimationExpression.ExpressionArguments arguments = new AnimationExpression.ExpressionArguments(local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null);
					Vector4D inner_cutout = AttributeAnimationDef.GetAnimatedVectorValue(this.ShapeColliderDefinition.Animations?.ShapeInnerCutoutAnimation, arguments, new Vector4D(this.ShapeColliderDefinition.InnerCutoutPercent, 0));
					Vector4D offset = AttributeAnimationDef.GetAnimatedVectorValue(this.ShapeColliderDefinition.Animations?.ParticleOffsetAnimation, arguments, new Vector4D(this.ShapeColliderDefinition.Position, 0));
					Vector4D locked_rotation = AttributeAnimationDef.GetAnimatedVectorValue(this.ShapeColliderDefinition.Animations?.ShapeLockedRotationAnimation, arguments, new Vector4D(this.ShapeColliderDefinition.LockedRotation, 0));
					Vector4D free_rotation = AttributeAnimationDef.GetAnimatedVectorValue(this.ShapeColliderDefinition.Animations?.ShapeFreeRotationAnimation, arguments, new Vector4D(this.ShapeColliderDefinition.FreeRotation, 0));
					Vector4D scale = AttributeAnimationDef.GetAnimatedVectorValue(this.ShapeColliderDefinition.Animations?.ShapeScaleAnimation, arguments, new Vector4D(this.ShapeColliderDefinition.Scale, 0));
					this.LastCalculatedInnerCutoutPercent = new Vector3(inner_cutout);

					MatrixD inverse, cutout;
					MatrixD matrix = MatrixD.CreateScale(scale.X, scale.Y, scale.Z);
					QuaternionD rotation_quat = QuaternionD.CreateFromYawPitchRoll(MathHelper.ToRadians(free_rotation.Y), MathHelper.ToRadians(free_rotation.Z + ((this.ShapeColliderDefinition.CollisionShape != CollisionShapeEnum.ELLIPSOID) ? 90 : 0)), MathHelper.ToRadians(free_rotation.X));
					matrix = Extensions.Extensions.Transform(ref matrix, ref rotation_quat);
					matrix = MatrixD.CreateTranslation(offset.X, offset.Y, offset.Z) * matrix;
					rotation_quat = QuaternionD.CreateFromYawPitchRoll(MathHelper.ToRadians(locked_rotation.Y), MathHelper.ToRadians(locked_rotation.Z), MathHelper.ToRadians(locked_rotation.X));
					matrix = Extensions.Extensions.Transform(ref matrix, ref rotation_quat);
					matrix *= gate.GetWorldMatrix(true, true);
					this.UpdatePhysicalCollider(ref matrix, extent_multiplier);
					MatrixD.Invert(ref matrix, out inverse);
					cutout = MatrixD.CreateScale(this.LastCalculatedInnerCutoutPercent) * matrix;
					cutout.Translation = matrix.Translation;
					cutout = MatrixD.Invert(cutout);

					lock (this.ColliderMutex)
					{
						this.RawColliderEntities.RemoveAll((entity) => this.JumpGate.IsEntityInArrivalQueue(entity) || this.JumpGate.JumpGateGrid.IsEntityPartOfConstruct(entity));

						switch (this.ShapeColliderDefinition.CollisionEffectType)
						{
							case CollisionEffectTypeEnum.DAMAGE:
								foreach (MyEntity entity in this.RawColliderEntities)
								{
									MyEntity topmost = entity.GetTopMostParent();
									if ((entity.Physics == null && topmost.Physics == null) || gate.JumpGateGrid.HasCubeGrid(topmost as MyCubeGrid) || (!(entity is IMyCubeGrid) && !this.IsEntityInsideCollider(entity, ref inverse, ref cutout))) continue;
									else if (entity.Physics == null && topmost is IMyDestroyableObject) ((IMyDestroyableObject) topmost).DoDamage((float) this.ShapeColliderDefinition.EffectArguments[1], MyDamageType.Environment, true);
									else if (entity.Physics != null && entity is IMyDestroyableObject) ((IMyDestroyableObject) entity).DoDamage((float) this.ShapeColliderDefinition.EffectArguments[1], MyDamageType.Environment, true);
									else if (entity is IMyCubeGrid)
									{
										MyCubeGrid grid = (MyCubeGrid) entity;
										BoundingBoxD bbox = grid.PositionComp.WorldAABB;
										Vector3D center = bbox.Center;

										if (this.IsPointInsideCollider(ref center, ref inverse, ref cutout) || Enumerable.Range(0, 8).Any((index) => {
											Vector3D corner = bbox.GetCorner(index);
											return this.IsPointInsideCollider(ref corner, ref inverse, ref cutout);
										}))
										{
											object mutex = new object();

											MyJumpGateModSession.ParallelFor(grid.GetBlocks(), (block) => {
												Vector3D world_pos = grid.GridIntegerToWorld(((IMySlimBlock) block).Position);
												if (!this.IsPointInsideCollider(ref world_pos, ref inverse, ref cutout)) return;
												lock (mutex) this.GridBlocks.Add(new KeyValuePair<IMySlimBlock, Vector3D>(block, world_pos));
											});
										}

										foreach (KeyValuePair<IMySlimBlock, Vector3D> pair in this.GridBlocks) pair.Key.DoDamage((float) this.ShapeColliderDefinition.EffectArguments[1], MyDamageType.Environment, true);
										this.GridBlocks.Clear();
									}
								}

								break;
							case CollisionEffectTypeEnum.JUMP:
								this.ColliderEntities.Clear();

								foreach (MyEntity entity in this.RawColliderEntities.Select((e) => e.GetTopMostParent()).Distinct())
								{
									if (entity.Physics == null || gate.JumpGateGrid.HasCubeGrid(entity.GetTopMostParent() as MyCubeGrid) || (!(entity is IMyCubeGrid) && !this.IsEntityInsideCollider(entity, ref inverse, ref cutout))) continue;
									else if (entity is IMyCubeGrid)
									{
										MyCubeGrid grid = (MyCubeGrid) entity;
										BoundingBoxD bbox = grid.PositionComp.WorldAABB;
										Vector3D center = bbox.Center;

										if (this.IsPointInsideCollider(ref center, ref inverse, ref cutout) || Enumerable.Range(0, 8).Any((index) => {
											Vector3D corner = bbox.GetCorner(index);
											return this.IsPointInsideCollider(ref corner, ref inverse, ref cutout);
										}))
										{
											object mutex = new object();

											MyJumpGateModSession.ParallelFor(grid.GetBlocks(), (block) => {
												Vector3D world_pos = grid.GridIntegerToWorld(((IMySlimBlock) block).Position);
												if (!this.IsPointInsideCollider(ref world_pos, ref inverse, ref cutout)) return;
												lock (mutex) this.ColliderEntities.Add(grid);
											});
										}
									}
									else this.ColliderEntities.Add(entity);
								}

								break;
							case CollisionEffectTypeEnum.DELETE:
								foreach (MyEntity entity in this.RawColliderEntities.Select((e) => e.GetTopMostParent()).Distinct())
								{
									if (entity.Physics == null || gate.JumpGateGrid.HasCubeGrid(entity as MyCubeGrid) || (!(entity is IMyCubeGrid) && !this.IsEntityInsideCollider(entity, ref inverse, ref cutout))) continue;
									else if (entity is IMyCubeGrid)
									{
										MyCubeGrid grid = (MyCubeGrid) entity;
										BoundingBoxD bbox = grid.PositionComp.WorldAABB;
										Vector3D center = bbox.Center;

										if (this.IsPointInsideCollider(ref center, ref inverse, ref cutout) || Enumerable.Range(0, 8).Any((index) => {
											Vector3D corner = bbox.GetCorner(index);
											return this.IsPointInsideCollider(ref corner, ref inverse, ref cutout);
										}))
										{
											object mutex = new object();

											MyJumpGateModSession.ParallelFor(grid.GetBlocks(), (block) => {
												Vector3D world_pos = grid.GridIntegerToWorld(((IMySlimBlock) block).Position);
												if (!this.IsPointInsideCollider(ref world_pos, ref inverse, ref cutout)) return;
												lock (mutex) this.GridBlocks.Add(new KeyValuePair<IMySlimBlock, Vector3D>(block, world_pos));
											});
										}

										foreach (KeyValuePair<IMySlimBlock, Vector3D> pair in this.GridBlocks) ((IMyCubeGrid) grid).RemoveDestroyedBlock(pair.Key);
										this.GridBlocks.Clear();
									}
									else if (entity is IMyCharacter && !MyAPIGateway.Session.CreativeMode) ((IMyCharacter) entity).Kill();
									else if (!(entity is IMyCharacter)) entity.Close();
								}

								break;
						}
					}
				}

				if (MyJumpGateModSession.Instance.DebugMode && this.LastCalculatedInnerCutoutPercent.AbsMin() < 1 && this.PhysicalCollider != null)
				{
					Color color;

					switch (this.ShapeColliderDefinition.CollisionEffectType)
					{
						case CollisionEffectTypeEnum.DAMAGE:
							color = Color.Gold;
							break;
						case CollisionEffectTypeEnum.JUMP:
							color = Color.Cyan;
							break;
						case CollisionEffectTypeEnum.DELETE:
							color = Color.Red;
							break;
						default:
							return;
					}

					Vector4 colorv4 = color;
					MatrixD matrix = this.PhysicalCollider.WorldMatrix;
					MatrixD inner_cutout = MatrixD.CreateScale(this.LastCalculatedInnerCutoutPercent) * matrix;

					switch (this.ShapeColliderDefinition.CollisionShape)
					{
						case CollisionShapeEnum.ELLIPSOID:
							MySimpleObjectDraw.DrawTransparentSphere(ref matrix, extent_multiplier_inv, ref color, MySimpleObjectRasterizer.Wireframe, 32, null, MyJumpGateModSession.Instance.Materials.WeaponLaser, 0.1f);
							if (this.LastCalculatedInnerCutoutPercent.AbsMax() == 0) break;
							MySimpleObjectDraw.DrawTransparentSphere(ref inner_cutout, extent_multiplier_inv, ref color, MySimpleObjectRasterizer.Wireframe, 32, null, MyJumpGateModSession.Instance.Materials.WeaponLaser, 0.1f);
							break;
						case CollisionShapeEnum.CUBE:
							Vector3D point = new Vector3D(extent_multiplier_inv);
							BoundingBoxD local = new BoundingBoxD(-point, point);
							MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref local, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.1f, null, MyJumpGateModSession.Instance.Materials.WeaponLaser);
							if (this.LastCalculatedInnerCutoutPercent.AbsMax() == 0) break;
							MySimpleObjectDraw.DrawTransparentBox(ref inner_cutout, ref local, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.1f, null, MyJumpGateModSession.Instance.Materials.WeaponLaser);
							break;
						case CollisionShapeEnum.CYLINDER:
							MySimpleObjectDraw.DrawTransparentCylinder(ref matrix, extent_multiplier_inv, extent_multiplier_inv, extent_multiplier_inv, ref colorv4, true, 32, 0.1f, MyJumpGateModSession.Instance.Materials.WeaponLaser);
							if (this.LastCalculatedInnerCutoutPercent.AbsMax() == 0) break;
							MySimpleObjectDraw.DrawTransparentCylinder(ref inner_cutout, extent_multiplier_inv, extent_multiplier_inv, extent_multiplier_inv, ref colorv4, true, 32, 0.1f, MyJumpGateModSession.Instance.Materials.WeaponLaser);
							break;
					}
				}
			}
		}

		public override void Clean()
		{
			base.Clean();
			if (this.PhysicalCollider != null) ShapeCollider.PhysicalColliders.Remove(this.PhysicalCollider);
			this.RawColliderEntities?.Clear();
			this.ColliderEntities?.Clear();
			this.PhysicalCollider?.Close();
			this.RawColliderEntities = null;
			this.ColliderEntities = null;
			this.PhysicalCollider = null;
			this.ShapeColliderDefinition = null;
		}

		/// <summary>
		/// Checks if a point is inside the collider defined by the given matrix<br />
		/// </summary>
		/// <param name="position">The point to check</param>
		/// <param name="inverse_collider">The collider inverse world matrix</param>
		/// <param name="inner_cutout">The cutout shape inverse world matrix</param>
		/// <returns>Whether position is inside collider</returns>
		public bool IsPointInsideCollider(ref Vector3D position, ref MatrixD inverse_collider, ref MatrixD inner_cutout)
		{
			Vector3D normal_pos, cutout_pos;
			Vector3D.Transform(ref position, ref inverse_collider, out normal_pos);
			if (inner_cutout.IsNan()) cutout_pos = Vector3D.One;
			else Vector3D.Transform(ref position, ref inner_cutout, out cutout_pos);

			switch (this.ShapeColliderDefinition.CollisionShape)
			{
				case CollisionShapeEnum.ELLIPSOID:
					return (normal_pos * normal_pos).Sum <= 1 && (cutout_pos * cutout_pos).Sum >= 1;
				case CollisionShapeEnum.CUBE:
					return normal_pos.Max() <= 1 && cutout_pos.Max() >= 1;
				case CollisionShapeEnum.CYLINDER:
					return Math.Abs(normal_pos.Y) <= 0.5 && Math.Abs(cutout_pos.Y) >= 0.5 && (normal_pos * normal_pos * new Vector3D(1, 0, 1)).Sum <= 1 && (cutout_pos * cutout_pos * new Vector3D(1, 0, 1)).Sum >= 1;
				default:
					return false;
			}
		}

		/// <summary>
		/// Checks if an entity is inside the collider defined by the given matrix<br />
		/// </summary>
		/// <param name="entity">The entity to check</param>
		/// <param name="inverse_collider">The collider inverse world matrix</param>
		/// <param name="inner_cutout">The cutout shape inverse world matrix</param>
		/// <returns>Whether entity translation is inside collider</returns>
		public bool IsEntityInsideCollider(MyEntity entity, ref MatrixD inverse_collider, ref MatrixD inner_cutout)
		{
			if (entity?.Physics == null) return false;
			Vector3D position;
			if (entity is IMyCubeGrid) position = MyJumpGateModSession.Instance.GetJumpGateGrid(entity as IMyCubeGrid)?.ConstructVolumeCenter() ?? ((IMyCubeGrid) entity).WorldVolume.Center;
			else position = entity.WorldMatrix.Translation;
			return this.IsPointInsideCollider(ref position, ref inverse_collider, ref inner_cutout);
		}

		/// <summary>
		/// Calculates the bounding box of this collider
		/// </summary>
		/// <param name="collider">The collider world matrix</param>
		/// <returns>The bounds</returns>
		public BoundingBoxD CalculateColliderBounds(ref MatrixD collider)
		{
			Vector3D min = -Vector3D.One;
			Vector3D max = Vector3D.One;
			Vector3D.Transform(ref min, ref collider, out min);
			Vector3D.Transform(ref max, ref collider, out max);
			return new BoundingBoxD(min, max);
		}

		/// <returns>All entities within this collider</returns>
		public IEnumerable<MyEntity> GetColliderEntities()
		{
			return this.ColliderEntities;
		}
		#endregion
	}
}
