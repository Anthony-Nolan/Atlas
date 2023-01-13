using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;

namespace Atlas.HlaMetadataDictionary.InternalExceptions
{
    internal class InvalidHlaException : HlaMetadataDictionaryException
    {
        public InvalidHlaException(Locus locus, string hlaName) : base(locus, hlaName, $"HLA type: {locus} {hlaName} is invalid.")
        {
        }
    }
}