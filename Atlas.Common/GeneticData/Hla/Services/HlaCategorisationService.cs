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
        bool IsRecognisableHla(string hlaDescriptor);
        HlaTypingCategory GetHlaTypingCategory(string hlaDescriptor);
    }

    internal class HlaCategorisationService : IHlaCategorisationService
    {
        private const string SingleFieldPattern = "\\d+";
        private static readonly string ExpressionSuffixPattern = $"[{MolecularTypingNameConstants.ExpressionSuffixes}]?";
        private static readonly string MolecularFirstFieldPattern = $"\\{MolecularTypingNameConstants.Prefix}?{SingleFieldPattern}";
        private static readonly string AlleleFinalFieldPattern = SingleFieldPattern + ExpressionSuffixPattern;
        private static readonly string AlleleDesignationPattern = $"{MolecularFirstFieldPattern}(:{SingleFieldPattern}){{1,3}}{ExpressionSuffixPattern}";

        private static readonly Dictionary<string, HlaTypingCategory> TypingCategoryPatterns = new Dictionary<string, HlaTypingCategory>
        {
            {$"^{MolecularFirstFieldPattern}:(?!XX$)[A-Z]{{2,}}$", HlaTypingCategory.NmdpCode},
            {$"^{MolecularFirstFieldPattern}:XX$", HlaTypingCategory.XxCode},
            {$"^{MolecularFirstFieldPattern}(:{SingleFieldPattern}){{2}}G$", HlaTypingCategory.GGroup},
            {$"^{MolecularFirstFieldPattern}:{SingleFieldPattern}P$", HlaTypingCategory.PGroup},
            {$"^(?!0){SingleFieldPattern}$", HlaTypingCategory.Serology},
            {$"^{AlleleDesignationPattern}$", HlaTypingCategory.Allele},
            {$"^{AlleleDesignationPattern}(\\/{AlleleDesignationPattern}){{1,}}$", HlaTypingCategory.AlleleStringOfNames},
            {$"^{MolecularFirstFieldPattern}:{AlleleFinalFieldPattern}(\\/{AlleleFinalFieldPattern}){{1,}}$", HlaTypingCategory.AlleleStringOfSubtypes}
        };

        public bool TryGetHlaTypingCategory(string hlaDescriptor, [NotNullWhen(true)]out HlaTypingCategory? category)
        {
            var name = hlaDescriptor.Trim().ToUpper();

            foreach (var pattern in TypingCategoryPatterns.Keys)
            {
                if (new Regex(pattern).IsMatch(name))
                {
                    category = TypingCategoryPatterns[pattern];
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

        public bool IsRecognisableHla(string hlaDescriptor)
        {
            return TryGetHlaTypingCategory(hlaDescriptor, out _);
        }
    }
}
