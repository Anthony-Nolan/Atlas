using System;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Helpers;
using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.Models;
using Atlas.Utils.Core.ApplicationInsights;
using Atlas.Utils.Core.ApplicationInsights.EventModels;
using FluentValidation;

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
            Properties.Add("DonorId", failedDonorInfo.DonorId);
            Properties.Add("DonorInfo", JsonConvert.SerializeObject(failedDonorInfo.DonorInfo));
        }
    }

    public class DonorHlaLookupFailureEventModel : DonorProcessingFailureEventModel
    {
        public DonorHlaLookupFailureEventModel(string eventName, DonorProcessingException<MatchingDictionaryException> exception)
            : base(eventName, exception.Exception, exception.FailedDonorInfo)
        {
            Properties.Add("Locus", exception.Exception.HlaInfo.Locus);
            Properties.Add("HlaName", exception.Exception.HlaInfo.HlaName);
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