using System;

namespace Atlas.MultipleAlleleCodeDictionary
{
    /// <summary>
    /// Public because it is part of this dictionary's contract, not an implementation detail: it is how
    /// <see cref="ExternalInterface.IMacDictionary.GetHlaFromMac(string)"/> says a MAC is not in the store, and a
    /// caller has to be able to tell that apart from a failed storage request. <c>MacLookup</c> in the HLA Metadata
    /// Dictionary does exactly that.
    /// </summary>
    public class MacNotFoundException : Exception
    {
        public MacNotFoundException(string mac) : base($"MAC {mac} could not be found in the MAC store.")
        {
        }
    }
}