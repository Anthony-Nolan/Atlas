namespace Atlas.HlaMetadataDictionary.Exceptions
{
    public class InvalidHlaException : HlaMetadataDictionaryException
    {
        public InvalidHlaException(HlaInfo hlaInfo) : base(hlaInfo, $"HLA type: {hlaInfo.Locus} {hlaInfo.HlaName} is invalid.")
        {
        }
    }
}