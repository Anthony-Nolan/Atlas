using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Http;

namespace Atlas.HlaMetadataDictionary.InternalExceptions
{
    /// <summary>
    /// The categorisation service reports a name it cannot parse by throwing <see cref="AtlasHttpException"/> - a
    /// sensible answer at the API boundary it was written for, and the wrong one here.
    /// </summary>
    /// <remarks>
    /// Inside this dictionary an unparseable name is a name with no data, and it has to say so in this dictionary's
    /// own vocabulary. <c>MetadataServiceBase.GetMetadata</c> used to re-label every exception as an
    /// <c>HlaMetadataDictionaryException</c>, so it did not matter what came past it; now that only genuine misses
    /// are re-thrown, an <see cref="AtlasHttpException"/> escaping a lookup would be read as an infrastructure fault
    /// and fail a whole donor batch over one malformed donor record.
    /// </remarks>
    internal static class HlaCategorisationExtensions
    {
        /// <summary>
        /// <see cref="IHlaCategorisationService.GetHlaTypingCategory"/>, reporting a name it cannot parse as
        /// <see cref="InvalidHlaException"/>.
        /// </summary>
        public static HlaTypingCategory GetCategoryOrThrowInvalidHla(
            this IHlaCategorisationService categorisationService,
            Locus locus,
            string hlaName)
        {
            try
            {
                return categorisationService.GetHlaTypingCategory(hlaName);
            }
            catch (AtlasHttpException)
            {
                throw new InvalidHlaException(locus, hlaName);
            }
        }

        /// <summary>
        /// <see cref="IHlaCategorisationService.GetHlaTypingCategory"/>, reporting a name it cannot parse as
        /// <c>false</c>. For the callers that are deciding whether a name is worth looking up at all, where "cannot
        /// be parsed" and "is not the category I handle" are the same answer.
        /// </summary>
        public static bool TryGetCategory(
            this IHlaCategorisationService categorisationService,
            string hlaName,
            out HlaTypingCategory category)
        {
            try
            {
                category = categorisationService.GetHlaTypingCategory(hlaName);
                return true;
            }
            catch (AtlasHttpException)
            {
                category = default;
                return false;
            }
        }
    }
}
