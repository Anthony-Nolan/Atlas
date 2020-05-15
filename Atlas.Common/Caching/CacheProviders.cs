using LazyCache;

namespace Atlas.Common.Caching
{
    /// <inheritdoc cref="ICacheProvider"/>
    public interface IPersistentCacheProvider : ICacheProvider { }
    /// <inheritdoc cref="ICacheProvider"/>
    public interface ITransientCacheProvider : ICacheProvider { }

    /// <summary>
    /// Used to explicitly state the scope of the cache your rely upon; Transient or Persistent as appropriate.
    /// </summary>
    public interface ICacheProvider
    {
        IAppCache Cache { get; }
    }

    public class PersistentCacheProvider : IPersistentCacheProvider
    { 
        public IAppCache Cache { get; }
        public PersistentCacheProvider(IAppCache cache) { Cache = cache; }
    }

    public class TransientCacheProvider : ITransientCacheProvider
    {
        public IAppCache Cache { get; }
        public TransientCacheProvider(IAppCache cache) { Cache = cache; }
    }
}