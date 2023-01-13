using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq.Extensions;
using NUnit.Framework;
using static Atlas.Common.Public.Models.GeneticData.Locus;
using static Atlas.Common.Public.Models.GeneticData.LocusPosition;
using static Atlas.MatchingAlgorithm.Client.Models.Donors.DonorType;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DonorUpdates
{
    internal static class TestExtensions
    {
        public static DonorAvailabilityUpdate ToUpdate(this DonorInfo donorInfo) => donorInfo.ToUpdateWithAvailability(true);
        public static DonorAvailabilityUpdate ToUnavailableUpdate(this DonorInfo donorInfo) => donorInfo.ToUpdateWithAvailability(false);

        private static int updateCounter = 0;

        private static DonorAvailabilityUpdate ToUpdateWithAvailability(this DonorInfo donorInfo, bool isAvailable)
        {
            donorInfo.IsAvailableForSearch = isAvailable;

            // The import process uses UpdateDateTime as a sequencing tool, which means the tests rely on 
            // order of update creation to determine the ordering of the UpdateDateTime.
            // Very occasionally, this code manages to execute fast enough that the timestamps are the same
            // for 2 records that need to be in a certain order.
            // That causes errors with the import.
            // So introduce a guaranteed separation between every update, to prevent that.
            // Note that adding a Thread.Sleep() [even Thread.Sleep(1)] causes meaningful slowdown of the tests.
            updateCounter++;
            var utcNowPlusOffset = DateTimeOffset.UtcNow.AddTicks(updateCounter);

            return new DonorAvailabilityUpdate
            {
                DonorInfo = donorInfo,
                DonorId = donorInfo.DonorId,
                IsAvailableForSearch = isAvailable,
                UpdateDateTime = utcNowPlusOffset,
                // UpdateSequenceNumber is never used.
            };
        }

        public static T UnorderedDequeueWhere<T>(this Queue<T> queue, Func<T, bool> predicate)
        {
            var toRequeue = new List<T>();
            while (true) //If the item is missing we want the empty Queue exception.
            {
                var value = queue.Dequeue();
                if (predicate(value))
                {
                    foreach (var removedValue in toRequeue)
                    {
                        queue.Enqueue(removedValue); //Yes, this changes the order. But we don't actually care about the order.
                    }

                    return value;
                }
                else
                {
                    toRequeue.Add(value);
                }
            }
        }

        public static IEnumerable<T> Dequeue<T>(this Queue<T> queue, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return queue.Dequeue();
            }
        }
    }

    [TestFixture]
    public partial class DonorUpdateImportSmokeTestsAndPerformanceBaselines
    {
        private IDonorManagementService managementService;
        private TestDonorInspectionRepository donorInspectionRepository;
        private string fileBackedHmdHlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion;
        private TransientDatabase activeDb;

        [OneTimeSetUp]
        public void PopulateHMDCacheOutsideOfTestTiming()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                // This object resolution triggers the reading of the HMD files.
                // We don't want that time included in the tests as it's not relevant to the perf figures
                // and would skew the results depending on which test ran first.
                // So run it once here to get it done before timing starts.
                // Don't need to worry about scopes as the FileBacked repos are all singletons.
                var factory = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMetadataDictionaryFactory>();

                // This isn't going to change between tests, and since it uses a Transient cache, it's a tiny bit slow.
                var dbProvider = DependencyInjection.DependencyInjection.Provider.GetService<IActiveDatabaseProvider>();
                activeDb = dbProvider.GetActiveDatabase();
            });
        }

        [SetUp]
        public void SetUp()
        {
            DependencyInjection.DependencyInjection.NewScope();

            var activeDbConnectionStringProvider = DependencyInjection.DependencyInjection.Provider.GetService<ActiveTransientSqlConnectionStringProvider>();
            donorInspectionRepository = new TestDonorInspectionRepository(activeDbConnectionStringProvider);

            managementService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorManagementService>();

            DatabaseManager.ClearTransientDatabases();
        }

        private async Task Import(params DonorAvailabilityUpdate[] updates)
        {
            var batchSize = 1000; // The prod code takes this from the DonorManagementSettings. 1000 is the default value.
            foreach (var updateBatch in updates.Batch(batchSize))
            {
                await managementService.ApplyDonorUpdatesToDatabase(
                    updateBatch.ToList().AsReadOnly(),
                    activeDb,
                    fileBackedHmdHlaNomenclatureVersion,
                    false //This makes a substantial difference to the runtime: 25-35% atm.
                );
            }
        }

        private async Task Import(IEnumerable<DonorAvailabilityUpdate> updates) => await Import(updates.ToArray());
        private async Task Import(params DonorInfo[] infos) => await Import(infos.Select(TestExtensions.ToUpdate).ToArray());
        private async Task Import(IEnumerable<DonorInfo> infos) => await Import(infos.Select(TestExtensions.ToUpdate).ToArray());

        private async Task ImportAsUnavailable(IEnumerable<DonorInfo> infos)
        {
            // Importing directly as unavailable update will lead to the donors being ignored.
            var infosList = infos.ToList();
            await Import(infosList.Select(TestExtensions.ToUpdate));
            await Import(infosList.Select(TestExtensions.ToUnavailableUpdate));
        }

        private void ExpectDonorsToBe(List<int> expectedDonorIds)
        {
            donorInspectionRepository.GetAllDonorIds().Should().BeEquivalentTo(expectedDonorIds);
            donorInspectionRepository.GetDonorCount().Should().Be(expectedDonorIds.Count); //Just in case, as a backup.
        }

        // When running this as a perf test, we don't want to spend time doing this check.
        // But when a developer is trying to figure out what broke, it'll be really useful!
        // Delete the " { } //" to "turn it on".
        private void Debug_ExpectDonorsToBe(List<int> expectedDonorIds)
        {
        } // => ExpectDonorsToBe(expectedDonorIds);

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_ImportingAllDonorsFromExistingTestsAsSeparateBatches_ResultsInCorrectNumberOfDonorsAtEnd()
        {
            // All of these "test cases" have been lifted directly from the DonorServiceTests class, with little further thought.
            // Each Debug_ExpectDonorsToBe() represents the end of a test (as of writing of this class!)
            // Look there to establish what these blocks were intended to test in their original state.
            // Note that there's no desperate need to keep the 2 sets of code in sync.

            var expectedDonorIds = new List<int>();
            ExpectDonorsToBe(expectedDonorIds);

            //New Donor.
            var donorInfo0 = new DonorInfoBuilder().Build();
            await Import(donorInfo0);
            expectedDonorIds.Add(donorInfo0.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //New Invalid Donor.
            var donorInfo0B = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*9999:9999").Build();
            await Import(donorInfo0B);
            //expectedDonorIds unchanged
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //New Donor with the same message delivered 3 times, separately.
            var donorInfo1 = new DonorInfoBuilder().Build();
            await Import(donorInfo1);
            await Import(donorInfo1);
            await Import(donorInfo1);
            expectedDonorIds.Add(donorInfo1.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //New Donor with the same message delivered 3 times, in a single batch.
            var donorInfo1B = new DonorInfoBuilder().Build();
            await Import(donorInfo1B, donorInfo1B, donorInfo1B);
            expectedDonorIds.Add(donorInfo1B.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //New Donor, later updated with different type.
            var donorInfo2 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor2 = new DonorInfoBuilder(donorInfo2.DonorId).WithDonorType(Cord).Build();
            await Import(donorInfo2);
            await Import(updatedDonor2);
            expectedDonorIds.Add(donorInfo2.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //New Donor, later updated with different hla.
            var donorInfo3 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor3 = new DonorInfoBuilder(donorInfo3.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            await Import(donorInfo3);
            await Import(updatedDonor3);
            expectedDonorIds.Add(donorInfo3.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //New Donor, later updated with different type but with an Invalid hla
            var donorInfo4 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor4 = new DonorInfoBuilder(donorInfo4.DonorId).WithDonorType(Cord).WithHlaAtLocus(A, One, "*9999:9999").Build();
            await Import(donorInfo4);
            await Import(updatedDonor4);
            expectedDonorIds.Add(donorInfo4.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //New Donor, later updated with invalid HLA.
            var donorInfo5 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor5 = new DonorInfoBuilder(donorInfo5.DonorId).WithHlaAtLocus(A, One, "*9999:9999").Build();
            await Import(donorInfo5);
            await Import(updatedDonor5);
            expectedDonorIds.Add(donorInfo5.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //Multiple new donors in a single batch
            var donorInfo6 = new DonorInfoBuilder().Build();
            var donorInfo7 = new DonorInfoBuilder().Build();
            await Import(donorInfo6, donorInfo7);
            expectedDonorIds.Add(donorInfo6.DonorId);
            expectedDonorIds.Add(donorInfo7.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //Multiple updated (type) donors in a single batch
            var donorInfo8 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo9 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor8 = new DonorInfoBuilder(donorInfo8.DonorId).WithDonorType(Cord).Build();
            var updatedDonor9 = new DonorInfoBuilder(donorInfo9.DonorId).WithDonorType(Cord).Build();
            await Import(donorInfo8, donorInfo9);
            await Import(updatedDonor8, updatedDonor9);
            expectedDonorIds.Add(donorInfo8.DonorId);
            expectedDonorIds.Add(donorInfo9.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //Multiple updated (hla) donors in a single batch
            var donorInfo10 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var donorInfo11 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02:01").Build();
            var updatedDonor10 = new DonorInfoBuilder(donorInfo10.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            var updatedDonor11 = new DonorInfoBuilder(donorInfo11.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            await Import(donorInfo10, donorInfo11);
            await Import(updatedDonor10, updatedDonor11);
            expectedDonorIds.Add(donorInfo10.DonorId);
            expectedDonorIds.Add(donorInfo11.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //Single Batch containing both updates and also new donors
            var donorInfo12 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo13 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo14 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo15 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor12 = new DonorInfoBuilder(donorInfo12.DonorId).WithDonorType(Cord).Build();
            var updatedDonor13 = new DonorInfoBuilder(donorInfo13.DonorId).WithDonorType(Cord).Build();
            await Import(donorInfo12, donorInfo13);
            await Import(donorInfo14, donorInfo15, updatedDonor12, updatedDonor13);
            expectedDonorIds.Add(donorInfo12.DonorId);
            expectedDonorIds.Add(donorInfo13.DonorId);
            expectedDonorIds.Add(donorInfo14.DonorId);
            expectedDonorIds.Add(donorInfo15.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //Newly created Donor is unavailable at point of creation.
            var donorInfo16 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            await Import(donorInfo16);
            //expectedDonorIds unchanged. Non-Available Creations are just ignored.
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //Single batch has a mix of available and unavailable donors.
            var donorInfo17 = new DonorInfoBuilder().Build().ToUpdate();
            var donorInfo18 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            await Import(donorInfo17, donorInfo18);
            expectedDonorIds.Add(donorInfo17.DonorId);
            //Non-Available Creations are just ignored.
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //Single batch has updates that mix changing from available to unavailable, and vice versa.
            var donorInfo19 = new DonorInfoBuilder().Build().ToUpdate();
            var donorInfo20 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            var updatedDonor19 = new DonorInfoBuilder(donorInfo19.DonorId).Build().ToUnavailableUpdate();
            var updatedDonor20 = new DonorInfoBuilder(donorInfo20.DonorId).Build().ToUpdate();
            await Import(donorInfo19, donorInfo20);
            await Import(updatedDonor19, updatedDonor20);
            expectedDonorIds.Add(donorInfo19.DonorId);
            expectedDonorIds.Add(donorInfo20.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //Donor is created, marked as unavailable, then marked as available. As 3 separate batches.
            var donorInfo21 = new DonorInfoBuilder().Build().ToUpdate();
            var updatedDonor21 = new DonorInfoBuilder(donorInfo21.DonorId).Build().ToUnavailableUpdate();
            var reUpdatedDonor21 = new DonorInfoBuilder(donorInfo21.DonorId).Build().ToUpdate();
            await Import(donorInfo21);
            await Import(updatedDonor21);
            await Import(reUpdatedDonor21);
            expectedDonorIds.Add(donorInfo21.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //Donor is created as initially unavailable, marked as available, then marked as unavailable again. As 3 separate batches.
            var donorInfo21B = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            var updatedDonor21B = new DonorInfoBuilder(donorInfo21B.DonorId).Build().ToUpdate();
            var reUpdatedDonor21B = new DonorInfoBuilder(donorInfo21B.DonorId).Build().ToUnavailableUpdate();
            await Import(donorInfo21B);
            await Import(updatedDonor21B);
            await Import(reUpdatedDonor21B);
            expectedDonorIds.Add(donorInfo21B.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            //New Donor, later updated with different type and hla
            var donorInfo22 = new DonorInfoBuilder().WithDonorType(Adult).WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor22 = new DonorInfoBuilder(donorInfo22.DonorId).WithDonorType(Cord).WithHlaAtLocus(A, One, "*01:XX").Build();
            await Import(donorInfo22);
            await Import(updatedDonor22);
            expectedDonorIds.Add(donorInfo22.DonorId);
            Debug_ExpectDonorsToBe(expectedDonorIds);

            ExpectDonorsToBe(expectedDonorIds);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_ImportingAllDonorsFromExistingTestsAsSingleBatch_ResultsInCorrectNumberOfDonorsAtEnd()
        {
            var expectedDonorIds = new List<int>();
            ExpectDonorsToBe(expectedDonorIds);

            //New Donor.
            var donorInfo0 = new DonorInfoBuilder().Build();
            expectedDonorIds.Add(donorInfo0.DonorId);

            //New Invalid Donor.
            var donorInfo0B = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*9999:9999").Build();
            //expectedDonorIds unchanged

            //New Donor with the same message delivered 3 times, in a single batch. (will be added to batch repeatedly, below)
            var donorInfo1 = new DonorInfoBuilder().Build();
            expectedDonorIds.Add(donorInfo1.DonorId);

            //New Donor, then updated with different type.
            var donorInfo2 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor2 = new DonorInfoBuilder(donorInfo2.DonorId).WithDonorType(Cord).Build();
            expectedDonorIds.Add(donorInfo2.DonorId);

            //New Donor, then updated with different hla.
            var donorInfo3 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor3 = new DonorInfoBuilder(donorInfo3.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            expectedDonorIds.Add(donorInfo3.DonorId);

            //New Donor, then updated with different type but with an Invalid hla
            var donorInfo4 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor4 = new DonorInfoBuilder(donorInfo4.DonorId).WithDonorType(Cord).WithHlaAtLocus(A, One, "*9999:9999").Build();
            //expectedDonorIds unchanged. The 2nd record supercedes the first, but isn't valid.

            //New Donor, then updated with invalid HLA.
            var donorInfo5 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor5 = new DonorInfoBuilder(donorInfo5.DonorId).WithHlaAtLocus(A, One, "*9999:9999").Build();
            //expectedDonorIds unchanged. The 2nd record supercedes the first, but isn't valid.

            //Multiple new donors in a single batch
            var donorInfo6 = new DonorInfoBuilder().Build();
            var donorInfo7 = new DonorInfoBuilder().Build();
            expectedDonorIds.Add(donorInfo6.DonorId);
            expectedDonorIds.Add(donorInfo7.DonorId);

            //Multiple updated (type) donors in a single batch
            var donorInfo8 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo9 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor8 = new DonorInfoBuilder(donorInfo8.DonorId).WithDonorType(Cord).Build();
            var updatedDonor9 = new DonorInfoBuilder(donorInfo9.DonorId).WithDonorType(Cord).Build();
            expectedDonorIds.Add(donorInfo8.DonorId);
            expectedDonorIds.Add(donorInfo9.DonorId);

            //Multiple updated (hla) donors in a single batch
            var donorInfo10 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var donorInfo11 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02:01").Build();
            var updatedDonor10 = new DonorInfoBuilder(donorInfo10.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            var updatedDonor11 = new DonorInfoBuilder(donorInfo11.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            expectedDonorIds.Add(donorInfo10.DonorId);
            expectedDonorIds.Add(donorInfo11.DonorId);

            //Single Batch containing both updates and also new donors
            var donorInfo12 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo13 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo14 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo15 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor12 = new DonorInfoBuilder(donorInfo12.DonorId).WithDonorType(Cord).Build();
            var updatedDonor13 = new DonorInfoBuilder(donorInfo13.DonorId).WithDonorType(Cord).Build();
            expectedDonorIds.Add(donorInfo12.DonorId);
            expectedDonorIds.Add(donorInfo13.DonorId);
            expectedDonorIds.Add(donorInfo14.DonorId);
            expectedDonorIds.Add(donorInfo15.DonorId);

            //Newly created Donor is unavailable at point of creation.
            var donorInfo16 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            //expectedDonorIds unchanged. Non-Available Creations are just ignored.

            //Single batch has a mix of available and unavailable donors.
            var donorInfo17 = new DonorInfoBuilder().Build().ToUpdate();
            var donorInfo18 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            expectedDonorIds.Add(donorInfo17.DonorId);
            //Non-Available Creations are just ignored.

            //Single batch has updates that mix changing from available to unavailable, and vice versa.
            var donorInfo19 = new DonorInfoBuilder().Build().ToUpdate();
            var donorInfo20 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            var updatedDonor19 = new DonorInfoBuilder(donorInfo19.DonorId).Build().ToUnavailableUpdate();
            var updatedDonor20 = new DonorInfoBuilder(donorInfo20.DonorId).Build().ToUpdate();
            //The updatedDonor19 supercedes donorInfo19, and thus isn't imported. (Which is good, because we don't really want the unavailable donors anyway.)
            expectedDonorIds.Add(donorInfo20.DonorId);

            //Donor is created, marked as unavailable, then marked as available, within the same batch.
            var donorInfo21 = new DonorInfoBuilder().Build().ToUpdate();
            var updatedDonor21 = new DonorInfoBuilder(donorInfo21.DonorId).Build().ToUnavailableUpdate();
            var reUpdatedDonor21 = new DonorInfoBuilder(donorInfo21.DonorId).Build().ToUpdate();
            expectedDonorIds.Add(donorInfo21.DonorId);

            //Donor is created as initially unavailable, marked as available, then marked as unavailable again. As 3 separate batches.
            var donorInfo21B = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            var updatedDonor21B = new DonorInfoBuilder(donorInfo21B.DonorId).Build().ToUpdate();
            var reUpdatedDonor21B = new DonorInfoBuilder(donorInfo21B.DonorId).Build().ToUnavailableUpdate();
            //expectedDonorIds unchanged. The 3rd record supercedes the 2nd, and thus isn't imported. (which is good, as above)

            //New Donor, then updated with different type and hla
            var donorInfo22 = new DonorInfoBuilder().WithDonorType(Adult).WithHlaAtLocus(A, One, "*01:01").Build();
            var updatedDonor22 = new DonorInfoBuilder(donorInfo22.DonorId).WithDonorType(Cord).WithHlaAtLocus(A, One, "*01:XX").Build();
            expectedDonorIds.Add(donorInfo22.DonorId);

            await Import(
                donorInfo0.ToUpdate(),
                donorInfo0B.ToUpdate(),
                donorInfo1.ToUpdate(), donorInfo1.ToUpdate(), donorInfo1.ToUpdate(),
                donorInfo2.ToUpdate(),
                updatedDonor2.ToUpdate(),
                donorInfo3.ToUpdate(),
                updatedDonor3.ToUpdate(),
                donorInfo4.ToUpdate(),
                updatedDonor4.ToUpdate(),
                donorInfo5.ToUpdate(),
                updatedDonor5.ToUpdate(),
                donorInfo6.ToUpdate(), donorInfo7.ToUpdate(),
                donorInfo8.ToUpdate(), donorInfo9.ToUpdate(),
                updatedDonor8.ToUpdate(), updatedDonor9.ToUpdate(),
                donorInfo10.ToUpdate(), donorInfo11.ToUpdate(),
                updatedDonor10.ToUpdate(), updatedDonor11.ToUpdate(),
                donorInfo12.ToUpdate(), donorInfo13.ToUpdate(),
                donorInfo14.ToUpdate(), donorInfo15.ToUpdate(), updatedDonor12.ToUpdate(), updatedDonor13.ToUpdate(),
                donorInfo16,
                donorInfo17, donorInfo18,
                donorInfo19, donorInfo20,
                updatedDonor19, updatedDonor20,
                donorInfo21,
                updatedDonor21,
                reUpdatedDonor21,
                donorInfo21B,
                updatedDonor21B,
                reUpdatedDonor21B,
                donorInfo22.ToUpdate(),
                updatedDonor22.ToUpdate()
            );

            ExpectDonorsToBe(expectedDonorIds);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_ImportingAFurtherSingleBatch_WhichExplicitlyExercisesAllPaths_CompletesWithoutErrors()
        {
            //This is a useful way to populate varied baseline data :)
            await ApplyDonorUpdatesToDatabase_ImportingAllDonorsFromExistingTestsAsSingleBatch_ResultsInCorrectNumberOfDonorsAtEnd();
            await PopulateSomeUnavailableDonors();

            var existingDonors = new Queue<DonorWithLog>(donorInspectionRepository.GetAllDonorsWithLogs().Values.OrderBy(d => d.Donor.DonorId));

            var updatesToApply = new List<DonorAvailabilityUpdate>();
            updatesToApply.AddRange(Ensure_FilterUpdates_IsExercised(existingDonors));
            updatesToApply.AddRange(Ensure_ApplyDonorUpdates_IsExercised(existingDonors));

            await Import(updatesToApply);
        }

        private async Task PopulateSomeUnavailableDonors()
        {
            var tenNewDonorIds = Enumerable.Repeat("", 10).Select(_ => DonorIdGenerator.NextId()).ToList();

            var newDonors = tenNewDonorIds.Select(id => new DonorInfoBuilder(id).Build().ToUpdate());
            await Import(newDonors);

            var updateDonorsAsUnavailable = tenNewDonorIds.Select(id => new DonorInfoBuilder(id).Build().ToUnavailableUpdate());
            await Import(updateDonorsAsUnavailable);
        }

        #region FurtherSingleOperationBatchToEnsureAllPathsAreExercised substeps

        // Yes. This does match the structure of the code it's calling. But that's really only a convenience for creation.
        // There's no particular need to maintain that matching, if the called code mutates.
        private IEnumerable<DonorAvailabilityUpdate> Ensure_FilterUpdates_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            foreach (var update in Ensure_RetainLatestUpdateInBatchPerDonorId_IsExercised())
            {
                yield return update;
            }

            foreach (var update in Ensure_RetainUpdatesThatAreNewerThanAnyPreviouslyAppliedUpdate_IsExercised(existingDonors))
            {
                yield return update;
            }
        }

        #region Ensure_FilterUpdates_IsExercised substeps

        private IEnumerable<DonorAvailabilityUpdate> Ensure_RetainLatestUpdateInBatchPerDonorId_IsExercised()
        {
            var superseded = new DonorInfoBuilder().Build().ToUpdate();
            var used = new DonorInfoBuilder(superseded.DonorId).Build().ToUpdate();
            superseded.UpdateDateTime = used.UpdateDateTime.AddMinutes(-1);
            yield return superseded;
            yield return used;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_RetainUpdatesThatAreNewerThanAnyPreviouslyAppliedUpdate_IsExercised(
            Queue<DonorWithLog> existingDonors)
        {
            var existing = existingDonors.Dequeue();

            var earlier = new DonorInfoBuilder(existing.Donor.DonorId).Build().ToUpdate();
            earlier.UpdateDateTime = existing.Log.LastUpdateDateTime.AddDays(-1);
            yield return earlier;

            var replay = new DonorInfoBuilder(existing.Donor.DonorId).Build().ToUpdate();
            replay.UpdateDateTime = existing.Log.LastUpdateDateTime;
            yield return replay;
        }

        #endregion

        private IEnumerable<DonorAvailabilityUpdate> Ensure_ApplyDonorUpdates_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            //Split by IsAvailableForSearch (implicitly covered by the details.)
            foreach (var update in Ensure_AddOrUpdateDonors_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_SetDonorsAsUnavailableForSearch_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_CreateOrUpdateManagementLogBatch_IsExercised(existingDonors))
            {
                yield return update;
            }
        }

        # region Ensure_ApplyDonorUpdates_IsExercised substeps

        private IEnumerable<DonorAvailabilityUpdate> Ensure_AddOrUpdateDonors_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            //vacuous Split by hasDonorInfo (ignored)
            foreach (var update in Ensure_CreateOrUpdateDonorBatch_FindInvalidHla_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_CreateOrUpdateDonorsWithHla_IsExercised(existingDonors))
            {
                yield return update;
            }
        }

        #region Ensure_AddOrUpdateDonors_IsExercised substeps

        private IEnumerable<DonorAvailabilityUpdate> Ensure_CreateOrUpdateDonorBatch_FindInvalidHla_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var invalidNewHlaDonor = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*9999:9999").Build().ToUpdate();
            yield return invalidNewHlaDonor;

            var existing = existingDonors.First().Donor;
            var invalidHlaDonorUpdate = new DonorInfoBuilder(existing.DonorId).WithHlaAtLocus(A, One, "*9999:9998").Build().ToUpdate();
            yield return invalidHlaDonorUpdate;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_CreateOrUpdateDonorsWithHla_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            //Split by DonorExistsInDb.
            foreach (var update in Ensure_CreateDonorBatch_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_UpdateDonorBatch_IsExercised(existingDonors))
            {
                yield return update;
            }
        }

        #region Ensure_CreateOrUpdateDonorsWithHla_IsExercised substeps

        private IEnumerable<DonorAvailabilityUpdate> Ensure_CreateDonorBatch_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var newDonor = new DonorInfoBuilder().Build().ToUpdate();
            yield return newDonor;

            var newDonorWithFullHla = new DonorInfoBuilder()
                .WithHlaAtLocus(A, One, "*24:02:01G")
                .WithHlaAtLocus(A, Two, "*26:01:01G")
                .WithHlaAtLocus(B, One, "*35:01:01G")
                .WithHlaAtLocus(B, Two, "*52:01:01G")
                .WithHlaAtLocus(C, One, "*04:01:01G")
                .WithHlaAtLocus(C, Two, "*12:02:01G")
                .WithHlaAtLocus(Dpb1, One, "*05:02:01G")
                .WithHlaAtLocus(Dpb1, Two, "*06:01:01G")
                .WithHlaAtLocus(Dqb1, One, "*15:02:01")
                .WithHlaAtLocus(Dqb1, Two, "*11:01:01G")
                .WithHlaAtLocus(Drb1, One, "*02:01:02G")
                .WithHlaAtLocus(Drb1, Two, "*04:02:01G")
                .Build().ToUpdate();
            yield return newDonorWithFullHla;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            //vacuous filter on donor still exists in DB. Ignored.
            foreach (var update in Ensure_UpdateDonorBatch_ChangeJustAvailability_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_UpdateDonorBatch_ChangeJustType_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_UpdateDonorBatch_ChangeJustHla_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_UpdateDonorBatch_ChangeAvailabilityAndType_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_UpdateDonorBatch_ChangeAvailabilityAndHla_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_UpdateDonorBatch_ChangeTypeAndHla_IsExercised(existingDonors))
            {
                yield return update;
            }

            foreach (var update in Ensure_UpdateDonorBatch_ChangeAll_IsExercised(existingDonors))
            {
                yield return update;
            }
        }

        #region Ensure_UpdateDonorBatch_IsExercised substeps

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeJustAvailability_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var existingDisabled = existingDonors.UnorderedDequeueWhere(d => !d.Donor.IsAvailableForSearch).Donor;

            var updateToDisabled = new DonorInfoBuilder(existingEnabled).Build().ToUnavailableUpdate();
            var updateToEnabled = new DonorInfoBuilder(existingDisabled).Build().ToUpdate();
            yield return updateToDisabled;
            yield return updateToEnabled;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeJustType_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var newType = existingEnabled.DonorType.Other();
            var updateToType = new DonorInfoBuilder(existingEnabled).WithDonorType(newType).Build().ToUpdate();

            yield return updateToType;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeJustHla_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var updateToHla = new DonorInfoBuilder(existingEnabled.DonorId)
                .WithHlaAtLocus(A, One, "*03:01")
                .WithHlaAtLocus(A, Two, "*30:02")
                .WithHlaAtLocus(B, One, "*18:01")
                .WithHlaAtLocus(B, Two, "*53:01")
                .WithHlaAtLocus(C, One, "*04:01")
                .WithHlaAtLocus(C, Two, "*12:03")
                .WithHlaAtLocus(Dpb1, One, "*02:01")
                .WithHlaAtLocus(Dpb1, Two, "*02:01")
                .WithHlaAtLocus(Dqb1, One, "*02:01")
                .WithHlaAtLocus(Dqb1, Two, "*06:04")
                .WithHlaAtLocus(Drb1, One, "*03:01")
                .WithHlaAtLocus(Drb1, Two, "*03:01")
                .Build().ToUpdate();
            yield return updateToHla;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeAvailabilityAndType_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var existingDisabled = existingDonors.UnorderedDequeueWhere(d => !d.Donor.IsAvailableForSearch).Donor;

            var updateToDisabledAndNewType = new DonorInfoBuilder(existingEnabled).WithDonorType(existingEnabled.DonorType.Other()).Build()
                .ToUnavailableUpdate();
            var updateToEnabledAndNewType =
                new DonorInfoBuilder(existingDisabled).WithDonorType(existingDisabled.DonorType.Other()).Build().ToUpdate();

            yield return updateToDisabledAndNewType;
            yield return updateToEnabledAndNewType;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeAvailabilityAndHla_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var existingDisabled = existingDonors.UnorderedDequeueWhere(d => !d.Donor.IsAvailableForSearch).Donor;

            var updateToDisabledAndNewHla = new DonorInfoBuilder(existingEnabled)
                .WithHlaAtLocus(A, One, "*01:01:01:01")
                .WithHlaAtLocus(A, Two, "*11:01:01:01")
                .WithHlaAtLocus(B, One, "*08:01:01")
                .WithHlaAtLocus(B, Two, "*27:05:02")
                .WithHlaAtLocus(C, One, "*02:02:02")
                .WithHlaAtLocus(C, Two, "*07:01:01")
                .WithHlaAtLocus(Dpb1, One, "*04:01:01")
                .WithHlaAtLocus(Dpb1, Two, "*06:01:01")
                .WithHlaAtLocus(Dqb1, One, "*02:01:01")
                .WithHlaAtLocus(Dqb1, Two, "*03:01:01")
                .WithHlaAtLocus(Drb1, One, "*03:01:01")
                .WithHlaAtLocus(Drb1, Two, "*03:01:01")
                .Build().ToUnavailableUpdate();
            var updateToEnabledAndNewHla = new DonorInfoBuilder(existingDisabled)
                .WithHlaAtLocus(A, One, "*03:01:01:01")
                .WithHlaAtLocus(A, Two, "*30:01:01")
                .WithHlaAtLocus(B, One, "*13:02:01:01")
                .WithHlaAtLocus(B, Two, "*13:02:01:01")
                .WithHlaAtLocus(C, One, "*06:02:01")
                .WithHlaAtLocus(C, Two, "*06:02:01")
                .WithHlaAtLocus(Dpb1, One, "*02:01")
                .WithHlaAtLocus(Dpb1, Two, "*04:01:01")
                .WithHlaAtLocus(Dqb1, One, "*02:02:01")
                .WithHlaAtLocus(Dqb1, Two, "*03:01:01")
                .WithHlaAtLocus(Drb1, One, "*11:01:01")
                .WithHlaAtLocus(Drb1, Two, "*11:01:01")
                .Build().ToUpdate();

            yield return updateToDisabledAndNewHla;
            yield return updateToEnabledAndNewHla;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeTypeAndHla_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var updateToNewTypeAndNewHla = new DonorInfoBuilder(existingEnabled.DonorId)
                .WithDonorType(existingEnabled.DonorType.Other())
                .WithHlaAtLocus(A, One, "*02:01")
                .WithHlaAtLocus(A, Two, "*24:02")
                .WithHlaAtLocus(B, One, "*35:02")
                .WithHlaAtLocus(B, Two, "*52:01")
                .WithHlaAtLocus(C, One, "*04:01")
                .WithHlaAtLocus(C, Two, "*12:02")
                .WithHlaAtLocus(Dpb1, One, "*04:01")
                .WithHlaAtLocus(Dpb1, Two, "*16:01")
                .WithHlaAtLocus(Dqb1, One, "*03:01")
                .WithHlaAtLocus(Dqb1, Two, "*03:01")
                .WithHlaAtLocus(Drb1, One, "*11:04")
                .WithHlaAtLocus(Drb1, Two, "*11:04")
                .Build().ToUpdate();
            yield return updateToNewTypeAndNewHla;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeAll_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var existingDisabled = existingDonors.UnorderedDequeueWhere(d => !d.Donor.IsAvailableForSearch).Donor;

            var updateToDisabledAndNewTypeAndNewHla = new DonorInfoBuilder(existingEnabled)
                .WithDonorType(existingEnabled.DonorType.Other())
                .WithHlaAtLocus(A, One, "*02:01:01G")
                .WithHlaAtLocus(A, Two, "*68:01:02G")
                .WithHlaAtLocus(B, One, "*35:05:01")
                .WithHlaAtLocus(B, Two, "*40:01:01G")
                .WithHlaAtLocus(C, One, "*03:04:01G")
                .WithHlaAtLocus(C, Two, "*04:01:01G")
                .WithHlaAtLocus(Dpb1, One, "*02:01:02G")
                .WithHlaAtLocus(Dpb1, Two, "*04:02:01G")
                .WithHlaAtLocus(Dqb1, One, "*03:01:01G")
                .WithHlaAtLocus(Dqb1, Two, "*03:02:01G")
                .WithHlaAtLocus(Drb1, One, "*04:07:01G")
                .WithHlaAtLocus(Drb1, Two, "*04:07:01G")
                .Build().ToUnavailableUpdate();
            var updateToEnabledAndNewTypeAndNewHla = new DonorInfoBuilder(existingDisabled)
                .WithDonorType(existingDisabled.DonorType.Other())
                .WithHlaAtLocus(A, One, "*11:XX")
                .WithHlaAtLocus(A, Two, "*24:XX")
                .WithHlaAtLocus(B, One, "*07:XX")
                .WithHlaAtLocus(B, Two, "*51:XX")
                .WithHlaAtLocus(C, One, "*02:02:02")
                .WithHlaAtLocus(C, Two, "*07:02:01")
                .WithHlaAtLocus(Dpb1, One, "*04:01:01")
                .WithHlaAtLocus(Dpb1, Two, "*04:01:01")
                .WithHlaAtLocus(Dqb1, One, "*02:01:01")
                .WithHlaAtLocus(Dqb1, Two, "*06:04:01")
                .WithHlaAtLocus(Drb1, One, "*03:01:01G")
                .WithHlaAtLocus(Drb1, Two, "*03:01:01G")
                .Build().ToUpdate();

            yield return updateToDisabledAndNewTypeAndNewHla;
            yield return updateToEnabledAndNewTypeAndNewHla;
        }

        #endregion

        #endregion

        #endregion

        private IEnumerable<DonorAvailabilityUpdate> Ensure_SetDonorsAsUnavailableForSearch_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var donorThatDoesNotExist = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            yield return donorThatDoesNotExist;

            var existing = existingDonors.Dequeue();
            var disableAnExistingDonor = new DonorInfoBuilder(existing.Donor.DonorId).Build().ToUnavailableUpdate();
            yield return disableAnExistingDonor;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_CreateOrUpdateManagementLogBatch_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var newDonor = new DonorInfoBuilder().Build().ToUpdate();
            yield return newDonor;

            var existing = existingDonors.Dequeue().Donor;
            var newHla = "*03:27";
            existing.A_2.Should().NotBe(newHla);
            var editToExistingDonor = new DonorInfoBuilder(existing.DonorId).WithHlaAtLocus(A, Two, newHla).Build().ToUpdate();
            yield return editToExistingDonor;

            var anotherExistingDonorWithLog = existingDonors.Dequeue();
            var nonEditToExistingDonor = new DonorAvailabilityUpdate
            {
                DonorId = anotherExistingDonorWithLog.Donor.DonorId,
                IsAvailableForSearch = anotherExistingDonorWithLog.Donor.IsAvailableForSearch,
                UpdateDateTime = anotherExistingDonorWithLog.Log.LastUpdateDateTime.AddMinutes(1),
                DonorInfo = anotherExistingDonorWithLog.Donor.ToDonorInfo()
            };
            yield return nonEditToExistingDonor;
        }

        #endregion

        #endregion
    }
}