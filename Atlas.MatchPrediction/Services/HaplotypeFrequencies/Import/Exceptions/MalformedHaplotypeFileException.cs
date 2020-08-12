using System;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions
{
    internal class MalformedHaplotypeFileException : Exception
    {
        internal MalformedHaplotypeFileException(string message) : base(message)
        {
        }
    }
}
