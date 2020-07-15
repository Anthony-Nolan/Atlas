using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using CsvHelper;
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

        [Test, Repeat(100)]
        //Before ErrorHandling Perf: 50 Reps = 4.138s, 100 Reps = 5.465s
        //After ErrorHandling Perf: 50 Reps = 3.35s, 100 Reps = 4.002s
        public async Task ImportingSingleDonorWith_Invalid_HlasPerformanceTest()
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

        [Test, Repeat(100)]
        //Before ErrorHandling Perf: 50 Reps = 4.907s, 100 Reps = 7.423s (+2.516)
        //After ErrorHandling Perf: 50 Reps = 5.333s, 100 Reps = 7.769s (+2.436)
        //Before, 100 Rep runs: 7.426, 6.886, 8.026, 7.403, 7.149
        //Before, 100 Rep runs: 7.769, 7.428, 7.836, 7.870, 7.735
        public async Task ImportingSingleDonorWith_Valid_HlasPerformanceTest()
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
         TestCase(1),
         TestCase(10),    // 0.0250 s/hla    =>    0.0264 s/hla
         TestCase(50),    // 0.0049 s/hla    =>    0.0056 s/hla
         TestCase(100),   // 0.0043 s/hla    =>    0.0048 s/hla
         TestCase(500),   // 0.0021 s/hla    =>    0.0022 s/hla
         TestCase(1_000), // 0.0022 s/hla    =>    0.0026 s/hla
         TestCase(5_000), // 0.0030 s/hla    =>    0.0027 s/hla
         TestCase(20_000)]// 0.0031 s/hla    =>    0.0028 s/hla
        public async Task MassHlaExpand_N(int n)
        {
            var newDonors =
                (await ParseTestDonorFile())
                .OrderBy(d => d.DonorId)
                .Take(n)
                .Select(d => d.ToDonorInfo())
                .ToList();

            var oldHlaVersion = FileBackedHlaMetadataRepositoryBaseReader.NewDonorUpdateTestsVersion;
            var expanderService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>().BuildForSpecifiedHlaNomenclatureVersion(oldHlaVersion);

            await expanderService.ExpandDonorHlaBatchAsync(newDonors.ToList(), "Testing");
        }

        [Test]
        public async Task MassImport()
        {
            var newDonors = new Queue<Donor>((await ParseTestDonorFile()).OrderBy(d => d.DonorId));// 20k. Used 8+4+2+3. 2 remain entirely unused

            var (existingAvailableDonors, existingUnavailableDonors) = await SetupInitialDonorsInDb(newDonors);

            var moreCreations = PrepareSomeCreations(newDonors);
            var drasticUpdates = PrepareSomeDrasticUpdates(existingAvailableDonors, newDonors);
            var gentleUpdates = PrepareSomeGentleUpdates(existingAvailableDonors, newDonors);
            var availabilityUpdated = ModifySomeAvailabilities(existingAvailableDonors, existingUnavailableDonors);

            var allUpdates = moreCreations.Concat(drasticUpdates).Concat(gentleUpdates).Concat(availabilityUpdated).ToList();

            var rand = new Random(123);// Need a fixed seed to ensure that this is determinstic, so that the tests are reproducible.
            var deterministicallyRandomizedUpdates = allUpdates.OrderBy(upd => rand.NextDouble()).ToList();
            var updateBatches = deterministicallyRandomizedUpdates.Batch(1000).ToList();

            foreach (var updateBatch in updateBatches)
            {
                await Import(updateBatch);
            }
        }

        public async Task<(Queue<Donor>, Queue<Donor>)> SetupInitialDonorsInDb(Queue<Donor> newDonors)
        {
            var initialCreations = newDonors.Dequeue(8000).ToList();
            await Import(initialCreations.Select(d => d.ToDonorInfo()));
            var newlyCreatedAvailableDonors = new Queue<Donor>(initialCreations); //8k. Used 3+2+2. 1k remain as they were originally inserted.

            var initialUnavailableCreations = newDonors.Dequeue(4000).ToList();
            await ImportAsUnavailable(initialUnavailableCreations.Select(d => d.ToDonorInfo()));
            var newlyCreatedUnavailableDonors = new Queue<Donor>(initialUnavailableCreations); //4k Used 2. 2k remain as they were originally inserted.

            return (newlyCreatedAvailableDonors, newlyCreatedUnavailableDonors);
        }

        private static List<DonorAvailabilityUpdate> PrepareSomeCreations(Queue<Donor> newDonors)
        {
            var moreCreations = newDonors.Dequeue(2000).Select(d => d.ToDonorInfo().ToUpdate()).ToList();
            return moreCreations;
        }

        private static List<DonorAvailabilityUpdate> PrepareSomeDrasticUpdates(Queue<Donor> existingAvailableDonors, Queue<Donor> newDonors)
        {
            var existingDonorsToDrasticallyEdit = existingAvailableDonors.Dequeue(3000).ToList();
            var newDonorValuesToUse = newDonors.Dequeue(3000).ToList();
            var drasticUpdates = existingDonorsToDrasticallyEdit
                .Zip(newDonorValuesToUse, ((existingDonor, newDonor) =>
                {
                    newDonor.DonorId = existingDonor.DonorId;
                    return newDonor;
                }))
                .Select(d => d.ToDonorInfo().ToUpdate())
                .ToList();
            return drasticUpdates;
        }

        private static IEnumerable<DonorAvailabilityUpdate> PrepareSomeGentleUpdates(Queue<Donor> existingAvailableDonors, Queue<Donor> newDonors)
        {
            var existingDonorsToGentlyEdit = existingAvailableDonors.Dequeue(2000).ToList();
            var newDonorsToGetValidAHlas = newDonors.Dequeue(2000).ToList();
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

        private static List<DonorAvailabilityUpdate> ModifySomeAvailabilities(Queue<Donor> existingAvailableDonors, Queue<Donor> existingUnavailableDonors)
        {
            var markAsUnavailable = existingAvailableDonors.Dequeue(2000).Select(d => d.ToDonorInfo().ToUnavailableUpdate()).ToList();
            var markAsAvailable = existingUnavailableDonors.Dequeue(2000).Select(d => d.ToDonorInfo().ToUpdate()).ToList();
            return markAsUnavailable.Union(markAsAvailable).ToList();
        }
    }
}
