using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    public interface IPositionalScorerBase<T>
    {
        LociInfo<LocusScoreResult<T>> Score(
            LociInfo<IEnumerable<MatchOrientation>> orientations,
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata);
    }

    public class PositionalScorerSettings<T>
    {
        public T DefaultLocusScore { get; set; }
        public T DefaultDpb1Score { get; set; }
        public bool HandleNonExpressingAlleles { get; set; }
    }

    public abstract class PositionalScorerBase<T> : IPositionalScorerBase<T>
    {
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly LocusScoreResult<T> defaultLocusScoreResult;
        private readonly LocusScoreResult<T> defaultDpb1ScoreResult;
        private readonly bool handleNonExpressingAlleles;

        protected PositionalScorerBase(
            IHlaCategorisationService hlaCategorisationService,
            PositionalScorerSettings<T> settings)
        {
            this.hlaCategorisationService = hlaCategorisationService;
            defaultLocusScoreResult = new LocusScoreResult<T>(settings.DefaultLocusScore);
            defaultDpb1ScoreResult = new LocusScoreResult<T>(settings.DefaultDpb1Score);
            handleNonExpressingAlleles = settings.HandleNonExpressingAlleles;
        }

        public LociInfo<LocusScoreResult<T>> Score(
            LociInfo<IEnumerable<MatchOrientation>> orientations,
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata)
        {
            if (orientations == null || patientMetadata == null || donorMetadata == null)
            {
                throw new ArgumentNullException($"{nameof(orientations)}/{nameof(patientMetadata)}/{nameof(donorMetadata)}");
            }

            var results = new LociInfo<LocusScoreResult<T>>();

            orientations.ForEachLocus((locus, orientationsAtLocus) =>
            {
                var orientationsList = orientationsAtLocus.ToList();
                var patientLocus = patientMetadata.GetLocus(locus);
                var donorLocus = donorMetadata.GetLocus(locus);

                static bool IsLocusInfoNull(LocusInfo<IHlaScoringMetadata> locusInfo) => locusInfo is null || locusInfo.Position1And2Null();
                if (orientationsList.IsNullOrEmpty() || IsLocusInfoNull(patientLocus) || IsLocusInfoNull(donorLocus))
                {
                    results = results.SetLocus(locus, locus == Locus.Dpb1 ? defaultDpb1ScoreResult : defaultLocusScoreResult);
                    return;
                }

                var locusResults = CalculateLocusScoreForBestOrientation(
                    orientationsList,
                    handleNonExpressingAlleles ? HandleNonExpressingAlleleIfAny(patientLocus) : patientLocus,
                    handleNonExpressingAlleles ? HandleNonExpressingAlleleIfAny(donorLocus) : donorLocus);

                results = results.SetLocus(locus, locusResults);
            });

            return results;
        }

        /// <summary>
        /// If locus contains a non-expressing allele, then return new object with scoring metadata of expressing typing copied to both positions,
        /// else return the original metadata.
        /// </summary>
        private LocusInfo<IHlaScoringMetadata> HandleNonExpressingAlleleIfAny(LocusInfo<IHlaScoringMetadata> metadata)
        {
            var nonExpressingCheck = metadata.Map(x => x != null && hlaCategorisationService.IsNullAllele(x.LookupName));

            return nonExpressingCheck.BothPositions(x => x == false)
                ? metadata
                : new LocusInfo<IHlaScoringMetadata>(
                    nonExpressingCheck.Position1 ? metadata.Position2 : metadata.Position1,
                    nonExpressingCheck.Position2 ? metadata.Position1 : metadata.Position2
                );
        }

        private LocusScoreResult<T> CalculateLocusScoreForBestOrientation(
            IReadOnlyCollection<MatchOrientation> orientations,
            LocusInfo<IHlaScoringMetadata> patientLocusData,
            LocusInfo<IHlaScoringMetadata> donorLocusData)
        {
            if (orientations.Count == 1)
            {
                var score = ScoreLocus(orientations.Single(), patientLocusData, donorLocusData);
                return new LocusScoreResult<T>(score, orientations);
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

        private LocusScoreResult<T> SelectBestOrientation(LocusInfo<T> directScore, LocusInfo<T> crossScore)
        {
            var difference = SumLocusScore(directScore) - SumLocusScore(crossScore);

            return difference switch
            {
                > 0 => new LocusScoreResult<T>(directScore, new[] { MatchOrientation.Direct }),
                < 0 => new LocusScoreResult<T>(crossScore, new[] { MatchOrientation.Cross }),
                _ => new LocusScoreResult<T>(directScore, new[] { MatchOrientation.Direct, MatchOrientation.Cross })
            };
        }

        protected abstract int SumLocusScore(LocusInfo<T> score);
    }
}