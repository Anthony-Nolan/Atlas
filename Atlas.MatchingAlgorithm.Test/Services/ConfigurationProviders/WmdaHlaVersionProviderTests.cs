using System;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Settings;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.ConfigurationProviders
{
    [TestFixture]
    public class WmdaHlaVersionProviderTests
    {
        private IOptions<WmdaSettings> wmdaSettings;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private ITransientCacheProvider transientCacheProvider;
        
        private IWmdaHlaVersionProvider wmdaHlaVersionProvider;
        
        [SetUp]
        public void SetUp()
        {
            wmdaSettings = Substitute.For<IOptions<WmdaSettings>>();
            wmdaSettings.Value.Returns(new WmdaSettings());
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            transientCacheProvider = Substitute.For<ITransientCacheProvider>();
            
            wmdaHlaVersionProvider = new WmdaHlaVersionProvider(wmdaSettings, dataRefreshHistoryRepository, transientCacheProvider);
        }

        [Test]
        public void GetActiveHlaDatabaseVersion_WhenActiveVersionIsNull_ThrowsException()
        {
            dataRefreshHistoryRepository.GetActiveWmdaDataVersion().Returns(null as string);

            wmdaHlaVersionProvider.Invoking(provider => provider.GetActiveHlaDatabaseVersion()).ShouldThrow<ArgumentNullException>();
        }
    }
}