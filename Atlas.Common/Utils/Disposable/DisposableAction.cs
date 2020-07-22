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
        /// <remarks>
        /// Implementations of Dispose must be Idempotent, but we do not require <see cref="onDispose"/> to be idempotent.
        /// Hence we track the disposed status with <see cref="hasDisposed"/> to avoid calling the action multiple times. 
        /// </remarks>
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