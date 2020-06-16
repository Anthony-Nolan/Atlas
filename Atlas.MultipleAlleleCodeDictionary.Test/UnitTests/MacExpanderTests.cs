using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using Atlas.MultipleAlleleCodeDictionary.utils;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests
{
    [TestFixture]
    public class MacExpanderTests
    {
        private IMacExpander macExpander;

        [SetUp]
        public void SetUp()
        {
            macExpander = new MacExpander();
        }
        
        [Test]
        public void MacExpander_WillExpandSingleSpecificMac()
        {
            var singleSpecificHla = "01:02";
            var singleSpecificMac = new MacEntityBuilder.New.With(m => m.RowKey, "AA").Build();

            var result = macExpander.ExpandMac(singleSpecificHla);
            
            
        }
    }
}