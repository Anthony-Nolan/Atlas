using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Notifications;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Import
{
    [TestFixture]
    public class ImportTests
    {
        private IHaplotypeFrequencyService service;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequencyInspectionRepository inspectionRepository;
        private INotificationSender notificationSender;

        private const string NomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                service = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();
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
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode, 1, NomenclatureVersion).Build();

            await service.ImportFrequencySet(file);

            var activeSet = await setRepository.GetActiveSet(registryCode, ethnicityCode);

            activeSet.Name.Should().Be(file.FileName);
        }

        [TestCase(null, null)]
        [TestCase("registry", null)]
        [TestCase("registry", "ethnicity")]
        public async Task Import_DeactivatesPreviouslyActiveSet(string registryCode, string ethnicityCode)
        {
            using var oldFile = FrequencySetFileBuilder.New(registryCode, ethnicityCode, 1, NomenclatureVersion).Build();
            await service.ImportFrequencySet(oldFile);

            using var newFile = FrequencySetFileBuilder.New(registryCode, ethnicityCode, 1, NomenclatureVersion).Build();
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
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode, 1, NomenclatureVersion, 10).Build();

            await service.ImportFrequencySet(file);

            var activeSet = await setRepository.GetActiveSet(registryCode, ethnicityCode);
            var count = await inspectionRepository.HaplotypeFrequencyCount(activeSet.Id);

            count.Should().BeGreaterThan(0);
        }

        [TestCase(null, null)]
        [TestCase("registry", null)]
        [TestCase("registry", "ethnicity")]
        public async Task Import_SendsNotification(string registryCode, string ethnicityCode)
        {
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode, 1, NomenclatureVersion).Build();

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
            using var file = FrequencySetFileBuilder.New(null, null, 1, NomenclatureVersion, frequencyValue: frequency).Build();

            await service.ImportFrequencySet(file);

            var activeSet = await setRepository.GetActiveSet(null, null);
            var haplotypeFrequency = await inspectionRepository.GetFirstHaplotypeFrequency(activeSet.Id);

            haplotypeFrequency.Frequency.Should().Be(frequency);
        }

        [Test]
        public async Task Import_ForHaplotypeWithoutNullAlleles_ConvertsToPGroups()
        {
            using var file = FrequencySetFileBuilder
                .New(null, null, 1, NomenclatureVersion, new List<HaplotypeFrequency>
                {
                    new HaplotypeFrequency
                    {
                        Hla = new LociInfo<string>
                        (
                            valueA: "01:01:01G",
                            valueB: "13:01:01G",
                            valueC: "04:01:01G",
                            valueDqb1: "06:02:01G",
                            valueDrb1: "03:07:01G"
                        ),
                        Frequency = 0.5m
                    }
                }).Build();

            await service.ImportFrequencySet(file);

            var activeSet = await setRepository.GetActiveSet(null, null);
            var importedFrequency = await inspectionRepository.GetFirstHaplotypeFrequency(activeSet.Id);

            importedFrequency.TypingCategory.Should().Be(HaplotypeTypingCategory.PGroup);
            importedFrequency.Hla.Should().BeEquivalentTo(new LociInfo<string>
            (
                valueA: "01:01P",
                valueB: "13:01P",
                valueC: "04:01P",
                valueDqb1: "06:02P",
                valueDrb1: "03:07P"
            ));
        }

        [Test]
        public async Task Import_ForHaplotypeWithoutNullAlleles_WhenPGroupConversionDisabled_DoesNotConvertToPGroups()
        {
            var hla = new LociInfo<string>
            (
                valueA: "01:01:01G",
                valueB: "13:01:01G",
                valueC: "04:01:01G",
                valueDqb1: "06:02:01G",
                valueDrb1: "03:07:01G"
            );
            using var file = FrequencySetFileBuilder
                .New(null, null, 1, NomenclatureVersion, new List<HaplotypeFrequency>
                {
                    new HaplotypeFrequency
                    {
                        Hla = hla,
                        Frequency = 0.5m
                    }
                }).Build();

            await service.ImportFrequencySet(file, false);

            var activeSet = await setRepository.GetActiveSet(null, null);
            var importedFrequency = await inspectionRepository.GetFirstHaplotypeFrequency(activeSet.Id);

            importedFrequency.TypingCategory.Should().Be(HaplotypeTypingCategory.GGroup);
            importedFrequency.Hla.Should().BeEquivalentTo(hla);
        }

        [Test]
        public async Task Import_ForHaplotypeWithNullAllele_DoesNotConvertToPGroups()
        {
            var hla = new LociInfo<string>
            (
                valueA: "01:01:01G",
                valueB: "13:63N",
                valueC: "04:01:01G",
                valueDqb1: "06:02:01G",
                valueDrb1: "03:07:01G"
            );
            using var file = FrequencySetFileBuilder
                .New(null, null, 1, NomenclatureVersion, new List<HaplotypeFrequency>
                {
                    new HaplotypeFrequency {Hla = hla, Frequency = 0.5m}
                }).Build();

            await service.ImportFrequencySet(file);

            var activeSet = await setRepository.GetActiveSet(null, null);
            var importedFrequency = await inspectionRepository.GetFirstHaplotypeFrequency(activeSet.Id);

            importedFrequency.TypingCategory.Should().Be(HaplotypeTypingCategory.GGroup);
            importedFrequency.Hla.Should().BeEquivalentTo(hla);
        }
        
        [Test]
        public async Task Import_WhenMultipleHaplotypesConvertToSamePGroups_ConsolidatesFrequencies()
        {
            var gGroupsBuilder = new LociInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:01:01G")
                .WithDataAt(Locus.B, "13:01:01G")
                .WithDataAt(Locus.C, "04:01:01G")
                .WithDataAt(Locus.Dqb1, "06:02:01G")
                .WithDataAt(Locus.Drb1, "03:02:01G");
            
            using var file = FrequencySetFileBuilder
                .New(null, null, 1, NomenclatureVersion, new List<HaplotypeFrequency>
                {
                    new HaplotypeFrequency
                    {
                        Hla = gGroupsBuilder.Build(),
                        Frequency = 0.5m
                    },
                    new HaplotypeFrequency
                    {
                        Hla = gGroupsBuilder.WithDataAt(Locus.A, "01:01:02").Build(),
                        Frequency = 0.04m
                    },
                    new HaplotypeFrequency
                    {
                        Hla = gGroupsBuilder.WithDataAt(Locus.B, "13:01:02").Build(),
                        Frequency = 0.003m
                    },
                    new HaplotypeFrequency
                    {
                        Hla = gGroupsBuilder.WithDataAt(Locus.Drb1, "03:02:02").Build(),
                        Frequency = 0.0002m
                    },
                    new HaplotypeFrequency
                    {
                        Hla = gGroupsBuilder.WithDataAt(Locus.C, "04:01:02").WithDataAt(Locus.Dqb1, "06:02:02").Build(),
                        Frequency = 0.0000001m
                    }
                })
                .Build();

            await service.ImportFrequencySet(file);

            var activeSet = await setRepository.GetActiveSet(null, null);
            var importedFrequency = await inspectionRepository.GetFirstHaplotypeFrequency(activeSet.Id);

            importedFrequency.TypingCategory.Should().Be(HaplotypeTypingCategory.PGroup);
            importedFrequency.Hla.Should().BeEquivalentTo(new LociInfo<string>
            (
                valueA: "01:01P",
                valueB: "13:01P",
                valueC: "04:01P",
                valueDqb1: "06:02P",
                valueDrb1: "03:02P"
            ));
            importedFrequency.Frequency.Should().Be(0.5432001m);
        }

        [Test]
        public async Task Import_FileWithInvalidCsvFormat_SendsAlert()
        {
            using var file = FrequencySetFileBuilder.WithInvalidCsvFormat(null, null, 1, NomenclatureVersion).Build();

            await service.ImportFrequencySet(file);

            await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, Priority.Medium, default);
        }

        [Test]
        public async Task Import_FileWithoutContents_SendsAlert()
        {
            using var file = FrequencySetFileBuilder.FileWithoutContents().Build();

            await service.ImportFrequencySet(file);

            await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, Priority.Medium, default);
        }

        [TestCase("")]
        [TestCase("//ethnicity-only/file")]
        [TestCase("/too/many/subfolders/file")]
        public async Task Import_InvalidFilePath_SendsAlert(string invalidPath)
        {
            using var file = FrequencySetFileBuilder.New(null, null, 1, NomenclatureVersion)
                .With(x => x.FileName, invalidPath)
                .Build();

            await service.ImportFrequencySet(file);

            await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, Priority.Medium, default);
        }

        [Test]
        public async Task Import_WithZeroFrequency_SendsAlert()
        {
            var hla = new LociInfo<string>
            (
                valueA: "04:01:01G",
                valueB: "04:01:01G",
                valueC: "04:01:01G",
                valueDqb1: "06:02:01G",
                valueDrb1: "03:07:01G"
            );
            using var file = FrequencySetFileBuilder
                .New(null, null, 1, NomenclatureVersion, new List<HaplotypeFrequency>
                {
                    new HaplotypeFrequency {Hla = hla, Frequency = 0m}
                })
                .Build();

            await service.ImportFrequencySet(file);
          
            await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, Priority.Medium, default);
            
        }

        [Test]
        public async Task Import_WhenDuplicateHaplotypes_SendsAlert()
        {
            var gGroupsBuilder = new LociInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:01:01G")
                .WithDataAt(Locus.B, "13:01:01G")
                .WithDataAt(Locus.C, "04:01:01G")
                .WithDataAt(Locus.Dqb1, "06:02:01G")
                .WithDataAt(Locus.Drb1, "03:02:01G");

            using var file = FrequencySetFileBuilder
                .New(null, null, 1, NomenclatureVersion, new List<HaplotypeFrequency>
                {
                    new HaplotypeFrequency
                    {
                        Hla = gGroupsBuilder.Build(),
                        Frequency = 0.5m
                    },
                    new HaplotypeFrequency
                    {
                        Hla = gGroupsBuilder.Build(),
                        Frequency = 0.04m
                    }
                })
                .Build();

            await service.ImportFrequencySet(file);
      
            await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, Priority.Medium, default);
            
        }

        [TestCase(null)]
        [TestCase("01:XX")]
        // A single allele can be a valid G-Group, if it doesn't share a G group with any other alleles.
        // In this case we are using one that does so it should be an invalid G group.
        [TestCase("01:01")]
        // Is a valid G group at locus B but not at locus A.
        [TestCase("13:01:01G")]
        public async Task Import_WhenHlaIsNotOfTypeGGroup_SendsAlert(string invalidHla)
        {
            var hla = new LociInfo<string>
            (
                valueA: invalidHla,
                valueB: invalidHla,
                valueC: "04:01:01G",
                valueDqb1: "06:02:01G",
                valueDrb1: "03:07:01G"
            );
            using var file = FrequencySetFileBuilder
                .New(null, null, 1, NomenclatureVersion, new List<HaplotypeFrequency>
                {
                    new HaplotypeFrequency {Hla = hla, Frequency = 0.1m}
                })
                .Build();

            await service.ImportFrequencySet(file);

            await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, Priority.Medium, default);
        }

        [TestCase(null)]
        [TestCase("01:XX")]
        [TestCase("01:01")]
        public async Task Import_WhenHlaIsNotOfTypeGGroupAndDoesNotConvertToPGroup_SendsAlert(string invalidHla)
        {
            var hla = new LociInfo<string>
            (
                valueA: invalidHla,
                valueB: invalidHla,
                valueC: "04:01:01G",
                valueDqb1: "06:02:01G",
                valueDrb1: "03:07:01G"
            );
            using var file = FrequencySetFileBuilder
                .New(null, null, 1, NomenclatureVersion, new List<HaplotypeFrequency>
                {
                    new HaplotypeFrequency {Hla = hla, Frequency = 0.1m}
                })
                .Build();

            await service.ImportFrequencySet(file, false);

            await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, Priority.Medium, default);
        }

        [TestCase(100, NomenclatureVersion, "Reg", "Ethn")]
        [TestCase(1, "Diff-Nomenclature", "Reg", "Ethn")]
        [TestCase(1, NomenclatureVersion, "Diff-Reg", "Ethn")]
        [TestCase(1, NomenclatureVersion, "Reg", "Diff-Ethn")]
        public async Task Import_WhenDifferingSetInfo_SendsAlert(int populationId, string nomenclatureVersion, string registry, string ethnicity)
        {
            var gGroupsBuilder = new LociInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:01:01G")
                .WithDataAt(Locus.B, "13:01:01G")
                .WithDataAt(Locus.C, "04:01:01G")
                .WithDataAt(Locus.Dqb1, "06:02:01G")
                .WithDataAt(Locus.Drb1, "03:02:01G");

            using var file = FrequencySetFileBuilder
                .New(new List<HaplotypeFrequencyMetadata>
                {
                    new HaplotypeFrequencyMetadata(gGroupsBuilder.Build())
                    {
                        Frequency = 0.5m,
                        PopulationId = 1,
                        HlaNomenclatureVersion = NomenclatureVersion,
                        RegistryCode = "Reg",
                        EthnicityCode = "Ethn"
                    },
                    new HaplotypeFrequencyMetadata(gGroupsBuilder.WithDataAt(Locus.A, "01:01:02").Build())
                    {
                        Frequency = 0.04m,
                        PopulationId = populationId,
                        HlaNomenclatureVersion = nomenclatureVersion,
                        RegistryCode = registry,
                        EthnicityCode = ethnicity
                    }
                })
                .Build();

            await service.ImportFrequencySet(file);

            await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, Priority.Medium, default);
        }
    }
}