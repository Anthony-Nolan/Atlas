using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    /// <summary>
    /// Contains a way of fetching the selected patient hla
    /// </summary>
    public interface IPatientHlaContainer
    {
        PhenotypeInfo<string> GetPatientHla();
    }
}