using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nova.SearchAlgorithm.Repositories.Hla
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

        // TODO:NOVA-918 this is a temporary in-memory fix.
        // We should get data by querying an HLA matching database, which can be regenerated as necessary.
        private readonly IEnumerable<RawMatchingHla> rawMatchingData = ReadJsonFromFile();

        private static IEnumerable<RawMatchingHla> ReadJsonFromFile()
        {
            System.Reflection.Assembly assem = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assem.GetManifestResourceStream("Nova.SearchAlgorithm.Resources.matching_hla.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<IEnumerable<RawMatchingHla>> (reader.ReadToEnd());
                }
            }
        }

        public HlaRepository(IMapper mapper, ICloudTableFactory cloudTableFactory)
        {
            selectedHlaTable = cloudTableFactory.GetTable(TableReference);
            this.mapper = mapper;
        }

        // TODO:NOVA-918 does the dictonary match both type positions at once or only one?
        public MatchingHla RetrieveHlaMatches(string locusName, SingleLocusDetails<string> names)
        {
            var raw1 = rawMatchingData.FirstOrDefault(hla => hla.Locus == locusName && hla.Name == names.One);
            var raw2 = rawMatchingData.FirstOrDefault(hla => hla.Locus == locusName && hla.Name == names.Two);

            return new MatchingHla {
                Locus = locusName,
                MatchingProteinGroups = raw1.MatchingPGroups.Union(raw2.MatchingPGroups),
                MatchingSerologyNames = raw1.MatchingSerology.Select(s => s.Name).Union(raw2.MatchingSerology.Select(s => s.Name))
            };
        }
    }
}
