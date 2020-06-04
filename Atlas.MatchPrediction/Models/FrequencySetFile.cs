using System;
using System.IO;

namespace Atlas.MatchPrediction.Models
{
    public class FrequencySetFile
    {
        public Stream Contents { get; set; }
        public string FileName { get; set; }
        public DateTimeOffset? UploadedDateTime { get; set; }
        public DateTimeOffset? ImportedDateTime { get; set; }
    }
}
