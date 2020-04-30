using System.Collections.Generic;

namespace Atlas.Utils.Core.Http
{
    public class ValidationErrorsModel
    {
        public IList<string> GlobalErrors { get; set; } = new List<string>();
        public IList<FieldErrorModel> FieldErrors { get; set; } = new List<FieldErrorModel>();
    }
}
