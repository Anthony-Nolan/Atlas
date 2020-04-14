using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.InputParsers
{
    public static class OrientationParser
    {
        public static MatchOrientation ParseOrientation(string orientation)
        {
            switch (orientation)
            {
                case "cross":
                    return MatchOrientation.Cross;
                case "direct":
                    return MatchOrientation.Direct;
                default:
                    return MatchOrientation.Arbitrary;
            }
        }
    }
}