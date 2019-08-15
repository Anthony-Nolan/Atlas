using LazyCache;

namespace Nova.SearchAlgorithm.Helpers
{
    /// <summary>
    /// Used to explicitly rely on a cache with transient scope - as opposed to the default "persistent" scope used for long-lasting caches
    /// </summary>
    public interface ITransientCacheProvider
    {
        IAppCache Cache { get; }
    }
    
    public class TransientCacheProvider : ITransientCacheProvider
    {
        public IAppCache Cache { get; }

        public TransientCacheProvider(IAppCache cache)
        {
            Cache = cache;
        }
    }
}