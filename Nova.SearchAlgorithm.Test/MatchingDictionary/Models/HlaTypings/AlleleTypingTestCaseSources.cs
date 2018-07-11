using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.HlaTypings
{
    public class AlleleTypingTestCaseSources
    {
        private const string FieldValue = "999";
        private const string FieldDelimiter = ":";

        private const string FourFieldName = FieldValue + FieldDelimiter + FieldValue + FieldDelimiter + FieldValue +
                                             FieldDelimiter + FieldValue;
        private const string ThreeFieldName = FieldValue + FieldDelimiter + FieldValue + FieldDelimiter + FieldValue;
        private const string TwoFieldName = FieldValue + FieldDelimiter + FieldValue;

        private static readonly object[] NormallyExpressedFourFieldAllele = { MatchLocus.A, FourFieldName };
        private static readonly object[] NormallyExpressedThreeFieldAllele = { MatchLocus.A, ThreeFieldName };
        private static readonly object[] NormallyExpressedTwoFieldAllele = { MatchLocus.A, TwoFieldName };
        private static readonly object[] LowExpressedAllele = { MatchLocus.B, FourFieldName + "L" };
        private static readonly object[] QuestionableExpressedAllele = { MatchLocus.C, FourFieldName + "Q" };
        private static readonly object[] SecretedExpressedAllele = { MatchLocus.Dqb1, FourFieldName + "S" };
        private static readonly object[] AberrantExpressedAllele = { MatchLocus.Drb1, FourFieldName + "A" };
        private static readonly object[] CytoplasmicExpressedAllele = { MatchLocus.A, FourFieldName + "C" };
        private static readonly object[] NullExpressedAllele = { MatchLocus.B, FourFieldName + "N" };

        public static readonly object[] AlleleTypingsToTest =
        {
            NormallyExpressedFourFieldAllele,
            NormallyExpressedThreeFieldAllele,
            NormallyExpressedTwoFieldAllele,
            LowExpressedAllele,
            QuestionableExpressedAllele,
            SecretedExpressedAllele,
            AberrantExpressedAllele,
            CytoplasmicExpressedAllele,
            NullExpressedAllele
        };

        public static readonly object[] ExpectedMolecularLocus =
        {
            new object[] {NormallyExpressedFourFieldAllele, "A*"},
            new object[] {NormallyExpressedThreeFieldAllele, "A*"},
            new object[] {NormallyExpressedTwoFieldAllele, "A*"},
            new object[] {LowExpressedAllele, "B*"},
            new object[] {QuestionableExpressedAllele, "C*"},
            new object[] {SecretedExpressedAllele, "DQB1*"},
            new object[] {AberrantExpressedAllele, "DRB1*"},
            new object[] {CytoplasmicExpressedAllele, "A*"},
            new object[] {NullExpressedAllele, "B*"}
        };

        public static readonly object[] ExpectedExpressionSuffixes =
        {
            new object[] {NormallyExpressedFourFieldAllele, ""},
            new object[] {NormallyExpressedThreeFieldAllele, ""},
            new object[] {NormallyExpressedTwoFieldAllele, ""},
            new object[] {LowExpressedAllele, "L"},
            new object[] {QuestionableExpressedAllele, "Q"},
            new object[] {SecretedExpressedAllele, "S"},
            new object[] {AberrantExpressedAllele, "A"},
            new object[] {CytoplasmicExpressedAllele, "C"},
            new object[] {NullExpressedAllele, "N"}
        };

        public static readonly object[] ExpectedIsNullExpresser =
        {
            new object[] {NormallyExpressedFourFieldAllele, false},
            new object[] {NormallyExpressedThreeFieldAllele, false},
            new object[] {NormallyExpressedTwoFieldAllele, false},
            new object[] {LowExpressedAllele, false},
            new object[] {QuestionableExpressedAllele, false},
            new object[] {SecretedExpressedAllele, false},
            new object[] {AberrantExpressedAllele, false},
            new object[] {CytoplasmicExpressedAllele, false},
            new object[] {NullExpressedAllele, true}
        };

        public static readonly object[] ExpectedFields =
        {
            new object[] {NormallyExpressedFourFieldAllele, new[] { FieldValue, FieldValue, FieldValue, FieldValue }},
            new object[] {NormallyExpressedThreeFieldAllele, new[] { FieldValue, FieldValue, FieldValue } },
            new object[] {NormallyExpressedTwoFieldAllele, new[] { FieldValue, FieldValue } },
            new object[] {LowExpressedAllele, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {QuestionableExpressedAllele, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {SecretedExpressedAllele, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {AberrantExpressedAllele, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {CytoplasmicExpressedAllele, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {NullExpressedAllele, new[] { FieldValue, FieldValue , FieldValue, FieldValue } }
        };

        public static readonly object[] ExpectedTwoFieldNames =
        {
            new object[] {NormallyExpressedFourFieldAllele, TwoFieldName},
            new object[] {NormallyExpressedThreeFieldAllele, TwoFieldName},
            new object[] {NormallyExpressedTwoFieldAllele, TwoFieldName},
            new object[] {LowExpressedAllele, TwoFieldName + "L"},
            new object[] {QuestionableExpressedAllele, TwoFieldName + "Q"},
            new object[] {SecretedExpressedAllele, TwoFieldName + "S"},
            new object[] {AberrantExpressedAllele, TwoFieldName + "A"},
            new object[] {CytoplasmicExpressedAllele, TwoFieldName + "C"},
            new object[] {NullExpressedAllele, TwoFieldName + "N" }
        };

        public static readonly object[] ExpectedAlleleNameVariants =
        {
            new object[] {NormallyExpressedFourFieldAllele, new[] { TwoFieldName, ThreeFieldName }},
            new object[] {NormallyExpressedThreeFieldAllele, new[] { TwoFieldName }},
            new object[] {NormallyExpressedTwoFieldAllele, new string[]{}},
            new object[] {LowExpressedAllele, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "L", ThreeFieldName + "L" }},
            new object[] {QuestionableExpressedAllele, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "Q", ThreeFieldName + "Q" } },
            new object[] {SecretedExpressedAllele, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "S", ThreeFieldName + "S" } },
            new object[] {AberrantExpressedAllele, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "A", ThreeFieldName + "A" } },
            new object[] {CytoplasmicExpressedAllele, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "C", ThreeFieldName + "C" } },
            new object[] {NullExpressedAllele, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "N", ThreeFieldName + "N" } }
        };
    }
}
