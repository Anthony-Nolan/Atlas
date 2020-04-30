using System;
using FluentAssertions;
using Atlas.Utils.Core.Common;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Common
{
    [TestFixture]
    public class PreconditionExtensionsTests
    {
        [Test]
        public void GivenNonNullParameter_AssertArgNotNull_ReturnsInputParameter()
        {
            var param = new object();
            param.AssertArgumentNotNull("someParam").Should().BeSameAs(param);
        }

        [Test]
        public void GivenNullParameter_AssertArgNotNull_ThrowsArgNullException()
        {
            Action act = () => ((object)null).AssertArgumentNotNull("someParam");
            act.ShouldThrow<ArgumentNullException>().WithMessage("Value cannot be null.\r\nParameter name: someParam");
        }

        [Test]
        public void GivenValidParameter_AssertArg_ReturnsInputParameter()
        {
            "param".AssertArgument(p => p == "param", "Param is dodgy").Should().BeSameAs("param");
        }

        [Test]
        public void GivenInvalidParameter_AssertAr_ThrowsArgException()
        {
            Action act = () => "wrong".AssertArgument(p => p == "right", "Dodgy parameter", "someParam");
            act.ShouldThrow<ArgumentException>().WithMessage("Dodgy parameter\r\nParameter name: someParam");
        }
    }
}
