using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.ApplicationInsights
{
    internal class HlaConversionFailureEventModel : EventModel
    {
        private const string EventName = "HLA Conversion Failed";

        public HlaConversionFailureEventModel(
            Locus locus,
            string hla,
            string hlaNomenclatureVersion,
            TargetHlaCategory? category,
            string stageOfFailure,
            HlaMetadataDictionaryException exception) : base(EventName)
        {
            Level = LogLevel.Warn;
            Properties.Add("Locus", locus.ToString());
            Properties.Add("Hla", hla);
            Properties.Add("HlaNomenclatureVersion", hlaNomenclatureVersion);
            Properties.Add(nameof(TargetHlaCategory), category.ToString());
            Properties.Add("Stage of Failure", stageOfFailure);
            Properties.Add(nameof(HlaMetadataDictionaryException), exception.ToString());
        }
    }
}
