using IOTA.ModularJumpGates.API.ModAPI.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IOTA.ModularJumpGates.API.ModAPI
{
	public abstract class MyAPIObjectBase : IDisposable, IEquatable<MyAPIObjectBase>
	{
		private static readonly ConcurrentDictionary<JumpGateUUID, MyAPIObjectBase> APIObjects = new ConcurrentDictionary<JumpGateUUID, MyAPIObjectBase>();

		private readonly Dictionary<string, object> ObjectAttributes = new Dictionary<string, object>();

		public bool HandleValid => this.ObjectID != null;
		public JumpGateUUID? ObjectID { get; private set; } = null;

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

		protected static T GetObjectOrNew<T>(Dictionary<string, object> attributes, Func<T> factory) where T : MyAPIObjectBase
		{
			if (attributes == null) return null;
			JumpGateUUID guid = new JumpGateUUID((long[]) attributes["GUID"]);
			T dynamic = null;

			for (byte i = 0; i < 100; ++i)
			{
				try { return (T) MyAPIObjectBase.APIObjects.GetValueOrDefault(guid, dynamic = (dynamic ?? factory())); }
				catch (ArgumentException _) { }
			}

			return null;
		}

		protected MyAPIObjectBase(Dictionary<string, object> attributes)
		{
			this.ObjectID = new JumpGateUUID((long[]) attributes["GUID"]);
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

		protected void SetAttribute<T>(string name, T value)
		{
			if (!this.ObjectAttributes.ContainsKey(name)) throw new InvalidOperationException($"No such attribute - {name}");
			object attribute = this.ObjectAttributes[name];
			if (!(attribute is object[])) throw new InvalidOperationException("Attribute is a method");
			Action<T> setter = (Action<T>) ((object[]) attribute)[1];
			if (setter == null) throw new InvalidOperationException("Attribute is readonly and has no get accessor");
			setter(value);
		}

		protected T GetAttribute<T>(string name)
		{
			if (!this.ObjectAttributes.ContainsKey(name)) throw new InvalidOperationException($"No such attribute - {name}");
			object attribute = this.ObjectAttributes[name];
			if (!(attribute is object[])) throw new InvalidOperationException("Attribute is a method");
			Func<T> getter = (Func<T>) ((object[]) attribute)[0];
			if (getter == null) throw new InvalidOperationException("Attribute is writeonly and has no set accessor");
			return getter();
		}

		public void Dispose()
		{
			if (this.ObjectID == null) return;
			else if (this.ObjectAttributes != null && this.ObjectAttributes.ContainsKey("#DEINIT")) ((Action) this.ObjectAttributes["#DEINIT"])();
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
			else return this.ObjectID.GetHashCode();
		}
	}
}
