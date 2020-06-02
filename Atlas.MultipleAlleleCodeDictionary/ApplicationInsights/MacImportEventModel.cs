using System;
using Atlas.Common.ApplicationInsights;

namespace Atlas.MultipleAlleleCodeDictionary.ApplicationInsights
{
    internal class MacImportEventModel : EventModel
    {
        public MacImportEventModel(string message) : base(message)
        {
            Level = LogLevel.Info;
        }

        public MacImportEventModel(Exception exception, string message): base(message)
        {
            Level = LogLevel.Critical;
            Properties.Add("Message", exception.Message);
            Properties.Add("StackTrace", exception.StackTrace);
        }
    }
}