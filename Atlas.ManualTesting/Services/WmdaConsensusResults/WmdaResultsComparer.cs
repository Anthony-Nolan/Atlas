using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.ManualTesting.Common;
using Atlas.ManualTesting.Common.SubjectImport;
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

    internal class WmdaResultsComparer : IWmdaResultsComparer
    {
        private const string FileDelimiter = ";";
        private readonly IFileReader<WmdaConsensusResultsFile> resultsFileReader;
        private readonly IFileReader<ImportedSubject> subjectFileReader;
        private record CombinedScoringResult(WmdaConsensusResultsFile Consensus, WmdaConsensusResultsFile Result);

        public WmdaResultsComparer(
            IFileReader<WmdaConsensusResultsFile> resultsFileReader,
            IFileReader<ImportedSubject> subjectFileReader)
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

        private static IEnumerable<(Locus, MismatchCountDetails)> GetLociWithDifferentMismatchCounts(WmdaConsensusResultsFile consensus, WmdaConsensusResultsFile atlasResult)
        {
            var details = new List<(Locus, MismatchCountDetails)>();

            void CheckLocusMismatchCounts(string consensusMismatchCount, string resultMismatchCount, Locus locus)
            {
                if (consensusMismatchCount != resultMismatchCount) details.Add(new ValueTuple<Locus, MismatchCountDetails>(locus,
                    new MismatchCountDetails
                    {
                        PatientId = consensus.PatientId,
                        DonorId = consensus.DonorId,
                        ConsensusMismatchCount = consensusMismatchCount,
                        AtlasMismatchCount = resultMismatchCount
                    }));
            }

            CheckLocusMismatchCounts(consensus.MismatchCountAtA, atlasResult.MismatchCountAtA, Locus.A);
            CheckLocusMismatchCounts(consensus.MismatchCountAtB, atlasResult.MismatchCountAtB, Locus.B);
            CheckLocusMismatchCounts(consensus.MismatchCountAtDrb1, atlasResult.MismatchCountAtDrb1, Locus.Drb1);

            return details;
        }
    }
}