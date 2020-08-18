using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Utils.Extensions;
using Atlas.Common.Utils.Http;
using FluentValidation;

namespace Atlas.Common.Validation
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

        internal static string ToErrorMessagesString(this ValidationException validationException)
        {
            var errorMessages = validationException.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
            return errorMessages.StringJoinWithNewline();
        }
    }
}