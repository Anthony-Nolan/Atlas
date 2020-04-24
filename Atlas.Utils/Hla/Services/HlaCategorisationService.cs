using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Atlas.Utils.Hla.Models;
using Atlas.Utils.Hla.Models.HlaTypings;
using Atlas.Utils.Core.Http.Exceptions;

namespace Atlas.Utils.Hla.Services
{
    public interface IHlaCategorisationService
    {
        HlaTypingCategory GetHlaTypingCategory(string hlaName);
    }

    public class HlaCategorisationService : IHlaCategorisationService
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

        public HlaTypingCategory GetHlaTypingCategory(string hlaName)
        {
            var name = hlaName.Trim().ToUpper();

            foreach (var pattern in TypingCategoryPatterns.Keys)
            {
                if (new Regex(pattern).IsMatch(name))
                {
                    return TypingCategoryPatterns[pattern];
                }
            }

            throw new AtlasHttpException(HttpStatusCode.BadRequest,
                    $"Typing category of HLA name: {name} could not be determined.");
        }
    }
}
