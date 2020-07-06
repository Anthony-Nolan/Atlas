using System;
using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers.Builders;
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

            transientCacheProvider.Cache.Returns(AppCacheBuilder.DefaultCache);
            
            hlaNomenclatureVersionAccessor = new ActiveHlaNomenclatureVersionAccessor(dataRefreshHistoryRepository, transientCacheProvider);
        }

        [Test]
        public void GetActiveHlaNomenclatureVersion_WhenActiveVersionIsNull_ThrowsException()
        {
            dataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion().Returns(null as string);

            hlaNomenclatureVersionAccessor.Invoking(provider => provider.GetActiveHlaNomenclatureVersion()).Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void DoesActiveHlaNomenclatureVersionExist_WhenActiveVersionIsNull_ReturnsTrue()
        {
            dataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion().Returns(null as string);

            var doesActiveVersionExist = hlaNomenclatureVersionAccessor.DoesActiveHlaNomenclatureVersionExist();

            doesActiveVersionExist.Should().BeTrue();
        }

        [Test]
        public void GetActiveHlaNomenclatureVersion_WhenActiveVersionIsNotNull_ReturnsValue()
        {
            const string activeVersion = "version";
            dataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion().Returns(activeVersion);

            var activeVersionReturned = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();

            activeVersionReturned.Should().Be(activeVersion);
        }

        [Test]
        public void DoesActiveHlaNomenclatureVersionExist_WhenActiveVersionIsNotNull_ReturnsFalse()
        {
            const string activeVersion = "version";
            dataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion().Returns(activeVersion);

            var doesActiveVersionExist = hlaNomenclatureVersionAccessor.DoesActiveHlaNomenclatureVersionExist();

            doesActiveVersionExist.Should().BeFalse();
        }
    }
}