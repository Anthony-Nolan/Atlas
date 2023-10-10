using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading
{
    /// see a publicly available version of this portion of the algorithm  https://www.ebi.ac.uk/cgi-bin/ipd/imgt/hla/dpb_v2.cgi
    public interface IDpb1TceGroupMatchCalculator
    {
        Task<Dpb1TceGroupMatchType> CalculateDpb1TceGroupMatchType(LocusInfo<string> patientHla, LocusInfo<string> donorHla);
    }

    internal class Dpb1TceGroupMatchCalculator : IDpb1TceGroupMatchCalculator
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;
        private readonly IHlaCategorisationService categoriser;
        private readonly ILogger logger;

        private const string TceGroup1 = "1";
        private const string TceGroup2 = "2";
        private const string TceGroup3 = "3";


        // Nested lookup of DonorTceGroups:(PatientTceGroups:MatchType)

        // Hashset<string> used for locus comparison as order does not matter.

        /// <summary>
        /// The number of TCE groups is known, and very limited. The specification of this algorithm explicitly lists the expected value for all possible
        /// cases, and has not defined the expected behaviour for any further TCE groups that may be introduced in future.
        ///
        /// Therefore this portion of the algorithm has been hard-coded to match the spec, rather than trying to work out any logical algorithm based
        /// on properties of the TCE groups. Such an algorithm could be derived, but would introduce a risk that the algorithm performs incorrectly
        /// upon introduction of any new TCE groups - which should all be considered <see cref="Dpb1TceGroupMatchType.Unknown"/> until a specification
        /// is agreed. 
        /// </summary>
        private readonly Dictionary<UnorderedPair<string>, Dictionary<UnorderedPair<string>, Dpb1TceGroupMatchType>> tceGroupMatchTypeLookups =
            new()
            {
                {
                    new UnorderedPair<string>(TceGroup1, TceGroup1),
                    new Dictionary<UnorderedPair<string>, Dpb1TceGroupMatchType>
                    {
                        {new UnorderedPair<string>(TceGroup1, TceGroup1), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup1, TceGroup2), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup1, TceGroup3), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup2, TceGroup2), Dpb1TceGroupMatchType.NonPermissiveHvG},
                        {new UnorderedPair<string>(TceGroup2, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveHvG},
                        {new UnorderedPair<string>(TceGroup3, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveHvG},
                    }
                },
                {
                    new UnorderedPair<string>(TceGroup1, TceGroup2),
                    new Dictionary<UnorderedPair<string>, Dpb1TceGroupMatchType>
                    {
                        {new UnorderedPair<string>(TceGroup1, TceGroup1), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup1, TceGroup2), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup1, TceGroup3), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup2, TceGroup2), Dpb1TceGroupMatchType.NonPermissiveHvG},
                        {new UnorderedPair<string>(TceGroup2, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveHvG},
                        {new UnorderedPair<string>(TceGroup3, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveHvG},
                    }
                },
                {
                    new UnorderedPair<string>(TceGroup1, TceGroup3),
                    new Dictionary<UnorderedPair<string>, Dpb1TceGroupMatchType>
                    {
                        {new UnorderedPair<string>(TceGroup1, TceGroup1), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup1, TceGroup2), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup1, TceGroup3), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup2, TceGroup2), Dpb1TceGroupMatchType.NonPermissiveHvG},
                        {new UnorderedPair<string>(TceGroup2, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveHvG},
                        {new UnorderedPair<string>(TceGroup3, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveHvG},
                    }
                },
                {
                    new UnorderedPair<string>(TceGroup2, TceGroup2),
                    new Dictionary<UnorderedPair<string>, Dpb1TceGroupMatchType>
                    {
                        {new UnorderedPair<string>(TceGroup1, TceGroup1), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup1, TceGroup2), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup1, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup2, TceGroup2), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup2, TceGroup3), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup3, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveHvG},
                    }
                },
                {
                    new UnorderedPair<string>(TceGroup2, TceGroup3),
                    new Dictionary<UnorderedPair<string>, Dpb1TceGroupMatchType>
                    {
                        {new UnorderedPair<string>(TceGroup1, TceGroup1), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup1, TceGroup2), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup1, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup2, TceGroup2), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup2, TceGroup3), Dpb1TceGroupMatchType.Permissive},
                        {new UnorderedPair<string>(TceGroup3, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveHvG},
                    }
                },
                {
                    new UnorderedPair<string>(TceGroup3, TceGroup3),
                    new Dictionary<UnorderedPair<string>, Dpb1TceGroupMatchType>
                    {
                        {new UnorderedPair<string>(TceGroup1, TceGroup1), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup1, TceGroup2), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup1, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup2, TceGroup2), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup2, TceGroup3), Dpb1TceGroupMatchType.NonPermissiveGvH},
                        {new UnorderedPair<string>(TceGroup3, TceGroup3), Dpb1TceGroupMatchType.Permissive},
                    }
                },
            };

        public Dpb1TceGroupMatchCalculator(
            IHlaMetadataDictionaryFactory hmdFactory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IHlaCategorisationService categoriser,
            ILogger logger)
        {
            hlaMetadataDictionary = hmdFactory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
            this.categoriser = categoriser;
            this.logger = logger;
        }

        public async Task<Dpb1TceGroupMatchType> CalculateDpb1TceGroupMatchType(LocusInfo<string> patientHla, LocusInfo<string> donorHla)
        {
            var patientTceGroups = await GetTceGroupsForMatchCalculation(patientHla);
            var donorTceGroups = await GetTceGroupsForMatchCalculation(donorHla);

            if (patientTceGroups is null || donorTceGroups is null)
            {
                return Dpb1TceGroupMatchType.Unknown;
            }

            if (!tceGroupMatchTypeLookups.TryGetValue(donorTceGroups, out var donorLookup))
            {
                logger.SendTrace(
                    $"Could not find donor TCE group pair: ({donorTceGroups.Item1}, {donorTceGroups.Item2}) in hardcoded DPB1 TCE group matching behaviour. Something has likely gone wrong, and the Unknown grade will be used until this is fixed.",
                    LogLevel.Error);

                return Dpb1TceGroupMatchType.Unknown;
            }

            if (!donorLookup.TryGetValue(patientTceGroups, out var result))
            {
                logger.SendTrace(
                    $"Could not find patient TCE group pair: ({patientTceGroups.Item1}, {patientTceGroups.Item2}) in hardcoded DPB1 TCE group matching behaviour. Something has likely gone wrong, and the Unknown grade will be used until this is fixed.",
                    LogLevel.Error);

                return Dpb1TceGroupMatchType.Unknown;
            }

            return result;
        }

        ///<summary>Note: typings with a single null allele will be treated as homozygous for the remaining expressing typing.</summary>
        /// <returns>Will return `null` in the following cases where match calculation would not be possible:
        /// 1) <paramref name="typing"/> is `null` or is empty at one or both positions.
        /// 2) Both positions are null expressing alleles.
        /// 3) After TCE group lookup, either position does not have a TCE group assigned.
        /// </returns>
        private async Task<UnorderedPair<string>> GetTceGroupsForMatchCalculation(LocusInfo<string> typing)
        {
            if (typing?.Position1 is null || typing.Position2 is null)
            {
                return null;
            }

            var isHla1ANullAllele = categoriser.IsNullAllele(typing.Position1);
            var isHla2ANullAllele = categoriser.IsNullAllele(typing.Position2);

            if (isHla1ANullAllele && isHla2ANullAllele)
            {
                return null;
            }

            // if either position is a null allele then use the tce group for the expressing allele in the other position
            var tceGroupResult1 = await LookupDpb1TceGroup(isHla1ANullAllele ? typing.Position2 : typing.Position1);
            var tceGroupResult2 = await LookupDpb1TceGroup(isHla2ANullAllele ? typing.Position1 : typing.Position2);

            if (NoTceGroupAssigned(tceGroupResult1) || NoTceGroupAssigned(tceGroupResult2))
            {
                return null;
            }

            static bool NoTceGroupAssigned(string lookupResult) => string.IsNullOrEmpty(lookupResult);

            return new UnorderedPair<string>(tceGroupResult1, tceGroupResult2);
        }

        private Task<string> LookupDpb1TceGroup(string hlaName) => hlaMetadataDictionary.GetDpb1TceGroup(hlaName);
    }
}