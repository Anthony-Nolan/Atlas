using System;
using Atlas.Utils.Core.Models;

namespace Atlas.Utils.Core.BMDW
{
    public static class BmdwFieldTranslation
    {
        public static string GetSexCode(string sex)
        {
            switch (sex)
            {
                case "Female":
                    return "F";
                case "Male":
                    return "M";
                case "Unknown":
                    return null;
                default:
                    throw new ArgumentException("Sex must be either 'Female' or 'Male'");
            }
        }

        public static string GetCmvStatusCode(CmvAntibodyType cmvType)
        {
            switch (cmvType)
            {
                case CmvAntibodyType.Positive:
                    return "P";
                case CmvAntibodyType.Negative:
                    return "N";
                case CmvAntibodyType.Equivocal:
                    return "Q";
                default:
                    throw new ArgumentException("Cmv status must not be 'Unknown'");
            }
        }

        public static string GetTestResultCode(string result)
        {
            switch (result)
            {
                case "Positive":
                    return "P";
                case "Negative":
                    return "N";
                default:
                    throw new ArgumentException("Test result must be either 'Positive' or 'Negative'");
            }
        }
    }
}