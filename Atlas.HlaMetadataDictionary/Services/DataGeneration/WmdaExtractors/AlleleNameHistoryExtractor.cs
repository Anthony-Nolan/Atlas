﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors
{
    internal class AlleleHistoryExtractor : WmdaDataExtractor<AlleleNameHistory>
    {
        private const string FileName = "Allelelist_history.txt";
        private const string ColumnDelimiter = ",";
        private const string OldestHlaNomenclatureVersionToImport = "3000";

        private static readonly Regex ColumnNamesRegex =
            new Regex("^HLA_ID" + ColumnDelimiter + @"(?:\d+" + ColumnDelimiter + "){1,}" + OldestHlaNomenclatureVersionToImport, RegexOptions.Compiled);
        private static readonly Regex AlleleHistoryRegex = new Regex(@"^HLA\d+,.+$", RegexOptions.Compiled);
        private static readonly Regex AlleleNameRegex = new Regex(@"\" + MolecularPrefix + @"([\w:]+)", RegexOptions.Compiled);
        private const string NoAlleleNamePlaceHolder = "NA";
        private const string MolecularPrefix = "*";

        private IEnumerable<string> hlaNomenclatureVersions;

        public AlleleHistoryExtractor() : base(FileName)
        {
        }

        protected override void ExtractHeaders(string headersLine)
        {
            // HLA Nomenclature versions are listed as column names in first line of file contents
            hlaNomenclatureVersions = ExtractHlaNomenclatureVersionsFromLine(headersLine);
        }

        protected override AlleleNameHistory MapLineOfFileContentsToWmdaHlaTyping(string line)
        {
            return GetAlleleNameHistory(line);
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
            if (!AlleleHistoryRegex.IsMatch(line))
            {
                return new List<string>();
            }

            return AlleleHistoryRegex
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

            var locusName = firstAlleleNameInLine ?.Substring(0, firstAlleleNameInLine.IndexOf(MolecularPrefix) + 1);

            return locusName;
        }

        private IEnumerable<VersionedAlleleName> GetVersionedAlleleNames(IEnumerable<string> lineSplitByColumnDelimiter)
        {
            var alleleNames = GetAlleleNames(lineSplitByColumnDelimiter).ToList();

            var versionedAlleleNames = new List<VersionedAlleleName>();

            if (!alleleNames.All(string.IsNullOrEmpty))
            {
                versionedAlleleNames = hlaNomenclatureVersions
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
                .Take(hlaNomenclatureVersions.Count())
                .Select(GetAlleleNameWithoutLocus);
        }

        private static string GetAlleleNameWithoutLocus(string input)
        {
            if (input.Equals(NoAlleleNamePlaceHolder) || !AlleleNameRegex.IsMatch(input))
            {
                return null;
            }

            return AlleleNameRegex.Match(input).Groups[1].Value;
        }

        private static IEnumerable<string> ExtractHlaNomenclatureVersionsFromLine(string line)
        {
            if (!TryExtractHlaNomenclatureVersions(line, out var hlaNomenclatureVersions))
            {
                throw new ArgumentException($"Could not extract HLA Nomenclature versions from {FileName} file.");
            }

            return hlaNomenclatureVersions;
        }

        private static bool TryExtractHlaNomenclatureVersions(string line, out IEnumerable<string> hlaNomenclatureVersions)
        {
            hlaNomenclatureVersions = ColumnNamesRegex
                .Match(line)
                .Value
                .Split(ColumnDelimiter.ToCharArray())
                .Skip(1);

            return hlaNomenclatureVersions.Any();
        }
    }
}