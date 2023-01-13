using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services
{
    [TestFixture]
    internal class DonorInputBatcherTests
    {
        private DonorInputBatcher donorInputBatcher;

        [SetUp]
        public void SetUp()
        {
            donorInputBatcher = new DonorInputBatcher();
        }

        [Test]
        public void BatchDonors_RetainsAllNonDonorSearchRequestInfo()
        {
            var requestInput = MatchProbabilityRequestInputBuilder.Default
                .WithPatientHla(new PhenotypeInfo<string>("hla"))
                .WithPatientMetadata(FrequencySetMetadataBuilder.New.ForEthnicity("eth").ForRegistry("reg").Build())
                .WithExcludedLoci(Locus.A)
                .WithSearchRequestId("request-id")
                .Build();

            var batchedInput = donorInputBatcher.BatchDonorInputs(requestInput, new List<DonorInput> {DonorInputBuilder.Default.Build()}).Single();

            batchedInput.ExcludedLoci.Should().BeEquivalentTo(requestInput.ExcludedLoci);
            batchedInput.PatientHla.Should().BeEquivalentTo(requestInput.PatientHla);
            batchedInput.PatientFrequencySetMetadata.Should().BeEquivalentTo(requestInput.PatientFrequencySetMetadata);
            batchedInput.SearchRequestId.Should().BeEquivalentTo(requestInput.SearchRequestId);
        }

        [Test]
        public void BatchDonors_WhenMultipleDonorsShareHlaAndMetadata_CombinesDonorInputs()
        {
            var sharedDonorInputBuilder = DonorInputBuilder.Default
                // Need to use factories here to ensure we have objects that differ by reference, but not by value.
                .WithFactory(i => i.DonorHla, () => new PhenotypeInfo<string>("donor-hla-shared").ToPhenotypeInfoTransfer())
                .WithFactory(
                    i => i.DonorFrequencySetMetadata,
                    () => FrequencySetMetadataBuilder.New.ForRegistry("shared-reg").ForEthnicity("shared-eth").Build()
                );

            var sharedDonorInputs = sharedDonorInputBuilder.Build(3).ToList();
            var donorInputWithDifferentHla = sharedDonorInputBuilder.WithHla(new PhenotypeInfo<string>("donor-hla-new")).Build();
            var donorInputWithDifferentMetadata = sharedDonorInputBuilder
                .WithMetadata(FrequencySetMetadataBuilder.New.ForRegistry("diff-reg"))
                .Build();

            var batch = donorInputBatcher.BatchDonorInputs(
                    MatchProbabilityRequestInputBuilder.Default.Build(),
                    sharedDonorInputs.Concat(new[] {donorInputWithDifferentHla}).Concat(new[] {donorInputWithDifferentMetadata}))
                .Single();

            // 5 donors, but only 3 unique hla/metadata sets
            batch.Donors.Count.Should().Be(3);
            batch.Donors.Should().Contain(d => sharedDonorInputs.SelectMany(i => i.DonorIds).All(d.DonorIds.Contains));
        }

        [TestCase(100, 2, 50)]
        [TestCase(5, 2, 3)]
        [TestCase(1, 2, 1)]
        public void BatchDonors_BatchesInGivenSize(int numberOfDonors, int batchSize, int expectedNumberOfBatches)
        {
            var donorInputBuilder = DonorInputBuilder.Default
                .WithHla(new PhenotypeInfo<string>("donor-hla-shared"))
                .WithFactory(i => i.DonorFrequencySetMetadata,
                    () => FrequencySetMetadataBuilder.New.ForRegistry(IncrementingIdGenerator.NextStringId("registry-")).Build()
                );

            var batches = donorInputBatcher.BatchDonorInputs(
                MatchProbabilityRequestInputBuilder.Default.Build(),
                donorInputBuilder.Build(numberOfDonors),
                batchSize
            );

            batches.Count().Should().Be(expectedNumberOfBatches);
        }
    }
}