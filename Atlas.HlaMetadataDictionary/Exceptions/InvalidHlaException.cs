using Atlas.Common.GeneticData;

namespace Atlas.HlaMetadataDictionary.Exceptions
{
    internal class InvalidHlaException : HlaMetadataDictionaryException
    {
        public InvalidHlaException(Locus locus, string hlaName) : base(locus, hlaName, $"HLA type: {locus} {hlaName} is invalid.")
        {
        }
    }
}