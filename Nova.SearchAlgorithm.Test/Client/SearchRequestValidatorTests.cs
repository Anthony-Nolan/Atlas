using System.Collections.Generic;
using FluentValidation.TestHelper;
using Nova.SearchAlgorithm.Client.Models;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Client
{
    [TestFixture]
    public class SearchRequestsValidatorTests
    {
        [Test]
        public void ShouldHaveValidationErrorFor_MissingMatchCriteria()
        {
            var validator = new SearchRequestValidator();
            validator.ShouldHaveValidationErrorFor(x => x.MatchCriteria, (MismatchCriteria)null);
        }

        [Test]
        public void ShouldHaveValidationErrorFor_MissingRegistries()
        {
            var validator = new SearchRequestValidator();
            validator.ShouldHaveValidationErrorFor(x => x.RegistriesToSearch, (IEnumerable<RegistryCode>)null);
        }
    }
}
