using System;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.MatchPrediction.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    internal interface IPreparationMatcher
    {
        Task<SubjectData> UpdateRenamedHla(SubjectData subjectData, MatchPredictionParameters parameters);
    }

    internal class PreparationMatcher : IPreparationMatcher
    {
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IHlaCategorisationService categoriser;

        public PreparationMatcher(IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory, IHlaCategorisationService categoriser)
        {
             this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
             this.categoriser = categoriser;
        }

        public async Task<SubjectData> UpdateRenamedHla(SubjectData subjectData, MatchPredictionParameters parameters)
        {
            var matchingAlgorithmHlaNomenclatureVersion = parameters.MatchingAlgorithmHlaNomenclatureVersion;
            SubjectData preparedSubjectData = subjectData;

            if (matchingAlgorithmHlaNomenclatureVersion == null)
            {
                preparedSubjectData = subjectData;
            }
            else
            {
                var matchingHmd = hlaMetadataDictionaryFactory.BuildDictionary(matchingAlgorithmHlaNomenclatureVersion);

                if (matchingHmd != null)
                {
                    preparedSubjectData = new SubjectData(
                        await subjectData.HlaTyping.MapAsync<string>(async (locus, _, hla) =>
                        {
                            if (hla == null)
                            {
                                return hla;
                            }

                            categoriser.TryGetHlaTypingCategory(hla, out HlaTypingCategory? category);

                            if (category != HlaTypingCategory.Allele)
                            {
                                return hla;
                            }

                            var currentAlleleNames = await matchingHmd.GetCurrentAlleleNames(locus, hla);

                            //Only require instances that return a single renamed allele, not records that return a string of names like *01:01:01:01, *01:01:01:02
                            if (currentAlleleNames.Count() != 1)
                            {
                                return hla;
                            }

                            return currentAlleleNames.Single();
                        }),

                        subjectData.SubjectFrequencySet);
                }
            }

            return preparedSubjectData;
        }
    }
}