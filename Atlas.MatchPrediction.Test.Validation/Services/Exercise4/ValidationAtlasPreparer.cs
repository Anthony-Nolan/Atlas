using Atlas.DonorImport.Data.Models;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.ManualTesting.Common.Services;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4
{
    public interface IValidationAtlasPreparer : IAtlasPreparer
    {
        Task PrepareAtlasWithImportedDonors();
    }

    internal class ValidationAtlasPreparer : AtlasPreparer, IValidationAtlasPreparer
    {
        private readonly ISubjectRepository subjectRepository;
        private readonly IHaplotypeFrequencySetReader setReader;

        private ConcurrentDictionary<string, HaplotypeFrequencySet> hfSets = new();

        public ValidationAtlasPreparer(
            ISubjectRepository subjectRepository,
            IHaplotypeFrequencySetReader setReader,
            ITestDonorExporter testDonorExporter,
            ITestDonorExportRepository exportRepository,
            string dataRefreshRequestUrl)
            : base(testDonorExporter, exportRepository, dataRefreshRequestUrl)
        {
            this.subjectRepository = subjectRepository;
            this.setReader = setReader;
        }

        public async Task PrepareAtlasWithImportedDonors()
        {
            await PrepareAtlasDonorStores();
        }

        protected override async Task<IEnumerable<Donor>> GetTestDonors()
        {
            var donors = await subjectRepository.GetDonors();
            return await Task.WhenAll(donors.Select(d => MapToDonorImportModel(d)));
        }

        private async Task<Donor> MapToDonorImportModel(SubjectInfo subject)
        {
            const string updateFileText = "UploadedDirectlyForValidationExercise4";
            var hfSet = await GetOrAddHfSet(subject.ExternalHfSetId);

            var donor = new Donor
            {
                ExternalDonorCode = subject.ExternalId,
                DonorType = subject.DonorType.Value.ToDatabaseType(),
                EthnicityCode = hfSet.EthnicityCode,
                RegistryCode = hfSet.RegistryCode,
                A_1 = subject.A_1,
                A_2 = subject.A_2,
                B_1 = subject.B_1,
                B_2 = subject.B_2,
                C_1 = subject.C_1,
                C_2 = subject.C_2,
                DQB1_2 = subject.DQB1_1,
                DQB1_1 = subject.DQB1_2,
                DRB1_1 = subject.DRB1_1,
                DRB1_2 = subject.DRB1_2,
                UpdateFile = updateFileText,
                LastUpdated = DateTimeOffset.UtcNow
            };

            // The hash is not needed but no harm in calculating it in case 
            // donors are also imported into the test environment via the normal donor import file route.
            donor.CalculateHash();

            return donor;
        }

        private async Task<HaplotypeFrequencySet> GetOrAddHfSet(string externalHfSetId)
        {
            if(hfSets.TryGetValue(externalHfSetId, out var set))
            {
                return set;
            }

            var sets = (await setReader.GetActiveHaplotypeFrequencySetByName(externalHfSetId)).ToList();

            switch (sets.Count)
            {
                case 0:
                    throw new Exception($"Could not find active HF set named {externalHfSetId}.");
                case 1:
                    hfSets.TryAdd(externalHfSetId, sets.Single());
                    return sets.Single();
                default:
                    throw new Exception($"Found multiple active HF sets named {externalHfSetId}.");
            }
        }
    }
}