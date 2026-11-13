using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Atlas.Common.AzureStorage.Blob
{
    /// <summary>
    /// Names and read order of the numbered files in a batched results folder.
    /// The order of the files is the ranking order of the results, so the files must be read in number order.
    /// </summary>
    public static class BatchedResultsFileNames
    {
        /// <summary>
        /// File numbers are padded with zeros to this width, so that blob listing (name) order is the same as number order.
        /// A larger number still works: it is written without padding, and Atlas reads files in number order.
        /// </summary>
        public const int FileNumberWidth = 6;

        private const string Extension = ".json";

        /// <param name="folder">Batch folder name</param>
        /// <param name="fileNumber">1-based file number</param>
        public static string FileName(string folder, int fileNumber) =>
            $"{folder}/{fileNumber.ToString(CultureInfo.InvariantCulture).PadLeft(FileNumberWidth, '0')}{Extension}";

        /// <summary>
        /// Orders blob names by the number in the file name (e.g. "folder/2.json" before "folder/10.json").
        /// Reads both padded and old, not padded names. Names without a number come last, in name order.
        /// </summary>
        public static IEnumerable<string> OrderByFileNumber(IEnumerable<string> blobNames) =>
            blobNames
                .Select(name => (Name: name, Number: TryGetFileNumber(name)))
                .OrderBy(x => x.Number.HasValue ? 0 : 1)
                .ThenBy(x => x.Number)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .Select(x => x.Name);

        private static long? TryGetFileNumber(string blobName)
        {
            var fileName = blobName[(blobName.LastIndexOf('/') + 1)..];
            if (fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName[..^Extension.Length];
            }

            return long.TryParse(fileName, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;
        }
    }
}
