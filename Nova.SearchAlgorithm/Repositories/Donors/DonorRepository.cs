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
            // TODO:NOVA-919 query database of donor data to get donors
            // TODO:NOVA-931 implement matching
           
            var query = new TableQuery<DonorTableEntity>();

            return donorTable.ExecuteQuery(query).Select(dte => dte.ToSearchableDonor(mapper));
        }
    }
}
