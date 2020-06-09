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
                CompiledRegex($"^{MolecularFirstFieldPattern}:(?!XX$)[A-Z]{{2,}}$"),
                HlaTypingCategory.NmdpCode
            },
            {
                CompiledRegex($"^{MolecularFirstFieldPattern}:XX$"),
                HlaTypingCategory.XxCode
            },
            {
                CompiledRegex($"^{MolecularFirstFieldPattern}(:{SingleFieldPattern}){{2}}G$"),
                HlaTypingCategory.GGroup
            },
            {
                CompiledRegex($"^{MolecularFirstFieldPattern}:{SingleFieldPattern}P$"),
                HlaTypingCategory.PGroup
            },
            {
                CompiledRegex($"^(?!0){SingleFieldPattern}$"),
                HlaTypingCategory.Serology
            },
            {
                CompiledRegex($"^{AlleleDesignationPattern}$"),
                HlaTypingCategory.Allele
            },
            {
                CompiledRegex($"^{AlleleDesignationPattern}(\\/{AlleleDesignationPattern}){{1,}}$"),
                HlaTypingCategory.AlleleStringOfNames
            },
            {
                CompiledRegex($"^{MolecularFirstFieldPattern}:{AlleleFinalFieldPattern}(\\/{AlleleFinalFieldPattern}){{1,}}$"),
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

        private static Regex CompiledRegex(string pattern) => new Regex(pattern, RegexOptions.Compiled);
    }
}