using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.RegularExpressions;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.Common.Utils.Http;

namespace Atlas.Common.GeneticData.Hla.Services
{
    public interface IHlaCategorisationService
    {
        /// <summary>
        /// Tests whether the specified string matches the syntactic pattern of an HLA.
        /// i.e. whether it conforms to the general Regex pattern of one of the supported Typing Categories.
        /// <br/>
        /// Does NOT check whether the exact values of the string are recognised. i.e. non-existent NMDP codes or Field values will NOT be detected.
        /// </summary>
        bool ConformsToValidHlaFormat(string hlaDescriptor);
        HlaTypingCategory GetHlaTypingCategory(string hlaDescriptor);
        bool TryGetHlaTypingCategory(string hlaDescriptor, [NotNullWhen(true)]out HlaTypingCategory? category);
    }

    internal class HlaCategorisationService : IHlaCategorisationService
    {
        private const string SingleFieldPattern = "\\d+";
        private static readonly string OptionalExpressionSuffixPattern = MolecularTypingNameConstants.ExpressionSuffixesRegexCharacterGroup + "?";
        private static readonly string MolecularFirstFieldPattern = $"\\{MolecularTypingNameConstants.Prefix}?{SingleFieldPattern}";
        private static readonly string AlleleFinalFieldPattern = SingleFieldPattern + OptionalExpressionSuffixPattern;

        private static readonly string AlleleDesignationPattern =
            $"{MolecularFirstFieldPattern}(:{SingleFieldPattern}){{1,3}}{OptionalExpressionSuffixPattern}";

        private static readonly List<(Regex, HlaTypingCategory)> TypingCategoryRegexes = new List<(Regex, HlaTypingCategory)>
        {
            (
                CompiledRegex($"^{MolecularFirstFieldPattern}:(?!XX$)[A-Z]{{2,}}$"),
                HlaTypingCategory.NmdpCode
            ),
            (
                CompiledRegex($"^{MolecularFirstFieldPattern}:XX$"),
                HlaTypingCategory.XxCode
            ),
            (
                CompiledRegex($"^{MolecularFirstFieldPattern}(:{SingleFieldPattern}){{2}}G$"),
                HlaTypingCategory.GGroup
            ),
            (
                CompiledRegex($"^{MolecularFirstFieldPattern}:{SingleFieldPattern}P$"),
                HlaTypingCategory.PGroup
            ),
            (
                CompiledRegex($"^(?!0){SingleFieldPattern}$"),
                HlaTypingCategory.Serology
            ),
            (
                CompiledRegex($"^{AlleleDesignationPattern}$"),
                HlaTypingCategory.Allele
            ),
            (
                CompiledRegex($"^{AlleleDesignationPattern}(\\/{AlleleDesignationPattern}){{1,}}$"),
                HlaTypingCategory.AlleleStringOfNames
            ),
            (
                CompiledRegex($"^{MolecularFirstFieldPattern}:{AlleleFinalFieldPattern}(\\/{AlleleFinalFieldPattern}){{1,}}$"),
                HlaTypingCategory.AlleleStringOfSubtypes
            )
        };

        public bool TryGetHlaTypingCategory(string hlaDescriptor, [NotNullWhen(true)]out HlaTypingCategory? category)
        {
            var name = hlaDescriptor.Trim().ToUpper();

            foreach (var (regex, hlaCategory) in TypingCategoryRegexes)
            {
                if (regex.IsMatch(name))
                {
                    category = hlaCategory;
                    return true;
                }
            }

            category = null;
            return false;
        }

        public HlaTypingCategory GetHlaTypingCategory(string hlaDescriptor)
        {
            if (TryGetHlaTypingCategory(hlaDescriptor, out var category))
            {
                return category.Value;
            }

            throw new AtlasHttpException(HttpStatusCode.BadRequest, $"Typing category of HLA name: {hlaDescriptor} could not be determined.");
        }

        /// <inheritdoc/>
        public bool ConformsToValidHlaFormat(string hlaDescriptor)
        {
            return TryGetHlaTypingCategory(hlaDescriptor, out _);
        }

        private static Regex CompiledRegex(string pattern) => new Regex(pattern, RegexOptions.Compiled);
    }
}