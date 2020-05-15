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
    public class WmdaHlaVersionProviderTests
    {
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private ITransientCacheProvider transientCacheProvider;
        
        private IActiveHlaVersionAccessor wmdaHlaVersionProvider;
        
        [SetUp]
        public void SetUp()
        {
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            transientCacheProvider = Substitute.For<ITransientCacheProvider>();
            
            wmdaHlaVersionProvider = new ActiveHlaVersionAccessor(dataRefreshHistoryRepository, transientCacheProvider);
        }

        [Test]
        public void GetActiveHlaDatabaseVersion_WhenActiveVersionIsNull_ThrowsException()
        {
            dataRefreshHistoryRepository.GetActiveWmdaDataVersion().Returns(null as string);

            wmdaHlaVersionProvider.Invoking(provider => provider.GetActiveHlaDatabaseVersion()).ShouldThrow<ArgumentNullException>();
        }
    }
}