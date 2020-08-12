using System;
namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions
{
    internal class InvalidFilePathException : Exception
    {
        internal InvalidFilePathException(string message) : base(message)
        {
        }
    }
}
