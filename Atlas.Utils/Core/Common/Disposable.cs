using System;
using System.Threading;

namespace Atlas.Utils.Core.Common
{
    /// <summary>
    /// A thread-safe implementation of the Disposable pattern
    /// </summary>
    public abstract class Disposable : IDisposable
    {
        private int disposed;

        protected bool IsDisposed
        {
            get
            {
                Interlocked.MemoryBarrier();
                return disposed == 1;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 1)
            {
                return;
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
