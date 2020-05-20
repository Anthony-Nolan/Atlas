using System;

namespace Atlas.Common.ApplicationInsights
{
    public class ErrorEventModel : EventModel
    {
        public ErrorEventModel(string messageName, Exception exception) : base(messageName)
        {
            Level = LogLevel.Error;
            Properties.Add("Exception", exception.ToString());
        }
    }
}