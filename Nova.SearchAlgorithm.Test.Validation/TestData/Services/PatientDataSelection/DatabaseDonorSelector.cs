using System;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IDatabaseDonorSelector
    {
        int GetExpectedMatchingDonorId(MetaDonor metaDonor, DatabaseDonorSelectionCriteria criteria);
    }
    
    public class DatabaseDonorSelector: IDatabaseDonorSelector
    {
        public int GetExpectedMatchingDonorId(MetaDonor metaDonor, DatabaseDonorSelectionCriteria criteria)
        {
            for (var i = 0; i < metaDonor.HlaTypingResolutionSets.Count; i++)
            {
                if (metaDonor.HlaTypingResolutionSets[i].Equals(criteria.MatchingTypingResolutions))
                {
                    return metaDonor.DatabaseDonors[i].DonorId;
                }
            }

            // This exception should never be thrown in practice, as the meta-donor should not have been selected if it did not contain a donor of the correct resolution
            throw new DonorNotFoundException("Failed to find the expected matched donor for this patient - does the corresponding test data exist?");
        }
    }
}