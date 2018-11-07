using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.InputParsers
{
    public static class PositionParser
    {
        public static IEnumerable<TypePosition> ParsePositions(string positionType)
        {
            switch (positionType)
            {
                case "position 1":
                case "position one":
                    return new[] {TypePosition.One};
                case "position 2":
                case "position two":
                    return new[] {TypePosition.Two};
                default:
                    throw new ArgumentOutOfRangeException($"Position: {positionType} not recognised");
            }
        }
    }
}