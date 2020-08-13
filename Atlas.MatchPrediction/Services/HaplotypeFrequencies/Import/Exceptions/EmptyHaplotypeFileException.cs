using System;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions
{
    internal class EmptyHaplotypeFileException : Exception
    {
        private const string ErrorMessage = "Haplotype file did not have any contents";
        internal EmptyHaplotypeFileException() : base(ErrorMessage)
        {
        }
    }
}
