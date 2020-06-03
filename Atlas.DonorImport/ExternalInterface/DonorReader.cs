using System.Collections.Generic;
using System.Linq;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.Mapping;

namespace Atlas.DonorImport.ExternalInterface
{
    public interface IDonorReader
    {
        IList<Donor> GetAllDonors();
    }

    public class DonorReader : IDonorReader
    {
        private readonly IDonorInspectionRepository donorInspectionRepository;

        public DonorReader(IDonorInspectionRepository donorInspectionRepository)
        {
            this.donorInspectionRepository = donorInspectionRepository;
        }

        public IList<Donor> GetAllDonors()
        {
            return donorInspectionRepository.GetAllDonors().Select(d => d.ToPublicDonor()).ToList();
        }
    }
}