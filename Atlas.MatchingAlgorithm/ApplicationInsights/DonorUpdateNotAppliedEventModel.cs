using System;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.Models;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.ApplicationInsights
{
    public class DonorUpdateNotAppliedEventModel : EventModel
    {
        private const string EventName = "Donor update not applied";

        internal DonorUpdateNotAppliedEventModel(
            DateTimeOffset lastUpdateDateTime,
            DonorAvailabilityUpdate update) : base(EventName)
        {
            Level = LogLevel.Warn;
            Properties.Add("DonorId", update.DonorId.ToString());
            Properties.Add("LastUpdateDateTime", lastUpdateDateTime.ToString());
            Properties.Add("DonorUpdate", JsonConvert.SerializeObject(update));
        }
    }
}