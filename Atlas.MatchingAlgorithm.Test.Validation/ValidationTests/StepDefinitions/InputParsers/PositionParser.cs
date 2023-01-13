using System;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.InputParsers
{
    public static class PositionParser
    {
        public static IEnumerable<LocusPosition> ParsePositions(string positionType)
        {
            switch (positionType)
            {
                case "position 1":
                case "position one":
                    return new[] {LocusPosition.One};
                case "position 2":
                case "position two":
                    return new[] {LocusPosition.Two};
                default:
                    throw new ArgumentOutOfRangeException($"Position: {positionType} not recognised");
            }
        }
    }
}