namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    public enum MatchLevel
    {
        // Exact allele match
        Allele,
        // First three fields of a four field allele match
        ThreeFieldAllele,
        // Alleles in same p-group (but not g-group)
        PGroup,
        // Alleles in same g-group
        GGroup,
    }
}