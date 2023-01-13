using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services
{
    /// <summary>
    /// Provides patient hla for use in a search
    /// </summary>
    public interface IPatientDataProvider
    {
        PhenotypeInfo<string> GetPatientHla();
    }
}