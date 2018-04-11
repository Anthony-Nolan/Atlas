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
        IEnumerable<SearchableDonor> MatchDonors(SearchCriteria criteria);
        void InsertDonor(ImportDonor donor);
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

        public IEnumerable<SearchableDonor> MatchDonors(SearchCriteria criteria)
        {
            // TODO:NOVA-931 implement matching
            var donorQuery = new TableQuery<DonorTableEntity>();

            return donorTable.ExecuteQuery(donorQuery).Select(dte => dte.ToSearchableDonor(mapper));
        }

        public void InsertDonor(ImportDonor donor)
        {
            var insertDonor = TableOperation.Insert(donor.ToTableEntity(mapper));
            donorTable.Execute(insertDonor);

            donor.MatchingHla.Each((locusName, position, matchingHla) => InsertLocusMatch(locusName, position, matchingHla, donor.DonorId));

            // TODO:NOVA-929 if this method stays, sort out a return value
        }

        public void UpdateDonorWithNewHla(ImportDonor donor)
        {
            // TODO:NOVA-929 implment for the (daily?) donor update process
            // It should include removing any HlaMatchTableEntities which no longer apply
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
