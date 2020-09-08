using System;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities
{
    /// <summary>
    /// Term "Parent" in <see cref="ParentEntityBase"/> refers to foreign key relationships,
    /// where the parent entity provides a principal key that is used as a foreign key in the child table(s).
    /// </summary>
    public abstract class ParentEntityBase
    {
        public int Id { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        /// <summary>
        /// Optional column to store comments.
        /// </summary>
        public string Comments { get; set; }
    }
}
