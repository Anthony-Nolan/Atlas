using System;
using System.Diagnostics;

namespace Atlas.Utils.Core.ApplicationInsights.EventModels
{
    public class SolarConnectionEventModel : EventModel
    {
        private const string ConnectionIdName = "SOLAR";
        private readonly Stopwatch connectionTimer;

        public SolarConnectionEventModel() : base("Oracle Connection")
        {
            connectionTimer = new Stopwatch();
            Properties.Add("Connection Id", IdGenerator.NewId(ConnectionIdName));
            Level = LogLevel.Verbose;
        }

        public void Opening()
        {
            connectionTimer.Start();
        }

        public void Opened()
        {
            Metrics.Add("Time to connect", connectionTimer.ElapsedMilliseconds);
        }

        public void OpenFailed(int retryCount, string connectionRetryGroupId, string exceptionMessage)
        {
            Metrics.Add("Connecting failed after", connectionTimer.ElapsedMilliseconds);
            Properties.Add("Connection Retry Group Id", connectionRetryGroupId);
            Properties.Add("Exception Message", exceptionMessage);
            Metrics.Add("Retry Count", retryCount);
        }

        public void Closed()
        {
            connectionTimer.Stop();
            Metrics.Add("Connection duration", connectionTimer.ElapsedMilliseconds);
        }
    }
}