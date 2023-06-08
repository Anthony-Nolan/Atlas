using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.ManualTesting.Models;
using Atlas.MatchPrediction.Models.FileSchema;

namespace Atlas.ManualTesting.Services.HaplotypeFrequencySet
{
    public record TransformedHaplotypeFrequencySet(
        FrequencySetFileSchema Set,
        IEnumerable<FrequencyRecord> OriginalRecordsContainingTarget,
        int OriginalRecordCount);

    public interface IHaplotypeFrequencySetTransformer
    {
        /// <returns>The original <paramref name="set"/> object, with an updated collection of <see cref="FrequencySetFileSchema.Frequencies"/>.</returns>
        TransformedHaplotypeFrequencySet TransformHaplotypeFrequencySet(FrequencySetFileSchema set, FindReplaceHlaNames hlaNames);
    }

    internal class HaplotypeFrequencySetTransformer : IHaplotypeFrequencySetTransformer
    {
        private readonly ICollection<FrequencyRecord> originalRecordsContainingTarget = new List<FrequencyRecord>();

        public TransformedHaplotypeFrequencySet TransformHaplotypeFrequencySet(FrequencySetFileSchema set, FindReplaceHlaNames hlaNames)
        {
            if (set.Frequencies.IsNullOrEmpty())
            {
                throw new ArgumentException($"No haplotype frequencies found in provided set.");
            }

            var originalRecordCount = set.Frequencies.Count();
            var transformedRecords = TransformFrequencyRecords(hlaNames, set.Frequencies);
            set.Frequencies = transformedRecords;
            return new TransformedHaplotypeFrequencySet(set, originalRecordsContainingTarget, originalRecordCount);
        }

        private IEnumerable<FrequencyRecord> TransformFrequencyRecords(FindReplaceHlaNames hlaNames, IEnumerable<FrequencyRecord> frequencyRecords)
        {
            return frequencyRecords
                .Select(f => TransformFrequencyRecord(hlaNames, f))
                .GroupBy(BuildFiveLocusHlaString)
                .Select(grp => UpdateFrequency(grp.First(), grp.Sum(f => f.Frequency)))
                .OrderByDescending(r => r.Frequency)
                .ToList();
        }

        private FrequencyRecord TransformFrequencyRecord(FindReplaceHlaNames hlaNames, FrequencyRecord frequencyRecord)
        {
            if (GetHlaNameAtLocus(hlaNames.Locus, frequencyRecord) != hlaNames.TargetHlaName)
            {
                return frequencyRecord;
            }

            originalRecordsContainingTarget.Add(new FrequencyRecord
            {
                A = frequencyRecord.A,
                B = frequencyRecord.B,
                C = frequencyRecord.C,
                Dqb1 = frequencyRecord.Dqb1,
                Drb1 = frequencyRecord.Drb1,
                Frequency = frequencyRecord.Frequency
            });

            return UpdateHlaNameAtLocus(hlaNames.Locus, frequencyRecord, hlaNames.ReplacementHlaName);
        }

        private static string GetHlaNameAtLocus(Locus locus, FrequencyRecord frequencyRecord)
        {
            switch (locus)
            {
                case Locus.A:
                    return frequencyRecord.A;
                case Locus.B:
                    return frequencyRecord.B;
                case Locus.C:
                    return frequencyRecord.C;
                case Locus.Dqb1:
                    return frequencyRecord.Dqb1;
                case Locus.Drb1:
                    return frequencyRecord.Drb1;
                case Locus.Dpb1:
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        private static FrequencyRecord UpdateHlaNameAtLocus(Locus locus, FrequencyRecord frequencyRecord, string replacementHlaName)
        {
            switch (locus)
            {
                case Locus.A:
                    frequencyRecord.A = replacementHlaName;
                    break;
                case Locus.B:
                    frequencyRecord.B = replacementHlaName;
                    break;
                case Locus.C:
                    frequencyRecord.C = replacementHlaName;
                    break;
                case Locus.Dqb1:
                    frequencyRecord.Dqb1 = replacementHlaName;
                    break;
                case Locus.Drb1:
                    frequencyRecord.Drb1 = replacementHlaName;
                    break;
                case Locus.Dpb1:
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return frequencyRecord;
        }

        private static string BuildFiveLocusHlaString(FrequencyRecord frequencyRecord)
        {
            return $"{frequencyRecord.A}~{frequencyRecord.B}~{frequencyRecord.C}~{frequencyRecord.Dqb1}~{frequencyRecord.Drb1}";
        }

        private static FrequencyRecord UpdateFrequency(FrequencyRecord frequencyRecord, decimal frequency)
        {
            frequencyRecord.Frequency = frequency;
            return frequencyRecord;
        }
    }
}