using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories.Hla
{
    public interface IHlaRepository
    {
        ExpandedHla RetrieveHlaMatches(Locus locusName, string hlaName);
        PhenotypeInfo<ExpandedHla> RetrieveHlaMatches(PhenotypeInfo<string> locusHla);
    }

    public class HlaRepository : IHlaRepository
    {
        private const string TableReference = "Hlas";
        private readonly CloudTable selectedHlaTable;
        private readonly IMapper mapper;

        // TODO:NOVA-928 this is a temporary in-memory solution based on a static file.
        // We will need to be able to regenerate the dictionary when needed, whether it remains as a file or moves into a DB.
        private readonly IEnumerable<RawMatchingHla> rawMatchingData = ReadJsonFromFile();

        private static IEnumerable<RawMatchingHla> ReadJsonFromFile()
        {
            System.Reflection.Assembly assem = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assem.GetManifestResourceStream("Nova.SearchAlgorithm.Resources.matching_hla.json"))
            {
                using (StreamReader reader = new StreamReader(stream))
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

        public ExpandedHla RetrieveHlaMatches(Locus locus, string hlaName)
        {
            var raw = hlaName == null
                ? Enumerable.Empty<RawMatchingHla>()
                : rawMatchingData.Where(hla => hla.Locus.Equals(locus.ToString(), StringComparison.InvariantCultureIgnoreCase) && hla.Name.StartsWith(hlaName));

            return new ExpandedHla {
                Name = hlaName,
                Locus = locus,
                PGroups = raw.SelectMany(r => r.MatchingPGroups).Distinct()
            };
        }

        public PhenotypeInfo<ExpandedHla> RetrieveHlaMatches(PhenotypeInfo<string> locusHla)
        {
            return locusHla.Map((locus, position, name) => RetrieveHlaMatches(locus, name));
        }
    }
}
