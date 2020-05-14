using System;
using FluentValidation;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.HlaMetadataDictionary.Exceptions;

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
                case DonorProcessingException<MatchingDictionaryException> hlaException:
                    return new DonorHlaLookupFailureEventModel(eventName, hlaException);
                default:
                    return new DonorInfoGenericFailureEventModel<TException>(eventName, exception);
            }
        }
    }
}
