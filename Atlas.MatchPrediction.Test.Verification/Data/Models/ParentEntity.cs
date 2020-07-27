using System;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public abstract class ParentEntity
    {
        public int Id { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        /// <summary>
        /// Optional column to store comments.
        /// </summary>
        public string Comments { get; set; }
    }
}
