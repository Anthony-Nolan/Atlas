using System;
using Atlas.Common.Caching;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.ConfigurationProviders
{
    [TestFixture]
    public class ActiveHlaNomenclatureVersionAccessorTests
    {
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private ITransientCacheProvider transientCacheProvider;
        
        private IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;
        
        [SetUp]
        public void SetUp()
        {
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            transientCacheProvider = Substitute.For<ITransientCacheProvider>();
            
            hlaNomenclatureVersionAccessor = new ActiveHlaNomenclatureVersionAccessor(dataRefreshHistoryRepository, transientCacheProvider);
        }

        [Test]
        public void GetActiveHlaNomenclatureVersion_WhenActiveVersionIsNull_ThrowsException()
        {
            dataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion().Returns(null as string);

            hlaNomenclatureVersionAccessor.Invoking(provider => provider.GetActiveHlaNomenclatureVersion()).Should().Throw<ArgumentNullException>();
        }
    }
}