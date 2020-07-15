using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services
{
    internal interface ILocusHlaConverter
    {
        /// <summary>
        /// HLA type at each locus within the provided phenotype will be converted to the target HLA category.
        /// </summary>
        public Task<PhenotypeInfo<IReadOnlyCollection<string>>> ConvertHla(
            PhenotypeInfo<string> hlaInfo,
            TargetHlaCategory targetHlaCategory,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci);
    }

    internal class LocusHlaConverter : ILocusHlaConverter
    {
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public LocusHlaConverter(IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        public async Task<PhenotypeInfo<IReadOnlyCollection<string>>> ConvertHla(
            PhenotypeInfo<string> hlaInfo,
            TargetHlaCategory targetHlaCategory,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci)
        {
            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            return await hlaInfo.MapAsync(async (locus, position, hla) =>
                allowedLoci.Contains(locus) ? await hlaMetadataDictionary.ConvertHla(locus, hla, targetHlaCategory) : null
            );
        }
    }
}