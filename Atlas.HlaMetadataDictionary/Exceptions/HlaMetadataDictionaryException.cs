using System;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.Wmda;

namespace Atlas.HlaMetadataDictionary.Exceptions
{
    internal class HlaMetadataDictionaryException : Exception
    {
        public string Locus { get; set; }
        public string HlaName { get; set; }

        internal HlaMetadataDictionaryException(string locus, string hlaName, string message, Exception inner = null)
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
