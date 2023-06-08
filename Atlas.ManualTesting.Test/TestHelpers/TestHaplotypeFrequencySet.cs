using System.Reflection;
using Atlas.MatchPrediction.Models.FileSchema;
using Newtonsoft.Json;

namespace Atlas.ManualTesting.Test.TestHelpers;

internal class TestHaplotypeFrequencySet
{
    /// <summary>
    /// Set containing a few haplotypes designed to test <see cref="Services.HaplotypeFrequencySet.IHaplotypeFrequencySetTransformer"/>.
    /// </summary>
    public FrequencySetFileSchema TransformerTestSet { get; set; }

    public FrequencySetFileSchema EmptySetWithNoFrequencies { get; set; }

    public TestHaplotypeFrequencySet()
    {
        TransformerTestSet = GetTransformerTestSetFromFile();
        EmptySetWithNoFrequencies = new FrequencySetFileSchema();
    }

    private static FrequencySetFileSchema GetTransformerTestSetFromFile()
    {
        const string testFileName = "Atlas.ManualTesting.Test.Resources.hf-set-for-testing-transformer.json";

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(testFileName);
        using var reader = new StreamReader(stream ?? throw new InvalidOperationException());
        var set = JsonConvert.DeserializeObject<FrequencySetFileSchema>(reader.ReadToEnd());

        if (set == null)
        {
            throw new Exception($"Test set could not be loaded from {testFileName}");
        }

        return set;
    }
}