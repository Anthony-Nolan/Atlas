using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.ManualTesting.Common.Services;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework;
using Atlas.MatchPrediction.Test.Validation.Models;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework
{
    public interface IHomeworkCreator
    {
        /// <summary>
        /// Creates new homework sets from the input files defined in the request.
        /// </summary>
        /// <returns>Homework Set Ids</returns>
        Task<IEnumerable<int>> CreateHomeworkSets(HomeworkRequest request);
    }

    internal class HomeworkCreator : IHomeworkCreator
    {
        private const string FileDelimiter = ",";
        private readonly IHomeworkSetRepository setRepository;
        private readonly IFileReader<SubjectIdPair> fileReader;
        private readonly IPatientDonorPairRepository pdpRepository;

        public HomeworkCreator(
            IHomeworkSetRepository setRepository,
            IFileReader<SubjectIdPair> fileReader,
            IPatientDonorPairRepository pdpRepository)
        {
            this.setRepository = setRepository;
            this.fileReader = fileReader;
            this.pdpRepository = pdpRepository;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<int>> CreateHomeworkSets(HomeworkRequest request)
        {
            var setIds = new List<int?>();

            var fileNames = GetCsvFileNames(request.InputPath);

            foreach (var file in fileNames)
            {
                setIds.Add(await ImportPatientDonorPairs(request, file));
            }

            return setIds.Where(id => id.HasValue).Select(id => id.Value);
        }

        private static IEnumerable<string> GetCsvFileNames(string path)
        {
            return Directory.GetFiles(path)
                .Where(file => Path.GetExtension(file) == ".csv")
                .Select(Path.GetFileName);
        }

        private async Task<int?> ImportPatientDonorPairs(HomeworkRequest request, string setFileName)
        {
            var subjectIdPairs = await fileReader.ReadAllLines(
                FileDelimiter,
                Path.Combine(request.InputPath, setFileName),
                hasHeaderRecord: false);

            if (subjectIdPairs.IsNullOrEmpty())
            {
                System.Diagnostics.Debug.WriteLine($"No patient-donor pairs found in file {setFileName}.");
                return null;
            }

            var setId = await setRepository.Add(
                setFileName,
                request.MatchLoci.MatchLociToString(),
                request.MatchingAlgorithmHlaNomenclatureVersion);

            await pdpRepository.BulkInsert(subjectIdPairs.Select(ids => MapToDatabaseModel(ids, setId)).ToList());

            return setId;
        }

        private static PatientDonorPair MapToDatabaseModel(SubjectIdPair ids, int homeworkSetId)
        {
            return new PatientDonorPair
            {
                PatientId = ids.PatientId,
                DonorId = ids.DonorId,
                HomeworkSet_Id = homeworkSetId
            };
        }
    }
}