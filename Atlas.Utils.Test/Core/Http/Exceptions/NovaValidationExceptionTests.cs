using System.Collections.Generic;
using FluentAssertions;
using Atlas.Utils.Core.Http;
using Atlas.Utils.Core.Http.Exceptions;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Http.Exceptions
{
    [TestFixture]
    public class NovaValidationExceptionTests
    {
        [Test]
        public void GivenException_WithGlobalErrors_ResultContainsGivenGlobalErrors()
        {
            var ex = new AtlasValidationException().WithGlobalErrors("Error 1", "Error 2");

            ex.GlobalErrors.Should().Equal("Error 1", "Error 2");
        }

        [Test]
        public void GivenException_WithFieldErrors_ResultContainsGivenFieldErrors()
        {
            var ex = new AtlasValidationException()
                .WithFieldErrors("Field1", "Foo", "Bar")
                .WithFieldErrors("Field2", "Baz");

            ex.FieldErrors.ShouldBeEquivalentTo(new List<FieldErrorModel>
            {
                new FieldErrorModel { Key = "Field1", Errors = new List<string> { "Foo", "Bar" } },
                new FieldErrorModel { Key = "Field2", Errors = new List<string> { "Baz" } },
            });
        }
    }
}
