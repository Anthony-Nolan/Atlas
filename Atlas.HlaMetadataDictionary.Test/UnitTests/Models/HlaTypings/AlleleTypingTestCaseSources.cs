using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Models.HlaTypings
{
    public class AlleleTypingTestCaseSources
    {
        private const string FieldValue = "999";
        private const string FieldDelimiter = ":";

        private const string FourFieldName = FieldValue + FieldDelimiter + FieldValue + FieldDelimiter + FieldValue +
                                             FieldDelimiter + FieldValue;
        private const string ThreeFieldName = FieldValue + FieldDelimiter + FieldValue + FieldDelimiter + FieldValue;
        private const string TwoFieldName = FieldValue + FieldDelimiter + FieldValue;

        private static readonly AlleleTestCase NormallyExpressedFourFieldAlleleTestCase = new AlleleTestCase { Locus = Locus.A, Name = FourFieldName };
        private static readonly AlleleTestCase NormallyExpressedThreeFieldAlleleTestCase = new AlleleTestCase { Locus = Locus.A, Name = ThreeFieldName };
        private static readonly AlleleTestCase NormallyExpressedTwoFieldAlleleTestCase = new AlleleTestCase { Locus = Locus.A, Name = TwoFieldName };
        private static readonly AlleleTestCase LowExpressedAlleleTestCase = new AlleleTestCase { Locus = Locus.B, Name = FourFieldName + "L" };
        private static readonly AlleleTestCase QuestionableExpressedAlleleTestCase = new AlleleTestCase { Locus = Locus.C, Name = FourFieldName + "Q" };
        private static readonly AlleleTestCase SecretedExpressedAlleleTestCase = new AlleleTestCase { Locus = Locus.Dqb1, Name = FourFieldName + "S" };
        private static readonly AlleleTestCase AberrantExpressedAlleleTestCase = new AlleleTestCase { Locus = Locus.Drb1, Name = FourFieldName + "A" };
        private static readonly AlleleTestCase CytoplasmicExpressedAlleleTestCase = new AlleleTestCase { Locus = Locus.Dpb1, Name = FourFieldName + "C" };
        private static readonly AlleleTestCase NullExpressedAlleleTestCase = new AlleleTestCase { Locus = Locus.B, Name = FourFieldName + "N" };

        public static readonly object[] AlleleTypingsToTest =
        {
            NormallyExpressedFourFieldAlleleTestCase,
            NormallyExpressedThreeFieldAlleleTestCase,
            NormallyExpressedTwoFieldAlleleTestCase,
            LowExpressedAlleleTestCase,
            QuestionableExpressedAlleleTestCase,
            SecretedExpressedAlleleTestCase,
            AberrantExpressedAlleleTestCase,
            CytoplasmicExpressedAlleleTestCase,
            NullExpressedAlleleTestCase
        };

        public static readonly object[] ExpectedMolecularLocus =
        {
            new object[] {NormallyExpressedFourFieldAlleleTestCase, "A*"},
            new object[] {NormallyExpressedThreeFieldAlleleTestCase, "A*"},
            new object[] {NormallyExpressedTwoFieldAlleleTestCase, "A*"},
            new object[] {LowExpressedAlleleTestCase, "B*"},
            new object[] {QuestionableExpressedAlleleTestCase, "C*"},
            new object[] {SecretedExpressedAlleleTestCase, "DQB1*"},
            new object[] {AberrantExpressedAlleleTestCase, "DRB1*"},
            new object[] {CytoplasmicExpressedAlleleTestCase, "DPB1*"},
            new object[] {NullExpressedAlleleTestCase, "B*"}
        };

        public static readonly object[] ExpectedExpressionSuffixes =
        {
            new object[] {NormallyExpressedFourFieldAlleleTestCase, ""},
            new object[] {NormallyExpressedThreeFieldAlleleTestCase, ""},
            new object[] {NormallyExpressedTwoFieldAlleleTestCase, ""},
            new object[] {LowExpressedAlleleTestCase, "L"},
            new object[] {QuestionableExpressedAlleleTestCase, "Q"},
            new object[] {SecretedExpressedAlleleTestCase, "S"},
            new object[] {AberrantExpressedAlleleTestCase, "A"},
            new object[] {CytoplasmicExpressedAlleleTestCase, "C"},
            new object[] {NullExpressedAlleleTestCase, "N"}
        };

        public static readonly object[] ExpectedIsNullExpresser =
        {
            new object[] {NormallyExpressedFourFieldAlleleTestCase, false},
            new object[] {NormallyExpressedThreeFieldAlleleTestCase, false},
            new object[] {NormallyExpressedTwoFieldAlleleTestCase, false},
            new object[] {LowExpressedAlleleTestCase, false},
            new object[] {QuestionableExpressedAlleleTestCase, false},
            new object[] {SecretedExpressedAlleleTestCase, false},
            new object[] {AberrantExpressedAlleleTestCase, false},
            new object[] {CytoplasmicExpressedAlleleTestCase, false},
            new object[] {NullExpressedAlleleTestCase, true}
        };

        public static readonly object[] ExpectedFields =
        {
            new object[] {NormallyExpressedFourFieldAlleleTestCase, new[] { FieldValue, FieldValue, FieldValue, FieldValue }},
            new object[] {NormallyExpressedThreeFieldAlleleTestCase, new[] { FieldValue, FieldValue, FieldValue } },
            new object[] {NormallyExpressedTwoFieldAlleleTestCase, new[] { FieldValue, FieldValue } },
            new object[] {LowExpressedAlleleTestCase, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {QuestionableExpressedAlleleTestCase, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {SecretedExpressedAlleleTestCase, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {AberrantExpressedAlleleTestCase, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {CytoplasmicExpressedAlleleTestCase, new[] { FieldValue, FieldValue , FieldValue, FieldValue } },
            new object[] {NullExpressedAlleleTestCase, new[] { FieldValue, FieldValue , FieldValue, FieldValue } }
        };

        public static readonly object[] ExpectedTwoFieldIncludingExpressionSuffixNames =
        {
            new object[] {NormallyExpressedFourFieldAlleleTestCase, TwoFieldName},
            new object[] {NormallyExpressedThreeFieldAlleleTestCase, TwoFieldName},
            new object[] {NormallyExpressedTwoFieldAlleleTestCase, TwoFieldName},
            new object[] {LowExpressedAlleleTestCase, TwoFieldName + "L"},
            new object[] {QuestionableExpressedAlleleTestCase, TwoFieldName + "Q"},
            new object[] {SecretedExpressedAlleleTestCase, TwoFieldName + "S"},
            new object[] {AberrantExpressedAlleleTestCase, TwoFieldName + "A"},
            new object[] {CytoplasmicExpressedAlleleTestCase, TwoFieldName + "C"},
            new object[] {NullExpressedAlleleTestCase, TwoFieldName + "N" }
        };

        public static readonly object[] ExpectedTwoFieldExcludingExpressionSuffixNames =
        {
            new object[] {NormallyExpressedFourFieldAlleleTestCase, TwoFieldName},
            new object[] {NormallyExpressedThreeFieldAlleleTestCase, TwoFieldName},
            new object[] {NormallyExpressedTwoFieldAlleleTestCase, TwoFieldName},
            new object[] {LowExpressedAlleleTestCase, TwoFieldName},
            new object[] {QuestionableExpressedAlleleTestCase, TwoFieldName},
            new object[] {SecretedExpressedAlleleTestCase, TwoFieldName},
            new object[] {AberrantExpressedAlleleTestCase, TwoFieldName},
            new object[] {CytoplasmicExpressedAlleleTestCase, TwoFieldName},
            new object[] {NullExpressedAlleleTestCase, TwoFieldName}
        };

        public static readonly object[] ExpectedFirstField =
        {
            new object[] {NormallyExpressedFourFieldAlleleTestCase, FieldValue},
            new object[] {NormallyExpressedThreeFieldAlleleTestCase, FieldValue},
            new object[] {NormallyExpressedTwoFieldAlleleTestCase, FieldValue},
            new object[] {LowExpressedAlleleTestCase, FieldValue},
            new object[] {QuestionableExpressedAlleleTestCase, FieldValue},
            new object[] {SecretedExpressedAlleleTestCase, FieldValue},
            new object[] {AberrantExpressedAlleleTestCase, FieldValue},
            new object[] {CytoplasmicExpressedAlleleTestCase, FieldValue},
            new object[] {NullExpressedAlleleTestCase, FieldValue}
        };

        public static readonly object[] ExpectedAlleleNameVariants =
        {
            new object[] {NormallyExpressedFourFieldAlleleTestCase, new[] { TwoFieldName, ThreeFieldName }},
            new object[] {NormallyExpressedThreeFieldAlleleTestCase, new[] { TwoFieldName }},
            new object[] {NormallyExpressedTwoFieldAlleleTestCase, new string[]{}},
            new object[] {LowExpressedAlleleTestCase, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "L", ThreeFieldName + "L" }},
            new object[] {QuestionableExpressedAlleleTestCase, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "Q", ThreeFieldName + "Q" } },
            new object[] {SecretedExpressedAlleleTestCase, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "S", ThreeFieldName + "S" } },
            new object[] {AberrantExpressedAlleleTestCase, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "A", ThreeFieldName + "A" } },
            new object[] {CytoplasmicExpressedAlleleTestCase, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "C", ThreeFieldName + "C" } },
            new object[] {NullExpressedAlleleTestCase, new[] { TwoFieldName, ThreeFieldName, FourFieldName, TwoFieldName + "N", ThreeFieldName + "N" } }
        };
    }
}
