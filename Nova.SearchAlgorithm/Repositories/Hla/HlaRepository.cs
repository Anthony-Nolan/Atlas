using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Models;
using System.Collections.Generic;


namespace Nova.SearchAlgorithm.Repositories.Hlas
{
    public interface IHlaRepository
    {
        MatchingHla RetrieveHlaMatches(string locusName, SingleLocusDetails<string> names);
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

        public MatchingHla RetrieveHlaMatches(string locusName, SingleLocusDetails<string> names)
        {
            // TODO:NOVA-918 get data by querying an HLA matching database
            return new MatchingHla {
                Locus = locusName,
                Type = "Allele",
                IsDeleted = false,
                MatchingProteinGroups = new List<string> { "01:01P" },
                MatchingSerologyNames = new List<string> { "1" }
            };
        }
    }
}
