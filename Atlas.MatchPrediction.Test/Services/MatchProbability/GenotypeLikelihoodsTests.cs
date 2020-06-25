using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.MatchProbability;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    public class GenotypeLikelihoodsTests
    {
        private IGenotypeLikelihoodService genotypeLikelihoodService;
        private IGenotypeLikelihoods genotypeLikelihoods;

        private const string PatientLocus1 = "patientGenotype1";
        private const string PatientLocus2 = "patientGenotype2";
        private const string DonorLocus1 = "donorGenotype1";
        private const string DonorLocus2 = "donorGenotype2";

        private static readonly PhenotypeInfo<string> PatientGenotype1 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = PatientLocus1, Position2 = PatientLocus1}).Build();
        private static readonly PhenotypeInfo<string> PatientGenotype2 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = PatientLocus2, Position2 = PatientLocus2}).Build();
        private static readonly PhenotypeInfo<string> DonorGenotype1 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = DonorLocus1, Position2 = DonorLocus1}).Build();
        private static readonly PhenotypeInfo<string> DonorGenotype2 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = DonorLocus2, Position2 = DonorLocus2}).Build();

        [SetUp]
        public void Setup()
        {
            genotypeLikelihoodService = Substitute.For<IGenotypeLikelihoodService>();

            genotypeLikelihoodService.CalculateLikelihood(Arg.Any<PhenotypeInfo<string>>())
                .Returns(0.5m);

            genotypeLikelihoods = new GenotypeLikelihoods(genotypeLikelihoodService);
        }

        [Test]
        public async Task CalculateLikelihoods_WhenSetOfUniqueDonorAndPatientGenotype_ReturnsListOfDonorPatientPairs()
        {
            var patientGenotypes = new HashSet<PhenotypeInfo<string>> {PatientGenotype1, PatientGenotype2};
            var donorGenotypes = new HashSet<PhenotypeInfo<string>> {DonorGenotype1, DonorGenotype2};

            var likelihoods = await genotypeLikelihoods.CalculateLikelihoods(patientGenotypes, donorGenotypes);

            likelihoods.Should().ContainKeys(patientGenotypes);
            likelihoods.Should().ContainKeys(donorGenotypes);
            likelihoods.Count.Should().Be(4);
        }

        [Test]
        public async Task CalculateLikelihoods_WhenSetOfDonorAndPatientHaveSameGenotype_ReturnsListOfDonorPatientPairs()
        {
            var patientGenotypes = new HashSet<PhenotypeInfo<string>> {PatientGenotype1, PatientGenotype2};
            var donorGenotypes = new HashSet<PhenotypeInfo<string>> {DonorGenotype1, PatientGenotype2};

            var likelihoods = await genotypeLikelihoods.CalculateLikelihoods(patientGenotypes, donorGenotypes);

            likelihoods.Should().ContainKeys(patientGenotypes);
            likelihoods.Should().ContainKeys(donorGenotypes);
            likelihoods.Count.Should().Be(3);
        }

        [Test]
        public async Task CalculateLikelihoods_WhenSetOfDonorAndPatientHaveSameGenotypes_ReturnsListOfDonorPatientPairs()
        {
            var patientGenotypes = new HashSet<PhenotypeInfo<string>> {PatientGenotype1, PatientGenotype2};
            var donorGenotypes = new HashSet<PhenotypeInfo<string>> {PatientGenotype1, PatientGenotype2};

            var likelihoods = await genotypeLikelihoods.CalculateLikelihoods(patientGenotypes, donorGenotypes);

            likelihoods.Should().ContainKeys(patientGenotypes);
            likelihoods.Should().ContainKeys(donorGenotypes);
            likelihoods.Count.Should().Be(2);
        }
    }
}
