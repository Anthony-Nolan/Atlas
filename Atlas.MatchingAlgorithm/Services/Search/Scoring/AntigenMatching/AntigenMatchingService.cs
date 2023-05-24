using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching
{
    public interface IAntigenMatchingService
    {
        PhenotypeInfo<bool?> CalculateAntigenMatches(
            LociInfo<List<MatchOrientation>> orientations,
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata);
    }

    internal class AntigenMatchingService : IAntigenMatchingService
    {
        private readonly IAntigenMatchCalculator calculator;
        private readonly IScoringCache scoringCache;

        public AntigenMatchingService(IAntigenMatchCalculator calculator, IScoringCache scoringCache)
        {
            this.calculator = calculator;
            this.scoringCache = scoringCache;
        }

        public PhenotypeInfo<bool?> CalculateAntigenMatches(
            LociInfo<List<MatchOrientation>> orientations,
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata)
        {
            var results = new PhenotypeInfo<bool?>();

            orientations.ForEachLocus((locus, orientationsAtLocus) =>
            {
                var patientLocus = patientMetadata.GetLocus(locus);
                var donorLocus = donorMetadata.GetLocus(locus);

                static bool IsLocusInfoNull<T>(LocusInfo<T> locusInfo) => locusInfo is null || locusInfo.Position1And2Null();
                if (orientationsAtLocus.IsNullOrEmpty() || IsLocusInfoNull(patientLocus) || IsLocusInfoNull(donorLocus))
                {
                    results.SetLocus(locus, null);
                    return;
                }

                // need to calculate antigen match according to the orientation provided in grading result
                var locusResults = CalculateLocusAntigenMatchInBestOrientation(orientationsAtLocus, patientLocus, donorLocus);

                results = results.SetLocus(locus, locusResults.Position1, locusResults.Position2);
            });

            return results;
        }

        private LocusInfo<bool?> CalculateLocusAntigenMatchInBestOrientation(
            IReadOnlyCollection<MatchOrientation> orientations,
            LocusInfo<IHlaScoringMetadata> patientLocusData,
            LocusInfo<IHlaScoringMetadata> donorLocusData)
        {
            if (!orientations.Any())
            {
                return new LocusInfo<bool?>();
            }

            if (orientations.Count == 1)
            {
                return CalculateLocusAntigenMatch(orientations.Single(), patientLocusData, donorLocusData);
            }

            var directAntigenMatches = CalculateLocusAntigenMatch(MatchOrientation.Direct, patientLocusData, donorLocusData);
            var crossAntigenMatches = CalculateLocusAntigenMatch(MatchOrientation.Cross, patientLocusData, donorLocusData);

            static int CountAntigenMatches(LocusInfo<bool?> input) => new[] { input.Position1, input.Position2 }.Count(r => r.HasValue && r.Value);
            var directMatchCount = CountAntigenMatches(directAntigenMatches);
            var crossMatchCount = CountAntigenMatches(crossAntigenMatches);

            return directMatchCount >= crossMatchCount ? directAntigenMatches : crossAntigenMatches;
        }

        private LocusInfo<bool?> CalculateLocusAntigenMatch(
            MatchOrientation orientation,
            LocusInfo<IHlaScoringMetadata> patientLocusData,
            LocusInfo<IHlaScoringMetadata> donorLocusData)
        {
            var isGradeOneAntigenMatched = IsAntigenMatch(
                patientLocusData.Position1, orientation == MatchOrientation.Cross ? donorLocusData.Position2 : donorLocusData.Position1);

            var isGradeTwoAntigenMatched = IsAntigenMatch(
                patientLocusData.Position2, orientation == MatchOrientation.Cross ? donorLocusData.Position1 : donorLocusData.Position2);

            return new LocusInfo<bool?>(isGradeOneAntigenMatched, isGradeTwoAntigenMatched);
        }

        private bool? IsAntigenMatch(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return scoringCache.GetOrAddIsAntigenMatch(
                patientMetadata?.Locus,
                patientMetadata?.LookupName,
                donorMetadata?.LookupName,
                c => calculator.IsAntigenMatch(patientMetadata, donorMetadata));
        }
    }
}