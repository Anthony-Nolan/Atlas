using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
// ONE haplotype's names - one per locus, at the resolution the frequency set stored them. A LociInfo, not a
// PhenotypeInfo: the genotype form (HfSetGenotypeNames) holds two names per locus, being a pair of these.
using HfSetHaplotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    /// <summary>
    /// Reads haplotype frequency sets and their frequencies. The read half of <see cref="IHaplotypeFrequencyService"/>,
    /// kept separate so that the genotype set pipeline can be hosted without the frequency set import - whose
    /// importer and notification sender would otherwise bring Service Bus settings into every host.
    /// </summary>
    public interface IHaplotypeFrequencyLookupService
    {
        public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(FrequencySetMetadata donorInfo, FrequencySetMetadata patientInfo);

        public Task<HaplotypeFrequencySet> GetSingleHaplotypeFrequencySet(FrequencySetMetadata setMetaData);

        Task<FrequencySetCacheEntry> GetAllHaplotypeFrequencies(int setId);

        /// <param name="setId"></param>
        /// <param name="hla"></param>
        /// <param name="excludedLoci">
        /// Any loci specified here will not be considered when fetching frequencies.
        /// If multiple haplotypes match the provided hla at all other loci, such frequencies will be summed.
        /// </param>
        /// <returns>
        /// The haplotype frequency for the given haplotype hla, from the given set.
        /// If the given hla is unrepresented in the set, will return 0.
        /// </returns>
        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        Task<decimal> GetFrequencyForHla(int setId, HfSetHaplotypeNames hla, ISet<Locus> excludedLoci);
    }

    internal class HaplotypeFrequencyLookupService : IHaplotypeFrequencyLookupService
    {
        protected readonly IAtlasLogger Logger;
        protected readonly IHaplotypeFrequencyCache HaplotypeFrequencyCache;

        public HaplotypeFrequencyLookupService(
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IHaplotypeFrequencyCache haplotypeFrequencyCache)
        {
            Logger = logger;
            HaplotypeFrequencyCache = haplotypeFrequencyCache;
        }

        public async Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(FrequencySetMetadata donorInfo, FrequencySetMetadata patientInfo)
        {
            using (Logger.RunTimed("Get HF Sets", LogLevel.Verbose))
            {
                donorInfo ??= new FrequencySetMetadata();
                patientInfo ??= new FrequencySetMetadata();

                var donorSet = await GetSingleHaplotypeFrequencySet(donorInfo);
                var patientSet = await GetSingleHaplotypeFrequencySet(patientInfo);

                Logger.SendTrace($"Frequency Set Selection: Donor {donorSet.RegistryCode}/{donorSet.EthnicityCode}/{donorSet.Id}");
                Logger.SendTrace($"Frequency Set Selection: Patient {patientSet.RegistryCode}/{patientSet.EthnicityCode}/{patientSet.Id}");

                return new HaplotypeFrequencySetResponse
                {
                    DonorSet = donorSet,
                    PatientSet = patientSet
                };
            }
        }

        public async Task<HaplotypeFrequencySet> GetSingleHaplotypeFrequencySet(FrequencySetMetadata setMetaData)
        {
            var activeSets = await HaplotypeFrequencyCache.GetActiveHaplotypeFrequencySets();

            // Attempt to get the most specific sets first
            var set = activeSets.GetValueOrDefault((setMetaData.RegistryCode, setMetaData.EthnicityCode));

            // If we didn't find ethnicity sets, find a generic one for that repository
            set ??= activeSets.GetValueOrDefault((setMetaData.RegistryCode, (string)null));

            // If no registry specific set exists, use a generic one.
            set ??= activeSets.GetValueOrDefault(((string)null, (string)null));
            if (set == null)
            {
                Logger.SendTrace(
                    $"Did not find Haplotype Frequency Set for: Registry: {setMetaData.RegistryCode} Donor Ethnicity: {setMetaData.EthnicityCode}",
                    LogLevel.Error
                );
                throw new Exception("No Global Haplotype frequency set was found");
            }

            return set;
        }

        /// <inheritdoc />
        public Task<FrequencySetCacheEntry> GetAllHaplotypeFrequencies(int setId)
        {
            return HaplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId);
        }

        /// <inheritdoc />
        public async Task<decimal> GetFrequencyForHla(int setId, HfSetHaplotypeNames hla, ISet<Locus> excludedLoci)
        {
            var entry = await HaplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId);

            // The interner resolves a key when every allele is known to the set individually - but that does NOT
            // guarantee the *combination* is a stored haplotype (e.g. two haplotypes sharing alleles produce
            // resolvable cross-combinations that were never imported). So we must still probe the dictionary
            // rather than indexing into it, otherwise an unrepresented-but-resolvable haplotype throws instead of
            // falling through to the unrepresented (0 / consolidate) handling below.
            if (entry.Interner.TryResolve(a: hla.A, b: hla.B, c: hla.C, dqb1: hla.Dqb1, drb1: hla.Drb1, out var resolvedHaplotypeKey)
                && entry.SetFrequencies.TryGetValue(resolvedHaplotypeKey, out var haplotypeFrequency))
            {
                return haplotypeFrequency.Frequency;
            }

            // If no loci are excluded, there is nothing to calculate - the haplotype is just unrepresented.
            // We do not want to add all unrepresented haplotypes to the cache - this drastically reduces algorithm speed, increases memory, and has no benefit
            if (!excludedLoci.Any())
            {
                return 0;
            }

            return await HaplotypeFrequencyCache.GetConsolidatedFrequency(setId, hla, excludedLoci);
        }
    }
}
