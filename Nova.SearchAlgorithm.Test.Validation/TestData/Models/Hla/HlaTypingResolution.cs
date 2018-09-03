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
        // A `/` separated list of possible allele names
        // e.g. 02:03:01/02:07:01/11:02:01
        AlleleStringOfNames,
        // An allele string of names, with alleles sharing a p-group
        AlleleStringOfNamesWithSinglePGroup,
        // An allele string of names, with at least two p-groups represented
        AlleleStringOfNamesWithMultiplePGroups,
        // A known first field, followed by a `/` separated list of possible second fields
        // This is what NMDP codes correspond to 
        // e.g. 01:01/02/03
        AlleleStringOfSubtypes,
        Serology,
        Untyped,
        // Used to indicate that any underlying typing resolution is acceptable
        // When comparing enum values, 'arbitary' should only match 'arbitrary', not any other values
        Arbitrary
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