using System.Collections.Generic;

namespace Atlas.Utils.Core.Http
{
    public class FieldErrorModel
    {
        public string Key { get; set; }
        public IList<string> Errors { get; set; }
    }
}