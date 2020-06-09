using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.Common.Utils.Http;

namespace Atlas.Common.GeneticData.Hla.Services
{
    public interface IHlaCategorisationService
    {
        HlaTypingCategory GetHlaTypingCategory(string hlaName);
    }

    internal class HlaCategorisationService : IHlaCategorisationService
    {
        private const string SingleFieldPattern = "\\d+";
        private static readonly string ExpressionSuffixPattern = $"[{MolecularTypingNameConstants.ExpressionSuffixes}]?";
        private static readonly string MolecularFirstFieldPattern = $"\\{MolecularTypingNameConstants.Prefix}?{SingleFieldPattern}";
        private static readonly string AlleleFinalFieldPattern = SingleFieldPattern + ExpressionSuffixPattern;

        private static readonly string AlleleDesignationPattern =
            $"{MolecularFirstFieldPattern}(:{SingleFieldPattern}){{1,3}}{ExpressionSuffixPattern}";

        private static readonly Dictionary<Regex, HlaTypingCategory> TypingCategoryRegexes = new Dictionary<Regex, HlaTypingCategory>
        {
            {
                new Regex($"^{MolecularFirstFieldPattern}:(?!XX$)[A-Z]{{2,}}$", RegexOptions.Compiled),
                HlaTypingCategory.NmdpCode
            },
            {
                new Regex($"^{MolecularFirstFieldPattern}:XX$", RegexOptions.Compiled),
                HlaTypingCategory.XxCode
            },
            {
                new Regex($"^{MolecularFirstFieldPattern}(:{SingleFieldPattern}){{2}}G$", RegexOptions.Compiled),
                HlaTypingCategory.GGroup
            },
            {
                new Regex($"^{MolecularFirstFieldPattern}:{SingleFieldPattern}P$", RegexOptions.Compiled),
                HlaTypingCategory.PGroup
            },
            {
                new Regex($"^(?!0){SingleFieldPattern}$", RegexOptions.Compiled),
                HlaTypingCategory.Serology
            },
            {
                new Regex($"^{AlleleDesignationPattern}$", RegexOptions.Compiled),
                HlaTypingCategory.Allele
            },
            {
                new Regex($"^{AlleleDesignationPattern}(\\/{AlleleDesignationPattern}){{1,}}$", RegexOptions.Compiled),
                HlaTypingCategory.AlleleStringOfNames
            },
            {
                new Regex($"^{MolecularFirstFieldPattern}:{AlleleFinalFieldPattern}(\\/{AlleleFinalFieldPattern}){{1,}}$", RegexOptions.Compiled),
                HlaTypingCategory.AlleleStringOfSubtypes
            }
        };

        public HlaTypingCategory GetHlaTypingCategory(string hlaName)
        {
            var name = hlaName.Trim().ToUpper();

            foreach (var categoryRegex in TypingCategoryRegexes.Keys)
            {
                if (categoryRegex.IsMatch(name))
                {
                    return TypingCategoryRegexes[categoryRegex];
                }
            }

            throw new AtlasHttpException(HttpStatusCode.BadRequest, $"Typing category of HLA name: {name} could not be determined.");
        }
    }
}