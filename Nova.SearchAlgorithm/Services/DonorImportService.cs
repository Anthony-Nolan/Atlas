using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Repositories.Hlas;


namespace Nova.SearchAlgorithm.Services
{
    public interface IDonorImportService
    {
        void Import();
    }

    public class DonorImportService : IDonorImportService
    {
        private readonly IDonorRepository donorRepository;
        private readonly IHlaRepository hlaRepository;

        public DonorImportService(IDonorRepository donorRepository, IHlaRepository hlaRepository)
        {
            this.donorRepository = donorRepository;
            this.hlaRepository = hlaRepository;
        }

        // TODO:NOVA-929 implement correctly
        public void Import()
        {
            donorRepository.InsertDonor(new ImportDonor
            {
                RegistryCode = RegistryCode.ANBMT,
                DonorType = "Adult",
                DonorId = "1",
                LocusA = new MatchingHla
                {
                    Locus = "A",
                    Name = "01:01:01:01",
                    Type = "Allele",
                    IsDeleted = false,
                    MatchingProteinGroups = new List<string> { "01:01P" },
                    MatchingSerologyNames = new List<string> { "1" }
                }
            });
        }
    }
}