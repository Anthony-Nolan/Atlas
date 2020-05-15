using FluentAssertions;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using Atlas.HlaMetadataDictionary.Services.HlaDataConversion;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Atlas.Common.GeneticData;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Services.HlaDataConversion
{
    public abstract class MatchedHlaDataConverterTestBase<TGenerator>
        where TGenerator : IMatchedHlaDataConverterBase, new()
    {
        protected TGenerator LookupResultGenerator;
        protected const Locus MatchedLocus = Locus.A;
        protected const SerologySubtype SeroSubtype = SerologySubtype.Broad;
        protected const string SerologyName = "999";
        protected const string PGroupName = "999:999P";
        protected const string GGroupName = "999:999G";
        protected const bool IsDirectMapping = true;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            LookupResultGenerator = new TGenerator();
        }

        [Test]
        public void ConvertToHlaLookupResults_WhenSerology_OneLookupResultGenerated()
        {
            var serologyTyping = new SerologyTyping(MatchedLocus.ToString(), SerologyName, SeroSubtype);

            var infoForMatching = Substitute.For<ISerologyInfoForMatching>();
            infoForMatching.HlaTyping.Returns(serologyTyping);
            infoForMatching.MatchingSerologies.Returns(new[] { new MatchingSerology(serologyTyping, IsDirectMapping) });

            var matchedSerology = new MatchedSerology(
                infoForMatching,
                new List<string> { PGroupName },
                new List<string> { GGroupName });

            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedSerology });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildSerologyHlaLookupResult()
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        #region Tests To Implement

        public abstract void ConvertToHlaLookupResults_WhenTwoFieldExpressingAllele_GeneratesLookupResults_ForOriginalNameAndXxCode(
            string alleleName, string xxCodeLookupName);

        public abstract void ConvertToHlaLookupResults_WhenThreeOrFourFieldExpressingAllele_GeneratesLookupResults_ForOriginalNameAndNmdpCodeAndXxCode(
            string alleleName, string expressionSuffix, string nmdpCodeLookupName, string xxCodeLookupName);

        public abstract void ConvertToHlaLookupResults_WhenNullAllele_GeneratesLookupResults_ForOriginalNameOnly(string alleleName);

        public abstract void ConvertToHlaLookupResults_WhenAllelesHaveSameTruncatedNameVariant_GeneratesLookupResult_ForEachUniqueLookupName();

        #endregion

        #region Methods to Implement

        protected abstract IHlaLookupResult BuildSerologyHlaLookupResult();

        #endregion

        /// <summary>
        /// Builds a Matched Allele.
        /// Submitted allele name is used for the Name, and the Matching P/G groups.
        /// Every allele will have the same Serology mapping, built from constant values.
        /// </summary>
        protected static MatchedAllele BuildMatchedAllele(string alleleName)
        {
            var hlaTyping = new AlleleTyping(MatchedLocus, alleleName);
            var alleleGroup = new[] { alleleName };
            var infoForMatching = new AlleleInfoForMatching(hlaTyping, hlaTyping, alleleGroup, alleleGroup);

            var serologyTyping = new SerologyTyping(MatchedLocus.ToString(), SerologyName, SeroSubtype);
            var matchingSerologies = new[] { new MatchingSerology(serologyTyping, true) };

            return new MatchedAllele(infoForMatching, matchingSerologies);
        }
    }
}
