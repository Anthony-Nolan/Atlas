using System;

namespace Atlas.Common.Utils.Disposable
{
    public class DisposableAction : IDisposable
    {
        private readonly Action onDispose;
        
        // ReSharper disable once RedundantDefaultMemberInitializer - make it clear that this should default to false
        private bool hasDisposed = false;

        // ReSharper disable once MemberCanBeInternal
        public DisposableAction(Action onDispose)
        {
            this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!hasDisposed)
            {
                onDispose();
                hasDisposed = true;
            }
        }
    }
}