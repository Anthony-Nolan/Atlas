using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Services;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using Atlas.MatchPrediction.Test.Validation.Models;

namespace Atlas.MatchPrediction.Test.Validation.Services
{
    public interface ISubjectInfoImporter
    {
        /// <summary>
        /// Imports subject info from file paths declared in <see cref="ImportRequest"/>.
        /// Note: Subjects not typed at the required loci will be ignored.
        /// </summary>
        Task Import(ImportRequest request);
    }

    internal class SubjectInfoImporter : ISubjectInfoImporter
    {
        private const string FileDelimiter = ";";
        private readonly IFileReader<ImportedSubject> fileReader;
        private readonly IValidationRepository validationRepository;
        private readonly ISubjectRepository subjectRepository;

        public SubjectInfoImporter(IFileReader<ImportedSubject> fileReader, IValidationRepository validationRepository, ISubjectRepository subjectRepository)
        {
            this.fileReader = fileReader;
            this.validationRepository = validationRepository;
            this.subjectRepository = subjectRepository;
        }

        public async Task Import(ImportRequest request)
        {
            await validationRepository.DeleteSubjectInfo();

            await ImportSubjects(request.PatientFilePath, SubjectType.Patient);
            await ImportSubjects(request.DonorFilePath, SubjectType.Donor);
        }

        private async Task ImportSubjects(string filePath, SubjectType subjectType)
        {
            var importedSubjects = await fileReader.ReadAllLines(FileDelimiter, filePath);

            var filteredSubjects = importedSubjects
                .Where(PositionOneOfMandatoryLociAreTyped)
                .Select(s => s.ToSubjectInfo(subjectType)).ToList();
            
            Debug.WriteLine($"Imported {subjectType} count: {importedSubjects.Count}; count after filtering for required HLA: {filteredSubjects.Count}.");

            await subjectRepository.BulkInsert(filteredSubjects);
        }

        private static bool PositionOneOfMandatoryLociAreTyped(ImportedSubject subject)
        {
            return !string.IsNullOrEmpty(subject.A_1) && !string.IsNullOrEmpty(subject.B_1) && !string.IsNullOrEmpty(subject.DRB1_1);
        }
    }
}