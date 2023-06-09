using System;

namespace Atlas.Common.ApplicationInsights
{
    public class UploadErrorEventModel : ErrorEventModel
    {
        public UploadErrorEventModel(string filename, string messageName, Exception exception)
            : base(messageName, exception)
        {
            Properties.Add("FileName", filename);
        }
    }
}
