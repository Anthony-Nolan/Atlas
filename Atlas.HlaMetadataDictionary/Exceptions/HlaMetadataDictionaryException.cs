using System;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.Wmda;

namespace Atlas.HlaMetadataDictionary.Exceptions
{
    public class HlaMetadataDictionaryException : Exception
    {
        public string Locus { get; set; }
        public string HlaName { get; set; }

        internal HlaMetadataDictionaryException(string locus, string hlaName, string message, Exception inner = null)
            : base(message, inner)
        {
            Locus = locus;
            HlaName = hlaName;
        }

        // TODO: ATLAS-46: Ensure this can be made internal, or decide to leave it public
        public HlaMetadataDictionaryException(Locus locus, string hlaName, string message, Exception inner = null)
            : this(locus.ToString(), hlaName, message, inner)
        { }

        internal HlaMetadataDictionaryException(AlleleStatus status, string message, Exception inner = null)
            : this(status.TypingLocus, status.Name, message, inner)
        { }
    }
}
