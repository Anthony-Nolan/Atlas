using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Donors.AzureStorage;
using System.Collections.Generic;
using System.Linq;


namespace Nova.SearchAlgorithm.Repositories.Donors
{
    public interface IDonorRepository
    {
        SearchableDonor GetDonor(int donorId);
        IEnumerable<SearchableDonor> AllDonors();
        IEnumerable<HlaMatch> GetDonorMatchesAtLocus(SearchType searchType, IEnumerable<RegistryCode> registries, string locus, LocusSearchCriteria criteria);
        void InsertDonor(SearchableDonor donor);
        void UpdateDonorWithNewHla(SearchableDonor donor);
    }

    static class TableQueryExtensions
    {
        public static TableQuery<TElement> AndWhere<TElement>(this TableQuery<TElement> @this, string filter)
        {
            @this.FilterString = TableQuery.CombineFilters(@this.FilterString, TableOperators.And, filter);
            return @this;
        }

        public static TableQuery<TElement> OrWhere<TElement>(this TableQuery<TElement> @this, string filter)
        {
            @this.FilterString = @this.FilterString == null ? filter : TableQuery.CombineFilters(@this.FilterString, TableOperators.Or, filter);
            return @this;
        }

        public static TableQuery<TElement> NotWhere<TElement>(this TableQuery<TElement> @this, string filter)
        {
            @this.FilterString = TableQuery.CombineFilters(@this.FilterString, TableOperators.Not, filter);
            return @this;
        }
    }

    public class DonorRepository : IDonorRepository
    {
        private const string TableReference = "Donors";
        private readonly CloudTable donorTable;
        private readonly IMapper mapper;

        public DonorRepository(IMapper mapper, ICloudTableFactory cloudTableFactory)
        {
            donorTable = cloudTableFactory.GetTable(TableReference);
            this.mapper = mapper;
        }
        
        public IEnumerable<HlaMatch> GetDonorMatchesAtLocus(SearchType searchType, IEnumerable<RegistryCode> registries, string locus, LocusSearchCriteria criteria)
        {
            var matchesFromPositionOne = GetMatches(locus, criteria.HlaNamesToMatchInPositionOne);
            var matchesFromPositionTwo = GetMatches(locus, criteria.HlaNamesToMatchInPositionTwo);

            return matchesFromPositionOne.Select(m => m.ToHlaMatch(1)).Union(matchesFromPositionTwo.Select(m => m.ToHlaMatch(2)));
        }

        private IEnumerable<HlaMatchTableEntity> GetMatches(string locus, IEnumerable<string> namesToMatch)
        {
            var matchesQuery = new TableQuery<HlaMatchTableEntity>();
            foreach (string name in namesToMatch)
            {
                matchesQuery = matchesQuery.OrWhere(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, HlaMatchTableEntity.GeneratePartitionKey(locus, name)));
            }

            return donorTable.ExecuteQuery(matchesQuery);
        }

        public SearchableDonor GetDonor(int donorId)
        {
            var donorQuery = new TableQuery<DonorTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, donorId.ToString()));
            return donorTable.ExecuteQuery(donorQuery).Select(dte => dte.ToSearchableDonor(mapper)).FirstOrDefault();
        }

        public void InsertDonor(SearchableDonor donor)
        {
            var insertDonor = TableOperation.Insert(donor.ToTableEntity(mapper));
            donorTable.Execute(insertDonor);

            donor.MatchingHla.Each((locusName, position, matchingHla) => InsertLocusMatch(locusName, position, matchingHla, donor.DonorId));

            // TODO:NOVA-929 if this method stays, sort out a return value
        }

        // TODO:NOVA-929 This will be too many donors
        // Can we stream them in batches with IEnumerable?
        public IEnumerable<SearchableDonor> AllDonors()
        {
            var query = new TableQuery<DonorTableEntity>();
            return donorTable.ExecuteQuery(query).Select(dte => dte.ToSearchableDonor(mapper));
        }

        public void UpdateDonorWithNewHla(SearchableDonor donor)
        {
            // Update the donor itself
            var insertDonor = TableOperation.InsertOrReplace(donor.ToTableEntity(mapper));
            donorTable.Execute(insertDonor);

            // First delete all the old matches
            var matchesQuery = new TableQuery<HlaMatchTableEntity>().Where(TableQuery.GenerateFilterConditionForInt("DonorId", QueryComparisons.Equal, donor.DonorId));
            foreach (var match in donorTable.ExecuteQuery(matchesQuery))
            {
                donorTable.Execute(TableOperation.Delete(match));
            }

            // Add back the new matches
            donor.MatchingHla.Each((locusName, position, matchingHla) => InsertLocusMatch(locusName, position, matchingHla, donor.DonorId));
        }

        private void InsertLocusMatch(string locusName, int typePosition, MatchingHla matchingHla, int donorId)
        {
            foreach (string matchName in matchingHla.MatchingProteinGroups.Union(matchingHla.MatchingSerologyNames))
            {
                var insertMatch = TableOperation.Insert(new HlaMatchTableEntity(locusName, typePosition, matchName, donorId));
                donorTable.Execute(insertMatch);
            }
        }
    }
}
