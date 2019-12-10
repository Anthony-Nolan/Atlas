using FluentValidation;
using Newtonsoft.Json;
using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public abstract class DonorInfoValidationException : Exception
    {
        public string DonorInfo { get; set; }
        public ValidationException ValidationException { get; set; }

        protected DonorInfoValidationException(object donorInfo, ValidationException exception)
        {
            DonorInfo = JsonConvert.SerializeObject(donorInfo);
            ValidationException = exception;
        }
    }
}