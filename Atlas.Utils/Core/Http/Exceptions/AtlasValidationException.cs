using System.Collections.Generic;
using System.Net;
using Atlas.Utils.Core.Common;

namespace Atlas.Utils.Core.Http.Exceptions
{
    public partial class AtlasValidationException : AtlasHttpException
    {
        public AtlasValidationException() : this(new List<string>(), new List<FieldErrorModel>())
        {
        }

        public AtlasValidationException(IList<string> globalErrors, IList<FieldErrorModel> fieldErrors)
            : base(HttpStatusCode.BadRequest, "One or more validation errors have occured.")
        {
            GlobalErrors = globalErrors.AssertArgumentNotNull(nameof(globalErrors));
            FieldErrors = fieldErrors.AssertArgumentNotNull(nameof(fieldErrors));
        }

        public IList<string> GlobalErrors { get; }
        public IList<FieldErrorModel> FieldErrors { get; }

        public AtlasValidationException WithGlobalErrors(params string[] errors)
        {
            foreach (var error in errors.AssertArgumentNotNull(nameof(errors)))
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
