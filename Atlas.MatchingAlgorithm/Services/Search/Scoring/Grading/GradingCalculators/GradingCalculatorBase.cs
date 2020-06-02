using System;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
{
    public abstract class GradingCalculatorBase : IGradingCalculator
    {
        public MatchGrade CalculateGrade(
            IHlaScoringMetadata patientMetadata, 
            IHlaScoringMetadata donorMetadata)
        {
            if (patientMetadata.Locus != donorMetadata.Locus)
            {
                throw new ArgumentException("Metadata does not all belong to same locus.");
            }

            if (!ScoringInfosAreOfPermittedTypes(patientMetadata.HlaScoringInfo, donorMetadata.HlaScoringInfo))
            {
                throw new ArgumentException("One or both scoring infos are not of the permitted types.");
            }

            return GetMatchGrade(patientMetadata, donorMetadata);
        }

        protected abstract bool ScoringInfosAreOfPermittedTypes(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo);

        protected abstract MatchGrade GetMatchGrade(
            IHlaScoringMetadata patientMetadata,
            IHlaScoringMetadata donorMetadata);
    }
}