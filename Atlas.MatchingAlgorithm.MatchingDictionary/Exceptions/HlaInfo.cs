using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Exceptions
{
    public class HlaInfo
    {
        public string Locus { get; set; }
        public string HlaName { get; set; }

        public HlaInfo(string locus, string hlaName)
        {
            Locus = locus;
            HlaName = hlaName;
        }

        public HlaInfo(Locus locus, string hlaName) : this(locus.ToString(), hlaName)
        {
        }

        public HlaInfo(string locus) : this(locus, string.Empty)
        {
        }
    }
}
