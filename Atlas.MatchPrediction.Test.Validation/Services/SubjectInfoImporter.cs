using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
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
            await validationRepository.DeleteAllExistingData();

            await ImportSubjects(request.PatientFilePath, SubjectType.Patient);
            await ImportSubjects(request.DonorFilePath, SubjectType.Donor);
        }

        private async Task ImportSubjects(string filePath, SubjectType subjectType)
        {
            var importedSubjects = await fileReader.ReadAllLines(FileDelimiter, filePath);

            var filteredSubjects = importedSubjects
                .Where(s => IsTyped(s.A_1, s.A_2) && IsTyped(s.B_1, s.B_2) && IsTyped(s.DRB1_1, s.DRB1_2))
                .Select(s => s.ToSubjectInfo(subjectType)).ToList();
            
            Debug.WriteLine($"Imported {subjectType} count: {importedSubjects.Count}; count after filtering for required HLA: {filteredSubjects.Count}.");

            await subjectRepository.BulkInsert(filteredSubjects);
        }

        private static bool IsTyped(string position1, string position2)
        {
            return !position1.IsNullOrEmpty() && !position2.IsNullOrEmpty();
        }
    }
}