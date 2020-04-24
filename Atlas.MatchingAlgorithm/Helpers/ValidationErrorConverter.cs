using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Utils.Core.Http;

namespace Atlas.MatchingAlgorithm.Helpers
{
    public static class ValidationErrorConverter
    {
        public static ValidationErrorsModel ToValidationErrorsModel(this ValidationException validationException)
        {
            return new ValidationErrorsModel
            {
                FieldErrors = validationException.Errors.Select(e => new FieldErrorModel
                {
                    Key = e.PropertyName,
                    Errors = new List<string> {e.ErrorMessage}
                }).ToList()
            };
        }

        public static string ToErrorMessagesString(this ValidationException validationException)
        {
            var errorMessages = validationException.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
            return string.Join(Environment.NewLine, errorMessages);
        }
    }
}