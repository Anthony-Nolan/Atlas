using System;

namespace Atlas.Common.ApplicationInsights
{
    public class FileErrorEventModel : ErrorEventModel
    {
        public FileErrorEventModel(string filename, string messageName, Exception exception)
            : base(messageName, exception)
        {
            Properties.Add("FileName", filename);
        }
    }
}
