using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.Services.HlaValidation;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    public interface IHlaMetadataDictionary
    {
        Task<string> RecreateHlaMetadataDictionary(CreationBehaviour recreationBehaviour);
        Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, TargetHlaCategory targetHlaCategory);

        /// <summary>
        /// Validate whether HLA is a type of the the target HLA category.
        /// </summary>
        /// <param name="locus">Locus hla is present at.</param>
        /// <param name="hlaName">The hla you want to validate the type of.</param>
        /// <param name="targetHlaCategory">The hla category you want to see if the hla is a part of.</param>
        /// <returns></returns>
        Task<bool> ValidateHla(Locus locus, string hlaName, HlaValidationCategory targetHlaCategory);

        /// <summary>
        /// Functionally the same as calling ConvertHla on GGroup typed hla, with a target type of PGroup.
        /// As GGroups are guaranteed to correspond to exactly 0 or 1 PGroups, this method makes this specific conversion much faster.  
        /// </summary>
        public Task<string> ConvertGGroupToPGroup(Locus locus, string gGroup);

        /// <summary>
        /// Functionally the same as calling ConvertHla on small g group typed hla, with a target type of P group.
        /// As small g groups are guaranteed to correspond to exactly 0 or 1 PGroups, this method makes this specific conversion much faster.  
        /// </summary>
        public Task<string> ConvertSmallGGroupToPGroup(Locus locus, string smallGGroup);

        Task<LocusInfo<INullHandledHlaMatchingMetadata>> GetLocusHlaMatchingMetadata(Locus locus, LocusInfo<string> locusTyping);
        Task<IHlaScoringMetadata> GetHlaScoringMetadata(Locus locus, string hlaName);
        Task<string> GetDpb1TceGroup(string dpb1HlaName);
        Task<IEnumerable<string>> GetAllPGroups();
        Task<IDictionary<Locus, List<string>>> GetAllGGroups();

        /// <summary>
        /// This is not the intended entry point for consumption of the metadata;
        /// the only expected use case is for manual recreation of the test HLA metadata dictionary.
        /// </summary>
        HlaMetadataCollection GenerateAllHlaMetadata(string version);

        /// <summary>
        /// Indicates whether there's a discrepancy between the version of the HLA Nomenclature that we would use from WMDA,
        /// and the version of the HLA Nomenclature that was used to pre-process the current Donor data.
        /// </summary>
        /// <returns>True if the versions are different, otherwise false.</returns>
        bool IsActiveVersionDifferentFromLatestVersion();

        string ActiveHlaNomenclatureVersion { get; }
    }

    internal class HlaMetadataDictionary : IHlaMetadataDictionary
    {
        /// <summary>
        /// The active HLA Nomenclature version, or <see cref="HlaMetadataDictionaryConstants.NoActiveVersionValue"/> in the case when no refresh has been run.
        /// </summary>
        private readonly string activeHlaNomenclatureVersionOrDefault;

        private readonly IRecreateHlaMetadataService recreateMetadataService;
        private readonly IHlaConverter hlaConverter;
        private readonly IHlaValidator hlaValidator;
        private readonly IHlaMatchingMetadataService hlaMatchingMetadataService;
        private readonly ILocusHlaMatchingMetadataService locusHlaMatchingMetadataService;
        private readonly IHlaScoringMetadataService hlaScoringMetadataService;
        private readonly IDpb1TceGroupMetadataService dpb1TceGroupMetadataService;
        private readonly IGGroupToPGroupMetadataService gGroupToPGroupMetadataService;
        private readonly ISmallGGroupToPGroupMetadataService smallGGroupToPGroupMetadataService;
        private readonly IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator;
        private readonly IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private readonly ILogger logger;

        public HlaMetadataDictionary(
            string activeHlaNomenclatureVersionOrDefault,
            IRecreateHlaMetadataService recreateMetadataService,
            IHlaConverter hlaConverter,
            IHlaValidator hlaValidator,
            IHlaMatchingMetadataService hlaMatchingMetadataService,
            ILocusHlaMatchingMetadataService locusHlaMatchingMetadataService,
            IHlaScoringMetadataService hlaScoringMetadataService,
            IDpb1TceGroupMetadataService dpb1TceGroupMetadataService,
            IGGroupToPGroupMetadataService gGroupToPGroupMetadataService,
            ISmallGGroupToPGroupMetadataService smallGGroupToPGroupMetadataService,
            IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator,
            IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor,
            ILogger logger)
        {
            this.activeHlaNomenclatureVersionOrDefault = activeHlaNomenclatureVersionOrDefault;
            this.recreateMetadataService = recreateMetadataService;
            this.hlaConverter = hlaConverter;
            this.hlaValidator = hlaValidator;
            this.hlaMatchingMetadataService = hlaMatchingMetadataService;
            this.locusHlaMatchingMetadataService = locusHlaMatchingMetadataService;
            this.hlaScoringMetadataService = hlaScoringMetadataService;
            this.dpb1TceGroupMetadataService = dpb1TceGroupMetadataService;
            this.gGroupToPGroupMetadataService = gGroupToPGroupMetadataService;
            this.smallGGroupToPGroupMetadataService = smallGGroupToPGroupMetadataService;
            this.hlaMetadataGenerationOrchestrator = hlaMetadataGenerationOrchestrator;
            this.wmdaHlaNomenclatureVersionAccessor = wmdaHlaNomenclatureVersionAccessor;
            this.logger = logger;
        }

        public string ActiveHlaNomenclatureVersion => activeHlaNomenclatureVersionOrDefault == HlaMetadataDictionaryConstants.NoActiveVersionValue
            ? throw new Exception(
                "HLA Metadata Dictionary with no HLA nomenclature version cannot be used for anything but regenerating a new dictionary.")
            : activeHlaNomenclatureVersionOrDefault;

        public bool IsActiveVersionDifferentFromLatestVersion()
        {
            var active = activeHlaNomenclatureVersionOrDefault;
            var latest = wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion();
            return active != latest;
        }

        public async Task<string> RecreateHlaMetadataDictionary(CreationBehaviour recreationBehaviour)
        {
            var version = IdentifyVersionToRecreate(recreationBehaviour);

            if (ShouldRecreate(recreationBehaviour))
            {
                logger.SendTrace($"HLA-METADATA-DICTIONARY REFRESH: Recreating HLA Metadata dictionary for desired HLA Nomenclature version.");
                await recreateMetadataService.RefreshAllHlaMetadata(version);
                logger.SendTrace($"HLA-METADATA-DICTIONARY REFRESH: HLA Metadata dictionary recreated at HLA Nomenclature version: {version}");
            }
            else
            {
                logger.SendTrace(
                    $"HLA-METADATA-DICTIONARY REFRESH: HLA Metadata dictionary was already using the desired HLA Nomenclature version, so did not update.");
            }

            return version;
        }

        public async Task<bool> ValidateHla(Locus locus, string hlaName, HlaValidationCategory targetHlaCategory)
        {
            return await hlaValidator.ValidateHla(locus, hlaName, new HlaValidationBehaviour
            {
                HlaNomenclatureVersion = ActiveHlaNomenclatureVersion,
                TargetHlaCategory = targetHlaCategory
            });
        }

        private bool ShouldRecreate(CreationBehaviour creationConfig)
        {
            return creationConfig.CreationMode switch
            {
                CreationBehaviour.Mode.Latest => IsActiveVersionDifferentFromLatestVersion() || creationConfig.ShouldForce,
                CreationBehaviour.Mode.Active => creationConfig.ShouldForce ? true : throw new NotImplementedException(),
                CreationBehaviour.Mode.Specific => creationConfig.ShouldForce ? true : throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(nameof(creationConfig.CreationMode), creationConfig.CreationMode, "Unexpected enum value")
            };
        }

        private string IdentifyVersionToRecreate(CreationBehaviour creationConfig)
        {
            return creationConfig.CreationMode switch
            {
                CreationBehaviour.Mode.Specific => creationConfig.SpecificVersion,
                CreationBehaviour.Mode.Active => ActiveHlaNomenclatureVersion,
                CreationBehaviour.Mode.Latest => wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion(),
                _ => throw new ArgumentOutOfRangeException(nameof(creationConfig.CreationMode), creationConfig.CreationMode, "Unexpected enum value")
            };
        }

        public async Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, TargetHlaCategory targetHlaCategory)
        {
            return await hlaConverter.ConvertHla(locus, hlaName, new HlaConversionBehaviour
            {
                HlaNomenclatureVersion = ActiveHlaNomenclatureVersion,
                TargetHlaCategory = targetHlaCategory
            });
        }

        public async Task<LocusInfo<INullHandledHlaMatchingMetadata>> GetLocusHlaMatchingMetadata(Locus locus, LocusInfo<string> locusTyping)
        {
            return await locusHlaMatchingMetadataService.GetHlaMatchingMetadata(locus, locusTyping, ActiveHlaNomenclatureVersion);
        }

        public async Task<IHlaScoringMetadata> GetHlaScoringMetadata(Locus locus, string hlaName)
        {
            return await hlaScoringMetadataService.GetHlaMetadata(locus, hlaName, ActiveHlaNomenclatureVersion);
        }

        public async Task<string> GetDpb1TceGroup(string dpb1HlaName)
        {
            return await dpb1TceGroupMetadataService.GetDpb1TceGroup(dpb1HlaName, ActiveHlaNomenclatureVersion);
        }

        public async Task<string> ConvertGGroupToPGroup(Locus locus, string gGroup)
        {
            return await gGroupToPGroupMetadataService.ConvertGGroupToPGroup(locus, gGroup, ActiveHlaNomenclatureVersion);
        }

        public async Task<string> ConvertSmallGGroupToPGroup(Locus locus, string smallGGroup)
        {
            return await smallGGroupToPGroupMetadataService.ConvertSmallGGroupToPGroup(locus, smallGGroup, ActiveHlaNomenclatureVersion);
        }

        public async Task<IEnumerable<string>> GetAllPGroups()
        {
            return await hlaMatchingMetadataService.GetAllPGroups(ActiveHlaNomenclatureVersion);
        }

        public async Task<IDictionary<Locus, List<string>>> GetAllGGroups()
        {
            return await hlaScoringMetadataService.GetAllGGroups(ActiveHlaNomenclatureVersion);
        }

        public HlaMetadataCollection GenerateAllHlaMetadata(string version)
        {
            return hlaMetadataGenerationOrchestrator.GenerateAllHlaMetadata(version);
        }
    }
}