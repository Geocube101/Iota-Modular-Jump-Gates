using IOTA.ModularJumpGates.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGates
{
	internal partial class MyJumpGate
	{
		#region Private Variables
		/// <summary>
		/// The next available sound emitter ID
		/// </summary>
		private ulong SoundEmitterID = 0;

		/// <summary>
		/// The master map of sound emitters bound to this gate
		/// </summary>
		private ConcurrentDictionary<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> SoundEmitters = new ConcurrentDictionary<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>>();
		#endregion

		#region Public Methods
		/// <summary>
		/// Stops all sound emitters with the specified sound
		/// </summary>
		/// <param name="sound">The sound to stop</param>
		public void StopSound(string sound)
		{
			if (this.Closed || !MyNetworkInterface.IsClientLike) return;

			foreach (KeyValuePair<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> pair in this.SoundEmitters)
			{
				MySoundEmitter3D sound_emitter = pair.Value.Value;
				if (sound_emitter.Sound == sound) sound_emitter.Dispose();
			}
		}

		/// <summary>
		/// Stops the sound emitter with the specified ID
		/// </summary>
		/// <param name="sound_id">The sound emitter ID to stop</param>
		public void StopSound(ulong? sound_id)
		{
			if (this.Closed || !MyNetworkInterface.IsClientLike || sound_id == null) return;
			KeyValuePair<Vector3D?, MySoundEmitter3D> pair;
			if (this.SoundEmitters.TryGetValue(sound_id.Value, out pair)) pair.Value?.Dispose();
		}

		/// <summary>
		/// Sets the volume for the specified sound emitter
		/// </summary>
		/// <param name="sound_id">The ID of the sound emitter</param>
		/// <param name="volume">The new volume</param>
		public void SetSoundVolume(ulong? sound_id, float volume)
		{
			if (this.Closed || !MyNetworkInterface.IsClientLike || sound_id == null) return;
			KeyValuePair<Vector3D?, MySoundEmitter3D> pair;
			if (this.SoundEmitters.TryGetValue(sound_id.Value, out pair)) pair.Value?.SetVolume(Math.Max(0, volume));
		}

		/// <summary>
		/// Sets the range for the specified sound emitter
		/// </summary>
		/// <param name="sound_id">The ID of the sound emitter</param>
		/// <param name="volume">The new range or null</param>
		public void SetSoundDistance(ulong? sound_id, float? distance)
		{
			if (this.Closed || !MyNetworkInterface.IsClientLike || sound_id == null) return;
			KeyValuePair<Vector3D?, MySoundEmitter3D> pair;
			if (this.SoundEmitters.TryGetValue(sound_id.Value, out pair)) pair.Value?.SetDistance((distance == null) ? distance : Math.Max(0, distance.Value));
		}

		/// <summary>
		/// Sets the range for the specified sound emitter
		/// </summary>
		/// <param name="sound_id">The ID of the sound emitter</param>
		/// <param name="position">The new position or null</param>
		public void SetSoundPosition(ulong? sound_id, Vector3D? position)
		{
			if (this.Closed || !MyNetworkInterface.IsClientLike || sound_id == null) return;
			KeyValuePair<Vector3D?, MySoundEmitter3D> pair;
			if (this.SoundEmitters.TryGetValue(sound_id.Value, out pair)) pair.Value?.SetPosition(ref position);
		}

		/// <summary>
		/// Stops all sounds from this gate
		/// </summary>
		public void StopSounds()
		{
			if (this.Closed || !MyNetworkInterface.IsClientLike) return;
			foreach (KeyValuePair<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> pair in this.SoundEmitters) pair.Value.Value.Dispose();
		}

		/// <summary>
		/// Plays a sound from this gate
		/// </summary>
		/// <param name="sound">The sound to play</param>
		/// <param name="distance">The sound range or null</param>
		/// <param name="volume">The sound volume</param>
		/// <param name="pos">The world position to play this sound at</param>
		/// <returns>The sound emitter ID or null if failed</returns>
		public ulong? PlaySound(string sound, float? distance = null, float volume = 1, Vector3D? pos = null)
		{
			if (this.Closed || !MyNetworkInterface.IsClientLike) return null;
			ulong sound_id = this.SoundEmitterID++;

			try
			{
				MySoundEmitter3D emitter = new MySoundEmitter3D(sound, pos ?? this.WorldJumpNode, distance, volume);
				if (this.SoundEmitters.TryAdd(sound_id, new KeyValuePair<Vector3D?, MySoundEmitter3D>(pos, emitter))) return sound_id;
				else return null;
			}
			catch (Exception)
			{
				return null;
			}
		}
		#endregion
	}
}
