using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Configuration;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IWmdaRepository
    {
        IEnumerable<string> HlaNom { get; }
        IEnumerable<string> HlaNomP { get; }
        IEnumerable<string> RelSerSer { get; }
        IEnumerable<string> RelDnaSer { get; }
        IEnumerable<string> VersionReport { get; }
    }

    /// <summary>
    /// This class is responsible for
    /// importing the WMDA files
    /// and preparing the file contents for data extraction.
    /// </summary>
    public sealed class WmdaRepository : IWmdaRepository
    {
        public static WmdaRepository Instance { get; } = new WmdaRepository();

        private WmdaRepository()
        {
        }

        public IEnumerable<string> HlaNom { get; } = GetFileContents("wmda/hla_nom");
        public IEnumerable<string> HlaNomP { get; } = GetFileContents("wmda/hla_nom_p");
        public IEnumerable<string> RelSerSer { get; } = GetFileContents("wmda/rel_ser_ser");
        public IEnumerable<string> RelDnaSer { get; } = GetFileContents("wmda/rel_dna_ser");
        public IEnumerable<string> VersionReport { get; } = GetFileContents("version_report");
        
        private static IEnumerable<string> GetFileContents(string fileName)
        {
            var fileUri = ConfigurationManager.ConnectionStrings["WmdaFileUri"].ConnectionString;
            return new WebClient()
                .DownloadString($"{fileUri}{fileName}.txt")
                .Split('\n')
                .SkipWhile(line => line.StartsWith("#"))
                .ToList();
        }
    }
}
