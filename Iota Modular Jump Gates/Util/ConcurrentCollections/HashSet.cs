using System;
using System.Collections;
using System.Collections.Generic;

namespace IOTA.ModularJumpGates.Util.ConcurrentCollections
{
	/// <summary>
	/// A concurrent hashset allowing iteration with a single linked list<br />
	/// Operations are locked with an RW lock
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

			public bool InStack(T item)
			{
				LinkedNode node = this;

				while (node != null)
				{
					if (node.Value.Equals(item)) return true;
					node = node.NextNode;
				}

				return false;
			}

			public LinkedNode CloneStack(LinkedNode prev)
			{
				LinkedNode clone = new LinkedNode(this.Value, this.Bucket, null, prev);
				clone.Next = this.Next?.CloneStack(clone);
				return clone;
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

		private bool RemoveNode(LinkedNode node)
		{
			if (node == null) return false;
			else if (node == this.FirstEntry && node == this.LastEntry)
			{
				this.FirstEntry = null;
				this.LastEntry = null;
			}
			else if (node == this.FirstEntry && node.Next != null)
			{
				this.FirstEntry = node.Next;
				node.Next.Prev = null;
			}
			else if (node == this.FirstEntry) this.FirstEntry = node.Next;
			else if (node == this.LastEntry && node.Prev != null)
			{
				this.LastEntry = node.Prev;
				node.Prev.Next = null;
			}
			else if (node == this.LastEntry) this.LastEntry = node.Prev;
			else
			{
				node.Prev.Next = node.Next;
				node.Next.Prev = node.Prev;
			}

			--this.Count;
			LinkedNode bucket_root = this.Buckets[node.Bucket];
			LinkedNode bucket_next;
			if (node == bucket_root && (bucket_next = node.NextNode) != null) this.Buckets[node.Bucket] = bucket_next;
			else if (node == bucket_root) this.Buckets.Remove(node.Bucket);
			return true;
		}

		public void Add(T item)
		{
			if (item == null) return;
			int bucket = item.GetHashCode();

			using (this.RWLock.WithWriter())
			{
				LinkedNode bucket_root = this.Buckets.GetValueOrDefault(bucket, null);
				LinkedNode node = new LinkedNode(item, bucket, bucket_root?.Next, bucket_root ?? this.LastEntry);

				if (bucket_root == null && this.LastEntry == null)
				{
					this.Buckets[bucket] = node;
					this.LastEntry = node;
					this.FirstEntry = node;
				}
				else if (bucket_root == null)
				{
					this.Buckets[bucket] = node;
					this.LastEntry.Next = node;
					this.LastEntry = node;
				}
				else if (bucket_root.InStack(item)) return;
				else if (bucket_root.Next == null)
				{
					bucket_root.Next = node;
					this.LastEntry = node;
				}
				else
				{
					bucket_root.Next.Prev = node;
					bucket_root.Next = node;
				}

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
				foreach (T item in collection) this.Add(item);
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
			if (item == null || this.FirstEntry == null) return false;
			int bucket = item.GetHashCode();

			using (this.RWLock.WithWriter())
			{
				LinkedNode curr = this.Buckets.GetValueOrDefault(bucket, null);
				if (curr == null) return false;
				
				while (curr != null)
				{
					if (object.Equals(curr.Value, item)) break;
					curr = curr.NextNode;
				}

				return this.RemoveNode(curr);
			}
		}

		public int RemoveAll(Func<T, bool> predicate)
		{
			if (predicate == null) return 0;
			int removed = 0;

			using (this.RWLock.WithWriter())
			{
				LinkedNode node = this.FirstEntry;
				while (node != null)
				{
					if (predicate(node.Value))
					{
						this.RemoveNode(node);
						++removed;
					}
					node = node.Next;
				}
			}

			return removed;
		}

		public IEnumerator<T> GetEnumerator()
		{
			LinkedNode node = this.FirstEntry;

			while (node != null)
			{
				yield return node.Value;
				node = node.Next;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			LinkedNode node = this.FirstEntry;

			while (node != null)
			{
				yield return node.Value;
				node = node.Next;
			}
		}
	}
}
