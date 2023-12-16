using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    public interface IPositionalScorerBase<T>
    {
        /// <summary>
        /// For each locus to be scored, will score the locus in the best orientation provided,
        /// also handling any null alleles by copying the expressing allele to both positions.
        /// </summary>
        PhenotypeInfo<T> Score(
            LociInfo<List<MatchOrientation>> orientations,
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata);
    }

    public abstract class PositionalScorerBase<T> : IPositionalScorerBase<T>
    {
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly T defaultScoreWhenNoInfo;

        protected PositionalScorerBase(IHlaCategorisationService hlaCategorisationService, T defaultScoreWhenNoInfo)
        {
            this.hlaCategorisationService = hlaCategorisationService;
            this.defaultScoreWhenNoInfo = defaultScoreWhenNoInfo;
        }

        public PhenotypeInfo<T> Score(
            LociInfo<List<MatchOrientation>> orientations,
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata)
        {
            var results = new PhenotypeInfo<T>();

            orientations.ForEachLocus((locus, orientationsAtLocus) =>
            {
                var patientLocus = patientMetadata.GetLocus(locus);
                var donorLocus = donorMetadata.GetLocus(locus);

                static bool IsLocusInfoNull(LocusInfo<IHlaScoringMetadata> locusInfo) => locusInfo is null || locusInfo.Position1And2Null();
                if (orientationsAtLocus.IsNullOrEmpty() || IsLocusInfoNull(patientLocus) || IsLocusInfoNull(donorLocus))
                {
                    results = results.SetLocus(locus, defaultScoreWhenNoInfo);
                    return;
                }

                var patientLocusForScoring = HandleNonExpressingAlleleIfAny(patientLocus);
                var donorLocusForScoring = HandleNonExpressingAlleleIfAny(donorLocus);

                var locusResults = CalculateLocusScoreForBestOrientation(
                    orientationsAtLocus, patientLocusForScoring, donorLocusForScoring);

                results = results.SetLocus(locus, locusResults.Position1, locusResults.Position2);
            });

            return results;
        }

        /// <summary>
        /// If locus contains a non-expressing allele, then return new object with scoring metadata of expressing typing copied to both positions,
        /// else return the original metadata.
        /// </summary>
        private LocusInfo<IHlaScoringMetadata> HandleNonExpressingAlleleIfAny(LocusInfo<IHlaScoringMetadata> metadata)
        {
            var nonExpressingCheck = metadata.Map(x => hlaCategorisationService.IsNullAllele(x.LookupName));

            return nonExpressingCheck.BothPositions(x => x == false)
                ? metadata
                : new LocusInfo<IHlaScoringMetadata>(
                    nonExpressingCheck.Position1 ? metadata.Position2 : metadata.Position1,
                    nonExpressingCheck.Position2 ? metadata.Position1 : metadata.Position2
                );
        }

        private LocusInfo<T> CalculateLocusScoreForBestOrientation(
            IReadOnlyCollection<MatchOrientation> orientations,
            LocusInfo<IHlaScoringMetadata> patientLocusData,
            LocusInfo<IHlaScoringMetadata> donorLocusData)
        {
            if (!orientations.Any())
            {
                return new LocusInfo<T>();
            }

            if (orientations.Count == 1)
            {
                return ScoreLocus(orientations.Single(), patientLocusData, donorLocusData);
            }

            var directScore = ScoreLocus(MatchOrientation.Direct, patientLocusData, donorLocusData);
            var crossScore = ScoreLocus(MatchOrientation.Cross, patientLocusData, donorLocusData);

            return SelectBestOrientation(directScore, crossScore);
        }

        private LocusInfo<T> ScoreLocus(
            MatchOrientation orientation,
            LocusInfo<IHlaScoringMetadata> patientLocusData,
            LocusInfo<IHlaScoringMetadata> donorLocusData)
        {
            var positionOneScore = ScorePosition(
                orientation == MatchOrientation.Cross ? patientLocusData.Position2 : patientLocusData.Position1, donorLocusData.Position1);

            var positionTwoScore = ScorePosition(
                orientation == MatchOrientation.Cross ? patientLocusData.Position1 : patientLocusData.Position2, donorLocusData.Position2);

            return new LocusInfo<T>(positionOneScore, positionTwoScore);
        }

        protected abstract T ScorePosition(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata);

        private LocusInfo<T> SelectBestOrientation(LocusInfo<T> directScore, LocusInfo<T> crossScore)
        {
            return SumLocusScore(directScore) >= SumLocusScore(crossScore) ? directScore : crossScore;
        }

        protected abstract int SumLocusScore(LocusInfo<T> score);
    }
}