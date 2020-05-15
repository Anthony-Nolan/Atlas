using System.Collections.Generic;
using System.Net;
using Atlas.Common.Utils.Http;

namespace Atlas.Common.NovaHttpClient.Http.Exceptions
{
    public class AtlasValidationException : AtlasHttpException
    {
        public AtlasValidationException() : this(new List<string>(), new List<FieldErrorModel>())
        {
        }

        public AtlasValidationException(IList<string> globalErrors, IList<FieldErrorModel> fieldErrors)
            : base(HttpStatusCode.BadRequest, "One or more validation errors have occured.")
        {
            GlobalErrors = globalErrors;
            FieldErrors = fieldErrors;
        }

        public IList<string> GlobalErrors { get; }
        public IList<FieldErrorModel> FieldErrors { get; }

        public AtlasValidationException WithGlobalErrors(params string[] errors)
        {
            foreach (var error in errors)
            {
                GlobalErrors.Add(error);
            }
            return this;
        }

        public AtlasValidationException WithFieldErrors(string fieldName, params string[] errors)
        {
            FieldErrors.Add(new FieldErrorModel { Key = fieldName, Errors = errors });
            return this;
        }
    }
}
