using System;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using NSubstitute;

namespace Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders
{
    internal class HlaMetadataDictionaryBuilder : IHlaMetadataDictionaryFactory
    {
        private IRecreateHlaMetadataService recreate;
        private IHlaConverter converter;
        private IHlaValidator validator;
        private IHlaMatchingMetadataService matching;
        private ILocusHlaMatchingMetadataService locus;
        private IHlaScoringMetadataService scoring;
        private IDpb1TceGroupMetadataService dpb1;
        private IGGroupToPGroupMetadataService gGroupToPGroup;
        private ISmallGGroupToPGroupMetadataService smallGGroupToPGroup;
        private IHlaMetadataGenerationOrchestrator metadata;
        private IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private ILogger logger;
        private IHlaMetadataDictionary cannedResponse = null;

        public HlaMetadataDictionaryBuilder()
        {
            ResetAllDependencies();
        }

        private void ResetAllDependencies()
        {
            recreate = Substitute.For<IRecreateHlaMetadataService>();
            converter = Substitute.For<IHlaConverter>();
            validator = Substitute.For<IHlaValidator>();
            matching = Substitute.For<IHlaMatchingMetadataService>();
            locus = Substitute.For<ILocusHlaMatchingMetadataService>();
            scoring = Substitute.For<IHlaScoringMetadataService>();
            dpb1 = Substitute.For<IDpb1TceGroupMetadataService>();
            gGroupToPGroup = Substitute.For<IGGroupToPGroupMetadataService>();
            smallGGroupToPGroup = Substitute.For<ISmallGGroupToPGroupMetadataService>();
            metadata = Substitute.For<IHlaMetadataGenerationOrchestrator>();
            wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
            logger = Substitute.For<ILogger>();
        }

        public HlaMetadataDictionaryBuilder Returning(IHlaMetadataDictionary cannedResponse)
        {
            this.cannedResponse = cannedResponse;
            return this;
        }

        public HlaMetadataDictionaryBuilder Using<T>(T dependency)
        {
            switch (dependency)
            {
                case IRecreateHlaMetadataService typedDependency:
                    recreate = typedDependency;
                    break;
                case IHlaConverter typedDependency:
                    converter = typedDependency;
                    break;
                case IHlaValidator typedDependency:
                    validator = typedDependency;
                    break;
                case IHlaMatchingMetadataService typedDependency:
                    matching = typedDependency;
                    break;
                case ILocusHlaMatchingMetadataService typedDependency:
                    locus = typedDependency;
                    break;
                case IHlaScoringMetadataService typedDependency:
                    scoring = typedDependency;
                    break;
                case IDpb1TceGroupMetadataService typedDependency:
                    dpb1 = typedDependency;
                    break;
                case IGGroupToPGroupMetadataService typedDependency:
                    gGroupToPGroup = typedDependency;
                    break;
                case ISmallGGroupToPGroupMetadataService typedDependency:
                    smallGGroupToPGroup = typedDependency;
                    break;
                case IHlaMetadataGenerationOrchestrator typedDependency:
                    metadata = typedDependency;
                    break;
                case IWmdaHlaNomenclatureVersionAccessor typedDependency:
                    wmdaHlaNomenclatureVersionAccessor = typedDependency;
                    break;
                default:
                    throw new InvalidOperationException($"Type '{typeof(T).FullName}' does not match any expected dependency");
            }

            return this;
        }

        public IHlaMetadataDictionary BuildDictionary(string activeVersion)
        {
            if (cannedResponse != null)
            {
                return cannedResponse;
            }

            return new ExternalInterface.HlaMetadataDictionary(
                activeVersion,
                recreate,
                converter,
                validator,
                matching,
                locus,
                scoring,
                dpb1,
                gGroupToPGroup,
                smallGGroupToPGroup,
                metadata,
                wmdaHlaNomenclatureVersionAccessor,
                logger
            );
        }

        public IHlaMetadataCacheControl BuildCacheControl(string version)
        {
            throw new NotImplementedException();
        }
    }
}