using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Validation;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Models;
using FluentValidation;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.DonorProcessing
{
    public abstract class DonorProcessingFailureEventModel : EventModel
    {
        protected DonorProcessingFailureEventModel(
            string eventName,
            Exception exception,
            FailedDonorInfo failedDonorInfo) : base(eventName)
        {
            Level = LogLevel.Error;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("AtlasDonorId", failedDonorInfo.AtlasDonorId.ToString());
            Properties.Add("DonorInfo", JsonConvert.SerializeObject(failedDonorInfo.DonorInfo));
        }
    }

    public class DonorHlaLookupFailureEventModel : DonorProcessingFailureEventModel
    {
        public DonorHlaLookupFailureEventModel(string eventName, DonorProcessingException<HlaMetadataDictionaryException> exception)
            : base(eventName, exception.Exception, exception.FailedDonorInfo)
        {
            Properties.Add("Locus", exception.Exception.Locus);
            Properties.Add("HlaName", exception.Exception.HlaName);
        }
    }

    public class DonorInfoValidationFailureEventModel : DonorProcessingFailureEventModel
    {
        public DonorInfoValidationFailureEventModel(string eventName, DonorProcessingException<ValidationException> exception)
            : base(eventName, exception, exception.FailedDonorInfo)
        {
            Properties.Add("ValidationErrors", exception.Exception.ToErrorMessagesString());
        }
    }

    public class DonorInfoGenericFailureEventModel<TException> : DonorProcessingFailureEventModel where TException : Exception
    {
        public DonorInfoGenericFailureEventModel(string eventName, DonorProcessingException<TException> exception)
            : base(eventName, exception, exception.FailedDonorInfo)
        {
            Properties.Add("ExceptionType", exception.Exception.GetType().FullName);
        }
    }

}