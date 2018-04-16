using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Donors.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors
{
    public interface IDonorBlobRepository
    {
        void InsertDonor(SearchableDonor donor);
        void UpdateDonorWithNewHla(SearchableDonor donor);
        SearchableDonor GetDonor(int donorId);
        IEnumerable<PotentialHlaMatchRelation> GetMatchesForDonor(int donorId);
        IEnumerable<SearchableDonor> AllDonors();
        IEnumerable<PotentialHlaMatchRelation> GetDonorMatchesAtLocus(SearchType searchType, IEnumerable<RegistryCode> registries, string locus, LocusSearchCriteria criteria);

    }

    static class MatchingHlaExtensions
    {
        public static IEnumerable<string> AllMatchingHlaNames(this MatchingHla hla)
        {
            return (hla.MatchingProteinGroups ?? Enumerable.Empty<string>()).Union(hla.MatchingSerologyNames ?? Enumerable.Empty<string>());
        }
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

    public class BlobDonorRepository : IDonorBlobRepository
    {
        private const string DonorTableReference = "Donors";
        private const string MatchTableReference = "Matches";
        private readonly CloudTable donorTable;
        private readonly CloudTable matchTable;
        private readonly IMapper mapper;

        public BlobDonorRepository(IMapper mapper, ICloudTableFactory cloudTableFactory)
        {
            donorTable = cloudTableFactory.GetTable(DonorTableReference);
            matchTable = cloudTableFactory.GetTable(MatchTableReference);
            this.mapper = mapper;
        }
        
        public IEnumerable<PotentialHlaMatchRelation> GetDonorMatchesAtLocus(SearchType searchType, IEnumerable<RegistryCode> registries, string locus, LocusSearchCriteria criteria)
        {
            var matchesFromPositionOne = GetMatches(locus, criteria.HlaNamesToMatchInPositionOne);
            var matchesFromPositionTwo = GetMatches(locus, criteria.HlaNamesToMatchInPositionTwo);

            return matchesFromPositionOne.Select(m => m.ToPotentialHlaMatchRelation(TypePositions.One)).Union(matchesFromPositionTwo.Select(m => m.ToPotentialHlaMatchRelation(TypePositions.Two)));
        }

        private IEnumerable<PotentialHlaMatchRelationTableEntity> GetMatches(string locus, IEnumerable<string> namesToMatch)
        {
            var matchesQuery = new TableQuery<PotentialHlaMatchRelationTableEntity>();
            foreach (string name in namesToMatch)
            {
                matchesQuery = matchesQuery.OrWhere(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PotentialHlaMatchRelationTableEntity.GeneratePartitionKey(locus, name)));
            }

            return matchTable.ExecuteQuery(matchesQuery);
        }

        public SearchableDonor GetDonor(int donorId)
        {
            var donorQuery = new TableQuery<DonorTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, donorId.ToString()));
            return donorTable.ExecuteQuery(donorQuery).Select(dte => dte.ToSearchableDonor(mapper)).FirstOrDefault();
        }

        public IEnumerable<PotentialHlaMatchRelation> GetMatchesForDonor(int donorId)
        {
            return AllMatchesForDonor(donorId).Select(m => m.ToPotentialHlaMatchRelation(0));
        }

        public void InsertDonor(SearchableDonor donor)
        {
            var insertDonor = TableOperation.InsertOrReplace(donor.ToTableEntity(mapper));
            donorTable.Execute(insertDonor);

            UpdateDonorHlaMatches(donor);

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
            UpdateDonorHlaMatches(donor);
        }

        private void UpdateDonorHlaMatches(SearchableDonor donor)
        {
            // First delete all the old matches
            var matches = AllMatchesForDonor(donor.DonorId);
            foreach (var match in matches)
            {
                matchTable.Execute(TableOperation.Delete(match));
            }

            // Add back the new matches
            donor.MatchingHla.EachLocus((locusName, matchingHla1, matchingHla2) => InsertLocusMatch(locusName, matchingHla1, matchingHla2, donor.DonorId));
        }

        private IEnumerable<PotentialHlaMatchRelationTableEntity> AllMatchesForDonor(int donorId)
        { 
            var matchesQuery = new TableQuery<PotentialHlaMatchRelationTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, donorId.ToString()));

            return matchTable.ExecuteQuery(matchesQuery);
        }

        private void InsertLocusMatch(string locusName, MatchingHla matchingHla1, MatchingHla matchingHla2, int donorId)
        {
            if (matchingHla1 == null)
            {
                return;
            }

            var list1 = matchingHla1.AllMatchingHlaNames();
            var list2 = matchingHla2.AllMatchingHlaNames();

            foreach (string matchName in list1.Union(list2))
            {
                TypePositions typePositions = (TypePositions.None);
                if (list1.Contains(matchName))
                {
                    typePositions |= TypePositions.One;
                }
                if (list2.Contains(matchName))
                {
                    typePositions |= TypePositions.Two;
                }
                var insertMatch = TableOperation.InsertOrMerge(new PotentialHlaMatchRelationTableEntity(locusName, typePositions, matchName, donorId));
                matchTable.Execute(insertMatch);
            }
        }
    }
}
