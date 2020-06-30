using Atlas.Common.Notifications;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.HaplotypeFrequencies
{
    [TestFixture]
    public class ImportTests
    {
        private IFrequencySetService service;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequencyInspectionRepository inspectionRepository;
        private INotificationSender notificationSender;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                service = DependencyInjection.DependencyInjection.Provider.GetService<IFrequencySetService>();
                setRepository = DependencyInjection.DependencyInjection.Provider
                    .GetService<IHaplotypeFrequencySetRepository>();
                inspectionRepository = DependencyInjection.DependencyInjection.Provider
                    .GetService<IHaplotypeFrequencyInspectionRepository>();
                notificationSender =
                    DependencyInjection.DependencyInjection.Provider.GetService<INotificationSender>();
            });
        }

        [TearDown]
        public void TearDown()
        {
            notificationSender.ClearReceivedCalls();
        }

        [TestCase(null, null)]
        [TestCase("registry", null)]
        [TestCase("registry", "ethnicity")]
        public async Task Import_ImportsSetAsActive(string registryCode, string ethnicityCode)
        {
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode).Build();

            await service.ImportFrequencySet(file);

            var activeSet = await setRepository.GetActiveSet(registryCode, ethnicityCode);
            
            activeSet.Name.Should().Be(file.FileName);
        }

        [TestCase(null, null)]
        [TestCase("registry", null)]
        [TestCase("registry", "ethnicity")]
        public async Task Import_DeactivatesPreviouslyActiveSet(string registryCode, string ethnicityCode)
        {
            using var oldFile = FrequencySetFileBuilder.New(registryCode, ethnicityCode).Build();
            await service.ImportFrequencySet(oldFile);

            using var newFile = FrequencySetFileBuilder.New(registryCode, ethnicityCode).Build();
            await service.ImportFrequencySet(newFile);

            var activeSet = await setRepository.GetActiveSet(registryCode, ethnicityCode);
            var activeSetCount = await inspectionRepository.ActiveSetCount(registryCode, ethnicityCode);

            activeSet.Name.Should().Be(newFile.FileName);
            activeSetCount.Should().Be(1);
        }

        [TestCase(null, null)]
        [TestCase("registry", null)]
        [TestCase("registry", "ethnicity")]
        public async Task Import_StoresFrequencies(string registryCode, string ethnicityCode)
        {
            const int frequencyCount = 10;
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode, 10).Build();

            await service.ImportFrequencySet(file);

            var activeSet = await setRepository.GetActiveSet(registryCode, ethnicityCode);
            var count = await inspectionRepository.HaplotypeFrequencyCount(activeSet.Id);

            count.Should().Be(frequencyCount);
        }

        [TestCase(null, null)]
        [TestCase("registry", null)]
        [TestCase("registry", "ethnicity")]
        public async Task Import_SendsNotification(string registryCode, string ethnicityCode)
        {
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode).Build();

            await service.ImportFrequencySet(file);

            await notificationSender.ReceivedWithAnyArgs().SendNotification(default, default, default);
        }

        /// <summary>
        /// Regression test for bug where frequency was being stored as 0.
        /// </summary>
        [Test]
        public async Task Import_StoresFrequencyValueAsDecimalToRequiredNumberOfPlaces()
        {
            const decimal frequency = 1E-16m;
            using var file = FrequencySetFileBuilder.New(null, null, frequencyValue: frequency).Build();

            await service.ImportFrequencySet(file);

            var activeSet = await setRepository.GetActiveSet(null, null);
            var haplotypeFrequency = await inspectionRepository.GetFirstHaplotypeFrequency(activeSet.Id);

            haplotypeFrequency.Frequency.Should().Be(frequency);
        }

        [TestCase("//ethnicity-only/file")]
        [TestCase("/too/many/subfolders/file")]
        public void Import_InvalidFilePath_ThrowsException(string invalidPath)
        {
            using var file = FrequencySetFileBuilder.New(null, null)
                .With(x => x.FullPath, invalidPath)
                .Build();

            service.Invoking(async importer => await service.ImportFrequencySet(file)).Should().Throw<Exception>();
        }

        [TestCase("//ethnicity-only/file")]
        [TestCase("/too/many/subfolders/file")]
        public async Task Import_InvalidFilePath_SendsAlert(string invalidPath)
        {
            using var file = FrequencySetFileBuilder.New(null, null)
                .With(x => x.FullPath, invalidPath)
                .Build();

            try
            {
                await service.ImportFrequencySet(file);
            }
            catch (Exception)
            {
                await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            }
        }

        [TestCase("//ethnicity-only/file")]
        [TestCase("/too/many/subfolders/file")]
        public async Task Import_InvalidFilePath_DoesNotSendNotification(string invalidPath)
        {
            using var file = FrequencySetFileBuilder.New(null, null)
                .With(x => x.FullPath, invalidPath)
                .Build();

            try
            {
                await service.ImportFrequencySet(file);
            }
            catch (Exception)
            {
                await notificationSender.DidNotReceiveWithAnyArgs().SendNotification(default, default, default);
            }
        }
    }
}
