using IOTA.ModularJumpGates.Animations;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.Animations
{
	public partial class AnimationDefinitions
	{

	}
}

namespace IOTA.ModularJumpGates.Animation
{
	/// <summary>
	/// Wrapper containing the full animation for all phases
	/// </summary>
	internal class MyJumpGateAnimation : IEquatable<MyJumpGateAnimation>
	{
		public enum AnimationTypeEnum { JUMPING, JUMPED, FAILED, WORMHOLE_OPEN, WORMHOLE_LOOP, WORMHOLE_CLOSE }

		#region Private Variables
		/// <summary>
		/// Whether entities should be updated or use a static list
		/// </summary>
		private readonly bool IsEntityCollectionLocked;

		/// <summary>
		/// The jump type of the calling gate
		/// </summary>
		private readonly MyJumpTypeEnum GateJumpType;

		private readonly int HashCode;
		#endregion

		#region Temporary Collections
		private List<MyJumpGateDrive> TEMP_JumpGateDrives = new List<MyJumpGateDrive>();
		private List<MyJumpGateDrive> TEMP_JumpGateAntiDrives = new List<MyJumpGateDrive>();
		private List<MyEntity> TEMP_JumpGateEntitiesL = new List<MyEntity>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The index indicating the currently playing animation
		/// </summary>
		public short ActiveAnimationIndex { get; private set; } = -1;

		/// <summary>
		/// Whether this animation is paused
		/// </summary>
		public bool Paused { get; private set; } = false;

		public readonly IMyPlayer Caller;

		/// <summary>
		/// The calling jump gate
		/// </summary>
		public MyJumpGate JumpGate { get; private set; }

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		public MyJumpGate TargetGate { get; private set; }

		/// <summary>
		/// The controller settings used to enact the jump
		/// </summary>
		public MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings { get; private set; }

		/// <summary>
		/// The controller settings of the targeted jump gate or null
		/// </summary>
		public MyJumpGateController.MyControllerBlockSettingsStruct TargetControllerSettings { get; private set; }

		/// <summary>
		/// The "jumping/charging" animation definition
		/// </summary>
		public JumpGateJumpingAnimationDef GateJumpingAnimationDef { get; private set; }

		/// <summary>
		/// The "jumped" animation definition
		/// </summary>
		public JumpGateJumpedAnimationDef GateJumpedAnimationDef { get; private set; }

		/// <summary>
		/// The "failed" animation definition
		/// </summary>
		public JumpGateFailedAnimationDef GateFailedAnimationDef { get; private set; }

		/// <summary>
		/// The wormhole open animation definition
		/// </summary>
		public JumpGateWormholeAnimationDef GateWormholeOpenAnimationDef { get; private set; }

		/// <summary>
		/// The wormhole loop animation definition
		/// </summary>
		public JumpGateWormholeAnimationDef GateWormholeLoopAnimationDef { get; private set; }

		/// <summary>
		/// The wormhole close animation definition
		/// </summary>
		public JumpGateWormholeAnimationDef GateWormholeCloseAnimationDef { get; private set; }

		/// <summary>
		/// The "jumping/charging" animation
		/// </summary>
		public MyJumpGateJumpingAnimation GateJumpingAnimation { get; private set; }

		/// <summary>
		/// The "jumped" animation
		/// </summary>
		public MyJumpGateJumpedAnimation GateJumpedAnimation { get; private set; }

		/// <summary>
		/// The "failed" animation
		/// </summary>
		public MyJumpGateFailedAnimation GateFailedAnimation { get; private set; }

		/// <summary>
		/// The wormhole open animation
		/// </summary>
		public MyJumpGateWormholeAnimation GateWormholeOpenAnimation { get; private set; }

		/// <summary>
		/// The wormhole loop animation
		/// </summary>
		public MyJumpGateWormholeAnimation GateWormholeLoopAnimation { get; private set; }

		/// <summary>
		/// The wormhole close animation
		/// </summary>
		public MyJumpGateWormholeAnimation GateWormholeCloseAnimation { get; private set; }

		/// <summary>
		/// Whether this animation can be cancelled immediatly<br />
		/// If false, animation in the jumping phase will cancel once complete
		/// </summary>
		public readonly bool ImmediateCancel;

		/// <summary>
		/// The name of this animation as defined in the animation definition
		/// </summary>
		public readonly string AnimationName;

		/// <summary>
		/// The full name of this animation as defined from the animation handler
		/// </summary>
		public readonly string FullAnimationName;
		#endregion

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first MyJumpGateAnimation operand</param>
		/// <param name="b">The second MyJumpGateAnimation operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(MyJumpGateAnimation a, MyJumpGateAnimation b)
		{
			if (object.ReferenceEquals(a, b)) return true;
			else if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) return false;
			else return a.Equals(b);
		}

		/// <summary>
		/// Overloads inequality operator "!=" to check inequality
		/// </summary>
		/// <param name="a">The first MyJumpGateAnimation operand</param>
		/// <param name="b">The second MyJumpGateAnimation operand</param>
		/// <returns>Inequality</returns>
		public static bool operator !=(MyJumpGateAnimation a, MyJumpGateAnimation b)
		{
			return !(a == b);
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new MyJumpGateAnimation animation wrapper
		/// </summary>
		/// <param name="def">The animation definition</param>
		/// <param name="full_name">The animation's full name</param>
		/// <param name="caller">Player activating this animation</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="controller_settings">The controller settings used to activate said jump gate</param>
		/// <param name="target_controller_settings">The target jump gate's controller settings</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <param name="jump_gate_entities">An optional fixed list of entities to apply animation to</param>
		public MyJumpGateAnimation(AnimationDef def, string full_name, IMyPlayer caller, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings, MyJumpTypeEnum jump_type, IEnumerable<MyEntity> jump_gate_entities = null)
		{
			this.GateJumpingAnimationDef = def.JumpingAnimationDef;
			this.GateJumpedAnimationDef = def.JumpedAnimationDef;
			this.GateFailedAnimationDef = def.FailedAnimationDef;
			this.GateWormholeOpenAnimationDef = def.WormholeOpenAnimationDef;
			this.GateWormholeLoopAnimationDef = def.WormholeLoopAnimationDef;
			this.GateWormholeCloseAnimationDef = def.WormholeCloseAnimationDef;
			this.JumpGate = jump_gate;
			this.TargetGate = target_gate;
			this.ControllerSettings = controller_settings ?? jump_gate?.Controller?.BlockSettings;
			this.TargetControllerSettings = target_controller_settings ?? target_gate?.Controller?.BlockSettings;
			this.AnimationName = def.AnimationName;
			this.FullAnimationName = full_name;
			this.ImmediateCancel = def.ImmediateCancel;
			this.GateJumpType = jump_type;
			this.Caller = caller;
			if (this.IsEntityCollectionLocked = jump_gate_entities != null) this.TEMP_JumpGateEntitiesL.AddRange(jump_gate_entities);
			this.HashCode = this.FullAnimationName.GetHashCode() ^ JumpGateUUID.FromJumpGate(jump_gate).GetHashCode() ^ jump_type.GetHashCode() ^ ((target_gate == null) ? JumpGateUUID.Empty : JumpGateUUID.FromJumpGate(target_gate)).GetHashCode();
		}
		#endregion

		#region "object" Methods
		/// <summary>
		/// Checks if this MyJumpGateAnimation equals another
		/// </summary>
		/// <param name="other">The object to check</param>
		/// <returns>Equality</returns>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as MyJumpGateAnimation);
		}

		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this object</returns>
		public override int GetHashCode()
		{
			return this.HashCode.GetHashCode();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Cleans this animation fully<br />
		/// MyAnimation cannot be replayed
		/// </summary>
		public void Clean()
		{
			this.GateJumpingAnimation?.Clean();
			this.GateJumpedAnimation?.Clean();
			this.GateFailedAnimation?.Clean();

			this.GateWormholeOpenAnimation?.Clean();
			this.GateWormholeLoopAnimation?.Clean();
			this.GateWormholeCloseAnimation?.Clean();

			this.GateJumpingAnimation = null;
			this.GateJumpedAnimation = null;
			this.GateFailedAnimation = null;
			this.GateJumpingAnimationDef = null;
			this.GateJumpedAnimationDef = null;
			this.GateFailedAnimationDef = null;

			this.GateWormholeOpenAnimation = null;
			this.GateWormholeLoopAnimation = null;
			this.GateWormholeCloseAnimation = null;
			this.GateWormholeOpenAnimationDef = null;
			this.GateWormholeLoopAnimationDef = null;
			this.GateWormholeCloseAnimationDef = null;

			this.ActiveAnimationIndex = -1;
			this.JumpGate = null;
			this.TargetGate = null;
			this.ControllerSettings = null;

			this.TEMP_JumpGateDrives?.Clear();
			this.TEMP_JumpGateAntiDrives?.Clear();
			this.TEMP_JumpGateEntitiesL?.Clear();

			this.TEMP_JumpGateDrives = null;
			this.TEMP_JumpGateAntiDrives = null;
			this.TEMP_JumpGateEntitiesL = null;
		}

		/// <summary>
		/// Pauses this animation
		/// </summary>
		public void Pause()
		{
			this.GateJumpingAnimation?.Stop();
			this.GateJumpedAnimation?.Stop();
			this.GateFailedAnimation?.Stop();
			this.GateWormholeOpenAnimation?.Stop();
			this.GateWormholeLoopAnimation?.Stop();
			this.GateWormholeCloseAnimation?.Stop();
			this.TEMP_JumpGateDrives.Clear();
			this.TEMP_JumpGateAntiDrives.Clear();
			this.Paused = true;
		}

		/// <summary>
		/// Resumes this animation
		/// </summary>
		public void Resume()
		{
			if (this.ActiveAnimationIndex == -1) return;

			switch (this.ActiveAnimationIndex)
			{
				case 0:
					this.GateJumpingAnimation?.Resume();
					break;
				case 1:
					this.GateJumpedAnimation?.Resume();
					break;
				case 2:
					this.GateFailedAnimation?.Resume();
					break;
				case 3:
					this.GateWormholeOpenAnimation?.Resume();
					break;
				case 4:
					this.GateWormholeLoopAnimation?.Resume();
					break;
				case 5:
					this.GateWormholeCloseAnimation?.Resume();
					break;
			}

			this.TEMP_JumpGateDrives.Clear();
			this.TEMP_JumpGateAntiDrives.Clear();
			this.Paused = false;
		}

		/// <summary>
		/// Changes this animation's source gate and controller settings<br />
		/// </summary>
		/// <param name="new_source_gate">The new source gate</param>
		/// <param name="new_source_controller_settings">The new controller settings</param>
		public void SetSourceGate(MyJumpGate new_source_gate, MyJumpGateController.MyControllerBlockSettingsStruct new_source_controller_settings)
		{
			if (new_source_gate == null || new_source_controller_settings == null) throw new NullReferenceException("Source gate or settings is null");
			this.JumpGate = new_source_gate;
			this.ControllerSettings = new_source_controller_settings;
		}

		/// <summary>
		/// Changes this animation's target gate and controller settings<br />
		/// </summary>
		/// <param name="new_target_gate">The new target gate</param>
		/// <param name="new_target_controller_settings">The new controller settings</param>
		public void SetTargetGate(MyJumpGate new_target_gate, MyJumpGateController.MyControllerBlockSettingsStruct new_target_controller_settings)
		{
			this.TargetGate = new_target_gate;
			this.TargetControllerSettings = new_target_controller_settings;
		}

		/// <summary>
		/// Ticks the specified effect
		/// </summary>
		/// <param name="index">The animation to tick<br />1 - Ticks jumping animation<br />2 - Ticks jumped animation<br />3 - Ticks failed animation</param>
		/// <exception cref="InvalidOperationException">If the animation index is invalid</exception>
		public void Tick(byte index)
		{
			if (this.Paused) return;
			else if (index < 0 || index > 5) throw new InvalidOperationException("Invalid animation index");
			else if (this.ActiveAnimationIndex != -1 && this.ActiveAnimationIndex != index)
			{
				switch (this.ActiveAnimationIndex)
				{
					case 0:
						this.GateJumpingAnimation?.Stop();
						break;
					case 1:
						this.GateJumpedAnimation?.Stop();
						break;
					case 2:
						this.GateFailedAnimation?.Stop();
						break;
					case 3:
						this.GateWormholeOpenAnimation?.Stop();
						break;
					case 4:
						this.GateWormholeLoopAnimation?.Stop();
						break;
					case 5:
						this.GateWormholeCloseAnimation?.Stop();
						break;
				}
			}

			this.ActiveAnimationIndex = index;
			if ((this.JumpGate?.Closed ?? true) || (this.JumpGate.JumpGateGrid?.Closed ?? true)) return;
			List<MyJumpGateDrive> drives = null;
			this.TEMP_JumpGateDrives.AddRange(this.JumpGate.GetJumpGateDrives());

			if (!this.IsEntityCollectionLocked && (this.JumpGate.Phase == MyJumpGatePhase.JUMPING || this.JumpGate.Phase == MyJumpGatePhase.RESETTING))
			{
				foreach (KeyValuePair<MyEntity, EntityBatch> pair in this.JumpGate.EntityBatches) this.TEMP_JumpGateEntitiesL.AddList(pair.Value.Batch);
				if (this.JumpGate.IsWormholeActive) this.TEMP_JumpGateEntitiesL.AddRange(this.JumpGate.GetEntitiesReadyForJump(true).Select((pair) => pair.Key));
			}

			if (!this.IsEntityCollectionLocked && (this.JumpGate.Phase != MyJumpGatePhase.JUMPING || this.JumpGate.Phase == MyJumpGatePhase.RESETTING || (this.ActiveAnimationIndex >= 3 && this.JumpGate.IsWormholeActive)))
			{
				this.TEMP_JumpGateEntitiesL.AddRange(this.JumpGate.GetEntitiesInJumpSpace(true).Select((pair) => pair.Key));
			}

			drives = this.TEMP_JumpGateDrives;
			List<MyJumpGateDrive> anti_drives = null;

			try
			{
				if (this.GateJumpType == MyJumpTypeEnum.INBOUND_VOID)
				{
					anti_drives = drives;
					drives = null;
				}
				else if (this.TargetGate != null && !this.TargetGate.Closed && !(this.TargetGate.JumpGateGrid?.Closed ?? true))
				{
					this.TEMP_JumpGateAntiDrives.AddRange(this.TargetGate.GetJumpGateDrives());
					anti_drives = this.TEMP_JumpGateAntiDrives;
				}

				Vector3D jump_node, endpoint, anti_node, target_world_jump_node;

				if (this.GateJumpType == MyJumpTypeEnum.INBOUND_VOID)
				{
					Vector3D? _startpoint = this.JumpGate.TrueEndpoint;
					if (_startpoint == null) return;
					endpoint = this.JumpGate.WorldJumpNode;
					anti_node = endpoint;
					jump_node = _startpoint.Value;
					target_world_jump_node = jump_node;
				}
				else
				{
					jump_node = this.JumpGate.WorldJumpNode;
					Vector3D? _endpoint = this.JumpGate.TrueEndpoint;
					if (_endpoint == null) return;
					endpoint = _endpoint.Value;
					anti_node = endpoint;
					target_world_jump_node = jump_node;

					if (!MyNetworkInterface.IsDedicatedMultiplayerServer && this.ControllerSettings.HasVectorNormalOverride() && Vector3D.Distance(MyAPIGateway.Session.Camera.Position, this.JumpGate.WorldJumpNode) <= this.JumpGate.JumpGateConfiguration.MinimumJumpDistance) endpoint = jump_node + this.JumpGate.GetWorldMatrix(true, true).Forward * Vector3D.Distance(jump_node, endpoint);
					if (!MyNetworkInterface.IsDedicatedMultiplayerServer && this.TargetControllerSettings != null && this.TargetGate != null && this.TargetControllerSettings.HasVectorNormalOverride() && Vector3D.Distance(MyAPIGateway.Session.Camera.Position, this.TargetGate.WorldJumpNode) <= this.JumpGate.JumpGateConfiguration.MinimumJumpDistance) target_world_jump_node = endpoint + this.TargetGate.GetWorldMatrix(false, true).Forward * Vector3D.Distance(endpoint, target_world_jump_node);
				}

				switch (this.ActiveAnimationIndex)
				{
					case 0:
						if (this.GateJumpingAnimation == null) this.GateJumpingAnimation = (this.GateJumpingAnimationDef == null) ? null : new MyJumpGateJumpingAnimation(this.GateJumpingAnimationDef, this, ref endpoint, ref anti_node, this.GateJumpType);
						this.GateJumpingAnimation?.Tick(this.Caller, ref endpoint, ref anti_node, ref target_world_jump_node, drives, anti_drives, this.TEMP_JumpGateEntitiesL);
						break;
					case 1:
						if (this.GateJumpedAnimation == null) this.GateJumpedAnimation = (this.GateJumpedAnimationDef == null) ? null : new MyJumpGateJumpedAnimation(this.GateJumpedAnimationDef, this, ref endpoint, ref anti_node, this.GateJumpType);
						this.GateJumpedAnimation?.Tick(this.Caller, ref endpoint, ref anti_node, ref target_world_jump_node, drives, anti_drives, this.TEMP_JumpGateEntitiesL);
						break;
					case 2:
						if (this.GateFailedAnimation == null) this.GateFailedAnimation = (this.GateFailedAnimationDef == null) ? null : new MyJumpGateFailedAnimation(this.GateFailedAnimationDef, this, ref endpoint, ref anti_node, this.GateJumpType);
						this.GateFailedAnimation?.Tick(this.Caller, ref endpoint, ref anti_node, ref target_world_jump_node, drives, anti_drives, this.TEMP_JumpGateEntitiesL);
						break;
					case 3:
						if (this.GateWormholeOpenAnimation == null) this.GateWormholeOpenAnimation = (this.GateWormholeOpenAnimationDef == null) ? null : new MyJumpGateWormholeAnimation(this.GateWormholeOpenAnimationDef, this, ref endpoint, ref anti_node, this.GateJumpType, false);
						this.GateWormholeOpenAnimation?.Tick(this.Caller, ref endpoint, ref anti_node, ref target_world_jump_node, drives, anti_drives, this.TEMP_JumpGateEntitiesL);
						break;
					case 4:
						if (this.GateWormholeLoopAnimation == null) this.GateWormholeLoopAnimation = (this.GateWormholeLoopAnimationDef == null) ? null : new MyJumpGateWormholeAnimation(this.GateWormholeLoopAnimationDef, this, ref endpoint, ref anti_node, this.GateJumpType, true);
						this.GateWormholeLoopAnimation?.Tick(this.Caller, ref endpoint, ref anti_node, ref target_world_jump_node, drives, anti_drives, this.TEMP_JumpGateEntitiesL);
						break;
					case 5:
						if (this.GateWormholeCloseAnimation == null) this.GateWormholeCloseAnimation = (this.GateWormholeCloseAnimationDef == null) ? null : new MyJumpGateWormholeAnimation(this.GateWormholeCloseAnimationDef, this, ref endpoint, ref anti_node, this.GateJumpType, false);
						this.GateWormholeCloseAnimation?.Tick(this.Caller, ref endpoint, ref anti_node, ref target_world_jump_node, drives, anti_drives, this.TEMP_JumpGateEntitiesL);
						break;
				}
			}
			finally
			{
				this.TEMP_JumpGateDrives.Clear();
				this.TEMP_JumpGateAntiDrives.Clear();
				if (!this.IsEntityCollectionLocked) this.TEMP_JumpGateEntitiesL.Clear();
			}
		}

		/// <summary>
		/// Restarts the specified animation
		/// </summary>
		/// <param name="index">The animation to restart<br />1 - Ticks jumping animation<br />2 - Ticks jumped animation<br />3 - Ticks failed animation</param>
		/// <exception cref="InvalidOperationException">If the animation index is invalid</exception>
		public void Restart(byte index)
		{
			if (index < 0 || index > 5) throw new InvalidOperationException("Invalid animation index");
			else
			{
				switch (index)
				{
					case 0:
						this.GateJumpingAnimation?.Restart();
						break;
					case 1:
						this.GateJumpedAnimation?.Restart();
						break;
					case 2:
						this.GateFailedAnimation?.Restart();
						break;
					case 3:
						this.GateWormholeOpenAnimation?.Restart();
						break;
					case 4:
						this.GateWormholeLoopAnimation?.Restart();
						break;
					case 5:
						this.GateWormholeCloseAnimation?.Restart();
						break;
				}

				this.TEMP_JumpGateDrives.Clear();
				this.TEMP_JumpGateAntiDrives.Clear();
			}
		}

		/// <summary>
		/// Stops all animations<br />
		/// Animations may be restarted
		/// </summary>
		public void Stop()
		{
			this.ActiveAnimationIndex = -1;
			this.GateJumpingAnimation?.Stop();
			this.GateJumpedAnimation?.Stop();
			this.GateFailedAnimation?.Stop();
			this.GateWormholeOpenAnimation?.Stop();
			this.GateWormholeLoopAnimation?.Stop();
			this.GateWormholeCloseAnimation?.Stop();

			this.TEMP_JumpGateDrives?.Clear();
			this.TEMP_JumpGateAntiDrives?.Clear();
			if (!this.IsEntityCollectionLocked) this.TEMP_JumpGateEntitiesL?.Clear();
		}

		/// <summary>
		/// Checks if this MyJumpGateAnimation equals another
		/// </summary>
		/// <param name="other">The MyJumpGateAnimation to check</param>
		/// <returns>Equality</returns>
		public bool Equals(MyJumpGateAnimation other)
		{
			if (object.ReferenceEquals(other, null)) return false;
			else if (object.ReferenceEquals(this, other)) return true;
			else return this.HashCode == other.HashCode;
		}

		/// <summary>
		/// Checks if the specified animation is stopped
		/// </summary>
		/// <param name="index">The animation to restart<br />1 - Ticks jumping animation<br />2 - Ticks jumped animation<br />3 - Ticks failed animation</param>
		/// <returns>True if the specified animation is stopped</returns>
		/// <exception cref="InvalidOperationException">If the animation index is invalid</exception>
		public bool Stopped(short index)
		{
			if (index < 0 || index > 5) throw new InvalidOperationException("Invalid animation index");
			ushort current_tick;
			ushort duration;
			bool stopped = true;

			switch (index)
			{
				case 0:
					if (this.GateJumpingAnimation == null) break;
					current_tick = this.GateJumpingAnimation.CurrentTick;
					duration = this.GateJumpingAnimation.Duration();
					stopped = current_tick >= duration || this.GateJumpingAnimation.Stopped();
					break;
				case 1:
					if (this.GateJumpedAnimation == null) break;
					current_tick = this.GateJumpedAnimation.CurrentTick;
					duration = this.GateJumpedAnimation.Duration();
					stopped = current_tick >= duration || this.GateJumpedAnimation.Stopped();
					break;
				case 2:
					if (this.GateFailedAnimation == null) break;
					current_tick = this.GateFailedAnimation.CurrentTick;
					duration = this.GateFailedAnimation.Duration();
					stopped = current_tick >= duration || this.GateFailedAnimation.Stopped();
					break;
				case 3:
					if (this.GateWormholeOpenAnimation == null) break;
					current_tick = this.GateWormholeOpenAnimation.CurrentTick;
					duration = this.GateWormholeOpenAnimation.Duration();
					stopped = current_tick >= duration || this.GateWormholeOpenAnimation.Stopped();
					break;
				case 4:
					stopped = this.GateWormholeLoopAnimation?.Stopped() ?? stopped;
					break;
				case 5:
					if (this.GateWormholeCloseAnimation == null) break;
					current_tick = this.GateWormholeCloseAnimation.CurrentTick;
					duration = this.GateWormholeCloseAnimation.Duration();
					stopped = current_tick >= duration || this.GateWormholeCloseAnimation.Stopped();
					break;
			}

			return stopped;
		}

		/// <summary>
		/// </summary>
		/// <returns>The durations for all animation effects or 0 if there is no effect for that index</returns>
		public ushort[] Durations()
		{
			return new ushort[6] {
				this.GateJumpingAnimation?.Duration() ?? this.GateJumpingAnimationDef?.Duration ?? (ushort) 0u,
				this.GateJumpedAnimation?.Duration() ?? this.GateJumpedAnimationDef?.Duration ?? (ushort) 0u,
				this.GateFailedAnimation?.Duration() ?? this.GateFailedAnimationDef?.Duration ?? (ushort) 0u,
				this.GateWormholeOpenAnimation?.Duration() ?? this.GateWormholeOpenAnimationDef?.Duration ?? (ushort) 0u,
				this.GateWormholeLoopAnimation?.Duration() ?? this.GateWormholeLoopAnimationDef?.Duration ?? (ushort) 0u,
				this.GateWormholeCloseAnimation?.Duration() ?? this.GateWormholeCloseAnimationDef?.Duration ?? (ushort) 0u
			};
		}
		#endregion
	}

	/// <summary>
	/// Container holding all defined animations
	/// </summary>
	internal static class MyAnimationHandler
	{
		#region Private Static Variables
		/// <summary>
		/// Stores the next available subtype ID for animations
		/// </summary>
		private static ulong NextSubtypeID = 0;

		/// <summary>
		/// Holds the list of pre-load animation definitions<br />
		/// All animations definined in code will be here
		/// </summary>
		private static List<AnimationDef> PreloadedAnimationDefinitions = new List<AnimationDef>();

		/// <summary>
		/// Master map mapping a full animation name with it's animation definition
		/// </summary>
		private static Dictionary<string, List<AnimationDef>> Animations = new Dictionary<string, List<AnimationDef>>();
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Stores a new preload animation definition
		/// </summary>
		/// <param name="animation">The animation definition to store</param>
		public static void AddAnimationDefinition(AnimationDef animation)
		{
			MyAnimationHandler.PreloadedAnimationDefinitions.Add(animation);
		}

		/// <summary>
		/// Loads all animation definitions from file(s)<br />
		/// This will scan all loaded mods and load any animations defined within them
		/// </summary>
		public static void Load()
		{
			new AnimationDefinitions();
			string animations_list_file = "Data/Animations.txt";

			// Load external mod animations
			foreach (MyObjectBuilder_Checkpoint.ModItem mod in MyAPIGateway.Session.Mods)
			{
				if (MyAPIGateway.Utilities.FileExistsInModLocation(animations_list_file, mod))
				{
					TextReader reader = null;

					try
					{
						uint count = 0;
						reader = MyAPIGateway.Utilities.ReadFileInModLocation(animations_list_file, mod);
						string[] animations_list = reader.ReadToEnd().Split('\n');
						Logger.Log($"Found animations list in {mod.FriendlyName}; LOADING ANIMATIONS...");

						foreach (string animation_path in animations_list)
						{
							if (MyAPIGateway.Utilities.FileExistsInModLocation(animation_path, mod))
							{
								try
								{
									Logger.Warn($"Animation serialization not yet supported: {mod.FriendlyName}::{animation_path} SKIPPED");
									reader = MyAPIGateway.Utilities.ReadFileInModLocation(animation_path, mod);
									string serialized_animation = reader.ReadToEnd();
									reader.Close();
									AnimationDef animation = MyAPIGateway.Utilities.SerializeFromXML<AnimationDef>(serialized_animation);

									if (animation == null || !animation.Enabled)
									{
										Logger.Warn($"\tSkipped loading of NULL or DISABLED animation: {mod.FriendlyName}::{animation_path}");
										continue;
									}

									string name = (animation.AnimationName == null || animation.AnimationName.Trim().Length == 0) ? "<NULL>" : animation.AnimationName.Trim();
									string full_name = $"{mod.FriendlyName}.{animation.GetType().FullName}.{name}";
									animation.SourceMod = mod.Name;
									animation.Prepare();
									MyAnimationHandler.Animations.Add(full_name, new List<AnimationDef>() { animation });
									++count;
								}
								catch (Exception e)
								{
									Logger.Error($"\tFailed to load animation at {mod.FriendlyName}::{animation_path}\n{e.Message}\n...\n{e.StackTrace}\n...\n...\n...\n{e.InnerException}");
								}
							}
							else Logger.Warn($"\tNo file at path: {mod.FriendlyName}::{animation_path}");
						}

						Logger.Log($"Loaded {count} animations from {mod.FriendlyName}");
					}
					catch (Exception e)
					{
						Logger.Error($"Failed to read animations list at {mod.FriendlyName}::{animations_list_file}\n{e.Message}\n...\n{e.StackTrace}\n...\n...\n...\n{e.InnerException}");
					}
					finally
					{
						reader?.Close();
					}
				}
			}

			// Load mod animations
			foreach (AnimationDef animation in MyAnimationHandler.PreloadedAnimationDefinitions)
			{
				if (!animation.Enabled) continue;
				string name = (animation.AnimationName == null || animation.AnimationName.Trim().Length == 0) ? "<NULL>" : animation.AnimationName.Trim();
				string full_name = $"{animation.GetType().FullName}.{name}";
				if (animation.AnimationConstraints == null || animation.AnimationConstraints.Length == 0) animation.SubtypeID = null;
				else animation.SubtypeID = MyAnimationHandler.NextSubtypeID++;
				animation.Prepare();
				if (MyAnimationHandler.Animations.ContainsKey(full_name)) MyAnimationHandler.Animations[full_name].Add(animation);
				else MyAnimationHandler.Animations.Add(full_name, new List<AnimationDef>() { animation });
			}

			// Load Mod-API animations
			foreach (KeyValuePair<IMyModContext, List<AnimationDef>> pair in MyJumpGateModSession.Instance.ModAPIInterface.ModAnimationDefinitions)
			{
				foreach (AnimationDef animation in pair.Value)
				{
					if (!animation.Enabled) continue;
					string name = (animation.AnimationName == null || animation.AnimationName.Trim().Length == 0) ? "<NULL>" : animation.AnimationName.Trim();
					string full_name = $"{pair.Key.ModName}.{animation.GetType().FullName}.{name}";
					if (animation.AnimationConstraints == null || animation.AnimationConstraints.Length == 0) animation.SubtypeID = null;
					else animation.SubtypeID = MyAnimationHandler.NextSubtypeID++;
					animation.SourceMod = pair.Key.ModName;
					animation.Prepare();
					if (MyAnimationHandler.Animations.ContainsKey(full_name)) MyAnimationHandler.Animations[full_name].Add(animation);
					else MyAnimationHandler.Animations.Add(full_name, new List<AnimationDef>() { animation });
				}
			}

			MyAnimationHandler.PreloadedAnimationDefinitions.Clear();
			Logger.Log($"Loaded {MyAnimationHandler.Animations.Count} animation(s)");
		}

		/// <summary>
		/// Unloads all animation definitions<br />
		/// For any animation marked for serialization, this will serialize it to XML and write it to global storage
		/// </summary>
		public static void Unload()
		{
			if (!MyNetworkInterface.IsMultiplayerClient)
			{
				foreach (KeyValuePair<string, List<AnimationDef>> pair in MyAnimationHandler.Animations)
				{
					string full_name = pair.Key;

					foreach (AnimationDef animation in pair.Value)
					{
						if (!animation.SerializeOnEnd) continue;
						Logger.Warn($"Serializing animation: {animation.SourceMod ?? MyJumpGateModSession.Instance.ModID}::{animation.AnimationName}");
						string out_file = $"{full_name}_{((animation.SubtypeID == null) ? "-1" : animation.SubtypeID.Value.ToString())}.xml";
						TextWriter writer = null;

						try
						{
							string xml = MyAPIGateway.Utilities.SerializeToXML(animation);
							writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(out_file, MyJumpGateModSession.Instance.GetType());
							writer.Write(xml);
							Logger.Log($"Wrote serialized animation to {out_file}");
						}
						catch (Exception e)
						{
							Logger.Error($"\tFailed to write serialized animation at {out_file}\n{e.Message}\n...\n{e.StackTrace}\n...\n...\n...\n{e.InnerException}");
						}
						finally
						{
							writer?.Close();
						}
					}
				}
			}

			MyAnimationHandler.Animations.Clear();
			MyAnimationHandler.PreloadedAnimationDefinitions?.Clear();
			MyAnimationHandler.Animations = null;
			MyAnimationHandler.PreloadedAnimationDefinitions = null;
			Logger.Log($"Animations Unloaded");
		}

		/// <summary>
		/// </summary>
		/// <returns>The full names of all stored animations</returns>
		public static HashSet<string> GetAnimationNames()
		{
			return new HashSet<string>(MyAnimationHandler.Animations.Keys);
		}

		/// <summary>
		/// Gets an animation definition
		/// </summary>
		/// <param name="name">The animation's full name</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="roll">Whether to run random selection or get first matching</param>
		/// <returns>The animation definition or null if the jump gate fails the animation's constraint</returns>
		public static AnimationDef GetAnimationDef(string name, MyJumpGate jump_gate, bool roll)
		{
			List<AnimationDef> animations = MyAnimationHandler.Animations.GetValueOrDefault(name, null);
			AnimationDef default_ = null;
			AnimationDef matched = null;

			if (animations == null) return null;
			else if (jump_gate == null) return animations.FirstOrDefault();
			else if (!MyNetworkInterface.IsStandaloneMultiplayerClient && !jump_gate.IsValid()) return null;
			else if (roll)
			{
				byte total_weight = (byte) animations.Sum((animation) => animation.RandomWeight);
				byte target_weight = (byte) new Random().Next(0x00, total_weight + 1);
				byte weights = 0;

				foreach (AnimationDef animation in animations)
				{
					if (default_ == null && (animation.AnimationConstraints == null || animation.AnimationConstraints.Length == 0)) default_ = animation;
					weights += animation.RandomWeight;
					if (weights < target_weight || animation.RandomWeight == 0 || (animation.AnimationConstraints != null && !animation.AnimationConstraints.All((constraint) => constraint.Validate(jump_gate)))) continue;
					matched = animation;
					break;
				}
			}
			else
			{
				default_ = animations.FirstOrDefault((animation) => animation.AnimationConstraints == null || animation.AnimationConstraints.Length == 0);
				matched = animations.FirstOrDefault((animation) => animation.AnimationConstraints != null && animation.AnimationConstraints.All((constraint) => constraint.Validate(jump_gate)));
			}

			return matched ?? default_ ?? null;
		}

		/// <summary>
		/// Gets a playable animation
		/// </summary>
		/// <param name="name">The animation's full name</param>
		/// <param name="caller">PLayer who activated this animation</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="controller_settings">The controller settings used to activate the jump gate</param>
		/// <param name="target_controller_settings">The target jump gate's controller settings</param>
		/// <param name="jump_gate_entities">An optional fixed list of entities to apply animation to</param>
		/// <returns>The playabe animation wrapper</returns>
		public static MyJumpGateAnimation GetAnimation(string name, IMyPlayer caller, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings, MyJumpTypeEnum jump_type, IEnumerable<MyEntity> jump_gate_entities = null, bool? get_wormhole_version = null)
		{
			AnimationDef animation_def = MyAnimationHandler.GetAnimationDef(name, jump_gate, true);
			if (animation_def == null || jump_gate == null || (!MyNetworkInterface.IsStandaloneMultiplayerClient && !jump_gate.IsValid()) || (get_wormhole_version ?? controller_settings.DoSustainedWormhole()) != animation_def.IsWormholeAnimation()) return null;
			return new MyJumpGateAnimation(animation_def, name, caller, jump_gate, target_gate, controller_settings, target_controller_settings, jump_type, jump_gate_entities);
		}
		#endregion
	}
}
