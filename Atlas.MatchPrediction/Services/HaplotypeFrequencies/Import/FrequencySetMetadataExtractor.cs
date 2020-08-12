using System;
using System.Linq;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    internal interface IFrequencySetMetadataExtractor
    {
        HaplotypeFrequencySetMetadata GetMetadataFromFullPath(string fullPath);
    }

    internal class FrequencySetMetadataExtractor : IFrequencySetMetadataExtractor
    {
        public HaplotypeFrequencySetMetadata GetMetadataFromFullPath(string fullPath)
        {
            var blobPath = GetBlobPath(fullPath);
            var filePathSections = GetFilePathSections(blobPath);

            return new HaplotypeFrequencySetMetadata
            {
                Registry = filePathSections.Length > 1 ? filePathSections.First() : null,
                Ethnicity = filePathSections.Length == 3 ? filePathSections[1] : null,
                Name = filePathSections.Length > 0 ? filePathSections.Last() : null
            };
        }

        private static string GetBlobPath(string fullPath)
        {
            // Full path from Event Grid triggered function includes some extra path information. 
            // Everything post the first instance of 'blobs/' is the blob file path - any further slashes indicate folder nesting.
            var pathPostBlobs = fullPath.Split("blobs/", 2);
            return pathPostBlobs.Skip(pathPostBlobs.Length == 1 ? 0 : 1).StringJoin("");
        }

        private static string[] GetFilePathSections(string fullPath)
        {
            if (fullPath.IsNullOrEmpty())
            {
                throw new InvalidFilePathException($"{nameof(fullPath)} cannot be null or empty.");
            }

            var filePathSections = fullPath.Split('/');

            if (filePathSections.Length < 1 || filePathSections.Length > 3)
            {
                throw new InvalidFilePathException($"'{fullPath}' is not a valid {nameof(fullPath)}.");
            }

            return filePathSections;
        }
    }
}
