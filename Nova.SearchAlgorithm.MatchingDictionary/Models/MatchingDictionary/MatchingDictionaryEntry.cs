using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    /// <summary>
    /// HLA data held within the matching dictionary.
    /// Properties are optimised for dictionary lookups.
    /// </summary>
    public class MatchingDictionaryEntry : 
        IMatchingHlaLookupResult,
        IStorableInCloudTable<MatchingDictionaryTableEntity>,
        IEquatable<MatchingDictionaryEntry>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public MolecularSubtype MolecularSubtype { get; }
        public SerologySubtype SerologySubtype { get; }
        public AlleleTypingStatus AlleleTypingStatus { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        [JsonConstructor]
        public MatchingDictionaryEntry(
            MatchLocus matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            MolecularSubtype molecularSubtype,
            SerologySubtype serologySubtype,
            AlleleTypingStatus alleleTypingStatus,
            IEnumerable<string> matchingPGroups,
            IEnumerable<string> matchingGGroups,
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            MolecularSubtype = molecularSubtype;
            SerologySubtype = serologySubtype;
            AlleleTypingStatus = alleleTypingStatus;
            MatchingPGroups = matchingPGroups;
            MatchingGGroups = matchingGGroups;
            MatchingSerologies = matchingSerologies;
        }

        public MatchingDictionaryEntry(IMatchingDictionarySource<SerologyTyping> serologySource)
        {
            MatchLocus = serologySource.TypingForMatchingDictionary.MatchLocus;
            LookupName = serologySource.TypingForMatchingDictionary.Name;
            TypingMethod = TypingMethod.Serology;
            MolecularSubtype = MolecularSubtype.NotMolecularTyping;
            SerologySubtype = serologySource.TypingForMatchingDictionary.SerologySubtype;
            MatchingPGroups = serologySource.MatchingPGroups;
            MatchingGGroups = serologySource.MatchingGGroups;
            MatchingSerologies = serologySource.MatchingSerologies.ToSerologyEntries();
        }

        public MatchingDictionaryEntry(IMatchingDictionarySource<AlleleTyping> alleleSource, string lookupName, MolecularSubtype molecularSubtype)
        {
            MatchLocus = alleleSource.TypingForMatchingDictionary.MatchLocus;
            LookupName = lookupName;

            TypingMethod = TypingMethod.Molecular;
            MolecularSubtype = molecularSubtype;
            SerologySubtype = SerologySubtype.NotSerologyTyping;

            AlleleTypingStatus = molecularSubtype == MolecularSubtype.CompleteAllele
                ? alleleSource.TypingForMatchingDictionary.Status
                : AlleleTypingStatus.GetDefaultStatus();

            MatchingPGroups = alleleSource.MatchingPGroups;
            MatchingGGroups = alleleSource.MatchingGGroups;
            MatchingSerologies = alleleSource.MatchingSerologies.ToSerologyEntries();
        }

        public MatchingDictionaryEntry(MatchLocus matchLocus, string lookupName, MolecularSubtype molecularSubtype, IEnumerable<MatchingDictionaryEntry> entries)
        {
            var entriesList = entries.ToList();

            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = TypingMethod.Molecular;
            MolecularSubtype = molecularSubtype;
            SerologySubtype = SerologySubtype.NotSerologyTyping;
            AlleleTypingStatus = AlleleTypingStatus.GetDefaultStatus();
            MatchingPGroups = entriesList.SelectMany(p => p.MatchingPGroups).Distinct();
            MatchingGGroups = entriesList.SelectMany(g => g.MatchingGGroups).Distinct();
            MatchingSerologies = entriesList.SelectMany(s => s.MatchingSerologies).Distinct();
        }

        public MatchingDictionaryTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }

        public bool BelongsToTablePartition(string partition)
        {
            return partition.Equals(MatchingDictionaryTableEntity.GetPartition(MatchLocus));
        }

        public bool Equals(MatchingDictionaryEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                MatchLocus == other.MatchLocus &&
                string.Equals(LookupName, other.LookupName) &&
                TypingMethod == other.TypingMethod &&
                MolecularSubtype == other.MolecularSubtype &&
                SerologySubtype == other.SerologySubtype &&
                AlleleTypingStatus.Equals(other.AlleleTypingStatus) &&
                MatchingPGroups.SequenceEqual(other.MatchingPGroups) &&
                MatchingGGroups.SequenceEqual(other.MatchingGGroups) &&
                MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MatchingDictionaryEntry)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)MatchLocus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)TypingMethod;
                hashCode = (hashCode * 397) ^ (int)MolecularSubtype;
                hashCode = (hashCode * 397) ^ (int)SerologySubtype;
                hashCode = (hashCode * 397) ^ AlleleTypingStatus.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingPGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingGGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
    }
}
