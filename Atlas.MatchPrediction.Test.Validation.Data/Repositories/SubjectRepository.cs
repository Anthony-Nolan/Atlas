using Atlas.Common.Sql.BulkInsert;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using ValidationContext = Atlas.MatchPrediction.Test.Validation.Data.Context.MatchPredictionValidationContext;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories
{
    public interface ISubjectRepository : IBulkInsertRepository<SubjectInfo>
    {
        Task<IEnumerable<SubjectInfo>> GetPatients(int firstPatientId = 0);
        Task<IEnumerable<SubjectInfo>> GetDonors();
        Task<SubjectInfo?> GetByExternalId(string externalId);
    }

    public class SubjectRepository : BulkInsertRepository<SubjectInfo>, ISubjectRepository
    {
        private const string TableName = nameof(ValidationContext.SubjectInfo);

        public SubjectRepository(string connectionString) : base(connectionString, TableName)
        {
        }

        public async Task<IEnumerable<SubjectInfo>> GetPatients(int firstPatientId = 0)
        {
            if (firstPatientId == 0)
            {
                return await GetAllSubjects(SubjectType.Patient);
            }

            var patientType = SubjectType.Patient.ToString();
            const string sql = @$"SELECT * FROM {TableName} WHERE
                {nameof(SubjectInfo.SubjectType)} = @{nameof(patientType)} AND
                {nameof(SubjectInfo.Id)} >= @{nameof(firstPatientId)}";

            await using (var conn = new SqlConnection(ConnectionString))
            {
                return await conn.QueryAsync<SubjectInfo>(sql, new { patientType, firstPatientId });
            }
        }

        public async Task<IEnumerable<SubjectInfo>> GetDonors()
        {
            return await GetAllSubjects(SubjectType.Donor);
        }

        /// <inheritdoc />
        public async Task<SubjectInfo?> GetByExternalId(string externalId)
        {
            const string sql = @$"SELECT * FROM {TableName} WHERE {nameof(SubjectInfo.ExternalId)} = @{nameof(externalId)}";

            await using (var conn = new SqlConnection(ConnectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<SubjectInfo>(sql, new { externalId });
            }
        }

        private async Task<IEnumerable<SubjectInfo>> GetAllSubjects(SubjectType subjectType)
        {
            var param = subjectType.ToString();
            const string sql = @$"SELECT * FROM {TableName} WHERE {nameof(SubjectInfo.SubjectType)} = @{nameof(param)}";

            await using (var conn = new SqlConnection(ConnectionString))
            {
                return await conn.QueryAsync<SubjectInfo>(sql, new { param });
            }
        }
    }
}
