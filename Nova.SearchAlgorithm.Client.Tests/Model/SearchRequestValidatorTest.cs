using FluentValidation;
using Nova.SearchAlgorithm.Client.Models;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Model
{
    [TestFixture]
    public class SearchRequestValidatorTest
    {
        [Test]
        public void ShouldHaveValidationError_WhenRequestHasNoMatchCriteria()
        {
            SearchRequestValidator validator = new SearchRequestValidator();
            validator.
        }
    }
}
