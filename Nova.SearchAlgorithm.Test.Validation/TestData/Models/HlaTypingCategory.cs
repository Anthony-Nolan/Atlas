using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    public enum HlaTypingCategory
    {
        // A TGS typed, 4 field allele
        TgsFourFieldAllele,
        // A TGS typed, 3 field allele
        TgsThreeFieldAllele,
        // A TGS typed, 2 field allele
        TgsTwoFieldAllele,
        // A 4 field allele that has been truncated to 3 fields
        ThreeFieldTruncatedAllele,
        // A 3/4 field allele that has been truncated to 2 fields
        TwoFieldTruncatedAllele,
        XxCode,
        NmdpCode,
        Serology,
        Untyped
    }

    public static class HlaTypingCategoryHelper
    {
        public static IEnumerable<HlaTypingCategory> AllCategories()
        {
            return Enum.GetValues(typeof(HlaTypingCategory)).Cast<HlaTypingCategory>();
        }
    }
    
}