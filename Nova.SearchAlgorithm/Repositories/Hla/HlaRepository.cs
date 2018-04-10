using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nova.SearchAlgorithm.Repositories.Hla
{
    public interface IHlaRepository
    {
        MatchingHla RetrieveHlaMatches(string locusName, string hlaName);
        SingleLocusDetails<MatchingHla> RetrieveHlaMatches(string locusName, SingleLocusDetails<string> locusHla);
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

        public MatchingHla RetrieveHlaMatches(string locusName, string hlaName)
        {
            var raw = rawMatchingData.FirstOrDefault(hla => hla.Locus == locusName && hla.Name == hlaName);

            if (raw == null)
            {
                return null;
            }

            return new MatchingHla {
                Name = raw.Name,
                Locus = raw.Locus,
                IsDeleted = raw.IsDeleted,
                Type = raw.Type,
                MatchingProteinGroups = raw.MatchingPGroups,
                MatchingSerologyNames = raw.MatchingSerology.Select(s => s.Name)
            };
        }

        public SingleLocusDetails<MatchingHla> RetrieveHlaMatches(string locusName, SingleLocusDetails<string> locusHla)
        {
            return new SingleLocusDetails<MatchingHla>
            {
                One = RetrieveHlaMatches(locusName, locusHla.One),
                Two = RetrieveHlaMatches(locusName, locusHla.Two)
            };
        }
    }
}
