using System.Collections.Generic;
using FluentValidation.TestHelper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.Utils.TestUtils.Assertions;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Client.Test.Models
{
    [TestFixture]
    public class SearchRequestsValidatorTests
    {
        [Test]
        public void ShouldHaveValidationErrorFor_MissingMatchCriteria()
        {
            var validator = new SearchRequestValidator();
            validator.ShouldHaveValidationErrorFor(x => x.MatchCriteria, (MatchCriteria)null);
        }

        [Test]
        public void ShouldHaveValidationErrorFor_MissingRegistries()
        {
            var validator = new SearchRequestValidator();
            validator.ShouldHaveValidationErrorFor(x => x.RegistriesToSearch, (IEnumerable<RegistryCode>)null);
        }
    }
}
