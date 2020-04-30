using Atlas.Utils.Core.Models;

namespace Atlas.Utils.Core.Helpers
{
    public class AntibodyTypeHelper
    {
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
    }
}
