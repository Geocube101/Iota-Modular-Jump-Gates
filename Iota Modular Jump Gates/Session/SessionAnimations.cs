using IOTA.ModularJumpGates.Animation;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Private Variables
		/// <summary>
		/// Stores the next index for queued animations
		/// </summary>
		private ulong AnimationQueueIndex = 0;

		/// <summary>
		/// Master map for storing in-progress animations
		/// </summary>
		private ConcurrentDictionary<ulong, AnimationInfo> JumpGateAnimations = new ConcurrentDictionary<ulong, AnimationInfo>();
		#endregion

		#region Private Methods
		/// <summary>
		/// Ticks all jump gate animations
		/// </summary>
		private void TickAnimations()
		{
			Particle.Render();
			foreach (KeyValuePair<ulong, AnimationInfo> pair in this.JumpGateAnimations)
			{
				AnimationInfo animation_info = pair.Value;
				MyJumpGateAnimation animation = animation_info.Animation;
				if (animation.Paused) continue;
				animation.Tick(animation_info.AnimationIndex);

				if (animation.Stopped(animation_info.AnimationIndex))
				{
					animation_info.CompletionCallback?.Invoke(animation_info.IterCallbackException);
					this.JumpGateAnimations.Remove(pair.Key);
				}
				else if (animation_info.IterCallback != null && !animation.Paused)
				{
					bool stop = true;

					try
					{
						stop = !animation_info.IterCallback();
					}
					catch (Exception e)
					{
						stop = true;
						animation_info.IterCallbackException = e;
						Logger.Error($"Error during animation iteration callback - {animation_info.Animation.FullAnimationName}/{(MyJumpGateAnimation.AnimationTypeEnum) animation.ActiveAnimationIndex} (SOURCE={animation.JumpGate?.GetPrintableName()})\n  ...\n[ {e} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
					}
					finally
					{
						if (stop)
						{
							animation.Stop();
							animation_info.CompletionCallback?.Invoke(animation_info.IterCallbackException);
							this.JumpGateAnimations.Remove(pair.Key);
						}
					}
				}
				else if (animation.JumpGate.Closed)
				{
					animation.Stop();
					animation_info.CompletionCallback?.Invoke(animation_info.IterCallbackException);
					this.JumpGateAnimations.Remove(pair.Key);
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Plays an animation on this session's game thread
		/// </summary>
		/// <param name="animation">The animation to play</param>
		/// <param name="animation_type">The animation type to play</param>
		/// <param name="callback">A callback called every animation tick; Iteration will stop if this callback returns false</param>
		/// <param name="on_complete">A callback called once the animation is stopped (either from an exception, the previous parameter returning false, or the animation stopped normally)<br/>The callback will be passed the exception that caused the animation to stop, or null if no exception occured</param>
		public void PlayAnimation(MyJumpGateAnimation animation, MyJumpGateAnimation.AnimationTypeEnum animation_type, Func<bool> callback = null, Action<Exception> on_complete = null)
		{
			if (animation == null) return;
			animation.Stop();
			animation.Restart((byte) animation_type);
			this.JumpGateAnimations[this.AnimationQueueIndex++] = new AnimationInfo(animation, animation_type, callback, on_complete);
		}

		/// <summary>
		/// Stops an animation currently playing on this session's game thread
		/// </summary>
		/// <param name="animation">The animation to stop</param>
		public void StopAnimation(MyJumpGateAnimation animation)
		{
			foreach (KeyValuePair<ulong, AnimationInfo> pair in this.JumpGateAnimations) if (pair.Value.Animation == animation) pair.Value.Animation.Stop();
		}

		/// <summary>
		/// Gets all animations playing from the specified jump gate
		/// </summary>
		/// <param name="gate">The gate to poll</param>
		/// <return>All animations playing for the specified gate</return>
		public IEnumerable<MyJumpGateAnimation> GetGateAnimationsPlaying(MyJumpGate gate)
		{
			if (gate != null)
			{
				foreach (KeyValuePair<ulong, AnimationInfo> pair in this.JumpGateAnimations)
				{
					if (pair.Value.Animation.JumpGate == gate)
					{
						yield return pair.Value.Animation;
					}
				}
			}
		}
		#endregion
	}
}
