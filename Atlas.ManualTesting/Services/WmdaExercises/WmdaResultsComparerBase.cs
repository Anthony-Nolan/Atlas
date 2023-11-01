using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Models;

namespace Atlas.ManualTesting.Services.WmdaConsensusResults
{
    public class WmdaComparerResult
    {
        public Locus Locus { get; set; }
        public LocusInfo<string> PatientHla { get; set; }
        public LocusInfo<string> DonorHla { get; set; }
        public MismatchCountDetails MismatchCountDetails { get; set; }
    }

    public class MismatchCountDetails
    {
        public string PatientId { get; set; }
        public string DonorId { get; set; }
        public string ConsensusMismatchCount { get; set; }
        public string AtlasMismatchCount { get; set; }
    }

    public interface IWmdaResultsComparer
    {
        /// <summary>
        /// Requires the consensus file and results file to both have the same number of lines and be ordered in the same way, by patient id and donor id.
        /// </summary>
        Task<IEnumerable<WmdaComparerResult>> GetDiscrepantResults(ReportDiscrepanciesRequest request);
    }

    internal abstract class WmdaResultsComparerBase<TResultsFile> : IWmdaResultsComparer where TResultsFile : WmdaConsensusResultsFile
    {
        private const string FileDelimiter = ";";
        private readonly IFileReader<TResultsFile> resultsFileReader;
        private readonly IFileReader<ImportedSubject> subjectFileReader;
        private record CombinedScoringResult(TResultsFile Consensus, TResultsFile Result);

        protected WmdaResultsComparerBase(IFileReader<TResultsFile> resultsFileReader, IFileReader<ImportedSubject> subjectFileReader)
        {
            this.resultsFileReader = resultsFileReader;
            this.subjectFileReader = subjectFileReader;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<WmdaComparerResult>> GetDiscrepantResults(ReportDiscrepanciesRequest request)
        {
            var comparerResults = new List<WmdaComparerResult>();

            var combinedResults = GetCombinedResults(request);
            var patients = await ImportSubjects(request.PatientFilePath);
            var donors = await ImportSubjects(request.DonorFilePath);

            await foreach (var combination in combinedResults)
            {
                var consensusStr = combination.Consensus.ToString();
                var resultStr = combination.Result.ToString();
                if (string.Equals(consensusStr, resultStr))
                {
                    continue;
                }

                var patient = patients[combination.Consensus.PatientId].ToPhenotypeInfo();
                var donor = donors[combination.Consensus.DonorId].ToPhenotypeInfo();

                var lociWithDiscrepantCounts = GetLociWithDifferentMismatchCounts(combination.Consensus, combination.Result);

                comparerResults.AddRange(lociWithDiscrepantCounts.Select(countDetails => new WmdaComparerResult
                {
                    Locus = countDetails.Item1,
                    PatientHla = patient.GetLocus(countDetails.Item1),
                    DonorHla = donor.GetLocus(countDetails.Item1),
                    MismatchCountDetails = countDetails.Item2
                }));
            }

            return comparerResults;
        }

        private IAsyncEnumerable<CombinedScoringResult> GetCombinedResults(ReportDiscrepanciesRequest request)
        {
            var consensus = resultsFileReader.ReadAsync(FileDelimiter, request.ConsensusFilePath);
            var results = resultsFileReader.ReadAsync(FileDelimiter, request.ResultsFilePath);
            return consensus.Zip(results, (c, r) => new CombinedScoringResult(c, r));
        }

        private async Task<IDictionary<string, ImportedSubject>> ImportSubjects(string filePath)
        {
            return (await subjectFileReader.ReadAllLines(FileDelimiter, filePath)).ToDictionary(s => s.ID, s => s);
        }

        private IEnumerable<(Locus, MismatchCountDetails)> GetLociWithDifferentMismatchCounts(TResultsFile consensus, TResultsFile atlasResult)
        {
            var details = new List<(Locus, MismatchCountDetails)>();

            var lociToCompare = new[] { Locus.A, Locus.B, Locus.Drb1 };
            var consensusCounts = SelectLocusMismatchCounts(consensus);
            var atlasCounts = SelectLocusMismatchCounts(atlasResult);

            foreach (var locus in lociToCompare)
            {
                var consensusMismatchCount = consensusCounts[locus];
                var interpretedConsensusCounts = InterpretConsensusMismatchCount(consensusMismatchCount);
                var atlasMismatchCount = atlasCounts[locus];

                if (!interpretedConsensusCounts.Contains(atlasMismatchCount)) details.Add((
                    locus,
                    new MismatchCountDetails
                    {
                        PatientId = consensus.PatientId,
                        DonorId = consensus.DonorId,
                        ConsensusMismatchCount = consensusMismatchCount,
                        AtlasMismatchCount = atlasMismatchCount
                    }));
            }

            return details;
        }

        private static IEnumerable<string> InterpretConsensusMismatchCount(string consensusMismatchCount)
        {
            switch (consensusMismatchCount)
            {
                case "0":
                case "1":
                case "2":
                    return new[] { consensusMismatchCount };
                case "A":
                case "C":
                case "G":
                    return new[] { "0", "1" };
                case "B":
                case "E":
                case "H":
                    return new[] { "0", "2" };
                case "D":
                case "F":
                case "I":
                    return new[] { "1", "2" };
                case "U":
                case "V":
                case "W":
                case "Z":
                    return new[] { "0", "1", "2" };
                default:
                    throw new ArgumentOutOfRangeException(nameof(consensusMismatchCount), consensusMismatchCount);
            }
        }

        /// <summary>
        /// Select out locus mismatch counts for comparison
        /// </summary>
        /// <returns>Dictionary with Locus as key, and mismatch count as value</returns>
        protected abstract IDictionary<Locus, string> SelectLocusMismatchCounts(TResultsFile results);
    }
}