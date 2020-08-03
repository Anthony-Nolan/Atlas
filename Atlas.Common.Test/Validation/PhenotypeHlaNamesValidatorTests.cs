﻿using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Validation;
using FluentAssertions;
using FluentValidation;
using NUnit.Framework;

namespace Atlas.Common.Test.Validation
{
    [TestFixture]
    public class PhenotypeHlaNamesValidatorTests
    {
        private PhenotypeHlaNamesValidator validator;
        
        [SetUp]
        public void SetUp()
        {
            validator = new PhenotypeHlaNamesValidator();
        }
        
        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.Drb1)]
        public void Validator_WhenMissingRequiredLocus_ShouldHaveValidationError(Locus locus)
        {
            var phenotypeInfo = new PhenotypeInfo<string>("hla");
            phenotypeInfo.SetLocus(locus, null);
            
            validator.Invoking(v => v.ValidateAndThrow(phenotypeInfo)).Should().Throw<ValidationException>();
        }
        
        [TestCase(Locus.C)]
        [TestCase(Locus.Dpb1)]
        [TestCase(Locus.Dqb1)]
        public void Validator_WhenMissingOptionalLocus_ShouldNotHaveValidationError(Locus locus)
        {
            var phenotypeInfo = new PhenotypeInfo<string>("hla");
            phenotypeInfo.SetLocus(locus, null);
            
            validator.Invoking(v => v.ValidateAndThrow(phenotypeInfo)).Should().NotThrow<ValidationException>();
        }
    }
}