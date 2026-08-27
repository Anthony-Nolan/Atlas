using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class MacLookup : AlleleNamesLookupBase
    {
        private readonly IMacDictionary macDictionary;

        public MacLookup(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IMacDictionary macDictionary)
            : base(hlaMetadataRepository, alleleNamesMetadataService)
        {
            this.macDictionary = macDictionary;
        }

        protected override async Task<List<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            try
            {
                return (await macDictionary.GetHlaFromMac(lookupName)).ToList();
            }
            // The MAC dictionary has its own vocabulary for "not in the store", and neither term of it is an
            // HlaMetadataDictionaryException. While GetMetadata re-labelled everything that did not matter; now it
            // does, and an unrecognised MAC would otherwise leave here as an infrastructure fault and fail the
            // request rather than being reported as a name with no data. Anything else - a failed storage request
            // inside the MAC store - is left alone, which is the whole point.
            catch (Exception e) when (e is MacNotFoundException or ArgumentException)
            {
                throw new InvalidHlaException(locus, lookupName);
            }
        }
    }
}