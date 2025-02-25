using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOTA.ModularJumpGates.Util
{
	/// <summary>
	/// A class implementing a Reader-Writer Lock
	/// </summary>
	public class ReaderWriterLock
	{
		private sealed class QueuedThreadInfo
		{
			public readonly bool IsWriter;
			public readonly List<int> ThreadQueue = new List<int>();

			public QueuedThreadInfo(int initial_thread, bool is_writer)
			{
				this.IsWriter = is_writer;
				this.ThreadQueue.Add(initial_thread);
			}
		}

		public sealed class ReaderContext : IDisposable
		{
			private readonly ReaderWriterLock RWLock;

			public ReaderContext(ReaderWriterLock RWLock)
			{
				this.RWLock = RWLock;
				this.RWLock.AcquireReader();
			}

			public void Dispose()
			{
				this.RWLock.ReleaseReader();
			}
		}

		public sealed class WriterContext : IDisposable
		{
			private readonly ReaderWriterLock RWLock;

			public WriterContext(ReaderWriterLock RWLock)
			{
				this.RWLock = RWLock;
				this.RWLock.AcquireWriter();
			}

			public void Dispose()
			{
				this.RWLock.ReleaseWriter();
			}
		}

		private readonly object Lock = new object();
		private readonly Queue<QueuedThreadInfo> QueuedThreads = new Queue<QueuedThreadInfo>();

		/// <summary>
		/// Whether this thread has the lock
		/// </summary>
		public bool IsAcquired
		{
			get
			{
				int thread_id = System.Environment.CurrentManagedThreadId;
				lock (this.Lock) return this.QueuedThreads.FirstOrDefault()?.ThreadQueue?.Contains(thread_id) ?? false;
			}
		}

		/// <summary>
		/// Whether no threads have the lock
		/// </summary>
		public bool Available
		{
			get
			{
				lock (this.Lock) return this.QueuedThreads.Count == 0;
			}
		}

		/// <summary>
		/// Acquires a reader lock and blocks until this thread acquires<br />
		/// Lock must be released after critical code
		/// </summary>
		public void AcquireReader()
		{
			int thread_id = System.Environment.CurrentManagedThreadId;
			Monitor.Enter(this.Lock);

			try
			{
				QueuedThreadInfo thread_info = this.QueuedThreads.LastOrDefault();
				bool writer_active = this.QueuedThreads.Any((info) => info.IsWriter);
				if (writer_active && (thread_info == null || thread_info.IsWriter)) this.QueuedThreads.Enqueue(new QueuedThreadInfo(thread_id, false));
				else if (writer_active) thread_info.ThreadQueue.Add(thread_id);
				else if (thread_info == null) this.QueuedThreads.Enqueue(new QueuedThreadInfo(thread_id, false));
				else thread_info.ThreadQueue.Add(thread_id);
				thread_info = this.QueuedThreads.FirstOrDefault();
				if (thread_info.ThreadQueue.Contains(thread_id)) return;

				while (true)
				{
					Monitor.Wait(this.Lock, new TimeSpan(10));
					thread_info = this.QueuedThreads.FirstOrDefault();
					if (thread_info.ThreadQueue.Contains(thread_id)) break;
				}
			}
			finally
			{ Monitor.Exit(this.Lock); }
		}

		/// <summary>
		/// Acquires a writer lock and blocks until this thread acquires<br />
		/// Lock must be released after critical code
		/// </summary>
		public void AcquireWriter()
		{
			int thread_id = System.Environment.CurrentManagedThreadId;
			Monitor.Enter(this.Lock);

			try
			{
				QueuedThreadInfo thread_info = new QueuedThreadInfo(thread_id, true);
				this.QueuedThreads.Enqueue(thread_info);

				while (true)
				{
					Monitor.Wait(this.Lock, new TimeSpan(10));
					thread_info = this.QueuedThreads.FirstOrDefault();
					if (thread_info != null && thread_info.ThreadQueue.Contains(thread_id)) break;
				}
			}
			finally { Monitor.Exit(this.Lock); }
		}

		/// <summary>
		/// Releases a reader lock from this thread<br />
		/// Does nothing if this thread's a writer
		/// </summary>
		public void ReleaseReader()
		{
			int thread_id = System.Environment.CurrentManagedThreadId;
			Monitor.Enter(this.Lock);

			try
			{
				QueuedThreadInfo thread_info = this.QueuedThreads.FirstOrDefault();
				if (thread_info == null || thread_info.IsWriter || !thread_info.ThreadQueue.Contains(thread_id)) return;
				thread_info.ThreadQueue.Remove(thread_id);
				if (thread_info.ThreadQueue.Count == 0) this.QueuedThreads.Dequeue();
			}
			finally { Monitor.Exit(this.Lock); }
		}

		/// <summary>
		/// Releases a writer lock from this thread<br />
		/// Does nothing if this thread's a reader
		/// </summary>
		public void ReleaseWriter()
		{
			int thread_id = System.Environment.CurrentManagedThreadId;
			Monitor.Enter(this.Lock);

			try
			{
				QueuedThreadInfo thread_info = this.QueuedThreads.FirstOrDefault();
				if (thread_info == null || !thread_info.IsWriter || !thread_info.ThreadQueue.Contains(thread_id)) return;
				this.QueuedThreads.Dequeue();
			}
			finally { Monitor.Exit(this.Lock); }
		}

		/// <summary>
		/// Executes a callback within a reader lock<br />
		/// Reader lock will be released
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public void WithReader(Action callback)
		{
			try
			{
				this.AcquireReader();
				callback();
			}
			finally { this.ReleaseReader(); }
		}

		/// <summary>
		/// Executes a callback within a writer lock<br />
		/// Writer lock will be released
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public void WithWriter(Action callback)
		{
			try
			{
				this.AcquireWriter();
				callback();
			}
			finally { this.ReleaseWriter(); }
		}

		/// <summary>
		/// Attempts to acquire a reader lock on this thread<br />
		/// This method waits at most "timeout" seconds
		/// </summary>
		/// <param name="timeout">The timeout to wait in seconds</param>
		/// <returns>Whether the lock was acquired</returns>
		public bool TryAcquireReader(double timeout)
		{
			int thread_id = System.Environment.CurrentManagedThreadId;
			DateTime now = DateTime.Now;
			Monitor.Enter(this.Lock);

			try
			{
				QueuedThreadInfo thread_info = this.QueuedThreads.LastOrDefault();
				bool writer_active = this.QueuedThreads.Any((info) => info.IsWriter);
				if (writer_active && (thread_info == null || thread_info.IsWriter)) this.QueuedThreads.Enqueue(new QueuedThreadInfo(thread_id, false));
				else if (writer_active) thread_info.ThreadQueue.Add(thread_id);
				else if (thread_info == null) this.QueuedThreads.Enqueue(new QueuedThreadInfo(thread_id, false));
				else thread_info.ThreadQueue.Add(thread_id);
				thread_info = this.QueuedThreads.FirstOrDefault();
				if (thread_info.ThreadQueue.Contains(thread_id)) return true;

				while ((DateTime.Now - now).TotalSeconds < timeout)
				{
					Monitor.Wait(this.Lock, new TimeSpan(10));
					thread_info = this.QueuedThreads.FirstOrDefault();
					if (thread_info.ThreadQueue.Contains(thread_id)) return true;
				}

				return false;
			}
			finally
			{ Monitor.Exit(this.Lock); }
		}

		/// <summary>
		/// Attempts to acquire a writer lock on this thread<br />
		/// This method waits at most "timeout" seconds
		/// </summary>
		/// <param name="timeout">The timeout to wait in seconds</param>
		/// <returns>Whether the lock was acquired</returns>
		public bool TryAcquireWriter(double timeout)
		{
			int thread_id = System.Environment.CurrentManagedThreadId;
			DateTime now = DateTime.Now;
			Monitor.Enter(this.Lock);

			try
			{
				QueuedThreadInfo thread_info = new QueuedThreadInfo(thread_id, true);
				this.QueuedThreads.Enqueue(thread_info);

				while ((DateTime.Now - now).TotalSeconds < timeout)
				{
					Monitor.Wait(this.Lock, new TimeSpan(10));
					thread_info = this.QueuedThreads.FirstOrDefault();
					if (thread_info != null && thread_info.ThreadQueue.Contains(thread_id)) return true;
				}

				return false;
			}
			finally { Monitor.Exit(this.Lock); }
		}

		/// <summary>
		/// Returns a reader context for use with "using"<br />
		/// Context will release lock on disposal
		/// </summary>
		/// <returns>The reader context</returns>
		public ReaderContext WithReader()
		{
			return new ReaderContext(this);
		}

		/// <summary>
		/// Returns a writer context for use with "using"<br />
		/// Context will release lock on disposal
		/// </summary>
		/// <returns>The writer context</returns>
		public WriterContext WithWriter()
		{
			return new WriterContext(this);
		}
	}
}
