using System;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions
{
    internal class HaplotypeFormatException : Exception
    {
        private const string ErrorMessage = "Error parsing haplotype format";
        public HaplotypeFormatException(Exception e) : base(ErrorMessage, e)
        {
        }
    }
}
