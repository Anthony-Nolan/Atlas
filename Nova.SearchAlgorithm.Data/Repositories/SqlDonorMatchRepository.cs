using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Models.Extensions;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public interface IDonorSearchRepository
    {
        IEnumerable<PotentialSearchResult> Search(DonorMatchCriteria matchRequest);
    }

    public interface IDonorImportRepository
    {
        /// <summary>
        /// If a donor with the given DonorId already exists, update the HLA and refresh the pre-processed matching groups.
        /// Otherwise, insert the donor and generate the matching groups.
        /// </summary>
        Task AddOrUpdateDonor(InputDonor donor);

        /// <summary>
        /// Refreshes the pre-processed matching groups for a single donor, for example if the HLA matching dictionary has been updated.
        /// </summary>
        Task RefreshMatchingGroupsForExistingDonor(InputDonor donor);
    }

    public interface IDonorInspectionRepository
    {
        Task<int> HighestDonorId();
        IEnumerable<DonorResult> AllDonors();
        DonorResult GetDonor(int donorId);
    }

    public class SqlDonorSearchRepository : IDonorSearchRepository, IDonorImportRepository, IDonorInspectionRepository
    {
        private readonly SearchAlgorithmContext context;

        public SqlDonorSearchRepository(SearchAlgorithmContext context)
        {
            this.context = context;
        }

        public Task<int> HighestDonorId()
        {
            return context.Donors.OrderByDescending(d => d.DonorId).Take(1).Select(d => d.DonorId).FirstOrDefaultAsync();
        }

        public IEnumerable<DonorResult> AllDonors()
        {
            return context.Donors.ToList().Select(d => d.ToRawDonor());
        }

        public DonorResult GetDonor(int donorId)
        {
            return context.Donors.FirstOrDefault(d => d.DonorId == donorId)?.ToRawDonor();
        }

        public async Task AddOrUpdateDonor(InputDonor donor)
        {
            var result = await context.Donors.FirstOrDefaultAsync(d => d.DonorId == donor.DonorId);
            if (result == null)
            {
                context.Donors.Add(donor.ToDonorEntity());
            }
            else
            {
                result.CopyRawHlaFrom(donor);
                await context.SaveChangesAsync();
                await RefreshMatchingGroupsForExistingDonor(donor);
            }

            await RefreshMatchingGroupsForExistingDonor(donor);

            await context.SaveChangesAsync();
        }

        public async Task RefreshMatchingGroupsForExistingDonor(InputDonor donor)
        {
            foreach (Locus locus in Enum.GetValues(typeof(Locus)).Cast<Locus>())
            {
                context.Database.ExecuteSqlCommand(
                    $@"DELETE FROM MatchingHlaAt{locus.ToString().ToUpper()} WHERE DonorId = {donor.DonorId}");
            }

            donor.MatchingHla.EachPosition((l, p, h) => InsertPGroupMatches(donor.DonorId, l, p, h));

            await context.SaveChangesAsync();
        }

        public void InsertPGroupMatches(int donorId, Locus locus, TypePositions position, ExpandedHla hla)
        {
            var table = context.MatchingHlasAtLocus(locus);
            if (hla?.PGroups != null)
            {
                table.AddRange(
                    hla.PGroups.Select(pg => {
                        var match = MatchingHla.EmptyMatchingEntityForLocus(locus);
                        match.DonorId = donorId;
                        match.PGroup = FindOrCreatePGroup(pg);
                        match.TypePosition = (int)position;
                    return match;
                    })
                );
            }
        }

        private PGroupName FindOrCreatePGroup(string pGroupName)
        {
            var existing = context.PGroupNames.FirstOrDefault(pg => pg.Name == pGroupName);

            if (existing != null)
            {
                return existing;
            }

            var newPGroup = context.PGroupNames.Add(new PGroupName { Name = pGroupName });
            context.SaveChanges();
            return newPGroup;
        }

        public IEnumerable<PotentialSearchResult> Search(DonorMatchCriteria matchRequest)
        {
            string sql = $@"SELECT DonorId, SUM(MatchCount) AS TotalMatchCount
FROM (
    -- get overall match count by Locus
    SELECT DonorId, Locus, MIN(MatchCount) AS MatchCount
    FROM (
        -- count number of matches in each direction
        SELECT DonorId, MatchingDirection, Locus, count(*) AS MatchCount
        FROM(
            -- get DISTINCT list of matches between search and donor type by Locus and position
            SELECT DISTINCT DonorId, MatchingDirection, Locus, TypePosition
            FROM (
                -- Select search and donor directional match lists by Locus & matching hla name
                -- First from type position 1 in the search hla
				{SelectForLocus(Locus.A, matchRequest.LocusMismatchA, TypePositions.One)}
                UNION
				{SelectForLocus(Locus.B, matchRequest.LocusMismatchB, TypePositions.One)}
                UNION
				{SelectForLocus(Locus.Drb1, matchRequest.LocusMismatchDRB1, TypePositions.One)}
                UNION
                -- Next from type position 2 in the search hla
				{SelectForLocus(Locus.A, matchRequest.LocusMismatchA, TypePositions.Two)}
                UNION
				{SelectForLocus(Locus.B, matchRequest.LocusMismatchB, TypePositions.Two)}
                UNION
				{SelectForLocus(Locus.Drb1, matchRequest.LocusMismatchDRB1, TypePositions.Two)}
                ) AS source
            UNPIVOT (TypePosition FOR MatchingDirection IN (GvH, HvG)) AS unpivoted
            ) ByDirection
        GROUP BY DonorId, MatchingDirection, Locus
        ) ByLocus
    GROUP BY DonorId, Locus
    ) ByDonor
GROUP BY DonorId
HAVING SUM(MatchCount) >= {6 - matchRequest.DonorMismatchCount}
ORDER BY TotalMatchCount DESC";
        
            // TODO:NOVA-1171 fix mapping from sql to PotentialSearchResult
            return context.Database.SqlQuery<FlatSearchQueryResult>(sql).Select(fr => fr.ToPotentialSearchResult());
        }

        private string SelectForLocus(Locus locus, DonorLocusMatchCriteria mismatch, TypePositions typePosition)
        {
            var names = typePosition.Equals(TypePositions.One) ? mismatch.HlaNamesToMatchInPositionOne : mismatch.HlaNamesToMatchInPositionTwo;
            return $@"SELECT d.DonorId, '{locus.ToString().ToUpper()}' as Locus, d.TypePosition AS GvH, {(int)typePosition} AS HvG
                      FROM MatchingHlaAt{locus.ToString().ToUpper()} d
                      JOIN dbo.PGroupNames p ON p.Id = d.PGroup_Id 
                      WHERE [Name] IN('{string.Join("', '", names)}')
                      GROUP BY d.DonorId, d.TypePosition";
        }
    }
}