using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.MatchedHlaConversion;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.MatchedHlaConversion
{
    internal abstract class MatchedHlaToMetaDataConverterTestBase<THlaDataConverter>
        where THlaDataConverter : IMatchedHlaToMetaDataConverterBase, new()
    {
        protected THlaDataConverter MetadataConverter;
        protected const Locus MatchedLocus = Locus.A;
        protected const SerologySubtype SeroSubtype = SerologySubtype.Broad;
        protected const string SerologyName = "999";
        protected const string PGroupName = "999:999P";
        protected const string GGroupName = "999:999G";
        protected const bool IsDirectMapping = true;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                MetadataConverter = new THlaDataConverter();
            });
        }

        [Test]
        public void ConvertToHlaMetadata_WhenSerology_OneMetadatumGenerated()
        {
            var serologyTyping = new SerologyTyping(MatchedLocus.ToString(), SerologyName, SeroSubtype);

            var infoForMatching = new SerologyInfoForMatching(
                serologyTyping, serologyTyping, new[] { new MatchingSerology(serologyTyping, IsDirectMapping) });

            var matchedSerology = new MatchedSerology(
                infoForMatching,
                new List<string> { PGroupName },
                new List<string> { GGroupName },
                new List<SerologyToAlleleMapping>()
                );

            var actualMetadata = MetadataConverter.ConvertToHlaMetadata(new[] { matchedSerology });

            var expectedMetadata = new List<IHlaMetadata>
            {
                BuildSerologyHlaMetadata()
            };

            actualMetadata.Should().BeEquivalentTo(expectedMetadata);
        }

        #region Tests To Implement

        public abstract void ConvertToHlaMetadata_WhenTwoFieldExpressingAllele_GeneratesMetadata_ForOriginalNameAndXxCode(
            string alleleName, string xxCodeLookupName);

        public abstract void ConvertToHlaMetadata_WhenThreeOrFourFieldExpressingAllele_GeneratesMetadata_ForOriginalNameAndNmdpCodeAndXxCode(
            string alleleName, string expressionSuffix, string nmdpCodeLookupName, string xxCodeLookupName);

        public abstract void ConvertToHlaMetadata_WhenNullAllele_GeneratesMetadata_ForOriginalNameOnly(string alleleName);

        public abstract void ConvertToHlaMetadata_WhenAllelesHaveSameTruncatedNameVariant_GeneratesMetadata_ForEachUniqueLookupName();

        #endregion

        #region Methods to Implement

        protected abstract IHlaMetadata BuildSerologyHlaMetadata();

        #endregion

        /// <summary>
        /// Builds a Matched Allele.
        /// Submitted allele name is used for the Name, and the Matching P/G groups.
        /// Every allele will have the same Serology mapping, built from constant values.
        /// </summary>
        protected static MatchedAllele BuildMatchedAllele(string alleleName)
        {
            var hlaTyping = new AlleleTyping(MatchedLocus, alleleName);
            var infoForMatching = new AlleleInfoForMatching(hlaTyping, hlaTyping, alleleName, alleleName);

            var serologyTyping = new SerologyTyping(MatchedLocus.ToString(), SerologyName, SeroSubtype);
            var matchingSerologies = new[] { new MatchingSerology(serologyTyping, true) };

            return new MatchedAllele(infoForMatching, matchingSerologies);
        }
    }
}
