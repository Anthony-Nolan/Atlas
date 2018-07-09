using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class AlleleHistoryExtractor : WmdaDataExtractor<AlleleNameHistory>
    {
        private const string FileName = "Allelelist_history";
        private const string ColumnDelimiter = ",";
        private const string OldestHlaDatabaseVersionToImport = "3000";
        private const string ColumnNamesRegexPattern =
            "^HLA_ID" + ColumnDelimiter + @"(?:\d+" + ColumnDelimiter + "){1,}" + OldestHlaDatabaseVersionToImport;
        private const string AlleleHistoryRegexPattern = @"^HLA\d+,.+$";
        private const string NoAlleleNamePlaceHolder = "NA";
        private const string MolecularPrefix = "*";
        private const string AlleleNameRegexPattern = @"\" + MolecularPrefix + @"([\w:]+)";

        private IEnumerable<string> hlaDatabaseVersions;

        public AlleleHistoryExtractor() : base(FileName)
        {
        }

        protected override AlleleNameHistory MapLineOfFileContentsToWmdaHlaTypingElseNull(string line)
        {
            if (hlaDatabaseVersions != null)
            {
                return GetAlleleNameHistory(line);
            }

            // HLA database versions are listed as column names in first line of file contents
            ExtractHlaDatabaseVersionsFromLine(line);
            return null;
        }

        private AlleleNameHistory GetAlleleNameHistory(string line)
        {
            var lineSplitByColumnDelimiter = SplitAlleleHistoryLine(line).ToList();

            if (!lineSplitByColumnDelimiter.Any())
            {
                return null;
            }

            var locus = GetLocusName(lineSplitByColumnDelimiter);
            var hlaId = lineSplitByColumnDelimiter[0];
            var versionedAlleleNames = GetVersionedAlleleNames(lineSplitByColumnDelimiter);

            // exclude entries that don't have any allele names listed on or after oldest version of interest
            return versionedAlleleNames.Any() 
                ? new AlleleNameHistory(locus, hlaId, versionedAlleleNames) 
                : null;
        }

        private static IEnumerable<string> SplitAlleleHistoryLine(string line)
        {
            var regex = new Regex(AlleleHistoryRegexPattern);

            if (!regex.IsMatch(line))
            {
                return new List<string>();
            }

            return regex
                .Match(line)
                .Value
                .TrimEnd(ColumnDelimiter.ToCharArray())
                .Split(ColumnDelimiter.ToCharArray());
        }

        private static string GetLocusName(IEnumerable<string> lineSplitByColumnDelimiter)
        {
            var firstAlleleNameInLine = lineSplitByColumnDelimiter
                .SkipWhile(str => !str.Contains(MolecularPrefix))
                .FirstOrDefault();

            var locusName = firstAlleleNameInLine
                ?.Substring(0, firstAlleleNameInLine.IndexOf(MolecularPrefix) + 1);

            return locusName;
        }

        private IEnumerable<VersionedAlleleName> GetVersionedAlleleNames(IEnumerable<string> lineSplitByColumnDelimiter)
        {
            var alleleNames = GetAlleleNames(lineSplitByColumnDelimiter).ToList();

            var versionedAlleleNames = new List<VersionedAlleleName>();

            if (!alleleNames.All(string.IsNullOrEmpty))
            {
                versionedAlleleNames = hlaDatabaseVersions
                    .Zip(alleleNames, (version, alleleName)
                        => new VersionedAlleleName(version, alleleName))
                    .ToList();
            }

            return versionedAlleleNames;
        }

        private IEnumerable<string> GetAlleleNames(IEnumerable<string> lineSplitByColumnDelimiter)
        {
            return lineSplitByColumnDelimiter
                .Skip(1)
                .Take(hlaDatabaseVersions.Count())
                .Select(GetAlleleNameWithoutLocus);
        }

        private static string GetAlleleNameWithoutLocus(string input)
        {
            var regex = new Regex(AlleleNameRegexPattern);

            if (input.Equals(NoAlleleNamePlaceHolder) || !regex.IsMatch(input))
            {
                return null;
            }

            return regex.Match(input).Groups[1].Value;
        }

        private void ExtractHlaDatabaseVersionsFromLine(string line)
        {
            if (!TryExtractHlaDatabaseVersions(line, out hlaDatabaseVersions))
            {
                throw new ArgumentException(
                    $"Could not extract HLA database versions from {FileName} file.");
            }
        }

        private static bool TryExtractHlaDatabaseVersions(string line, out IEnumerable<string> hlaDatabaseVersions)
        {
            var regex = new Regex(ColumnNamesRegexPattern);

            hlaDatabaseVersions = regex
                .Match(line)
                .Value
                .Split(ColumnDelimiter.ToCharArray())
                .Skip(1);

            return hlaDatabaseVersions.Any();
        }
    }
}
