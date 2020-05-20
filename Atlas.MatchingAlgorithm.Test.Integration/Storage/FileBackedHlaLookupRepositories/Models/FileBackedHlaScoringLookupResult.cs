using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models
{
    public class FileBackedHlaScoringLookupResult : IHlaScoringLookupResult
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise => HlaScoringInfo;
        public HlaTypingCategoryzxyzxtzx HlaTypingCategoryzxyzxtzx { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }

        public FileBackedHlaScoringLookupResult(
            Locus locus, 
            string lookupName, 
            TypingMethod typingMethod, 
            HlaTypingCategoryzxyzxtzx hlaTypingCategoryzxyzxtzx,
            object hlaScoringInfo)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaTypingCategoryzxyzxtzx = hlaTypingCategoryzxyzxtzx;
            HlaScoringInfo = GetHlaScoringInfo(hlaTypingCategoryzxyzxtzx, hlaScoringInfo.ToString());
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this)
            {
                LookupNameCategoryAsString = HlaTypingCategoryzxyzxtzx.ToString() //QQ needs attention for rename
            };
        }

        private static IHlaScoringInfo GetHlaScoringInfo(
            HlaTypingCategoryzxyzxtzx hlaTypingCategoryzxyzxtzx,
            string hlaScoringInfoString)
        {
            switch (hlaTypingCategoryzxyzxtzx)
            {
                case HlaTypingCategoryzxyzxtzx.Serology:
                    return JsonConvert.DeserializeObject<SerologyScoringInfo>(hlaScoringInfoString);
                case HlaTypingCategoryzxyzxtzx.OriginalAllele:
                    return JsonConvert.DeserializeObject<SingleAlleleScoringInfo>(hlaScoringInfoString);
                case HlaTypingCategoryzxyzxtzx.NmdpCodeAllele:
                    return JsonConvert.DeserializeObject<MultipleAlleleScoringInfo>(hlaScoringInfoString);
                case HlaTypingCategoryzxyzxtzx.XxCode:
                    return JsonConvert.DeserializeObject<ConsolidatedMolecularScoringInfo>(hlaScoringInfoString);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
