using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTA.ModularJumpGates.Util.ConcurrentCollections
{
	/// <summary>
	/// A concurrent hashset allowing iteration with a single linked list<br />
	/// Duplicity is not checked
	/// </summary>
	/// <typeparam name="T">The type of elements stored</typeparam>
	public class ConcurrentLinkedHashSet<T> : ICollection<T>
	{
		private sealed class LinkedNode
		{
			public T Value;
			public int Bucket;
			public LinkedNode Next;
			public LinkedNode Prev;

			public LinkedNode NextNode
			{
				get
				{
					return (this.Next?.Bucket == this.Bucket) ? this.Next : null;
				}
			}

			public LinkedNode(T value, int bucket, LinkedNode next, LinkedNode prev)
			{
				this.Value = value;
				this.Bucket = bucket;
				this.Next = next;
				this.Prev = prev;
				if (next == this || prev == this || (next == prev && next != null)) throw new InvalidOperationException("Neighboring nodes are self or invalid");
			}

			public LinkedNode CloneStack(LinkedNode prev)
			{
				LinkedNode clone = new LinkedNode(this.Value, this.Bucket, null, prev);
				clone.Next = this.Next?.CloneStack(clone);
				return clone;
			}
		}

		public class ConcurrentLinkedHashSetIterator : IEnumerator<T>
		{
			private LinkedNode Root;
			private LinkedNode CurrentNode;
			private T CurrentValue;

			public ConcurrentLinkedHashSetIterator(ConcurrentLinkedHashSet<T> hashset)
			{
				if (hashset == null) throw new ArgumentNullException(nameof(hashset));
				this.Root = new LinkedNode(default(T), -1, hashset.FirstEntry, null);
				this.CurrentNode = this.Root;
			}

			public T Current
			{
				get
				{
					return this.CurrentValue;
				}
			}

			object IEnumerator.Current
			{
				get
				{
					return this.CurrentValue;
				}
			}

			public void Dispose()
			{
				this.Root = null;
				this.CurrentNode = null;
			}

			public bool MoveNext()
			{
				this.CurrentNode = this.CurrentNode?.Next;
				this.CurrentValue = (this.CurrentNode == null) ? default(T) : this.CurrentNode.Value;
				return this.CurrentNode != null;
			}

			public void Reset()
			{
				this.CurrentNode = this.Root;
			}
		}

		private readonly ReaderWriterLock RWLock = new ReaderWriterLock();
		private readonly Dictionary<int, LinkedNode> Buckets = new Dictionary<int, LinkedNode>();
		private LinkedNode FirstEntry = null;
		private LinkedNode LastEntry = null;

		/// <summary>
		/// The number of items in this collection
		/// </summary>
		public int Count { get; private set; }

		bool ICollection<T>.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public ConcurrentLinkedHashSet() { }

		public ConcurrentLinkedHashSet(IEnumerable<T> collection)
		{
			try
			{
				this.RWLock.AcquireWriter();

				foreach (T item in collection)
				{
					int bucket = item.GetHashCode();
					LinkedNode bucket_root = this.Buckets.GetValueOrDefault(bucket, null);
					LinkedNode node = new LinkedNode(item, bucket, bucket_root?.Next, this.LastEntry);

					if (bucket_root == null && this.LastEntry == null)
					{
						this.Buckets.Add(bucket, node);
					}
					else if (bucket_root == null)
					{
						this.Buckets.Add(bucket, node);
						this.LastEntry.Next = node;
					}
					else
					{
						bucket_root.Next = node;
					}

					this.LastEntry = node;
					this.FirstEntry = this.FirstEntry ?? node;
					++this.Count;
				}
			}
			finally { this.RWLock.ReleaseWriter(); }
		}

		public void Add(T item)
		{
			if (item == null) return;
			int bucket = item.GetHashCode();

			using (this.RWLock.WithWriter())
			{
				LinkedNode bucket_root = this.Buckets.GetValueOrDefault(bucket, null);
				LinkedNode node = new LinkedNode(item, bucket, bucket_root?.Next, this.LastEntry);

				if (bucket_root == null && this.LastEntry == null)
				{
					this.Buckets.Add(bucket, node);
				}
				else if (bucket_root == null)
				{
					this.Buckets.Add(bucket, node);
					this.LastEntry.Next = node;
				}
				else
				{
					bucket_root.Next = node;
				}

				this.LastEntry = node;
				this.FirstEntry = this.FirstEntry ?? node;
				++this.Count;
			}
		}

		public void Clear()
		{
			using (this.RWLock.WithWriter())
			{
				this.Buckets.Clear();
				this.Count = 0;
				this.LastEntry = null;
				this.FirstEntry = null;
			}
		}

		public void CopyTo(T[] array, int array_index)
		{
			using (this.RWLock.WithReader())
			{
				int index = array_index;
				foreach (T item in this) array[index++] = item;
			}
		}

		/// <summary>
		/// Extends this collection with the elements from another
		/// </summary>
		/// <param name="collection">The second collection</param>
		public void AddRange(IEnumerable<T> collection)
		{
			using (this.RWLock.WithWriter())
			{
				foreach (T item in collection)
				{
					if (item == null) continue;
					int bucket = item.GetHashCode();
					LinkedNode bucket_root = this.Buckets.GetValueOrDefault(bucket, null);
					LinkedNode node = new LinkedNode(item, bucket, bucket_root?.Next, this.LastEntry);

					if (bucket_root == null && this.LastEntry == null)
					{
						this.Buckets.Add(bucket, node);
					}
					else if (bucket_root == null)
					{
						this.Buckets.Add(bucket, node);
						this.LastEntry.Next = node;
					}
					else
					{
						bucket_root.Next = node;
					}

					this.LastEntry = node;
					this.FirstEntry = this.FirstEntry ?? node;
					++this.Count;
				}
			}
		}

		public bool Contains(T item)
		{
			if (item == null) return false;
			int bucket = item.GetHashCode();

			using (this.RWLock.WithReader())
			{
				LinkedNode bucket_root = this.Buckets.GetValueOrDefault(bucket, null);
				if (bucket_root == null) return false;

				while (bucket_root != null)
				{
					if (object.Equals(bucket_root.Value, item)) return true;
					bucket_root = bucket_root.NextNode;
				}

				return false;
			}
		}

		public bool Remove(T item)
		{
			if (item == null) return false;
			int bucket = item.GetHashCode();

			using (this.RWLock.WithWriter())
			{
				LinkedNode curr = this.Buckets.GetValueOrDefault(bucket, null);
				if (curr == null) return false;
				LinkedNode last = null;
				LinkedNode next = null;

				while (curr != null)
				{
					bool found = object.Equals(curr.Value, item);
					next = curr.NextNode;

					if (found && curr == this.FirstEntry)
					{
						if (next == null) this.Buckets.Remove(bucket);
						else this.Buckets[bucket] = next;
						this.FirstEntry = curr.Next;
						--this.Count;
						return true;
					}
					else if (found && last == null)
					{
						if (next == null) this.Buckets.Remove(bucket);
						else this.Buckets[bucket] = next;
						curr.Prev.Next = next;
						this.LastEntry = (curr == this.LastEntry) ? curr.Prev : this.LastEntry;
						this.FirstEntry = (curr == this.FirstEntry) ? curr.Next : this.FirstEntry;
						--this.Count;
						return true;
					}
					else if (found)
					{
						last.Next = curr.Next;
						this.LastEntry = (curr == this.LastEntry) ? curr.Prev : this.LastEntry;
						this.FirstEntry = (curr == this.FirstEntry) ? curr.Next : this.FirstEntry;
						--this.Count;
						return true;
					}

					last = curr;
					curr = next;
				}

				return false;
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			using (this.RWLock.WithReader()) return new ConcurrentLinkedHashSetIterator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			using (this.RWLock.WithReader()) return new ConcurrentLinkedHashSetIterator(this);
		}
	}
}
