using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal static class WmdaDataExtraction
    {
        private static readonly IWmdaDataRepository WmdaDataNameProvider = null;
        private const string HlaNomFileName = "wmda/hla_nom";
        private const string HlaNomRegexPattern = @"^(\w+\*{0,1})\;([\w:]+)\;\d+\;(\d*)\;([\w:]*)\;";

        private static readonly Dictionary<string, WmdaDataExtractionToolSet> WmdaDataExtractionToolSets =
            new Dictionary<string, WmdaDataExtractionToolSet>
        {
            {
                nameof(WmdaDataNameProvider.Serologies),
                new WmdaDataExtractionToolSet(
                    HlaNomFileName,
                    SerologyFilter.Instance.Filter,
                    HlaNomRegexPattern,
                    new HlaNomMapper())
            },
            {
                nameof(WmdaDataNameProvider.Alleles),
                new WmdaDataExtractionToolSet(
                    HlaNomFileName,
                    MolecularFilter.Instance.Filter,
                    HlaNomRegexPattern,
                    new HlaNomMapper())
            },
            {
                nameof(WmdaDataNameProvider.PGroups),
                new WmdaDataExtractionToolSet(
                    "wmda/hla_nom_p",
                    MolecularFilter.Instance.Filter,
                    @"^(\w+\*)\;([\w:\/]+)\;([\w:]*)$",
                    new HlaNomPMapper())
            },
            {
                nameof(WmdaDataNameProvider.GGroups),
                new WmdaDataExtractionToolSet(
                    "wmda/hla_nom_g",
                    MolecularFilter.Instance.Filter,
                    @"^(\w+\*)\;([\w:\/]+)\;([\w:]*)$",
                    new HlaNomGMapper())
            },
            {
                nameof(WmdaDataNameProvider.SerologyToSerologyRelationships),
                new WmdaDataExtractionToolSet(
                    "wmda/rel_ser_ser",
                    SerologyFilter.Instance.Filter,
                    @"(\w+)\;(\d*)\;([\d\/]*)\;([\d\/]*)",
                    new RelSerSerMapper())
            },
            {
                nameof(WmdaDataNameProvider.DnaToSerologyRelationships),
                new WmdaDataExtractionToolSet(
                    "wmda/rel_dna_ser",
                    MolecularFilter.Instance.Filter,
                    @"^(\w+\*)\;([\w:]+)\;([\d\/\\?]*);([\d\/\\?]*)\;([\d\/\\?]*)\;([\d\/\\?]*)$",
                    new RelDnaSerMapper())
            },
            {
                nameof(WmdaDataNameProvider.ConfidentialAlleles),
                new WmdaDataExtractionToolSet(
                    "version_report",
                    MolecularFilter.Instance.Filter,
                    @"^Confidential,(\w+\*)([\w:]+),",
                    new ConfidentialAlleleMapper())
            }
        };

        public static WmdaDataExtractionToolSet GetWmdaDataExtractionToolSet(string wmdaDataName)
        {
            WmdaDataExtractionToolSets.TryGetValue(wmdaDataName, out var extractionToolSet);

            if (extractionToolSet == null)
                throw new ArgumentException($"Toolset does not exist for {wmdaDataName}.");

            return extractionToolSet;
        }
    }
}
