using System;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.MatchingAlgorithm.Exceptions;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.DonorProcessing
{
    public static class DonorProcessingFailureEventModelFactory<TException> where TException : Exception
    {
        public static DonorProcessingFailureEventModel GetEventModel(
            string eventName,
            DonorProcessingException<TException> exception)
        {
            switch (exception)
            {
                case DonorProcessingException<ValidationException> validationException:
                    return new DonorInfoValidationFailureEventModel(eventName, validationException);
                case DonorProcessingException<HlaMetadataDictionaryException> hlaException:
                    return new DonorHlaLookupFailureEventModel(eventName, hlaException);
                default:
                    return new DonorInfoGenericFailureEventModel<TException>(eventName, exception);
            }
        }
    }
}
