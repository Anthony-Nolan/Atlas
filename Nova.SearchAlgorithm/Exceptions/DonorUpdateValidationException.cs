using FluentValidation;
using Newtonsoft.Json;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.Utils.ServiceBus.Models;
using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class DonorUpdateValidationException : Exception
    {
        public string DonorUpdate { get; set; }
        public ValidationException ValidationException { get; set; }

        public DonorUpdateValidationException(ServiceBusMessage<SearchableDonorUpdateModel> info, ValidationException exception)
        {
            DonorUpdate = JsonConvert.SerializeObject(info);
            ValidationException = exception;
        }
    }
}