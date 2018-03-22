using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.SearchRequests.AzureStorage;
using System.Collections.Generic;


namespace Nova.SearchAlgorithm.Repositories.SearchRequests
{
    public interface IHlaRepository
    {
        void RetrieveHlaMatches();
    }

    public class HlaRepository : IHlaRepository
    {
        private const string TableReference = "Hlas";
        private readonly CloudTable selectedHlaTable;
        private readonly IMapper mapper;

        public HlaRepository(IMapper mapper, ICloudTableFactory cloudTableFactory)
        {
            selectedHlaTable = cloudTableFactory.GetTable(TableReference);
            this.mapper = mapper;
        }

        public void RetrieveHlaMatches()
        {
            // TODO:NOVA-931 implement query to get p-groups and serologies for given HLA data
        }
    }
}
