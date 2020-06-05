using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;
using System;
using System.Linq;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    internal interface IFrequencySetMetadataExtractor
    {
        HaplotypeFrequencySetMetadata GetMetadataFromFullPath(string fullPath);
    }

    internal class FrequencySetMetadataExtractor : IFrequencySetMetadataExtractor
    {
        public HaplotypeFrequencySetMetadata GetMetadataFromFullPath(string fullPath)
        {
            var filePathSections = GetFilePathSections(fullPath);

            return new HaplotypeFrequencySetMetadata
            {
                Registry = filePathSections.Length > 1 ? filePathSections.First() : null,
                Ethnicity = filePathSections.Length == 3 ? filePathSections[1] : null,
                Name = filePathSections.Length > 0 ? filePathSections.Last() : null
            };
        }

        private static string[] GetFilePathSections(string fullPath)
        {
            if (fullPath.IsNullOrEmpty())
            {
                throw new ArgumentException($"{nameof(fullPath)} cannot be null or empty.");
            }

            var filePathSections = fullPath.Split('/');

            if (filePathSections.Length < 1 || filePathSections.Length > 3)
            {
                throw new ArgumentException($"'{fullPath}' is not a valid {nameof(fullPath)}.");
            }

            return filePathSections;
        }
    }
}
