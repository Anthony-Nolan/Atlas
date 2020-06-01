using System.Collections.Generic;
using System.Linq;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;

namespace Atlas.DonorImport.ExternalInterface
{
    public interface IDonorReader
    {
        IEnumerable<Donor> GetAllDonors();
    }

    public class DonorReader : IDonorReader
    {
        private readonly IDonorRepository donorRepository;

        public DonorReader(IDonorRepository donorRepository)
        {
            this.donorRepository = donorRepository;
        }
        
        public IEnumerable<Donor> GetAllDonors()
        {
            return donorRepository.GetAllDonors().Select(d => d.ToPublicDonor());
        }
    }
}