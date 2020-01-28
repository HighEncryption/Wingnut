namespace Wingnut.Data
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A thread-safe lock
    /// </summary>
    /// <remarks>
    /// Credit to Stephen Toub
    /// https://blogs.msdn.microsoft.com/pfxteam/2012/02/12/building-async-coordination-primitives-part-6-asynclock/
    /// </remarks>
    public class AsyncLock
    {
        private readonly Task<IDisposable> _releaserTask;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IDisposable _releaser;

        public AsyncLock()
        {
            this._releaser = new Releaser(this._semaphore);
            this._releaserTask = Task.FromResult(this._releaser);
        }
        public IDisposable Lock()
        {
            this._semaphore.Wait();
            return this._releaser;
        }
        public Task<IDisposable> LockAsync()
        {
            var waitTask = this._semaphore.WaitAsync();
            return waitTask.IsCompleted
                ? this._releaserTask
                : waitTask.ContinueWith(
                    (_, releaser) => (IDisposable)releaser,
                    this._releaser,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
        private class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            public Releaser(SemaphoreSlim semaphore)
            {
                this._semaphore = semaphore;
            }
            public void Dispose()
            {
                this._semaphore.Release();
            }
        }
    }
}