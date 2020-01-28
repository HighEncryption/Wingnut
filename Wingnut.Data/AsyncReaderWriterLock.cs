namespace Wingnut.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// An async-friendly implementation of ReaderWriterLock
    /// </summary>
    /// <remarks>
    /// Credit to Stephen Toub
    /// https://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10267069.aspx
    /// </remarks>
    public class AsyncReaderWriterLock
    {
        private readonly Task<Releaser> readerReleaser;
        private readonly Task<Releaser> writerReleaser;

        private readonly Queue<TaskCompletionSource<Releaser>> waitingWriters =
            new Queue<TaskCompletionSource<Releaser>>();
        private TaskCompletionSource<Releaser> waitingReader =
            new TaskCompletionSource<Releaser>();
        private int readersWaiting;

        public AsyncReaderWriterLock()
        {
            this.readerReleaser = Task.FromResult(new Releaser(this, false));
            this.writerReleaser = Task.FromResult(new Releaser(this, true));
        }
        public Task<Releaser> ReaderLockAsync()
        {
            lock (this.waitingWriters)
            {
                if (this.status >= 0 && this.waitingWriters.Count == 0)
                {
                    ++this.status;
                    return this.readerReleaser;
                }

                ++this.readersWaiting;
                return this.waitingReader.Task.ContinueWith(t => t.Result);
            }
        }

        public Task<Releaser> WriterLockAsync()
        {
            lock (this.waitingWriters)
            {
                if (this.status == 0)
                {
                    this.status = -1;
                    return this.writerReleaser;
                }

                var waiter = new TaskCompletionSource<Releaser>();
                this.waitingWriters.Enqueue(waiter);
                return waiter.Task;
            }
        }

        private void ReaderRelease()
        {
            TaskCompletionSource<Releaser> toWake = null;

            lock (this.waitingWriters)
            {
                --this.status;
                if (this.status == 0 && this.waitingWriters.Count > 0)
                {
                    this.status = -1;
                    toWake = this.waitingWriters.Dequeue();
                }
            }

            toWake?.SetResult(new Releaser(this, true));
        }

        private void WriterRelease()
        {
            TaskCompletionSource<Releaser> toWake = null;
            bool toWakeIsWriter = false;

            lock (this.waitingWriters)
            {
                if (this.waitingWriters.Count > 0)
                {
                    toWake = this.waitingWriters.Dequeue();
                    toWakeIsWriter = true;
                }
                else if (this.readersWaiting > 0)
                {
                    toWake = this.waitingReader;
                    this.status = this.readersWaiting;
                    this.readersWaiting = 0;
                    this.waitingReader = new TaskCompletionSource<Releaser>();
                }
                else this.status = 0;
            }

            toWake?.SetResult(new Releaser(this, toWakeIsWriter));
        }

        private int status;

        public struct Releaser : IDisposable
        {
            private readonly AsyncReaderWriterLock toRelease;
            private readonly bool writer;

            internal Releaser(AsyncReaderWriterLock toRelease, bool writer)
            {
                this.toRelease = toRelease;
                this.writer = writer;
            }

            public void Dispose()
            {
                if (this.toRelease != null)
                {
                    if (this.writer) this.toRelease.WriterRelease();
                    else this.toRelease.ReaderRelease();
                }
            }
        }
    }
}