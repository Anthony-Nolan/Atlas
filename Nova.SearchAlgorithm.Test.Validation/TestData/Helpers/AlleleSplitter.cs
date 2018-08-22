using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Helpers
{
    public static class AlleleSplitter
    {
        public static int NumberOfFields(string allele)
        {
            return SplitToFields(allele).Count();
        }
        
        public static IEnumerable<string> FirstThreeFields(string allele)
        {
            return SplitToFields(allele).Take(3);
        }
        
        public static string FirstThreeFieldsAsString(string allele)
        {
            return FirstThreeFields(allele).Aggregate((agg, s) => agg + s);
        }
        
        public static IEnumerable<string> FirstTwoFields(string allele)
        {
            return SplitToFields(allele).Take(2);
        }
        
        public static string FirstTwoFieldsAsString(string allele)
        {
            return FirstTwoFields(allele).Aggregate((agg, s) => agg + s);
        }
        
        public static string RemoveLastField(string allele)
        {
            var splitAllele = SplitToFields(allele).ToList();
            return JoinFields(splitAllele.Take(splitAllele.Count - 1));
        }

        public static string FirstField(string allele)
        {
            return SplitToFields(allele).First();
        }

        private static IEnumerable<string> SplitToFields(string alleleString)
        {
            // TODO: NOVA-1571: Handle alleles with an expression suffix. This truncation will remove expression suffix.
            return alleleString.Split(':');
        }

        private static string JoinFields(IEnumerable<string> fields)
        {
            return string.Join(":", fields);
        }
    }
}