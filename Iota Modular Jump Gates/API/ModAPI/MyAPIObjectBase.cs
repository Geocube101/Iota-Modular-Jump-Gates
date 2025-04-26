using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Utils;

namespace IOTA.ModularJumpGates.API.ModAPI
{
	public abstract class MyAPIObjectBase : IDisposable, IEquatable<MyAPIObjectBase>
	{
		private static ulong NextObjectID = 0;
		private static readonly ConcurrentDictionary<ulong, MyAPIObjectBase> APIObjects = new ConcurrentDictionary<ulong, MyAPIObjectBase>();

		public static readonly long ModAPIID = 3313236685;

		private readonly Dictionary<string, object> ObjectAttributes = new Dictionary<string, object>();

		public bool HandleValid => this.ObjectID != null;
		public ulong? ObjectID { get; private set; } = null;

		public static bool operator== (MyAPIObjectBase a, MyAPIObjectBase b)
		{
			if (object.ReferenceEquals(a, b)) return true;
			else if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
			else return a.Equals(b);
		}
		public static bool operator!= (MyAPIObjectBase a, MyAPIObjectBase b)
		{
			return !(a == b);
		}

		protected MyAPIObjectBase()
		{
			this.ObjectID = MyAPIObjectBase.NextObjectID++;
			MyAPIObjectBase.APIObjects[this.ObjectID.Value] = this;
			Dictionary<string, object> attributes = null;
			Action<Dictionary<string, object>> callback = (_attributes) => attributes = _attributes;
			MyAPIGateway.Utilities.SendModMessage(MyAPIObjectBase.ModAPIID, callback);
			this.ObjectAttributes = attributes;
		}
		protected MyAPIObjectBase(Dictionary<string, object> attributes)
		{
			this.ObjectID = MyAPIObjectBase.NextObjectID++;
			this.ObjectAttributes = attributes;
			MyAPIObjectBase.APIObjects[this.ObjectID.Value] = this;
		}

		protected T GetMethod<T>(string name)
		{
			if (!this.ObjectAttributes.ContainsKey(name)) throw new InvalidOperationException($"No such attribute - {name}");
			object method = this.ObjectAttributes[name];
			if (method is object[]) throw new InvalidOperationException("Method is an attribute");
			else if (!(method is T)) throw new InvalidOperationException("Method deduced type does not match declared type");
			return (T) method;
		}

		public void Dispose()
		{
			MyAPIObjectBase.APIObjects.Remove(this.ObjectID.Value);
			this.ObjectID = null;
			this.ObjectAttributes.Clear();
		}

		public override bool Equals(object other)
		{
			if (this.ObjectAttributes.ContainsKey("#EQUALS")) return this.GetMethod<Func<object, bool>>("#EQUALS")(other);
			else return other is MyAPIObjectBase && this.Equals((MyAPIObjectBase) other);
		}
		public bool Equals(MyAPIObjectBase other)
		{
			if (this.ObjectAttributes.ContainsKey("#EQUALS")) return this.GetMethod<Func<object, bool>>("#EQUALS")(other);
			else return other != null && other.ObjectID == this.ObjectID;
		}

		public override int GetHashCode()
		{
			if (this.ObjectAttributes.ContainsKey("#HASH")) return this.GetMethod<Func<int>>("#HASH")();
			int upper = (int) (this.ObjectID & 0xFFFFFFFF);
			int lower = (int) ((this.ObjectID >> 32) & 0xFFFFFFFF);
			return upper ^ lower;
		}

		public void SetAttribute<T>(string name, T value)
		{
			if (!this.ObjectAttributes.ContainsKey(name)) throw new InvalidOperationException($"No such attribute - {name}");
			object attribute = this.ObjectAttributes[name];
			if (!(attribute is object[])) throw new InvalidOperationException("Attribute is a method");
			Action<T> setter = (Action<T>) ((object[]) attribute)[1];
			if (setter == null) throw new InvalidOperationException("Attribute is readonly and has no get accessor");
			setter(value);
		}

		public T GetAttribute<T>(string name)
		{
			if (!this.ObjectAttributes.ContainsKey(name)) throw new InvalidOperationException($"No such attribute - {name}");
			object attribute = this.ObjectAttributes[name];
			if (!(attribute is object[])) throw new InvalidOperationException("Attribute is a method");
			Func<T> getter = (Func<T>) ((object[]) attribute)[0];
			if (getter == null) throw new InvalidOperationException("Attribute is writeonly and has no set accessor");
			return getter();
		}
	}
}
