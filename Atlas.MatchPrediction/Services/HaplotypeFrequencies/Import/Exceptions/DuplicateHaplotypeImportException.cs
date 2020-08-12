using System;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions
{
    public class DuplicateHaplotypeImportException : Exception
    {
        private const string ErrorMessage = "Duplicate haplotype import attempted.";
        public DuplicateHaplotypeImportException() : base(ErrorMessage)
        {
        }
    }
}
