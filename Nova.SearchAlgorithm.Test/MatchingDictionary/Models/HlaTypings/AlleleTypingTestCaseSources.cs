using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation;

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

        private static readonly Allele NormallyExpressedFourFieldAllele = new Allele { Locus = Locus.A, Name = FourFieldName };
        private static readonly Allele NormallyExpressedThreeFieldAllele = new Allele { Locus = Locus.A, Name = ThreeFieldName };
        private static readonly Allele NormallyExpressedTwoFieldAllele = new Allele { Locus = Locus.A, Name = TwoFieldName };
        private static readonly Allele LowExpressedAllele = new Allele { Locus = Locus.B, Name = FourFieldName + "L" };
        private static readonly Allele QuestionableExpressedAllele = new Allele { Locus = Locus.C, Name = FourFieldName + "Q" };
        private static readonly Allele SecretedExpressedAllele = new Allele { Locus = Locus.Dqb1, Name = FourFieldName + "S" };
        private static readonly Allele AberrantExpressedAllele = new Allele { Locus = Locus.Drb1, Name = FourFieldName + "A" };
        private static readonly Allele CytoplasmicExpressedAllele = new Allele { Locus = Locus.Dpb1, Name = FourFieldName + "C" };
        private static readonly Allele NullExpressedAllele = new Allele { Locus = Locus.B, Name = FourFieldName + "N" };

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
            new object[] {CytoplasmicExpressedAllele, "DPB1*"},
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

        public static readonly object[] ExpectedTwoFieldWithExpressionSuffixNames =
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

        public static readonly object[] ExpectedTwoFieldWithoutExpressionSuffixNames =
        {
            new object[] {NormallyExpressedFourFieldAllele, TwoFieldName},
            new object[] {NormallyExpressedThreeFieldAllele, TwoFieldName},
            new object[] {NormallyExpressedTwoFieldAllele, TwoFieldName},
            new object[] {LowExpressedAllele, TwoFieldName},
            new object[] {QuestionableExpressedAllele, TwoFieldName},
            new object[] {SecretedExpressedAllele, TwoFieldName},
            new object[] {AberrantExpressedAllele, TwoFieldName},
            new object[] {CytoplasmicExpressedAllele, TwoFieldName},
            new object[] {NullExpressedAllele, TwoFieldName}
        };

        public static readonly object[] ExpectedFirstField =
        {
            new object[] {NormallyExpressedFourFieldAllele, FieldValue},
            new object[] {NormallyExpressedThreeFieldAllele, FieldValue},
            new object[] {NormallyExpressedTwoFieldAllele, FieldValue},
            new object[] {LowExpressedAllele, FieldValue},
            new object[] {QuestionableExpressedAllele, FieldValue},
            new object[] {SecretedExpressedAllele, FieldValue},
            new object[] {AberrantExpressedAllele, FieldValue},
            new object[] {CytoplasmicExpressedAllele, FieldValue},
            new object[] {NullExpressedAllele, FieldValue}
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
