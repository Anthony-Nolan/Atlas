using System;

namespace Atlas.MultipleAlleleCodeDictionary
{
    internal class MacNotFoundException : Exception
    {
        public MacNotFoundException(string mac) : base($"MAC {mac} could not be found in the MAC store.")
        {
        }
    }
}