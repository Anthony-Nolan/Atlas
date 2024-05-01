using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework
{
    public interface IMissingHlaChecker
    {
        Task<(bool, SubjectInfo)> SubjectHasMissingHla(string externalId, bool isDonor, LociInfo<bool> matchLoci);
    }

    internal class MissingHlaChecker : IMissingHlaChecker
    {
        private readonly ISubjectRepository subjectRepository;

        public MissingHlaChecker(ISubjectRepository subjectRepository)
        {
            this.subjectRepository = subjectRepository;
        }

        public async Task<(bool, SubjectInfo)> SubjectHasMissingHla(string externalId, bool isDonor, LociInfo<bool> matchLoci)
        {
            var subject = await subjectRepository.GetByExternalId(externalId);

            // Missing subject was likely rejected during the initial import of test data
            if (subject == null)
            {
                return (true, null);
            }

            // Can assume that if donor is in the db, it has all the required loci
            if (isDonor)
            {
                return (false, subject);
            }

            // patient must be typed at all match loci
            var patientHla = subject.ToPhenotypeInfo();
            var patientHasAllHla = matchLoci.AllAtLoci((locus, isRequired) => !isRequired || patientHla.GetLocus(locus).Position1And2NotNull());
            return (!patientHasAllHla, subject);
        }
    }
}