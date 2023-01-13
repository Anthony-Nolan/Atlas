using System;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models
{
    public class HlaAtKnownTypingCategory
    {
        // ReSharper disable once MemberCanBeInternal
        public string Hla { get;  }

        public HaplotypeTypingCategory TypingCategory { get;  }

        // ReSharper disable once MemberCanBeInternal
        public HlaAtKnownTypingCategory(string hla, HaplotypeTypingCategory typingCategory)
        {
            TypingCategory = typingCategory;
            Hla = hla;
        }

        #region Equality members

        private bool Equals(HlaAtKnownTypingCategory other)
        {
            return Hla == other.Hla && TypingCategory == other.TypingCategory;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((HlaAtKnownTypingCategory) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Hla, (int) TypingCategory);
        }

        public static bool operator ==(HlaAtKnownTypingCategory left, HlaAtKnownTypingCategory right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HlaAtKnownTypingCategory left, HlaAtKnownTypingCategory right)
        {
            return !Equals(left, right);
        }

        #endregion
    }

    internal static class HlaAtKnownTypingCategoryPhenotypeExtensions
    {
        public static PhenotypeInfo<string> ToHlaNames(this PhenotypeInfo<HlaAtKnownTypingCategory> phenotypeInfo)
        {
            return phenotypeInfo.Map(hla => hla?.Hla);
        } 
    }
}