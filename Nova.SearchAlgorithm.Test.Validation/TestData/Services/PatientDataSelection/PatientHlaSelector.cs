using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IPatientHlaSelector
    {
        PhenotypeInfo<string> GetPatientHla(MetaDonor metaDonor, PatientHlaSelectionCriteria criteria);
    }

    public class PatientHlaSelector : IPatientHlaSelector
    {
        private readonly IAlleleRepository alleleRepository;

        public PatientHlaSelector(IAlleleRepository alleleRepository)
        {
            this.alleleRepository = alleleRepository;
        }

        public PhenotypeInfo<string> GetPatientHla(MetaDonor metaDonor, PatientHlaSelectionCriteria criteria)
        {
            return metaDonor.Genotype.Hla.Map((locus, position, allele) => GetHlaName(locus, position, allele, criteria));
        }

        private string GetHlaName(Locus locus, TypePositions position, TgsAllele tgsAllele, PatientHlaSelectionCriteria criteria)
        {
            var allele = GetTgsAllele(locus, position, tgsAllele, criteria);
            var typingResolution = criteria.PatientTypingResolutions.DataAtPosition(locus, position);

            return allele.GetHlaForCategory(typingResolution);
        }

        private TgsAllele GetTgsAllele(
            Locus locus,
            TypePositions position,
            TgsAllele genotypeAllele,
            PatientHlaSelectionCriteria criteria
        )
        {
            // patient should be mismatched at this position
            if (!criteria.HlaMatches.DataAtPosition(locus, position))
            {
                return GetNonMatchingAllele(locus, position);
            }

            // patient should have a P-group match at this position
            if (criteria.MatchLevels.DataAtPosition(locus, position) == MatchLevel.PGroup)
            {
                return GetPGroupMatchLevelTgsAllele(locus);
            }
            
            // patient should have a G-group match at this position
            if (criteria.MatchLevels.DataAtPosition(locus, position) == MatchLevel.GGroup)
            {
                return GetGGroupMatchLevelTgsAllele(locus, position, genotypeAllele);
            }

            return genotypeAllele;
        }

        // TODO: NOVA-1654: Remove static dependency on GenotypeGenerator so we can unit test this
        private static TgsAllele GetNonMatchingAllele(Locus locus, TypePositions position)
        {
            return GenotypeGenerator.NonMatchingGenotype.Hla.DataAtPosition(locus, position);
        }

        private TgsAllele GetPGroupMatchLevelTgsAllele(Locus locus)
        {
            var alleleAtLocus = alleleRepository.PatientAllelesForPGroupMatching().DataAtLocus(locus);

            return TgsAllele.FromTestDataAllele(alleleAtLocus, locus);
        }

        private TgsAllele GetGGroupMatchLevelTgsAllele(Locus locus, TypePositions position, TgsAllele genotypeAllele)
        {
            var allelesAtLocus = alleleRepository.AllelesForGGroupMatching().DataAtPosition(locus, position);
            var allele = allelesAtLocus.First(a => a.AlleleName != genotypeAllele.TgsTypedAllele); 
            
            return TgsAllele.FromTestDataAllele(allele, locus);
        }
    }
}