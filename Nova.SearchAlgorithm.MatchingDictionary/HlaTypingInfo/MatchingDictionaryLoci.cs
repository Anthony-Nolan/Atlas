using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo
{
    /// <summary>
    /// The matching dictionary will only contain typing data for a subset of all the possible HLA loci.
    /// This class defines these loci of interest and their typing method name variants.
    /// </summary>
    internal static class MatchingDictionaryLoci
    {
        private class LocusDetails
        {
            public MatchLocus MatchName { get; }
            public string MolecularName { get; }
            public string SerologyName { get; }

            /// <summary>
            /// Serology-based matching is becoming less relevant, especially for those HLA loci
            /// that have recently been deemed important to transplant success.
            /// Set this property to false if serology data is not required for the match locus.
            /// </summary>
            public bool IsSerologyTypingDataRequired { get; }

            public LocusDetails(
                MatchLocus matchName, 
                string molecularName, 
                string serology = null, 
                bool isSerologyTypingDataRequired = true)
            {
                MatchName = matchName;
                MolecularName = molecularName;
                SerologyName = serology ?? string.Empty;
                IsSerologyTypingDataRequired = isSerologyTypingDataRequired;
            }
        }

        private static readonly List<LocusDetails> LociDetails = new List<LocusDetails>
        {
            new LocusDetails(MatchLocus.A, "A*", "A"),
            new LocusDetails(MatchLocus.B, "B*", "B"),
            new LocusDetails(MatchLocus.C, "C*", "Cw"),
            new LocusDetails(MatchLocus.Dpb1, "DPB1*", isSerologyTypingDataRequired: false),
            new LocusDetails(MatchLocus.Dqb1, "DQB1*", "DQ"),
            new LocusDetails(MatchLocus.Drb1, "DRB1*", "DR")
        };

        public static bool IsMolecularLocus(string locusName)
        {
            return LociDetails.Select(n => n.MolecularName).Contains(locusName);
        }

        public static bool IsSerologyLocus(string locusName)
        {
            return LociDetails
                .Where(locus => locus.IsSerologyTypingDataRequired)
                .Select(locus => locus.SerologyName)
                .Contains(locusName);
        }

        public static IEnumerable<MatchLocus> GetMatchLoci()
        {
            return LociDetails.Select(n => n.MatchName);
        }

        public static MatchLocus GetMatchLocusFromTypingLocusIfExists(TypingMethod typingMethod, string locusName)
        {
            var locusDetails = LociDetails.FirstOrDefault(locus =>
                typingMethod == TypingMethod.Molecular ?
                    locus.MolecularName.Equals(locusName) :
                    locus.SerologyName.Equals(locusName));

            return locusDetails?.MatchName ?? throw new LocusNameException(locusName);
        }

        public static string GetSerologyLocusFromMolecularIfExists(string molecularLocusName)
        {
            var locusDetails = LociDetails.FirstOrDefault(l => l.MolecularName.Equals(molecularLocusName));
                
            return locusDetails?.SerologyName ?? throw new LocusNameException(molecularLocusName);
        }

        public static string ToMolecularLocusIfExists(this MatchLocus matchLocusName)
        {
            var locusDetails = LociDetails.FirstOrDefault(l => l.MatchName.Equals(matchLocusName));

            return locusDetails?.MolecularName ?? throw new LocusNameException(matchLocusName.ToString());
        }
    }
}
