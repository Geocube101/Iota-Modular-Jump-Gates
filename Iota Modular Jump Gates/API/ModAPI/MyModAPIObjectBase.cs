using IOTA.ModularJumpGates.API.ModAPI.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IOTA.ModularJumpGates.API.ModAPI
{
	/// <summary>
	/// Base class responsible for wrapping an attribute map into a contained logical class
	/// </summary>
	public abstract class MyModAPIObjectBase : IDisposable, IEquatable<MyModAPIObjectBase>
	{
		/// <summary>
		/// Master map of all created API object wrappers
		/// </summary>
		private static ConcurrentDictionary<JumpGateUUID, MyModAPIObjectBase> APIObjects = new ConcurrentDictionary<JumpGateUUID, MyModAPIObjectBase>();

		/// <summary>
		/// Master map of this wrapper's attributes
		/// </summary>
		private Dictionary<string, object> ObjectAttributes = new Dictionary<string, object>();

		/// <summary>
		/// Whether this wrapper is ok to be used<br />
		/// Will be false if "ObjectID" is null
		/// </summary>
		public bool HandleValid => this.ObjectID != null;

		/// <summary>
		/// This wrapper's object ID
		/// </summary>
		public JumpGateUUID? ObjectID { get; private set; } = null;

		public static bool operator== (MyModAPIObjectBase a, MyModAPIObjectBase b)
		{
			if (object.ReferenceEquals(a, b)) return true;
			else if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
			else return a.Equals(b);
		}
		public static bool operator!= (MyModAPIObjectBase a, MyModAPIObjectBase b)
		{
			return !(a == b);
		}

		/// <summary>
		/// Gets the wrapper specified by the ID or creates a new wrapper if none exist
		/// </summary>
		/// <typeparam name="T">The wrapper type to create</typeparam>
		/// <param name="attributes">The master attribute map</param>
		/// <param name="factory">The creator factory to execute if no existing wrapper was found</param>
		/// <returns>The wrapper</returns>
		protected static T GetObjectOrNew<T>(Dictionary<string, object> attributes, Func<T> factory) where T : MyModAPIObjectBase
		{
			if (attributes == null) return null;
			JumpGateUUID guid = new JumpGateUUID((long[]) attributes["GUID"]);
			MyModAPIObjectBase existing;
			if (MyModAPIObjectBase.APIObjects.TryGetValue(guid, out existing)) return (T) existing;
			T new_object = factory();
			MyModAPIObjectBase.APIObjects[guid] = new_object;
			return new_object;
		}

		/// <summary>
		/// Closes all wrappers and releases the internal map<br />
		/// No new API object wrapper should be created beyond this point<br />
		/// Called automatically by ModAPISession when unloading
		/// </summary>
		internal static void DisposeAll()
		{
			if (MyModAPIObjectBase.APIObjects == null) return;
			foreach (KeyValuePair<JumpGateUUID, MyModAPIObjectBase> pair in MyModAPIObjectBase.APIObjects) pair.Value.Dispose();
			MyModAPIObjectBase.APIObjects.Clear();
			MyModAPIObjectBase.APIObjects = null;
		}

		protected MyModAPIObjectBase(Dictionary<string, object> attributes)
		{
			this.ObjectID = new JumpGateUUID((long[]) attributes["GUID"]);
			this.ObjectAttributes = attributes;
			MyModAPIObjectBase.APIObjects[this.ObjectID.Value] = this;
		}

		/// <summary>
		/// Gets a method from this wrapper by name
		/// </summary>
		/// <typeparam name="T">The method signature</typeparam>
		/// <param name="name">The method name</param>
		/// <returns>The method</returns>
		/// <exception cref="InvalidOperationException">If specified attribute does not exist, is not a method, or does not match the specified signature</exception>
		protected T GetMethod<T>(string name)
		{
			if (!this.ObjectAttributes.ContainsKey(name)) throw new InvalidOperationException($"No such attribute - {name}");
			object method = this.ObjectAttributes[name];
			if (method is object[]) throw new InvalidOperationException("Method is an attribute");
			else if (!(method is T)) throw new InvalidOperationException("Method deduced type does not match declared type");
			return (T) method;
		}

		/// <summary>
		/// Sets an attribute by name to the specified value
		/// </summary>
		/// <typeparam name="T">The attribute's type</typeparam>
		/// <param name="name">The attribute's name</param>
		/// <param name="value">The new attribute value</param>
		/// <exception cref="InvalidOperationException">If the specified attribute does not exist, is a method, or cannot be written</exception>
		protected void SetAttribute<T>(string name, T value)
		{
			if (!this.ObjectAttributes.ContainsKey(name)) throw new InvalidOperationException($"No such attribute - {name}");
			object attribute = this.ObjectAttributes[name];
			if (!(attribute is object[])) throw new InvalidOperationException("Attribute is a method");
			Action<T> setter = (Action<T>) ((object[]) attribute)[1];
			if (setter == null) throw new InvalidOperationException("Attribute has no set accessor");
			setter(value);
		}

		/// <summary>
		/// Closes this API wrapper<br />
		/// Called during wrapper disposal
		/// </summary>
		protected virtual void Close() { }

		/// <summary>
		/// Gets an attribute by name
		/// </summary>
		/// <typeparam name="T">The attribute's type</typeparam>
		/// <param name="name">The attribute's name</param>
		/// <returns>The attribute's value</returns>
		/// <exception cref="InvalidOperationException">If the specified attribute does not exist, is a method, or cannot be read</exception>
		protected T GetAttribute<T>(string name)
		{
			if (!this.ObjectAttributes.ContainsKey(name)) throw new InvalidOperationException($"No such attribute - {name}");
			object attribute = this.ObjectAttributes[name];
			if (!(attribute is object[])) throw new InvalidOperationException("Attribute is a method");
			Func<T> getter = (Func<T>) ((object[]) attribute)[0];
			if (getter == null) throw new InvalidOperationException("Attribute has no set accessor");
			return getter();
		}

		/// <summary>
		/// Closes this object wrapper<br />
		/// Any further attempt to use this wrapper is erroneous
		/// </summary>
		public void Dispose()
		{
			if (this.ObjectID == null) return;
			if (this.ObjectAttributes != null && this.ObjectAttributes.ContainsKey("#DEINIT")) ((Action) this.ObjectAttributes["#DEINIT"])();
			this.Close();
			MyModAPIObjectBase.APIObjects.Remove(this.ObjectID.Value);
			this.ObjectID = null;
			this.ObjectAttributes.Clear();
			this.ObjectAttributes = null;
		}

		public override bool Equals(object other)
		{
			if (this.ObjectAttributes.ContainsKey("#EQUALS")) return this.GetMethod<Func<object, bool>>("#EQUALS")(other);
			else return other is MyModAPIObjectBase && this.Equals((MyModAPIObjectBase) other);
		}
		public bool Equals(MyModAPIObjectBase other)
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
