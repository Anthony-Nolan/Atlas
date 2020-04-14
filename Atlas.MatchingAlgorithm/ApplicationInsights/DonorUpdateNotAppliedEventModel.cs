using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.Models;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;
using System;

namespace Atlas.MatchingAlgorithm.ApplicationInsights
{
    public class DonorUpdateNotAppliedEventModel : EventModel
    {
        private const string EventName = "Donor update not applied";

        public DonorUpdateNotAppliedEventModel(
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