using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla
{
    /// <summary>
    /// The typing resolutions that TGS typings can be 'dumbed down' to
    /// </summary>
    public enum HlaTypingResolution
    {
        // A TGS typed allele, 2/3/4 fields
        Tgs,
        // A 4 field allele that has been truncated to 3 fields
        ThreeFieldTruncatedAllele,
        // A 3/4 field allele that has been truncated to 2 fields
        TwoFieldTruncatedAllele,
        XxCode,
        NmdpCode,
        Serology,
        Untyped,
        // Used to indicate that any underlying typing resolution is acceptable
        // When comparing enum values, 'arbitary' should only match 'arbitrary', not any other values
        Arbitrary,
    }
    
    /// <summary>
    /// The categories of tgs typed data available.
    /// Note that 2/3 field TGS typings are still as high resolution as possible, unlike 4 field alleles truncated to 2/3 field
    /// </summary>
    public enum TgsHlaTypingCategory
    {
        // A TGS typed, 4 field allele
        FourFieldAllele,
        // A TGS typed, 3 field allele
        ThreeFieldAllele,
        // A TGS typed, 2 field allele
        TwoFieldAllele,
        // Used to indicate that any underlying TGS typed allele is acceptable
        // When comparing enum values, 'arbitary' should only match 'arbitrary', not any other values
        Arbitrary,
    }

    public static class HlaTypingCategoryHelper
    {
        public static IEnumerable<HlaTypingResolution> AllResolutions()
        {
            return Enum.GetValues(typeof(HlaTypingResolution)).Cast<HlaTypingResolution>();
        }
    }
    
}