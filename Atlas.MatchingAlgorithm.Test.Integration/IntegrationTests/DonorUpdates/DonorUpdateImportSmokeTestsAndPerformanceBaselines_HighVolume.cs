using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using CsvHelper;
using FluentAssertions;
using MoreLinq.Extensions;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DonorUpdates
{
    [TestFixture]
    partial class DonorUpdateImportSmokeTestsAndPerformanceBaselines
    {
        private async Task<List<Donor>> ParseTestDonorFile()
        {
            const string fileName = "TestDonorsForUpdatesForSmokeTests.tsv";
            // Relies on namespace matching file nesting, but is resilient to file re-structure.
            var donorTestFilePath = $"{GetType().Namespace}.{fileName}";
            await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(donorTestFilePath))
            {
                using (var reader = new StreamReader(stream))
                using (var csv = new CsvReader(reader))
                {
                    csv.Configuration.Delimiter = "\t";
                    csv.Configuration.HeaderValidated = null;
                    csv.Configuration.MissingFieldFound = null;
                    csv.Configuration.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("NULL");
                    return csv.GetRecords<Donor>().ToList();
                }
            }
        }

        [Test, Repeat(1000)]
        [IgnoreExceptOnCiPerfTest("50 Reps = 4.138s, 100 Reps = 5.465s")]
        public async Task ApplyDonorUpdatesToDatabase_ImportingSingleDonorWith_Invalid_Hlas_CompletesWithoutErrors_WithAnExpectedPerformance()
        {
            var donor = new Donor
            {
                DonorId = 787105,
                DonorType = (DonorType)1,
                IsAvailableForSearch = true,
                A_1 = "*01:BKSXG",
                A_2 = "*29:BKSXT",
                B_1 = "*08:BJHMV",
                B_2 = "*44:03:01",
                C_1 = "*07:BKSYP",
                C_2 = "*16:YBEA",
                DPB1_1 = "*04:BJFYG",
                DPB1_2 = "*11:BJFYM",
                DQB1_1 = "*06:BJFYY",
                DQB1_2 = "*02:BJFRX",
                DRB1_1 = "*15:BGNGP",
                DRB1_2 = "*15:BGNGP",
            };
            await Import(donor.ToDonorInfo().ToUpdate());
        }

        [Test, Repeat(1000)]
        [IgnoreExceptOnCiPerfTest(@"50 Reps = 4.907s. 100 Reps = 7.426, 6.886, 8.026, 7.403, 7.149")]
        public async Task ApplyDonorUpdatesToDatabase_ImportingSingleDonorWith_Valid_Hlas_CompletesWithoutErrors_WithAnExpectedPerformance()
        {
            var donor = new Donor
            {
                DonorId = 1990644,
                DonorType = (DonorType)1,
                IsAvailableForSearch = true,
                A_1 = "*02:01:01G",
                A_2 = "*11:01:01G",
                B_1 = "*07:02:01G",
                B_2 = "*35:09:01",
                C_1 = "*04:01:01G",
                C_2 = "**07:02:01G",
                DPB1_1 = "*04:01:01G",
                DPB1_2 = "*04:02:01G",
                DQB1_1 = "*03:01:01G",
                DQB1_2 = "*06:02:01G",
                DRB1_1 = "*14:02:01",
                DRB1_2 = "*14:02:01",
            };
            await Import(donor.ToDonorInfo().ToUpdate());
        }

        [Test,
         TestCase(1_000),  // 0.0022 s/hla
         TestCase(5_000),  // 0.0030 s/hla
         TestCase(20_000)] // 0.0031 s/hla
        [IgnoreExceptOnCiPerfTest(@"See comments per scale.")]
        public async Task ExpandDonorHlaBatchAsync_OnLargeNumbersOfDonors_RunsWithExpectedPerformance(int n)
        {
            await ExpandDonorHlaBatchAsync_OnModerateNumbersOfDonors_RunsWithExpectedPerformance(n);
        }

        [Test,
         TestCase(1),
         TestCase(10),    // 0.0250 s/hla
         TestCase(50),    // 0.0049 s/hla
         TestCase(100),   // 0.0043 s/hla
         TestCase(500)]   // 0.0021 s/hla
        public async Task ExpandDonorHlaBatchAsync_OnModerateNumbersOfDonors_RunsWithExpectedPerformance(int n)
        {
            var newDonors =
                (await ParseTestDonorFile())
                .OrderBy(d => d.DonorId)
                .Take(n)
                .Select(d => d.ToDonorInfo())
                .ToList();

            var oldHlaVersion = FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion;
            var expanderService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>().BuildForSpecifiedHlaNomenclatureVersion(oldHlaVersion);

            await expanderService.ExpandDonorHlaBatchAsync(newDonors.ToList(), "Testing");
        }

        [Test,
         TestCase(8000, 4000, 2000, 3000, 2000, 2000, 2000, 1000),
         TestCase(400, 200, 100, 150, 100, 100, 100, 1000),
        ]
        [IgnoreExceptOnCiPerfTest(@"See comments per scale.")]
        public async Task ApplyDonorUpdatesToDatabase_RunningHighVolumeMassImport_CompletesWithoutErrors_AndRunsWithExpectedPerformance(
            int initialAvailableCreationsCount,
            int initialUnavailableCreationsCount,
            int secondaryCreationsCount,
            int drasticUpdatesCount,
            int gentleUpdatesCount,
            int makeAvailableCount,
            int makeUnavailableCount,
            int artificialBatchSize
        )
        {
            await ApplyDonorUpdatesToDatabase_RunningModerateVolumeMassImport_CompletesWithoutErrors_AndRunsWithExpectedPerformance(
                initialAvailableCreationsCount,
                initialUnavailableCreationsCount,
                secondaryCreationsCount,
                drasticUpdatesCount,
                gentleUpdatesCount,
                makeAvailableCount,
                makeUnavailableCount,
                artificialBatchSize
            );
        }

        [Test,
         TestCase(100, 50, 25, 35, 25, 25, 25, 100),
        ]
        public async Task ApplyDonorUpdatesToDatabase_RunningModerateVolumeMassImport_CompletesWithoutErrors_AndRunsWithExpectedPerformance(
            int initialAvailableCreationsCount,
            int initialUnavailableCreationsCount,
            int secondaryCreationsCount,
            int drasticUpdatesCount,
            int gentleUpdatesCount,
            int makeAvailableCount,
            int makeUnavailableCount,
            int artificialBatchSize
        )
        {
            ValidateInputCounts(
                initialAvailableCreationsCount,
                initialUnavailableCreationsCount,
                secondaryCreationsCount,
                drasticUpdatesCount,
                gentleUpdatesCount,
                makeAvailableCount,
                makeUnavailableCount,
                artificialBatchSize);
            var newDonors = new Queue<Donor>((await ParseTestDonorFile()).OrderBy(d => d.DonorId));

            var (existingAvailableDonors, existingUnavailableDonors) = await SetupInitialDonorsInDb(newDonors, initialAvailableCreationsCount, initialUnavailableCreationsCount);

            var moreCreations = PrepareSomeCreations(newDonors, secondaryCreationsCount);
            var drasticUpdates = PrepareSomeDrasticUpdates(existingAvailableDonors, newDonors, drasticUpdatesCount);
            var gentleUpdates = PrepareSomeGentleUpdates(existingAvailableDonors, newDonors, gentleUpdatesCount);
            var availabilityUpdated = ModifySomeAvailabilities(existingAvailableDonors, existingUnavailableDonors, makeAvailableCount, makeUnavailableCount);

            var allUpdates = moreCreations.Concat(drasticUpdates).Concat(gentleUpdates).Concat(availabilityUpdated).ToList();

            var rand = new Random(123);// Need a fixed seed to ensure that this is deterministic, so that the tests are reproducible.
            var deterministicallyRandomizedUpdates = allUpdates.OrderBy(upd => rand.NextDouble()).ToList();
            var updateBatches = deterministicallyRandomizedUpdates.Batch(artificialBatchSize).ToList();

            foreach (var updateBatch in updateBatches)
            {
                await Import(updateBatch);
            }
        }

        private void ValidateInputCounts(
            int initialAvailableCreationsCount,
            int initialUnavailableCreationsCount,
            int secondaryCreationsCount,
            int drasticUpdatesCount,
            int gentleUpdatesCount,
            int makeAvailableCount,
            int makeUnavailableCount,
            int artificialBatchSize)
        {
            var totalUsedFromFile =
                initialAvailableCreationsCount +
                initialUnavailableCreationsCount +
                secondaryCreationsCount +
                drasticUpdatesCount +
                gentleUpdatesCount;
            totalUsedFromFile.Should().BeLessOrEqualTo(20_000, "only 20k donors are available in the File.");

            var totalInitialAvailableDonorsEdited =
                drasticUpdatesCount +
                gentleUpdatesCount +
                makeUnavailableCount;
            totalInitialAvailableDonorsEdited.Should().BeLessOrEqualTo(initialAvailableCreationsCount);

            var totalInitialUnavailableDonorsEdited = makeAvailableCount;
            totalInitialUnavailableDonorsEdited.Should().BeLessOrEqualTo(initialUnavailableCreationsCount);

            artificialBatchSize.Should().BeLessOrEqualTo(1_000, "Batching is imposed elsewhere, so anything greater than that won't actually take effect.");
        }

        public async Task<(Queue<Donor>, Queue<Donor>)> SetupInitialDonorsInDb(Queue<Donor> newDonors, int availableCount, int unavailableCount)
        {
            var initialCreations = newDonors.Dequeue(availableCount).ToList();
            await Import(initialCreations.Select(d => d.ToDonorInfo()));
            var newlyCreatedAvailableDonors = new Queue<Donor>(initialCreations);

            var initialUnavailableCreations = newDonors.Dequeue(unavailableCount).ToList();
            await ImportAsUnavailable(initialUnavailableCreations.Select(d => d.ToDonorInfo()));
            var newlyCreatedUnavailableDonors = new Queue<Donor>(initialUnavailableCreations);

            return (newlyCreatedAvailableDonors, newlyCreatedUnavailableDonors);
        }

        private static List<DonorAvailabilityUpdate> PrepareSomeCreations(Queue<Donor> newDonors, int count)
        {
            var moreCreations = newDonors.Dequeue(count).Select(d => d.ToDonorInfo().ToUpdate()).ToList();
            return moreCreations;
        }

        private static List<DonorAvailabilityUpdate> PrepareSomeDrasticUpdates(Queue<Donor> existingAvailableDonors, Queue<Donor> newDonors, int count)
        {
            var existingDonorsToDrasticallyEdit = existingAvailableDonors.Dequeue(count).ToList();
            var newDonorsToGetValidFullHlas = newDonors.Dequeue(count).ToList();
            var drasticUpdates = existingDonorsToDrasticallyEdit
                .Zip(newDonorsToGetValidFullHlas, ((existingDonor, newDonor) =>
                {
                    newDonor.DonorId = existingDonor.DonorId;
                    return newDonor;
                }))
                .Select(d => d.ToDonorInfo().ToUpdate())
                .ToList();
            return drasticUpdates;
        }

        private static IEnumerable<DonorAvailabilityUpdate> PrepareSomeGentleUpdates(Queue<Donor> existingAvailableDonors, Queue<Donor> newDonors, int count)
        {
            var existingDonorsToGentlyEdit = existingAvailableDonors.Dequeue(count).ToList();
            var newDonorsToGetValidAHlas = newDonors.Dequeue(count).ToList();
            var gentleUpdates = existingDonorsToGentlyEdit
                .Zip(newDonorsToGetValidAHlas, ((existingDonor, newDonor) =>
                {
                    existingDonor.A_1 = newDonor.A_1;
                    existingDonor.A_2 = newDonor.A_2;
                    return existingDonor;
                }))
                .Select(d => d.ToDonorInfo().ToUpdate())
                .ToList();
            return gentleUpdates;
        }

        private static List<DonorAvailabilityUpdate> ModifySomeAvailabilities(Queue<Donor> existingAvailableDonors, Queue<Donor> existingUnavailableDonors, int availableCount, int unavailableCount)
        {
            var markAsAvailable = existingUnavailableDonors.Dequeue(availableCount).Select(d => d.ToDonorInfo().ToUpdate()).ToList();
            var markAsUnavailable = existingAvailableDonors.Dequeue(unavailableCount).Select(d => d.ToDonorInfo().ToUnavailableUpdate()).ToList();
            return markAsUnavailable.Union(markAsAvailable).ToList();
        }
    }
}
