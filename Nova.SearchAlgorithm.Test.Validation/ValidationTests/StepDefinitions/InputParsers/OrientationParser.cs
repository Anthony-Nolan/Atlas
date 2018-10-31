using Nova.SearchAlgorithm.Test.Validation.TestData.Models;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.InputParsers
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