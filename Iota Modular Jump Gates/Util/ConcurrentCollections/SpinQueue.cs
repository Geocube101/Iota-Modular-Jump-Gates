using System;
using System.Collections;
using System.Collections.Generic;

namespace IOTA.ModularJumpGates.Util.ConcurrentCollections
{
	public class SpinQueue<Type> : IEnumerable<Type>
	{
		public class Iterator : IEnumerator<Type>
		{
			private SpinQueue<Type> Queue;
			private int Index = 0;

			public Iterator(SpinQueue<Type> queue)
			{
				this.Queue = queue;
			}

			public void Reset()
			{
				this.Index = 0;
			}

			public void Dispose()
			{
				this.Queue = null;
			}

			public bool MoveNext()
			{
				return ++this.Index < this.Queue.Count;
			}

			public Type Current { get { return this.Queue[this.Index]; } }

			object IEnumerator.Current { get { return this.Queue[this.Index]; } }
		}

		private readonly Type[] Buffer;
		private int Index = 0;

		public int Count { get; private set; } = 0;

		public SpinQueue(int size)
		{
			if (size <= 0) throw new ArgumentException("size cannot be less than or equal to 0");
			this.Buffer = new Type[size];
		}

		public void Clear()
		{
			for (uint i = 0; i < this.Buffer.Length; ++i) this.Buffer[i] = default(Type);
			this.Index = 0;
			this.Count = 0;
		}

		public void Enqueue(Type element)
		{
			this.Buffer[this.Index] = element;
			this.Index = (this.Index + 1) % this.Buffer.Length;
			if (this.Count < this.Buffer.Length) ++this.Count;
		}

		public Type Dequeue()
		{
			if (this.Count == 0) throw new InvalidOperationException("Queue is empty");
			return this.Buffer[--this.Count];
		}

		public Type this[int index]
		{
			get
			{
				index += this.Index;
				if (index < 0) index += this.Count;
				return this.Buffer[index % this.Buffer.Length];
			}

			set
			{
				index += this.Index;
				if (index < 0) index += this.Count;
				this.Buffer[index % this.Buffer.Length] = value;
			}
		}

		public SpinQueue<Type> Clone()
		{
			SpinQueue<Type> clone = new SpinQueue<Type>(this.Buffer.Length) {
				Count = this.Count,
				Index = this.Index,
			};
			for (int i = 0; i < this.Buffer.Length; ++i) clone.Buffer[i] = this.Buffer[i];
			return clone;
		}

		public IEnumerator<Type> GetEnumerator()
		{
			return new Iterator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Iterator(this);
		}
	}

	public class ConcurrentSpinQueue<Type> : SpinQueue<Type>, IEnumerable<Type>
	{
		private readonly ReaderWriterLock RWLock = new ReaderWriterLock();

		public ConcurrentSpinQueue(int size) : base(size) { }

		public new void Clear()
		{
			this.RWLock.AcquireWriter();
			try { base.Clear(); }
			finally { this.RWLock.ReleaseWriter(); }
		}

		public new void Enqueue(Type element)
		{
			this.RWLock.AcquireWriter();
			try { base.Enqueue(element); }
			finally { this.RWLock.ReleaseWriter(); }
		}

		public bool TryDequeue(out Type element)
		{
			this.RWLock.AcquireWriter();

			try
			{
				if (this.Count == 0)
				{
					element = default(Type);
					return false;
				}
				else
				{
					element = base.Dequeue();
					return true;
				}
			}
			finally { this.RWLock.ReleaseWriter(); }
		}

		public new Type Dequeue()
		{
			this.RWLock.AcquireWriter();
			try { return base.Dequeue(); }
			finally { this.RWLock.ReleaseWriter(); }
		}

		public new Type this[int index]
		{
			get
			{
				this.RWLock.AcquireReader();
				try { return base[index]; }
				finally { this.RWLock.ReleaseReader(); }
			}

			set
			{
				this.RWLock.AcquireWriter();
				try { base[index] = value; }
				finally { this.RWLock.ReleaseWriter(); }
			}
		}

		public new IEnumerator<Type> GetEnumerator()
		{
			this.RWLock.AcquireReader();
			try { return new Iterator(this.Clone()); }
			finally { this.RWLock.ReleaseReader(); }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			this.RWLock.AcquireReader();
			try { return new Iterator(this.Clone()); }
			finally { this.RWLock.ReleaseReader(); }
		}
	}
}
