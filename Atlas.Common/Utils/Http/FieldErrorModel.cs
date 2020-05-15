using System.Collections.Generic;

namespace Atlas.Common.Utils.Http
{
    public class FieldErrorModel
    {
        public string Key { get; set; }
        public IList<string> Errors { get; set; }
    }
}