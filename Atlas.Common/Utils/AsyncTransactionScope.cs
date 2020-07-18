using System;
using System.Transactions;

namespace Atlas.Common.Utils
{
    public class AsyncTransactionScope : IDisposable
    {
        private readonly TransactionScope wrappedScope;
        public AsyncTransactionScope(TransactionScopeOption option = TransactionScopeOption.Required)
        {
            // Notes on interactions with Entity framework, if that ever happens: https://www.thinktecture.com/en/entity-framework-core/use-transactionscope-with-caution-in-2-1/
            wrappedScope =  new TransactionScope(
                option,
                TransactionManager.MaximumTimeout,        // This TransScope shouldn't be responsible for timeouts.
                TransactionScopeAsyncFlowOption.Enabled); // .NET 4.5 implementation of TransactionScope was just buggy for async/await. This is a back-compat fix for that (introduced in 4.5.1 ) See here: https://particular.net/blog/transactionscope-and-async-await-be-one-with-the-flow
        }

        public void Dispose() => wrappedScope.Dispose();
        public void Complete() => wrappedScope.Complete();
    }
}
