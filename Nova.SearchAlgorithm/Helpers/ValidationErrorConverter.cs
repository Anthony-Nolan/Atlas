using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Nova.Utils.Http;

namespace Nova.SearchAlgorithm.Helpers
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
    }
}