using System;
using Nova.Utils.Models;

namespace Nova.Utils.Helpers
{
    public class AntibodyTypeHelper
    {
        [Obsolete("Use GetVirologyStatus")]
        public static CmvAntibodyType GetCmvAntibodyType(dynamic cmvType)
        {
            if (cmvType == null)
            {
                return CmvAntibodyType.Unknown;
            }

            string cmvTypeString = cmvType.ToString();

            switch (cmvTypeString.ToUpperInvariant())
            {
                case "POSITIVE":
                    return CmvAntibodyType.Positive;
                case "NEGATIVE":
                    return CmvAntibodyType.Negative;
                case "EQUIVOCAL":
                    return CmvAntibodyType.Equivocal;
                default:
                    return CmvAntibodyType.Unknown;
            }
        }
        
        public static VirologyStatus GetVirologyStatus(string virologyStatusString)
        {
            if (virologyStatusString == null)
            {
                return VirologyStatus.Unknown;
            }
            
            switch (virologyStatusString.ToUpperInvariant())
            {
                case "POSITIVE":
                    return VirologyStatus.Positive;
                case "NEGATIVE":
                    return VirologyStatus.Negative;
                case "EQUIVOCAL":
                    return VirologyStatus.Equivocal;
                default:
                    return VirologyStatus.Unknown;
            }
        }
    }
}
