using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface IVerificationResultsRepository
    {
        Task<IEnumerable<PdpPrediction>> GetMaskedPdpPredictions(PdpPredictionsRequest request);
        Task<IEnumerable<PatientDonorPair>> GetMatchedGenotypePdpsForCrossLociPrediction(MatchedPdpsRequest request);
        Task<IEnumerable<PatientDonorPair>> GetMatchedGenotypePdpsForSingleLocusPrediction(SingleLocusMatchedPdpsRequest request);
    }

	public class VerificationResultsRepository : IVerificationResultsRepository
    {
        private readonly string connectionString;

        public VerificationResultsRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<IEnumerable<PdpPrediction>> GetMaskedPdpPredictions(PdpPredictionsRequest request)
        {
            const string nullLocusValue = "CrossLoci";
            var locus = request.Locus == null ? nullLocusValue : request.Locus.ToString();

            var sql = @$"
                SELECT 
				    ps.SourceSimulantId AS {nameof(PdpPrediction.PatientGenotypeSimulantId)},
				    ds.SourceSimulantId AS {nameof(PdpPrediction.DonorGenotypeSimulantId)},
				    p.Probability AS {nameof(PdpPrediction.Probability)}
			    FROM SearchRequests r
			    JOIN MatchedDonors d
			    ON r.Id = d.SearchRequestRecord_Id
			    JOIN MatchProbabilities p
			    ON d.Id = p.MatchedDonor_Id
			    JOIN Simulants ps
			    ON r.PatientId = ps.Id
			    JOIN Simulants ds
			    ON d.MatchedDonorSimulant_Id = ds.Id
			    WHERE 
				    r.SearchResultsRetrieved = 1 AND
				    ps.SimulatedHlaTypingCategory = '{SimulatedHlaTypingCategory.Masked}' AND
				    ds.SimulatedHlaTypingCategory = '{SimulatedHlaTypingCategory.Masked}' AND
				    r.VerificationRun_Id = @{nameof(request.VerificationRunId)} AND
				    p.MismatchCount = @{nameof(request.MismatchCount)} AND
				    ISNULL(p.Locus, '{nullLocusValue}') = @{nameof(locus)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<PdpPrediction>(
                    sql, new { request.VerificationRunId, request.MismatchCount, locus }, commandTimeout: 180);
            }
        }

        public async Task<IEnumerable<PatientDonorPair>> GetMatchedGenotypePdpsForCrossLociPrediction(MatchedPdpsRequest request)
        {
            var sql = @$"
                SELECT 
				    ps.Id AS {nameof(PatientDonorPair.PatientGenotypeSimulantId)},
				    ds.Id AS {nameof(PatientDonorPair.DonorGenotypeSimulantId)}
			    FROM SearchRequests r
			    JOIN MatchedDonors d
			    ON r.Id = d.SearchRequestRecord_Id
			    JOIN Simulants ps
			    ON r.PatientId = ps.Id
			    JOIN Simulants ds
			    ON d.MatchedDonorSimulant_Id = ds.Id
			    WHERE 
				    r.SearchResultsRetrieved = 1 AND
				    r.VerificationRun_Id = @{nameof(request.VerificationRunId)} AND
				    ps.SimulatedHlaTypingCategory = '{SimulatedHlaTypingCategory.Genotype}' AND
				    ds.SimulatedHlaTypingCategory = '{SimulatedHlaTypingCategory.Genotype}' AND
				    d.TotalMatchCount = @{nameof(request.MatchCount)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<PatientDonorPair>(
                    sql, new { request.VerificationRunId, request.MatchCount }, commandTimeout: 180);
            }
        }

        public async Task<IEnumerable<PatientDonorPair>> GetMatchedGenotypePdpsForSingleLocusPrediction(SingleLocusMatchedPdpsRequest request)
        {
            var locus = request.Locus.ToString();

            var sql = @$"
                SELECT 
				    ps.Id AS {nameof(PatientDonorPair.PatientGenotypeSimulantId)},
				    ds.Id AS {nameof(PatientDonorPair.DonorGenotypeSimulantId)}
			    FROM SearchRequests r
			    JOIN MatchedDonors d
			    ON r.Id = d.SearchRequestRecord_Id
			    JOIN Simulants ps
			    ON r.PatientId = ps.Id
			    JOIN Simulants ds
			    ON d.MatchedDonorSimulant_Id = ds.Id
                JOIN MatchCounts mc
                ON d.Id = mc.MatchedDonor_Id
			    WHERE 
				    r.SearchResultsRetrieved = 1 AND
				    r.VerificationRun_Id = @{nameof(request.VerificationRunId)} AND
				    ps.SimulatedHlaTypingCategory = '{SimulatedHlaTypingCategory.Genotype}' AND
				    ds.SimulatedHlaTypingCategory = '{SimulatedHlaTypingCategory.Genotype}' AND
                    mc.Locus = @{nameof(locus)} AND
				    mc.MatchCount = @{nameof(request.MatchCount)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<PatientDonorPair>(
                    sql, new { request.VerificationRunId, request.MatchCount, locus }, commandTimeout: 180);
            }
		}
    }
}