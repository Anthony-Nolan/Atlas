using System.IO;
using Microsoft.Azure.WebJobs;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class ImportHaplotypeFrequencySet
    {
        [FunctionName("ImportSetWithRegistryEthnicityFilename")]
        public static void Run([BlobTrigger("{registry}/{ethnicity}/{filename}", Connection = "")]
            Stream myBlob, string filename, string ethnicity, string registry)
        {

        }

        [FunctionName("ImportSetWithRegistryFilename")]
        public static void Run([BlobTrigger("{registry}/{filename}", Connection = "")]
            Stream myBlob, string filename, string ethnicity)
        {

        }

        [FunctionName("ImportSetWithFilename")]
        public static void Run([BlobTrigger("{filename}", Connection = "")]
            Stream myBlob, string filename)
        {

        }
    }
}
