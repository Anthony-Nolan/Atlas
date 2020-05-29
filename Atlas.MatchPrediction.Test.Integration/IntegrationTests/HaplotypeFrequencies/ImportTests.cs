using Atlas.Common.Notifications;
using Atlas.Common.Notifications.MessageModels;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.HaplotypeFrequencies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.HaplotypeFrequencies
{
    [TestFixture]
    public class ImportTests
    {
        private IFrequencySetService service;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequencyInspectionRepository inspectionRepository;
        private INotificationsClient notificationsClient;

        [SetUp]
        public void SetUp()
        {
            service = DependencyInjection.DependencyInjection.Provider.GetService<IFrequencySetService>();
            setRepository = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencySetRepository>();
            inspectionRepository = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyInspectionRepository>();
            notificationsClient = DependencyInjection.DependencyInjection.Provider.GetService<INotificationsClient>();
        }

        [TestCase(null, null)]
        [TestCase("registry", null)]
        [TestCase("registry", "ethnicity")]
        public async Task Import_ImportsSetAsActive(string registryCode, string ethnicityCode)
        {
            var file = FrequencyFileBuilder.Build(registryCode, ethnicityCode, 1);

            await using (var stream = GetHaplotypeFrequenciesStream(file.Contents))
            {
                await service.ImportFrequencySet(stream, file.FullPath);
            }

            var activeSet = await setRepository.GetActiveSet(registryCode, ethnicityCode);

            activeSet.Name.Should().Be(file.FileName);
        }

        [TestCase(null, null)]
        [TestCase("registry", null)]
        [TestCase("registry", "ethnicity")]
        public async Task Import_DeactivatesPreviouslyActiveSet(string registryCode, string ethnicityCode)
        {
            var oldFile = FrequencyFileBuilder.Build(registryCode, ethnicityCode, 1);
            await using (var stream = GetHaplotypeFrequenciesStream(oldFile.Contents))
            {
                await service.ImportFrequencySet(stream, oldFile.FullPath);
            }

            var newFile = FrequencyFileBuilder.Build(registryCode, ethnicityCode, 1);
            await using (var stream = GetHaplotypeFrequenciesStream(newFile.Contents))
            {
                await service.ImportFrequencySet(stream, newFile.FullPath);
            }

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
            var file = FrequencyFileBuilder.Build(registryCode, ethnicityCode, 10);

            await using (var stream = GetHaplotypeFrequenciesStream(file.Contents))
            {
                await service.ImportFrequencySet(stream, file.FullPath);
            }

            var activeSet = await setRepository.GetActiveSet(registryCode, ethnicityCode);
            var count = await inspectionRepository.HaplotypeFrequencyCount(activeSet.Id);

            count.Should().Be(frequencyCount);
        }

        [TestCase(null, null)]
        [TestCase("registry", null)]
        [TestCase("registry", "ethnicity")]
        public async Task Import_SoftDeletesFrequenciesOfDeactivatedSet(string registryCode, string ethnicityCode)
        {
            const int oldExpectedFrequencyCount = 5;
            var oldFile = FrequencyFileBuilder.Build(registryCode, ethnicityCode, oldExpectedFrequencyCount);
            await using (var stream = GetHaplotypeFrequenciesStream(oldFile.Contents))
            {
                await service.ImportFrequencySet(stream, oldFile.FullPath);
            }
            var oldSet = await setRepository.GetActiveSet(registryCode, ethnicityCode);

            const int newExpectedFrequencyCount = 12;
            var newFile = FrequencyFileBuilder.Build(registryCode, ethnicityCode, newExpectedFrequencyCount);
            await using (var stream = GetHaplotypeFrequenciesStream(newFile.Contents))
            {
                await service.ImportFrequencySet(stream, newFile.FullPath);
            }
            var newSet = await setRepository.GetActiveSet(registryCode, ethnicityCode);

            var oldCount = await inspectionRepository.HaplotypeFrequencyCount(oldSet.Id);
            var newCount = await inspectionRepository.HaplotypeFrequencyCount(newSet.Id);

            oldCount.Should().Be(oldExpectedFrequencyCount);
            newCount.Should().Be(newExpectedFrequencyCount);
        }

        [TestCase("//ethnicity-only/file")]
        [TestCase("/too/many/subfolders/file")]
        public async Task Import_InvalidFilePath_ThrowsException(string invalidFileName)
        {
            var fileWithValidContents = FrequencyFileBuilder.Build(null, null, 1);
            await using (var stream = GetHaplotypeFrequenciesStream(fileWithValidContents.Contents))
            {
                service.Invoking(async importer =>
                    await service.ImportFrequencySet(stream, invalidFileName)).Should().Throw<Exception>();
            }
        }

        [TestCase("//ethnicity-only/file")]
        [TestCase("/too/many/subfolders/file")]
        public async Task Import_InvalidFilePath_SendsAlert(string invalidFileName)
        {
            var fileWithValidContents = FrequencyFileBuilder.Build(null, null, 1);
            await using (var stream = GetHaplotypeFrequenciesStream(fileWithValidContents.Contents))
            {
                try
                {
                    await service.ImportFrequencySet(stream, invalidFileName);
                }
                catch (Exception)
                {
                    await notificationsClient.Received().SendAlert(Arg.Any<Alert>());
                }
            }
        }

        private static Stream GetHaplotypeFrequenciesStream(string fileContents)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(fileContents));
        }
    }
}
