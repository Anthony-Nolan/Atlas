using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Import
{
    [TestFixture]
    public class HaplotypeFrequenciesRepositoryTests
    {
        private IHaplotypeFrequenciesRepository repository;
        private IHaplotypeFrequencyInspectionRepository inspectionRepository;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                repository = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequenciesRepository>();
                inspectionRepository = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyInspectionRepository>();
            });
        }

        [Test]
        public async Task AddHaplotypeFrequencies_WithDuplicateFrequencies_ShouldThrowExceptionAndDoNotLeaveAnyFrequencies()
        {
            const int batchSize = 10000;
            var latestSetId = await inspectionRepository.GetLatestSetId();
            var setId = latestSetId + 1;

            var haplotypeFrequencies = GenerateHaplotypeFrequencies(setId, batchSize);
            var firstEntry = haplotypeFrequencies.First();

            haplotypeFrequencies.Add(new HaplotypeFrequency
            {
                A = firstEntry.A,
                B = firstEntry.B,
                C = firstEntry.C,
                DQB1 = firstEntry.DQB1,
                DRB1 = firstEntry.DRB1,
                SetId = firstEntry.SetId,
                TypingCategory = firstEntry.TypingCategory
            });

            repository.Invoking(r => r.AddHaplotypeFrequencies(setId, haplotypeFrequencies)).Should().Throw<Exception>();

            var latestHaplotypeFrequenciesSetId = await inspectionRepository.GetLatestSetId();
            var hasHaplotypeFrequency = await inspectionRepository.HasHaplotypeFrequencies(setId);

            hasHaplotypeFrequency.Should().BeFalse();
            latestHaplotypeFrequenciesSetId.Should().NotBe(setId);
            latestHaplotypeFrequenciesSetId.Should().Be(latestSetId);
        }

        private List<HaplotypeFrequency> GenerateHaplotypeFrequencies(int setId, int numberOfElements)
        {
            const decimal frequency = 0.00001m;
            var lociCollection = new[] { "01:01", "01:02", "01:03", "02:01", "02:02", "02:03", "03:01" };
            var uniqueHaplotypeFrequencies = new List<HaplotypeFrequency>();

            foreach (var a in lociCollection)
                foreach (var b in lociCollection)
                    foreach (var c in lociCollection)
                        foreach (var dqb1 in lociCollection)
                            foreach (var drb1 in lociCollection)
                                uniqueHaplotypeFrequencies.Add(new HaplotypeFrequency 
                                { 
                                    A = a,
                                    B = b,
                                    C = c,
                                    DQB1 = dqb1,
                                    DRB1 = drb1,
                                    SetId = setId,
                                    Frequency = frequency,
                                    TypingCategory = HaplotypeTypingCategory.PGroup
                                });

            return uniqueHaplotypeFrequencies.Take(numberOfElements).ToList();
        }
    }
}
