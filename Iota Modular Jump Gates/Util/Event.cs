using System;
using System.Collections.Generic;

namespace IOTA.ModularJumpGates.Util
{
	internal class Event
	{
		private readonly object Mutex = new object();
		private readonly List<Delegate> Delegates = new List<Delegate>();

		public Event(params Delegate[] parameters)
		{
			this.Delegates.AddArray(parameters);
		}

		public void Add(Delegate callback)
		{
			if (callback == null) return;
			lock (this.Mutex) this.Delegates.Add(callback);
		}

		public void Remove(Delegate callback)
		{
			lock (this.Mutex) this.Delegates.Remove(callback);
		}

		public void Clear()
		{
			lock (this.Mutex) this.Delegates.Clear();
		}

		public void Invoke()
		{
			lock (this.Mutex) foreach (Delegate callback in this.Delegates) callback?.DynamicInvoke();
		}

		public void Invoke<A>(A param1)
		{
			lock (this.Mutex) foreach (Delegate callback in this.Delegates) callback?.DynamicInvoke(param1);
		}
	}
}
