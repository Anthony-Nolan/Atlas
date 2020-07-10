using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests;
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
using static Atlas.Common.GeneticData.Locus;
using static Atlas.Common.GeneticData.LocusPosition;
using static Atlas.MatchingAlgorithm.Client.Models.Donors.DonorType;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DonorUpdates
{
    internal static class TestExtensions
    {
        public static DonorAvailabilityUpdate ToUpdate(this DonorInfo donorInfo) => donorInfo.ToUpdateWithAvailability(true);
        public static DonorAvailabilityUpdate ToUnavailableUpdate(this DonorInfo donorInfo) => donorInfo.ToUpdateWithAvailability(false);

        private static DonorAvailabilityUpdate ToUpdateWithAvailability(this DonorInfo donorInfo, bool isAvailable)
        {
            donorInfo.IsAvailableForSearch = isAvailable;
            return new DonorAvailabilityUpdate
            {
                DonorInfo = donorInfo,
                DonorId = donorInfo.DonorId,
                IsAvailableForSearch = isAvailable,
                UpdateDateTime = DateTimeOffset.UtcNow,
                // UpdateSequenceNumber is never used. QQ Delete it entirely? Check the LogInfo usage.
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
        private string fileBackedHmdHlaNomenclatureVersion = Constants.SnapshotHlaNomenclatureVersion;
        private TransientDatabase activeDb;

        [SetUp]
        public void SetUp()
        {
            var dbProvider = DependencyInjection.DependencyInjection.Provider.GetService<IActiveDatabaseProvider>();
            activeDb = dbProvider.GetActiveDatabase();

            var activeDbConnectionStringProvider = DependencyInjection.DependencyInjection.Provider.GetService<ActiveTransientSqlConnectionStringProvider>();
            donorInspectionRepository = new TestDonorInspectionRepository(activeDbConnectionStringProvider);

            managementService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorManagementService>();

            DatabaseManager.ClearDatabases();
        }

        private async Task Import(params DonorAvailabilityUpdate[] updates)
        {
            var batchSize = 1000; // The prod code takes this from the DonorManagementSettings. 1000 is the default value.
            foreach (var updateBatch in updates.Batch(batchSize))
            {
                await managementService.ApplyDonorUpdatesToDatabase(
                    updateBatch.ToList().AsReadOnly(),
                    activeDb,
                    fileBackedHmdHlaNomenclatureVersion);
            }
        }

        private async Task ImportMixOfUpdatesAndInfos(params object[] inconsistentObjects) //Yeah, I know. But this is still less ugly that loads of noisy `.ToUpdate()` calls.
        {
            var naturalUpdates = inconsistentObjects.OfType<DonorAvailabilityUpdate>().ToList();
            var convertedUpdates = inconsistentObjects.OfType<DonorInfo>().Select(TestExtensions.ToUpdate).ToList();
            await Import(naturalUpdates.Union(convertedUpdates));
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

        private void ExpectDonorCountToBe(int expectedCount)
        {
            donorInspectionRepository.GetDonorCount().Should().Be(expectedCount);
        }

        // When running this as a perf test, we don't want to spend time doing this check.
        // But when a developer is trying to figure out what broke, it'll be really useful!
        // Delete the " { } //" to "turn it on".
        private void Debug_ExpectDonorCountToBe(int expectedCount) { } // => ExpectDonorCountToBe(expectedCount);

        [Test]
        public async Task RunAllDonorsFromExistingTestsAs_Relatively_IndividualInserts()
        {
            var expectedRunningTotal = 0;
            var donorInfo0 = new DonorInfoBuilder().Build();
            await Import(donorInfo0);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo0B = new DonorInfoBuilder().WithHlaAtLocus(A, One, "invalid-hla-name").Build();
            await Import(donorInfo0B);
            //expectedRunningTotal unchanged
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo1 = new DonorInfoBuilder().Build();
            await Import(donorInfo1);
            await Import(donorInfo1);
            await Import(donorInfo1);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo1B = new DonorInfoBuilder().Build();
            await Import(donorInfo1B, donorInfo1B, donorInfo1B);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo2 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor2 = new DonorInfoBuilder(donorInfo2.DonorId).WithDonorType(Cord).Build();
            await Import(donorInfo2);
            await Import(updatedDonor2);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo3 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor3 = new DonorInfoBuilder(donorInfo3.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            await Import(donorInfo3);
            await Import(updatedDonor3);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo4 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor4 = new DonorInfoBuilder(donorInfo4.DonorId).WithDonorType(Cord).WithHlaAtLocus(A, One, "invalid-hla-name").Build();
            await Import(donorInfo4);
            await Import(updatedDonor4);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo5 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor5 = new DonorInfoBuilder(donorInfo5.DonorId).WithHlaAtLocus(A, One, "invalid-hla-name").Build();
            await Import(donorInfo5);
            await Import(updatedDonor5);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo6 = new DonorInfoBuilder().Build();
            var donorInfo7 = new DonorInfoBuilder().Build();
            await Import(donorInfo6, donorInfo7);
            expectedRunningTotal += 2;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo8 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo9 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor8 = new DonorInfoBuilder(donorInfo8.DonorId).WithDonorType(Cord).Build();
            var updatedDonor9 = new DonorInfoBuilder(donorInfo9.DonorId).WithDonorType(Cord).Build();
            await Import(donorInfo8, donorInfo9);
            await Import(updatedDonor8, updatedDonor9);
            expectedRunningTotal += 2;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo10 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var donorInfo11 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02:01").Build();
            var updatedDonor10 = new DonorInfoBuilder(donorInfo10.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            var updatedDonor11 = new DonorInfoBuilder(donorInfo11.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            await Import(donorInfo10, donorInfo11);
            await Import(updatedDonor10, updatedDonor11);
            expectedRunningTotal += 2;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);
            
            var donorInfo12 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo13 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo14 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo15 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor12 = new DonorInfoBuilder(donorInfo12.DonorId).WithDonorType(Cord).Build();
            var updatedDonor13 = new DonorInfoBuilder(donorInfo13.DonorId).WithDonorType(Cord).Build();
            await Import(donorInfo12, donorInfo13);
            await Import(donorInfo14, donorInfo15, updatedDonor12, updatedDonor13);
            expectedRunningTotal += 4;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo16 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            await Import(donorInfo16);
            //expectedRunningTotal unchanged. Non-Available Creations are just ignored. QQ reviewer, Seems Fair.
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo17 = new DonorInfoBuilder().Build().ToUpdate();
            var donorInfo18 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            await Import(donorInfo17, donorInfo18);
            expectedRunningTotal++; //Non-Available Creations are just ignored.
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo19 = new DonorInfoBuilder().Build().ToUpdate();
            var donorInfo20 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            var updatedDonor19 = new DonorInfoBuilder(donorInfo19.DonorId).Build().ToUnavailableUpdate();
            var updatedDonor20 = new DonorInfoBuilder(donorInfo20.DonorId).Build().ToUpdate();
            await Import(donorInfo19, donorInfo20);
            await Import(updatedDonor19, updatedDonor20);
            expectedRunningTotal += 2;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo21 = new DonorInfoBuilder().Build().ToUpdate();
            var updatedDonor21 = new DonorInfoBuilder(donorInfo21.DonorId).Build().ToUnavailableUpdate();
            var reUpdatedDonor21 = new DonorInfoBuilder(donorInfo21.DonorId).Build().ToUpdate();
            await Import(donorInfo21);
            await Import(updatedDonor21);
            await Import(reUpdatedDonor21);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo21B = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            var updatedDonor21B = new DonorInfoBuilder(donorInfo21B.DonorId).Build().ToUpdate();
            var reUpdatedDonor21B = new DonorInfoBuilder(donorInfo21B.DonorId).Build().ToUnavailableUpdate();
            await Import(donorInfo21B);
            await Import(updatedDonor21B);
            await Import(reUpdatedDonor21B);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            var donorInfo22 = new DonorInfoBuilder().WithDonorType(Adult).WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor22 = new DonorInfoBuilder(donorInfo22.DonorId).WithDonorType(Cord).WithHlaAtLocus(A, One, "*01:XX").Build();
            await Import(donorInfo22);
            await Import(updatedDonor22);
            expectedRunningTotal++;
            Debug_ExpectDonorCountToBe(expectedRunningTotal);

            ExpectDonorCountToBe(expectedRunningTotal);
        }

        [Test]
        public async Task RunAllDonorsFromExistingTestsAsSingleLargeInsert()
        {
            var expectedRunningTotal = 0;

            var donorInfo0 = new DonorInfoBuilder().Build();
            expectedRunningTotal++;

            var donorInfo0B = new DonorInfoBuilder().WithHlaAtLocus(A, One, "invalid-hla-name").Build();
            //expectedRunningTotal unchanged

            var donorInfo1 = new DonorInfoBuilder().Build();
            expectedRunningTotal++;

            var donorInfo1B = new DonorInfoBuilder().Build();
            expectedRunningTotal++;

            var donorInfo2 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor2 = new DonorInfoBuilder(donorInfo2.DonorId).WithDonorType(Cord).Build();
            expectedRunningTotal++;

            var donorInfo3 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor3 = new DonorInfoBuilder(donorInfo3.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            expectedRunningTotal++;
            
            var donorInfo4 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor4 = new DonorInfoBuilder(donorInfo4.DonorId).WithDonorType(Cord).WithHlaAtLocus(A, One, "invalid-hla-name").Build();
            //expectedRunningTotal unchanged. The 2nd record supercedes the first, but isn't valid. QQ Reviewer!

            var donorInfo5 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var updatedDonor5 = new DonorInfoBuilder(donorInfo5.DonorId).WithHlaAtLocus(A, One, "invalid-hla-name").Build();
            //expectedRunningTotal unchanged. The 2nd record supercedes the first, but isn't valid. QQ Reviewer!

            var donorInfo6 = new DonorInfoBuilder().Build();
            var donorInfo7 = new DonorInfoBuilder().Build();
            expectedRunningTotal += 2;
            
            var donorInfo8 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo9 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor8 = new DonorInfoBuilder(donorInfo8.DonorId).WithDonorType(Cord).Build();
            var updatedDonor9 = new DonorInfoBuilder(donorInfo9.DonorId).WithDonorType(Cord).Build();
            expectedRunningTotal += 2;
            
            var donorInfo10 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02").Build();
            var donorInfo11 = new DonorInfoBuilder().WithHlaAtLocus(A, One, "*01:02:01").Build();
            var updatedDonor10 = new DonorInfoBuilder(donorInfo10.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            var updatedDonor11 = new DonorInfoBuilder(donorInfo11.DonorId).WithHlaAtLocus(A, One, "*01:XX").Build();
            expectedRunningTotal += 2;
            
            var donorInfo12 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo13 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo14 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var donorInfo15 = new DonorInfoBuilder().WithDonorType(Adult).Build();
            var updatedDonor12 = new DonorInfoBuilder(donorInfo12.DonorId).WithDonorType(Cord).Build();
            var updatedDonor13 = new DonorInfoBuilder(donorInfo13.DonorId).WithDonorType(Cord).Build();
            expectedRunningTotal += 4;
            
            var donorInfo16 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            //expectedRunningTotal unchanged. Non-Available Creations are just ignored. QQ reviewer, Seems Fair.

            var donorInfo17 = new DonorInfoBuilder().Build().ToUpdate();
            var donorInfo18 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            expectedRunningTotal++; //Non-Available Creations are just ignored.

            var donorInfo19 = new DonorInfoBuilder().Build().ToUpdate();
            var donorInfo20 = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            var updatedDonor19 = new DonorInfoBuilder(donorInfo19.DonorId).Build().ToUnavailableUpdate();
            var updatedDonor20 = new DonorInfoBuilder(donorInfo20.DonorId).Build().ToUpdate();
            expectedRunningTotal++;   //expectedRunningTotal unchanged. The updatedDonor19 supercedes donorInfo19, and thus isn't imported. QQ Reviewer!

            var donorInfo21 = new DonorInfoBuilder().Build().ToUpdate();
            var updatedDonor21 = new DonorInfoBuilder(donorInfo21.DonorId).Build().ToUnavailableUpdate();
            var reUpdatedDonor21 = new DonorInfoBuilder(donorInfo21.DonorId).Build().ToUpdate();
            expectedRunningTotal++;

            var donorInfo21B = new DonorInfoBuilder().Build().ToUnavailableUpdate();
            var updatedDonor21B = new DonorInfoBuilder(donorInfo21B.DonorId).Build().ToUpdate();
            var reUpdatedDonor21B = new DonorInfoBuilder(donorInfo21B.DonorId).Build().ToUnavailableUpdate();
            //expectedRunningTotal unchanged. The 3rd record supercedes the 2nd, and thus isn't imported. QQ Reviewer!

            var donorInfo22 = new DonorInfoBuilder().WithDonorType(Adult).WithHlaAtLocus(A, One, "*01:01").Build();
            var updatedDonor22 = new DonorInfoBuilder(donorInfo22.DonorId).WithDonorType(Cord).WithHlaAtLocus(A, One, "*01:XX").Build();
            expectedRunningTotal++;


            await ImportMixOfUpdatesAndInfos( //Yeah, I know. But this is still less ugly that loads of noisy `.ToUpdate()` calls.
                donorInfo0,
                donorInfo0B,
                donorInfo1,
                donorInfo1,
                donorInfo1,
                donorInfo1B, donorInfo1B, donorInfo1B,
                donorInfo2,
                updatedDonor2,
                donorInfo3,
                updatedDonor3,
                donorInfo4,
                updatedDonor4,
                donorInfo5,
                updatedDonor5,
                donorInfo6, donorInfo7,
                donorInfo8, donorInfo9,
                updatedDonor8, updatedDonor9,
                donorInfo10, donorInfo11,
                updatedDonor10, updatedDonor11,
                donorInfo12, donorInfo13,
                donorInfo14, donorInfo15, updatedDonor12, updatedDonor13,
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
                donorInfo22,
                updatedDonor22
            );

            ExpectDonorCountToBe(expectedRunningTotal);
        }

        [Test]
        public async Task FurtherSingleOperationBatchToEnsureAllPathsAreExercised()
        {
            //This is a useful way to populate varied baseline data :)
            await RunAllDonorsFromExistingTestsAsSingleLargeInsert();
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
            foreach (var update in Ensure_RetainLatestUpdateInBatchPerDonorId_IsExercised()) { yield return update; }
            foreach (var update in Ensure_RetainUpdatesThatAreNewerThanAnyPreviouslyAppliedUpdate_IsExercised(existingDonors)) { yield return update; }
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

        private IEnumerable<DonorAvailabilityUpdate> Ensure_RetainUpdatesThatAreNewerThanAnyPreviouslyAppliedUpdate_IsExercised(Queue<DonorWithLog> existingDonors)
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
            foreach (var update in Ensure_AddOrUpdateDonors_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_SetDonorsAsUnavailableForSearch_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_CreateOrUpdateManagementLogBatch_IsExercised(existingDonors)) { yield return update; }
        }

        # region Ensure_ApplyDonorUpdates_IsExercised substeps
        private IEnumerable<DonorAvailabilityUpdate> Ensure_AddOrUpdateDonors_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            //vacuous Split by hasDonorInfo (ignored)
            foreach (var update in Ensure_CreateOrUpdateDonorBatch_FindInvalidHla_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_CreateOrUpdateDonorsWithHla_IsExercised(existingDonors)) { yield return update; }
        }

        #region Ensure_AddOrUpdateDonors_IsExercised substeps
        private IEnumerable<DonorAvailabilityUpdate> Ensure_CreateOrUpdateDonorBatch_FindInvalidHla_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var invalidNewHlaDonor = new DonorInfoBuilder().WithHlaAtLocus(A, One, "invalid-hla-name").Build().ToUpdate();
            yield return invalidNewHlaDonor;

            var existing = existingDonors.First().Donor;
            var invalidHlaDonorUpdate = new DonorInfoBuilder(existing.DonorId).WithHlaAtLocus(A, One, "invalid-hla-name-2").Build().ToUpdate();
            yield return invalidHlaDonorUpdate;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_CreateOrUpdateDonorsWithHla_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            //Split by DonorExistsInDb.
            foreach (var update in Ensure_CreateDonorBatch_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_UpdateDonorBatch_IsExercised(existingDonors)) { yield return update; }
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
                .WithHlaAtLocus(Dqb1, Two, "**11:01:01G")
                .WithHlaAtLocus(Drb1, One, "*02:01:02G")
                .WithHlaAtLocus(Drb1, Two, "*04:02:01G")
                .Build().ToUpdate();
            yield return newDonorWithFullHla;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            //vacuous filter on donor still exists in DB. Ignored.
            foreach (var update in Ensure_UpdateDonorBatch_ChangeJustAvailability_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_UpdateDonorBatch_ChangeJustType_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_UpdateDonorBatch_ChangeJustHla_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_UpdateDonorBatch_ChangeAvailabilityAndType_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_UpdateDonorBatch_ChangeAvailabilityAndHla_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_UpdateDonorBatch_ChangeTypeAndHla_IsExercised(existingDonors)) { yield return update; }
            foreach (var update in Ensure_UpdateDonorBatch_ChangeAll_IsExercised(existingDonors)) { yield return update; }
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
            var updateToHla = new DonorInfoBuilder(existingEnabled.DonorId) //QQ
                .WithHlaAtLocus(A, One, "*24:02:01G")
                .WithHlaAtLocus(A, Two, "*26:01:01G")
                .WithHlaAtLocus(B, One, "*35:01:01G")
                .WithHlaAtLocus(B, Two, "*52:01:01G")
                .WithHlaAtLocus(C, One, "*04:01:01G")
                .WithHlaAtLocus(C, Two, "*12:02:01G")
                .WithHlaAtLocus(Dpb1, One, "*05:02:01G")
                .WithHlaAtLocus(Dpb1, Two, "*06:01:01G")
                .WithHlaAtLocus(Dqb1, One, "*15:02:01")
                .WithHlaAtLocus(Dqb1, Two, "**11:01:01G")
                .WithHlaAtLocus(Drb1, One, "*02:01:02G")
                .WithHlaAtLocus(Drb1, Two, "*04:02:01G")
                .Build().ToUpdate();
            yield return updateToHla;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeAvailabilityAndType_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var existingDisabled = existingDonors.UnorderedDequeueWhere(d => !d.Donor.IsAvailableForSearch).Donor;

            var updateToDisabledAndNewType = new DonorInfoBuilder(existingEnabled).WithDonorType(existingEnabled.DonorType.Other()).Build().ToUnavailableUpdate();
            var updateToEnabledAndNewType = new DonorInfoBuilder(existingDisabled).WithDonorType(existingDisabled.DonorType.Other()).Build().ToUpdate();

            yield return updateToDisabledAndNewType;
            yield return updateToEnabledAndNewType;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeAvailabilityAndHla_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var existingDisabled = existingDonors.UnorderedDequeueWhere(d => !d.Donor.IsAvailableForSearch).Donor;

            var updateToDisabledAndNewTypeAndNewHla = new DonorInfoBuilder(existingEnabled)
                .WithHlaAtLocus(A, One, "*24:02:01G") //QQ
                .WithHlaAtLocus(A, Two, "*26:01:01G")
                .WithHlaAtLocus(B, One, "*35:01:01G")
                .WithHlaAtLocus(B, Two, "*52:01:01G")
                .WithHlaAtLocus(C, One, "*04:01:01G")
                .WithHlaAtLocus(C, Two, "*12:02:01G")
                .WithHlaAtLocus(Dpb1, One, "*05:02:01G")
                .WithHlaAtLocus(Dpb1, Two, "*06:01:01G")
                .WithHlaAtLocus(Dqb1, One, "*15:02:01")
                .WithHlaAtLocus(Dqb1, Two, "**11:01:01G")
                .WithHlaAtLocus(Drb1, One, "*02:01:02G")
                .WithHlaAtLocus(Drb1, Two, "*04:02:01G")
                .Build().ToUnavailableUpdate();
            var updateToEnabledAndNewTypeAndNewHla = new DonorInfoBuilder(existingDisabled)
                .WithHlaAtLocus(A, One, "*24:02:01G") //QQ
                .WithHlaAtLocus(A, Two, "*26:01:01G")
                .WithHlaAtLocus(B, One, "*35:01:01G")
                .WithHlaAtLocus(B, Two, "*52:01:01G")
                .WithHlaAtLocus(C, One, "*04:01:01G")
                .WithHlaAtLocus(C, Two, "*12:02:01G")
                .WithHlaAtLocus(Dpb1, One, "*05:02:01G")
                .WithHlaAtLocus(Dpb1, Two, "*06:01:01G")
                .WithHlaAtLocus(Dqb1, One, "*15:02:01")
                .WithHlaAtLocus(Dqb1, Two, "**11:01:01G")
                .WithHlaAtLocus(Drb1, One, "*02:01:02G")
                .WithHlaAtLocus(Drb1, Two, "*04:02:01G")
                .Build().ToUpdate();

            yield return updateToDisabledAndNewTypeAndNewHla;
            yield return updateToEnabledAndNewTypeAndNewHla;
        }
        
        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeTypeAndHla_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var updateToHla = new DonorInfoBuilder(existingEnabled.DonorId) //QQ
                .WithDonorType(existingEnabled.DonorType.Other())
                .WithHlaAtLocus(A, One, "*24:02:01G")
                .WithHlaAtLocus(A, Two, "*26:01:01G")
                .WithHlaAtLocus(B, One, "*35:01:01G")
                .WithHlaAtLocus(B, Two, "*52:01:01G")
                .WithHlaAtLocus(C, One, "*04:01:01G")
                .WithHlaAtLocus(C, Two, "*12:02:01G")
                .WithHlaAtLocus(Dpb1, One, "*05:02:01G")
                .WithHlaAtLocus(Dpb1, Two, "*06:01:01G")
                .WithHlaAtLocus(Dqb1, One, "*15:02:01")
                .WithHlaAtLocus(Dqb1, Two, "**11:01:01G")
                .WithHlaAtLocus(Drb1, One, "*02:01:02G")
                .WithHlaAtLocus(Drb1, Two, "*04:02:01G")
                .Build().ToUpdate();
            yield return updateToHla;
        }

        private IEnumerable<DonorAvailabilityUpdate> Ensure_UpdateDonorBatch_ChangeAll_IsExercised(Queue<DonorWithLog> existingDonors)
        {
            var existingEnabled = existingDonors.UnorderedDequeueWhere(d => d.Donor.IsAvailableForSearch).Donor;
            var existingDisabled = existingDonors.UnorderedDequeueWhere(d => !d.Donor.IsAvailableForSearch).Donor;

            var updateToDisabledAndNewTypeAndNewHla = new DonorInfoBuilder(existingEnabled)
                .WithDonorType(existingEnabled.DonorType.Other())
                .WithHlaAtLocus(A, One, "*24:02:01G") //QQ
                .WithHlaAtLocus(A, Two, "*26:01:01G")
                .WithHlaAtLocus(B, One, "*35:01:01G")
                .WithHlaAtLocus(B, Two, "*52:01:01G")
                .WithHlaAtLocus(C, One, "*04:01:01G")
                .WithHlaAtLocus(C, Two, "*12:02:01G")
                .WithHlaAtLocus(Dpb1, One, "*05:02:01G")
                .WithHlaAtLocus(Dpb1, Two, "*06:01:01G")
                .WithHlaAtLocus(Dqb1, One, "*15:02:01")
                .WithHlaAtLocus(Dqb1, Two, "**11:01:01G")
                .WithHlaAtLocus(Drb1, One, "*02:01:02G")
                .WithHlaAtLocus(Drb1, Two, "*04:02:01G")
                .Build().ToUnavailableUpdate();
            var updateToEnabledAndNewTypeAndNewHla = new DonorInfoBuilder(existingDisabled)
                .WithDonorType(existingDisabled.DonorType.Other())
                .WithHlaAtLocus(A, One, "*24:02:01G") //QQ
                .WithHlaAtLocus(A, Two, "*26:01:01G")
                .WithHlaAtLocus(B, One, "*35:01:01G")
                .WithHlaAtLocus(B, Two, "*52:01:01G")
                .WithHlaAtLocus(C, One, "*04:01:01G")
                .WithHlaAtLocus(C, Two, "*12:02:01G")
                .WithHlaAtLocus(Dpb1, One, "*05:02:01G")
                .WithHlaAtLocus(Dpb1, Two, "*06:01:01G")
                .WithHlaAtLocus(Dqb1, One, "*15:02:01")
                .WithHlaAtLocus(Dqb1, Two, "**11:01:01G")
                .WithHlaAtLocus(Drb1, One, "*02:01:02G")
                .WithHlaAtLocus(Drb1, Two, "*04:02:01G")
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

            var anotherExisting = existingDonors.Dequeue().Donor;
            var nonEditToExistingDonor = new DonorAvailabilityUpdate
            {
                DonorId = anotherExisting.DonorId,
                IsAvailableForSearch = anotherExisting.IsAvailableForSearch,
                UpdateDateTime = DateTimeOffset.UtcNow,
                DonorInfo = anotherExisting.ToDonorInfo()
            };
            yield return nonEditToExistingDonor;
        }
        #endregion
        #endregion
    }
}