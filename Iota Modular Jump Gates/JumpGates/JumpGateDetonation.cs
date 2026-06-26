using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using VRage.Utils;

namespace IOTA.ModularJumpGates.JumpGates
{
	internal partial class MyJumpGate
	{
		#region Private Variables
		/// <summary>
		/// The master set of explosions that ocurred within this gate's jump space
		/// </summary>
		private ConcurrentLinkedHashSet<JumpGateExplosionInfo> JumpSpaceExplosions = new ConcurrentLinkedHashSet<JumpGateExplosionInfo>();
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this jump gate is actively detonating
		/// </summary>
		public bool IsDetonating { get; private set; } = false;

		/// <summary>
		/// This jump gate's self destruct time in game ticks or -1 if not armed
		/// </summary>
		public int ManualDetonationTimeout { get; private set; } = -1;

		/// <summary>
		/// Called when gate begins detonation countdown<br />
		/// Accepts one float parameter indicating time to detonation in seconds
		/// </summary>
		public event Action<MyJumpGate, float> OnGateDetonation;
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this object receives a gate detonation event from clients
		/// </summary>
		/// <param name="packet">The detonation request packet</param>
		private void OnGateDetonate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || (packet.PhaseFrame != 1 && packet.PhaseFrame != 2)) return;
			KeyValuePair<JumpGateUUID, float> payload = packet.Payload<KeyValuePair<JumpGateUUID, float>>();

			if (payload.Key != JumpGateUUID.FromJumpGate(this)) return;
			else if (MyNetworkInterface.IsMultiplayerServer)
			{
				if (payload.Value == -1) this.ClearDetonation();
				else this.QueueDetonation(payload.Value);
				packet.Forward(0, true).Send();
			}
			else
			{
				this.ManualDetonationTimeout = (payload.Value == -1) ? -1 : (int) Math.Round(payload.Value * 60);
				this.OnGateDetonation?.Invoke(this, payload.Value);
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
			}
		}

		/// <summary>
		/// Callback called when an explosion occurs in this gate's jump space
		/// </summary>
		/// <param name="explosion">The explosion info</param>
		private void OnExplosion(ref MyExplosionInfo explosion)
		{
			if (MyJumpGateModSession.Instance == null)
			{
				MyLog.Default.WriteLineAndConsole($"IOTA.ModularJumpGates.CriticalError - MEMORY_LEAK: A Jump gate survived mod closure // ID={JumpGateUUID.FromJumpGate(this)}");
				MyExplosions.OnExplosion -= this.OnExplosion;
				this.Close();
				return;
			}

			if (this.Closed || !this.JumpGateConfiguration.AllowWormholeStargateJumps || MyNetworkInterface.IsStandaloneMultiplayerClient || !this.IsWormholeActive || !this.JumpEllipse.Intersects(ref explosion.ExplosionSphere)) return;
			this.JumpSpaceExplosions.Add(new JumpGateExplosionInfo(ref explosion));
			Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_SPACE_EXPLOSION; COUNT={this.JumpSpaceExplosions.Count}", 5);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Detonates this gate
		/// </summary>
		/// <param name="initiator">The drive at which to start the explosion chain</param>
		public void Detonate(MyJumpGateDrive initiator = null)
		{
			if (this.IsDetonating) return;
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.GATE_DETONATE,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(new KeyValuePair<JumpGateUUID, float>(JumpGateUUID.FromJumpGate(this), 0));
				packet.Send();
				return;
			}

			this.IsDetonating = true;
			MyJumpGateModSession.Instance.DetonateJumpGate(this, initiator);
			this.StopSounds();
			this.OnGateDetonation?.Invoke(this, 0);
		}

		/// <summary>
		/// Sets the detonation timer on this gate
		/// </summary>
		/// <param name="timeout">The time in seconds until detonation</param>
		/// <exception cref="ArgumentException">If timeout is negative</exception>
		public void QueueDetonation(float timeout)
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.GATE_DETONATE,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(new KeyValuePair<JumpGateUUID, float>(JumpGateUUID.FromJumpGate(this), timeout));
				packet.Send();
				return;
			}
			else if (this.IsDetonating) return;

			if (timeout == 0) this.Detonate();
			else if (timeout < 0) throw new ArgumentException("Timeout must be >= 0");
			else
			{
				this.ManualDetonationTimeout = (int) Math.Round(timeout * 60);
				this.OnGateDetonation?.Invoke(this, timeout);
			}
		}

		/// <summary>
		/// Clears a pending detonation
		/// </summary>
		public void ClearDetonation()
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.GATE_DETONATE,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(new KeyValuePair<JumpGateUUID, float>(JumpGateUUID.FromJumpGate(this), -1));
				packet.Send();
				return;
			}
			else if (this.IsDetonating) return;

			this.ManualDetonationTimeout = -1;
			this.OnGateDetonation?.Invoke(this, -1);
		}
		#endregion
	}
}
