using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.SearchRequests.AzureStorage;
using System.Collections.Generic;


namespace Nova.SearchAlgorithm.Repositories.SearchRequests
{
    public interface IDonorRepository
    {
        IEnumerable<Donor> MatchDonors(MatchCriteria criteria);
    }

    public class DonorRepository : IDonorRepository
    {
        private const string TableReference = "Donors";
        private readonly CloudTable selectedDonorTable;
        private readonly IMapper mapper;

        public DonorRepository(IMapper mapper, ICloudTableFactory cloudTableFactory)
        {
            selectedDonorTable = cloudTableFactory.GetTable(TableReference);
            this.mapper = mapper;
        }

        public IEnumerable<Donor> MatchDonors(MatchCriteria criteria)
        {
            // TODO:NOVA-931 implement matching
            return new List<Donor>();
        }
    }
}
