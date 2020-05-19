using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories
{
    public interface IMetaDonorRepository
    {
        IEnumerable<MetaDonor> AllMetaDonors();
    }
    
    /// <summary>
    /// Generates and stores meta-donors for use in testing.
    /// </summary>
    public class MetaDonorRepository: IMetaDonorRepository
    {
        private readonly IEnumerable<MetaDonor> metaDonors;

        public MetaDonorRepository(IMetaDonorsData metaDonorsData)
        {
            metaDonors = metaDonorsData.MetaDonors.ToList();
        }
        
        public IEnumerable<MetaDonor> AllMetaDonors()
        {
            return metaDonors;
        }
    }
}