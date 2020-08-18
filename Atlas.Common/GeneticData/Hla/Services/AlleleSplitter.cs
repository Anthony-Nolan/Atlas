using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Utils.Extensions;

namespace Atlas.Common.GeneticData.Hla.Services
{
    public static class AlleleSplitter
    {
        private const char AlleleSeparator = ':';

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
            return FirstThreeFields(allele).StringJoin(AlleleSeparator);
        }
        
        public static IEnumerable<string> FirstTwoFields(string allele)
        {
            return SplitToFields(allele).Take(2);
        }
        
        public static string FirstTwoFieldsAsString(string allele)
        {
            return FirstTwoFields(allele).StringJoin(AlleleSeparator);
        }
        
        public static string RemoveLastField(string allele)
        {
            var splitAllele = SplitToFields(allele).ToList();
            return splitAllele.Take(splitAllele.Count - 1).StringJoin(AlleleSeparator);
        }

        public static string FirstField(string allele)
        {
            return SplitToFields(allele).First();
        }
        
        public static string SecondField(string allele)
        {
            return SplitToFields(allele).ToList()[1];
        }

        private static IEnumerable<string> SplitToFields(string alleleString)
        {
            // TODO: NOVA-1571: Handle alleles with an expression suffix. This truncation will remove expression suffix.
            return alleleString.Split(AlleleSeparator);
        }
    }
}