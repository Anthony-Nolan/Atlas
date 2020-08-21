using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.Models;
using System;
using System.Globalization;

namespace Atlas.MatchPrediction.ApplicationInsights
{
    internal class HaplotypeFrequencySetImportEventModel : EventModel
    {
        public HaplotypeFrequencySetImportEventModel(string name, FrequencySetFile file) : base(name)
        {
            Level = LogLevel.Info;
            Properties.Add(nameof(file.FileName), file.FileName);
            Properties.Add("TotalImportDurationInMs", GetDurationInMilliseconds(file));
            Properties.Add(nameof(file.UploadedDateTime), GetFormattedDateTimeString(file.UploadedDateTime));
            Properties.Add(nameof(file.ImportedDateTime), GetFormattedDateTimeString(file.ImportedDateTime));
        }

        private static string GetFormattedDateTimeString(DateTimeOffset? dateTime)
        {
            return dateTime?.UtcDateTime.ToString(CultureInfo.InvariantCulture) + " UTC";
        }

        private static string GetDurationInMilliseconds(FrequencySetFile file)
        {
            var timeSpan = file.ImportedDateTime - file.UploadedDateTime;

            if (timeSpan == null)
            {
                return "Unknown";
            }

            var totalMs = (int)Math.Round(timeSpan.Value.TotalMilliseconds);
            return totalMs.ToString();
        }
    }
}
