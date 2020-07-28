/* ******************************
   **  Copyright Softwire 2020 ** 
   ****************************** */
// This was taken from a Softwire shareable Repo. At soem point it may get nugetified, in which case we might want
// migrate to that. Worth checking whether we've diverged, from the original code, though.

using System;

namespace LoggingStopwatch
{
    /// <summary>
    /// Defined in case you ever want something that looks like a ILongOperationLoggingStopwatch but does nothing, and has no overhead.
    /// </summary>
    public class FastFakeLongOperationLoggingStopwatch : FakeDisposable, ILongOperationLoggingStopwatch
    {
        private readonly IDisposable inner = new FakeDisposable();
        public IDisposable TimeInnerOperation() => inner;
    }

    public class FakeDisposable : IDisposable
    {
        public void Dispose() { /*Do Nothing*/ }
    }
}
