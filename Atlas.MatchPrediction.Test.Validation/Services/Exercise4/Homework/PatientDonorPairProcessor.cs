using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework
{
    public interface IPatientDonorPairProcessor
    {
        Task Process(PatientDonorPair pdp, LociInfo<bool> matchLoci);
    }

    internal class PatientDonorPairProcessor : IPatientDonorPairProcessor
    {
        private readonly ISubjectRepository subjectRepository;
        private readonly IPatientDonorPairRepository pdpRepository;

        public PatientDonorPairProcessor(
            ISubjectRepository subjectRepository,
            IPatientDonorPairRepository pdpRepository)
        {
            this.pdpRepository = pdpRepository;
            this.subjectRepository = subjectRepository;
        }

        /// <inheritdoc />
        public async Task Process(PatientDonorPair pdp, LociInfo<bool> matchLoci)
        {
            if (await PatientHasMissingHla(pdp, matchLoci)) return;

            if (await DonorHasMissingHla(pdp, matchLoci)) return;

            // Else submit patient imputation request

            // Then submit donor imputation request

            // Then submit matching genotypes request
        }

        private async Task<bool> PatientHasMissingHla(PatientDonorPair pdp, LociInfo<bool> matchLoci)
        {
            var patientHasMissingHla = !await HasAllRequiredLoci(pdp.PatientId, false, matchLoci);

            // ReSharper disable once InvertIf
            if (patientHasMissingHla)
            {
                pdp.DidPatientHaveMissingHla = true;
                pdp.IsProcessed = true;
                await UpdatePatientDonorPairRecord(pdp);
            }

            return patientHasMissingHla;
        }

        private async Task<bool> DonorHasMissingHla(PatientDonorPair pdp, LociInfo<bool> matchLoci)
        {
            var donorHasMissingHla = !await HasAllRequiredLoci(pdp.DonorId, true, matchLoci);

            // ReSharper disable once InvertIf
            if (donorHasMissingHla)
            {
                pdp.DidDonorHaveMissingHla = true;
                pdp.IsProcessed = true;
                await UpdatePatientDonorPairRecord(pdp);
            }

            return donorHasMissingHla;
        }

        /// <returns>
        /// Will return false if either the subject is missing required HLA,
        /// or if the subject does not exist, as this suggests it was not imported due to missing required HLA.
        /// </returns>
        private async Task<bool> HasAllRequiredLoci(string externalId, bool isDonor, LociInfo<bool> matchLoci)
        {
            var subject = await subjectRepository.GetByExternalId(externalId);

            if (subject == null)
            {
                return false;
            }

            // Can assume that if donor is in the db, it has all the required loci
            if (isDonor)
            {
                return true;
            }

            // patient must be typed at all match loci
            var patientHla = subject.ToPhenotypeInfo();
            return matchLoci.AllAtLoci((locus, isRequired) => !isRequired || patientHla.GetLocus(locus).Position1And2NotNull());
        }

        private async Task UpdatePatientDonorPairRecord(PatientDonorPair pdp)
        {
            await pdpRepository.UpdateEditableFields(pdp);
        }
    }
}