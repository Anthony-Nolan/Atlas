using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories
{
    public class MockWmdaRepository: IWmdaRepository
    {
        public static MockWmdaRepository Instance { get; } = new MockWmdaRepository();

        private MockWmdaRepository()
        {
        }

        public IEnumerable<string> HlaNom { get; } = GetFileContents("hla_nom");
        public IEnumerable<string> HlaNomP { get; } = GetFileContents("hla_nom_p");
        public IEnumerable<string> HlaNomG { get; } = GetFileContents("hla_nom_g");
        public IEnumerable<string> RelSerSer { get; } = GetFileContents("rel_ser_ser");
        public IEnumerable<string> RelDnaSer { get; } = GetFileContents("rel_dna_ser");
        public IEnumerable<string> VersionReport { get; } = GetFileContents("version_report");


        private static IEnumerable<string> GetFileContents(string fileName)
        {
            var testDir = $"{TestContext.CurrentContext.TestDirectory}";
            var filePath = ConfigurationManager.ConnectionStrings["TestWmdaFilePath"].ConnectionString;
            return File.ReadAllLines($"{testDir}{filePath}{fileName}.txt")
                    .SkipWhile(line => line.StartsWith("#"))
                    .ToList();
        }
    }
}
