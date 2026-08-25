using System.Collections.Generic;
using Atlas.Common.Matching.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

// P groups, or absent. Not "P groups or G groups" - the remarks on CalculateMatchCounts_Fast below give the evidence.
using PGroupGenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Services.MatchCalculation
{
    public interface IMatchCalculationService
    {
        /// <returns>
        /// null for non calculated, 0, 1, or 2 if calculated representing the match count.
        ///
        /// Patient genotype and donor genotype *MUST* be typed to <b>P group</b> resolution, or be absent at a
        /// position. A null-expressing allele has no P group, and is handled by carrying its paired allele's P group
        /// into the null position - not by leaving a G group there. See remarks.
        /// </returns>
        /// <remarks>
        /// <para>
        /// It is tempting to write "P groups, or G groups when a null allele is present" here, and that is wrong. It
        /// contradicts <c>Atlas.Common.Matching.Services.IStringBasedLocusMatchCalculator.MatchCount</c> - the method
        /// this one hands every locus to - whose own comment says it <i>"will *NOT* give accurate results for any
        /// resolution other than P-Group"</i>. The stricter one is right, on four independent grounds:
        /// </para>
        /// <para>
        /// 1. <b>The nomenclature.</b> A G group is alleles sharing a nucleotide sequence over the antigen binding
        /// domain; a P group is alleles sharing a <i>protein</i> sequence over it. Identical DNA implies identical
        /// protein, so a G group's expressing alleles all fall in one P group - G <b>refines</b> P. Comparing G group
        /// names by string equality therefore calls a mismatch whenever two typings share a P group but sit in
        /// different G groups, which is a <b>false mismatch</b>: <c>README_HlaMetadataDictionary.md</c> line 55 has a
        /// P-group match as the <i>minimum</i> requirement for an allele-level match (WMDA "AI3"). Mixing the two
        /// resolutions in one comparison is worse again, since a G group name never equals a P group name.
        /// </para>
        /// <para>
        /// 2. <b>The specification.</b> <c>README_MatchPredictionAlgorithm.md</c> lines 56-59: <i>"Match counts are
        /// determined by comparing P Group values"</i>; null-expressing alleles use <i>"the P group of its paired
        /// allele ... in keeping with the logic used in the matching algorithm"</i>; and a non-P-group HF set
        /// <i>"must first be converted to P groups"</i>, every permitted resolution converting to exactly 1 or 0 P
        /// groups. <c>README_HlaMetadataDictionary.md</c> line 60 says the same.
        /// </para>
        /// <para>
        /// 3. <b>The code.</b> The one production producer is
        /// <c>GenotypeConverter.ConvertGenotypeToPGroups</c>, whose every branch either passes through an
        /// already-P-group name or converts to a P group, and which then runs
        /// <c>CopyExpressingAllelesToNullPositions</c>. So it emits a P group or a null, never a G group; a locus
        /// null at both positions stays null and is treated as untyped downstream.
        /// </para>
        /// <para>
        /// 4. <b>What can mislead a reader into the wrong rule.</b> Two things, and both are the same mistake.
        /// <c>MatchCalculationTests</c> and <c>MatchCalculationPerformanceTests</c> feed this method G groups
        /// (<c>UnambiguousAlleleDetails.GGroups()</c>) - which proves G-group input <i>runs</i>, not that it is
        /// correct, because every case gives both sides the same string and so cannot detect a false mismatch. And
        /// "P group, or G group where a null allele meant no P group existed" is verbatim the description of
        /// <c>GenotypeAtDesiredResolutions.HaplotypeResolution</c>, the <i>sibling</i> property of the one that feeds
        /// this method. That sentence describes the storage resolution and does not belong on this member.
        /// </para>
        /// </remarks>
        public LociInfo<int?> CalculateMatchCounts_Fast(
            PGroupGenotype patientGenotype,
            PGroupGenotype donorGenotype,
            ISet<Locus> allowedLoci);
    }

    internal class MatchCalculationService : IMatchCalculationService
    {
        private readonly IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator;

        public MatchCalculationService(IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator)
        {
            this.stringBasedLocusMatchCalculator = stringBasedLocusMatchCalculator;
        }

        // This method will be called millions of times in match prediction, and needs to stay as fast as possible.
        // The explicit, unrolled form below is deliberate: it avoids the per-call closure allocation, the six
        // Func<Locus, int?> delegate invocations, and the GetLocus(..) switch lookups that the lambda-based
        // LociInfo constructor incurred. Accessing the A/B/C.. properties directly is allocation-free and inlinable.
        public LociInfo<int?> CalculateMatchCounts_Fast(
            PGroupGenotype patientGenotype,
            PGroupGenotype donorGenotype,
            ISet<Locus> allowedLoci)
        {
            return new LociInfo<int?>(
                valueA: allowedLoci.Contains(Locus.A) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.A, donorGenotype.A) : null,
                valueB: allowedLoci.Contains(Locus.B) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.B, donorGenotype.B) : null,
                valueC: allowedLoci.Contains(Locus.C) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.C, donorGenotype.C) : null,
                valueDpb1: allowedLoci.Contains(Locus.Dpb1) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.Dpb1, donorGenotype.Dpb1) : null,
                valueDqb1: allowedLoci.Contains(Locus.Dqb1) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.Dqb1, donorGenotype.Dqb1) : null,
                valueDrb1: allowedLoci.Contains(Locus.Drb1) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.Drb1, donorGenotype.Drb1) : null
            );
        }
    }
}