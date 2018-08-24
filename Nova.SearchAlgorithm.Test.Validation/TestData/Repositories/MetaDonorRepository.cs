using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
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