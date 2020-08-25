using System;
using System.Transactions;

namespace Atlas.Common.Utils
{
    /// <summary>
    /// This simulates <see cref="TransactionScope"/>, so that we can provide our own wrappers of that.
    /// </summary>
    public interface ITransactionScope : IDisposable
    {
        void Complete();
    }

    /// <summary>
    /// An object that satisfies ITransactionScope without actually
    /// doing anything, so that we can declare optional scope.
    /// </summary>
    public class FakeTransactionScope : ITransactionScope
    {
        public void Dispose() { /* Do nothing */ }
        public void Complete() { /* Do nothing */ }
    }

    /// <summary>
    /// A Transaction scope that supports Async code, without being verbose to declare.
    /// </summary>
    public class AsyncTransactionScope : ITransactionScope
    {
        private readonly TransactionScope wrappedScope;
        public AsyncTransactionScope(TransactionScopeOption option = TransactionScopeOption.Required)
        {
            // Notes on interactions with Entity framework, if that ever happens: https://www.thinktecture.com/en/entity-framework-core/use-transactionscope-with-caution-in-2-1/
            wrappedScope = new TransactionScope(
                option,
                TransactionManager.MaximumTimeout,        // This TransScope shouldn't be responsible for timeouts.
                TransactionScopeAsyncFlowOption.Enabled); // .NET 4.5 implementation of TransactionScope was just buggy for async/await. This is a back-compat fix for that (introduced in 4.5.1 ) See here: https://particular.net/blog/transactionscope-and-async-await-be-one-with-the-flow
        }

        public void Dispose() => wrappedScope.Dispose();
        public void Complete() => wrappedScope.Complete();
    }

    /// <summary>
    /// Depending on the input parameter, is either an AsyncTransactionScope.
    /// Or a FakeTransactionScope that does nothing.
    /// </summary>
    public class OptionalAsyncTransactionScope : ITransactionScope
    {
        private readonly ITransactionScope wrappedScope;
        public OptionalAsyncTransactionScope(bool useRealScope, TransactionScopeOption option = TransactionScopeOption.Required)
        {
            wrappedScope = useRealScope ? (ITransactionScope)new AsyncTransactionScope(option) : new FakeTransactionScope();
        }

        public void Dispose() => wrappedScope.Dispose();
        public void Complete() => wrappedScope.Complete();
    }
}
