using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    public enum HlaTypingCategory
    {
        // Either 3 or 4 field allele, depending on the allele
        Tgs,
        ThreeField,
        TwoField,
        XxCode,
        NmdpCode,
        Serology
    }

    public static class HlaTypingCategoryHelper
    {
        public static IEnumerable<HlaTypingCategory> AllCategories()
        {
            return Enum.GetValues(typeof(HlaTypingCategory)).Cast<HlaTypingCategory>();
        }
    }
    
}