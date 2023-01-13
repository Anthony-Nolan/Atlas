using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Common.Config;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    public class HlaNamePGroupRelation : IEquatable<HlaNamePGroupRelation>
    {
        internal static string TableName(Locus locus) => $"HlaNamePGroupRelationAt{locus.ToString().ToUpperInvariant()}";

        public long Id { get; set; }

        [NotNull]
        public int HlaNameId { get; set; }

        [NotNull]
        public int PGroupId { get; set; }

        #region Equality members

        public bool Equals(HlaNamePGroupRelation other)
        {
            return Id == other.Id && HlaNameId == other.HlaNameId && PGroupId == other.PGroupId;
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

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((HlaNamePGroupRelation) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, HlaNameId, PGroupId);
        }

        public static bool operator ==(HlaNamePGroupRelation left, HlaNamePGroupRelation right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HlaNamePGroupRelation left, HlaNamePGroupRelation right)
        {
            return !Equals(left, right);
        }

        #endregion
    }

    [Table("HlaNamePGroupRelationAtA")]
    public class HlaNamePGroupRelationAtA : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtB")]
    public class HlaNamePGroupRelationAtB : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtC")]
    public class HlaNamePGroupRelationAtC : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtDQB1")]
    public class HlaNamePGroupRelationAtDqb1 : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtDRB1")]
    public class HlaNamePGroupRelationAtDrb1 : HlaNamePGroupRelation
    {
    }
}