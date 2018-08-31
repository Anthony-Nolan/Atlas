using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services
{
    /// <summary>
    /// Provides patient hla for use in a search
    /// </summary>
    public interface IPatientDataProvider
    {
        PhenotypeInfo<string> GetPatientHla();
    }
}