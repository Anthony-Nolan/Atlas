using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Models;
using System.Collections.Generic;


namespace Nova.SearchAlgorithm.Repositories.Hlas
{
    public interface IHlaRepository
    {
        // TODO:NOVA-931 input the allele or other HLA search data to match
        IEnumerable<MatchingHla> RetrieveHlaMatches();
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

        public IEnumerable<MatchingHla> RetrieveHlaMatches()
        {
            // TODO:NOVA-918 get data by querying an HLA matching database
            yield return new MatchingHla {
                Locus = "A",
                Name = "01:01:01:01",
                Type = "Allele",
                IsDeleted = false,
                MatchingProteinGroups = new List<string> { "01:01P" },
                MatchingSerologies = new List<Serology> { new Serology { Name = "1", SubType = 0} }
            };
        }
    }
}
