using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;
using System;
using System.Linq;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequencySetMetaDataService
    {
        HaplotypeFrequencySetMetaData GetMetadataFromFileName(string fileName);

    }

    public class HaplotypeFrequencySetMetaDataService : IHaplotypeFrequencySetMetaDataService
    {
        public HaplotypeFrequencySetMetaData GetMetadataFromFileName(string fileName)
        {
            var filePathSections = GetFilePathSections(fileName);

            return new HaplotypeFrequencySetMetaData
            {
                Registry = filePathSections.Length > 1 ? filePathSections.First() : null,
                Ethnicity = filePathSections.Length == 3 ? filePathSections[1] : null,
                Name = filePathSections.Length > 0 ? filePathSections.Last() : null
            };
        }

        private static string[] GetFilePathSections(string fileName)
        {
            if (fileName.IsNullOrEmpty())
            {
                throw new ArgumentException($"{nameof(fileName)} cannot be null or empty.");
            }

            var filePathSections = fileName.Split('/');

            if (filePathSections.Length < 1 || filePathSections.Length > 3)
            {
                throw new ArgumentException($"'{fileName}' is not a valid {nameof(fileName)}.");
            }

            return filePathSections;
        }
    }
}
