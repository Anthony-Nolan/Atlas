using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Config;

namespace Atlas.MatchPrediction.Services
{
    public interface ILocusHlaConverter
    {
        /// <summary>
        /// HLA type at each locus within the provided phenotype will be converted to the target HLA category.
        /// </summary>
        public Task<PhenotypeInfo<IReadOnlyCollection<string>>> ConvertHla(
            PhenotypeInfo<string> hlaInfo,
            TargetHlaCategory targetHlaCategory,
            string hlaNomenclatureVersion);
    }

    public class LocusHlaConverter : ILocusHlaConverter
    {
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public LocusHlaConverter(IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        public async Task<PhenotypeInfo<IReadOnlyCollection<string>>> ConvertHla(
            PhenotypeInfo<string> hlaInfo,
            TargetHlaCategory targetHlaCategory,
            string hlaNomenclatureVersion)
        {
            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);
            var allowedLoci = LocusSettings.MatchPredictionLoci.ToList();

            return await hlaInfo.MapAsync(async (locus, position, hla) =>
                allowedLoci.Contains(locus)
                    ? await hlaMetadataDictionary.ConvertHla(locus, hla, targetHlaCategory)
                    : null);
        }
    }
}