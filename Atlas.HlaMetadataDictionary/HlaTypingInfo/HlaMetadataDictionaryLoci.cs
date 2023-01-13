using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalExceptions;

namespace Atlas.HlaMetadataDictionary.HlaTypingInfo
{
    /// <summary>
    /// The HLA Metadata dictionary will only contain typing data for a subset of all the possible HLA loci.
    /// This class defines these loci of interest and their typing method name variants.
    /// </summary>
    internal static class HlaMetadataDictionaryLoci
    {
        private class LocusDetails
        {
            public Locus Locus { get; }
            public string MolecularName { get; }
            public string SerologyName { get; }

            /// <summary>
            /// Serology-based matching is becoming less relevant, especially for those HLA loci
            /// that have recently been deemed important to transplant success.
            /// Set this property to false if serology data is not required for the match locus.
            /// </summary>
            public bool IsSerologyTypingDataRequired { get; }

            public LocusDetails(
                Locus locus, 
                string molecularName, 
                string serologyName = null, 
                bool isSerologyTypingDataRequired = true)
            {
                Locus = locus;
                MolecularName = molecularName;
                SerologyName = serologyName ?? string.Empty;
                IsSerologyTypingDataRequired = isSerologyTypingDataRequired;
            }
        }

        private static readonly List<LocusDetails> LociDetails = new List<LocusDetails>
        {
            new LocusDetails(Locus.A, "A*", "A"),
            new LocusDetails(Locus.B, "B*", "B"),
            new LocusDetails(Locus.C, "C*", "Cw"),
            new LocusDetails(Locus.Dpb1, "DPB1*", isSerologyTypingDataRequired: false),
            new LocusDetails(Locus.Dqb1, "DQB1*", "DQ"),
            new LocusDetails(Locus.Drb1, "DRB1*", "DR")
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

        public static Locus GetLocusFromTypingLocusNameIfExists(TypingMethod typingMethod, string locusName)
        {
            var locusDetails = LociDetails.FirstOrDefault(locus =>
                typingMethod == TypingMethod.Molecular ?
                    locus.MolecularName.Equals(locusName) :
                    locus.SerologyName.Equals(locusName));

            return locusDetails?.Locus ?? throw new LocusNameException(locusName);
        }

        public static string ToMolecularLocusIfExists(this Locus locus)
        {
            var locusDetails = LociDetails.FirstOrDefault(l => l.Locus.Equals(locus));

            return locusDetails?.MolecularName ?? throw new LocusNameException(locus.ToString());
        }
    }
}
