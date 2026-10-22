using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services;

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
        requestInput.MatchingAlgorithmHlaNomenclatureVersion = "3330";
        requestInput.MatchingAlgorithmDataRefreshRecordId = 42;
        requestInput.PatientGenotypeSetKey = new PatientGenotypeSetKey("typing-key", 7, "ABCDrb1Dqb1");

        var batchedInput = donorInputBatcher.BatchDonorInputs(requestInput, new List<DonorInput> {DonorInputBuilder.Default.Build()}).Single();

        batchedInput.MatchingAlgorithmHlaNomenclatureVersion.Should().Be(requestInput.MatchingAlgorithmHlaNomenclatureVersion);
        batchedInput.MatchingAlgorithmDataRefreshRecordId.Should().Be(requestInput.MatchingAlgorithmDataRefreshRecordId);
        batchedInput.PatientGenotypeSetKey.Should().Be(requestInput.PatientGenotypeSetKey);
        batchedInput.SingleDonorMatchProbabilityInputs.Single().PatientGenotypeSetKey.Should().Be(requestInput.PatientGenotypeSetKey);
        batchedInput.SingleDonorMatchProbabilityInputs.Single().MatchingAlgorithmDataRefreshRecordId
            .Should().Be(requestInput.MatchingAlgorithmDataRefreshRecordId);
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
            .With(i => i.DonorHla, () => new PhenotypeInfo<string>("donor-hla-shared").ToPhenotypeInfoTransfer())
            .With(
                i => i.DonorFrequencySetMetadata,
                () => FrequencySetMetadataBuilder.New.ForRegistry("shared-reg").ForEthnicity("shared-eth").Build()
            );

        var sharedDonorInputs = sharedDonorInputBuilder.Build(3).ToList();
        var donorInputWithDifferentHla = sharedDonorInputBuilder.WithHla(new PhenotypeInfo<string>("donor-hla-new")).Build();
        var donorInputWithDifferentMetadata = sharedDonorInputBuilder
            .WithMetadata(FrequencySetMetadataBuilder.New.ForRegistry("diff-reg").Build())
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
            .With(i => i.DonorFrequencySetMetadata,
                () => FrequencySetMetadataBuilder.New.ForRegistry(IncrementingIdGenerator.NextStringId("registry-")).Build()
            );

        var batches = donorInputBatcher.BatchDonorInputs(
            MatchProbabilityRequestInputBuilder.Default.Build(),
            donorInputBuilder.Build(numberOfDonors),
            batchSize
        );

        batches.Count().Should().Be(expectedNumberOfBatches);
    }

    [Test]
    public void BatchDonors_PutsDonorsWithTheSameFrequencySetMetadataTogether()
    {
        // Interleaved, as matching results come: two registries, each with two ethnicities.
        var donors = Enumerable.Range(1, 12)
            .Select(i => DonorInputBuilder.Default
                .WithDonorIds(i)
                .WithHla(new PhenotypeInfo<string>($"hla-{i}"))
                .WithMetadata(FrequencySetMetadataBuilder.New.ForRegistry(i % 2 == 0 ? "reg-b" : "reg-a").ForEthnicity(i % 3 == 0 ? "eth-1" : "eth-2").Build())
                .Build())
            .ToList();

        var batches = donorInputBatcher.BatchDonorInputs(MatchProbabilityRequestInputBuilder.Default.Build(), donors, 2).ToList();

        var metadataInOrder = batches.SelectMany(b => b.Donors).Select(d => (d.DonorFrequencySetMetadata.RegistryCode, d.DonorFrequencySetMetadata.EthnicityCode)).ToList();
        metadataInOrder.Select(m => m.RegistryCode + "/" + m.EthnicityCode).Should().BeInAscendingOrder(StringComparer.Ordinal);
        // The metadata pairs hold 2, 4, 2 and 4 donors here, so with batches of 2 each batch spans exactly one pair.
        batches.Should().AllSatisfy(b => b.Donors.Select(d => (d.DonorFrequencySetMetadata.RegistryCode, d.DonorFrequencySetMetadata.EthnicityCode)).Distinct().Should().HaveCount(1));
        batches.SelectMany(b => b.Donors).SelectMany(d => d.DonorIds).Should().BeEquivalentTo(Enumerable.Range(1, 12));
    }

    [Test]
    public void BatchDonors_WithNoFrequencySetMetadata_KeepsTheDonors()
    {
        var donors = new List<DonorInput>
        {
            DonorInputBuilder.Default.WithDonorIds(1).WithHla(new PhenotypeInfo<string>("hla-1"))
                .WithMetadata(FrequencySetMetadataBuilder.New.ForRegistry("reg").Build()).Build(),
            DonorInputBuilder.Default.WithDonorIds(2).WithHla(new PhenotypeInfo<string>("hla-2")).With(d => d.DonorFrequencySetMetadata, (FrequencySetMetadata)null).Build(),
        };

        var batch = donorInputBatcher.BatchDonorInputs(MatchProbabilityRequestInputBuilder.Default.Build(), donors, 10).Single();

        batch.Donors.SelectMany(d => d.DonorIds).Should().BeEquivalentTo(new[] { 1, 2 });
        batch.Donors.First().DonorIds.Should().Equal(2);
    }
}