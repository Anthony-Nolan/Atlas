using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
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
            new Dictionary<UnorderedPair<string>, Dictionary<UnorderedPair<string>, Dpb1TceGroupMatchType>>
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
            ILogger logger)
        {
            hlaMetadataDictionary = hmdFactory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
            this.logger = logger;
        }

        public async Task<Dpb1TceGroupMatchType> CalculateDpb1TceGroupMatchType(LocusInfo<string> patientHla, LocusInfo<string> donorHla)
        {
            if (patientHla == null
                || patientHla.Position1 == null
                || patientHla.Position2 == null
                || donorHla == null
                || donorHla.Position1 == null
                || donorHla.Position2 == null)
            {
                return Dpb1TceGroupMatchType.Unknown;
            }

            var patientTceGroup1 = await GetDpb1TceGroup(patientHla.Position1);
            var patientTceGroup2 = await GetDpb1TceGroup(patientHla.Position2);
            var donorTceGroup1 = await GetDpb1TceGroup(donorHla.Position1);
            var donorTceGroup2 = await GetDpb1TceGroup(donorHla.Position2);

            if (patientTceGroup1 == null
                || patientTceGroup2 == null
                || donorTceGroup1 == null
                || donorTceGroup2 == null)
            {
                return Dpb1TceGroupMatchType.Unknown;
            }

            var patientTceGroups = new UnorderedPair<string>(patientTceGroup1, patientTceGroup2);
            var donorTceGroups = new UnorderedPair<string>(donorTceGroup1, donorTceGroup2);

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

        private Task<string> GetDpb1TceGroup(string alleleName) => hlaMetadataDictionary.GetDpb1TceGroup(alleleName);
    }
}