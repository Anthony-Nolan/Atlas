using System;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions
{
    public class HlaMetadataDictionaryException : Exception
    {
        public string Locus { get; set; }
        public string HlaName { get; set; }

        public HlaMetadataDictionaryException(string locus, string hlaName, string message, Exception inner = null)
            : base(message, inner)
        {
            Locus = locus;
            HlaName = hlaName;
        }

        internal HlaMetadataDictionaryException(Locus locus, string hlaName, string message, Exception inner = null)
            : this(locus.ToString(), hlaName, message, inner)
        { }

        internal HlaMetadataDictionaryException(AlleleStatus status, string message, Exception inner = null)
            : this(status.TypingLocus, status.Name, message, inner)
        { }
    }
}
