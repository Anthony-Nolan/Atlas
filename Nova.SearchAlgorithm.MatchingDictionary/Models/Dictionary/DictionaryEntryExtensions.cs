using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    internal static class DictionaryEntryExtensions
    {
        internal static MatchingDictionaryEntry ToDictionaryEntry(this MatchedAllele matchedAllele, MolecularSubtype molecularSubtype)
        {
            var allele = (Allele)matchedAllele.HlaType;
            var lookupName = "";

            switch (molecularSubtype)
            {
                case MolecularSubtype.CompleteAllele:
                    lookupName = allele.Name;
                    break;
                case MolecularSubtype.TruncatedAllele:
                    lookupName = allele.TwoFieldName;
                    break;
                case MolecularSubtype.FirstFieldAllele:
                    lookupName = allele.Fields.ElementAt(0);
                    break;
            }

            var entry = ConvertToDictionaryEntry(
                matchedAllele, lookupName, TypingMethod.Molecular, molecularSubtype, SerologySubtype.NotSerologyType);

            return entry;
        }

        internal static MatchingDictionaryEntry ToDictionaryEntry(this IMatchedHla matchedHla, SerologySubtype serologySubtype)
        {
            var entry = ConvertToDictionaryEntry(
                matchedHla, matchedHla.HlaType.Name, TypingMethod.Serology, MolecularSubtype.NotMolecularType, serologySubtype);

            return entry;
        }

        private static MatchingDictionaryEntry ConvertToDictionaryEntry(
            IMatchedHla matchedHla, 
            string lookupName, 
            TypingMethod typingMethod,
            MolecularSubtype molecularSubtype,
            SerologySubtype serologySubtype)
        {
            return new MatchingDictionaryEntry
            (
                matchedHla.HlaType.MatchLocus,
                lookupName,
                typingMethod,
                molecularSubtype,
                serologySubtype,
                matchedHla.MatchingPGroups,
                matchedHla.MatchingSerologies.Select(s => new SerologyEntry(s.Name, s.SerologySubtype))
            );
        }
    }
}