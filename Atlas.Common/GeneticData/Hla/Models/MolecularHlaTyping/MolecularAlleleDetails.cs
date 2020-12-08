using System;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;

namespace Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping
{
    public class MolecularAlleleDetails
    {

        public MolecularAlleleDetails()
        {
        }

        public string AlleleNameWithoutPrefix { get; }

        /// <summary>
        /// AKA "First Field"
        /// </summary>
        public string FamilyField { get; }

        /// <summary>
        /// AKA "Second Field"
        /// </summary>
        public string SubtypeField { get; }

        /// <summary>
        /// AKA "Third Field"
        /// </summary>
        public string IntronicField { get; }

        /// <summary>
        /// AKA "Fourth Field"
        /// </summary>
        public string SilentField { get; }

        public string ExpressionSuffix { get; }

        public MolecularAlleleDetails(string alleleName)
        {
            if (string.IsNullOrEmpty(alleleName))
            {
                throw new ArgumentException("Allele name cannot be null or empty.");
            }

            AlleleNameWithoutPrefix = AlleleSplitter.RemovePrefix(alleleName);

            var fields = AlleleSplitter.SplitToFields(alleleName).ToList();
            FamilyField = fields[0];
            SubtypeField = fields.Count > 1 ? fields[1] : string.Empty;
            IntronicField = fields.Count > 2 ? fields[2] : string.Empty;
            SilentField = fields.Count > 3 ? fields[3] : string.Empty;

            ExpressionSuffix = AlleleSplitter.GetExpressionSuffix(alleleName);
        }

        public MolecularAlleleDetails(string family, string subtype) : this(family + MolecularTypingNameConstants.FieldDelimiter + subtype)
        {
        }
    }
}