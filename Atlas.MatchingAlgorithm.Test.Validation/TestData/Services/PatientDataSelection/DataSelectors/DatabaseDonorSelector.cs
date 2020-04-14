using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IDatabaseDonorSelector
    {
        int GetExpectedMatchingDonorId(MetaDonor metaDonor, DatabaseDonorSpecification criteria);
    }
    
    public class DatabaseDonorSelector: IDatabaseDonorSelector
    {
        public int GetExpectedMatchingDonorId(MetaDonor metaDonor, DatabaseDonorSpecification criteria)
        {
            for (var i = 0; i < metaDonor.DatabaseDonorSpecifications.Count; i++)
            {
                if (metaDonor.DatabaseDonorSpecifications[i].MatchingTypingResolutions == criteria.MatchingTypingResolutions)
                {
                    return metaDonor.DatabaseDonors[i].DonorId;
                }
            }

            // This exception should never be thrown in practice, as the meta-donor should not have been selected if it did not contain a donor of the correct resolution
            throw new DonorNotFoundException("Failed to find the expected matched donor for this patient - does the corresponding test data exist?");
        }
    }
}