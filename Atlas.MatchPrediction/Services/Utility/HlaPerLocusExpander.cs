using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services.Utility
{
    public interface IHlaPerLocusExpander
    {
        public Task<PhenotypeInfo<IReadOnlyCollection<string>>> Expand(
            PhenotypeInfo<string> hlaInfo,
            TargetHlaCategory targetHlaCategory,
            string hlaNomenclatureVersion);
    }

    public class HlaPerLocusExpander : IHlaPerLocusExpander
    {
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public HlaPerLocusExpander(IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        public async Task<PhenotypeInfo<IReadOnlyCollection<string>>> Expand(
            PhenotypeInfo<string> hlaInfo,
            TargetHlaCategory targetHlaCategory,
            string hlaNomenclatureVersion)
        {
            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            return await hlaInfo.MapAsync(async (locus, position, hla) => 
                {
                    if (locus == Locus.Dpb1)
                    {
                        return null;
                    }

                    return await hlaMetadataDictionary.ConvertHla(locus, hla, targetHlaCategory);
                }
            );
        }
    }
}