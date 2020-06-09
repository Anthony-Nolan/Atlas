using System;
using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Models.FileSchema
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ClassNeverInstantiated.Global - Instantiated by JSON parser
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    internal class Hla
    {
        public Locus A { get; set; }
        public Locus B { get; set; }
        public Locus C { get; set; }
        public Locus DPB1 { get; set; }
        public Locus DQB1 { get; set; }
        public Locus DRB1 { get; set; }
    }

    internal class Locus
    {
        // ReSharper disable once MemberCanBePrivate.Global - Needed for JSON parsing
        [Obsolete("Access via ReadField1 and ReadField2, not directly - this property is only for deserialization purposes.")]
        public DnaLocus Dna { get; set; }

        [Obsolete("Access via ReadField1 and ReadField2, not directly - this property is only for deserialization purposes.")]
        [JsonProperty(PropertyName = "ser")]
        public SerologyLocus Serology { get; set; }

#pragma warning disable 618 // Dna & Serology are not Obsolete, but would be considered private if not for deserialization to this class
        #region Field1
        private bool field1IsPrecalculated = false;
        private string precalculatedField1 = null;
        public string ReadField1(IHlaCategorisationService hlaCategoriser, ILogger logger)
        {
            if (!field1IsPrecalculated)
            {
                precalculatedField1 = CalculateField1(hlaCategoriser, logger);
                field1IsPrecalculated = true;
            }
            return precalculatedField1;
        }
        private string CalculateField1(IHlaCategorisationService hlaCategoriser, ILogger logger)
        {
            var standardisedDnaField1 = StandardiseDnaField(Dna?.Field1, hlaCategoriser, logger);

            if(standardisedDnaField1 != null) { return standardisedDnaField1; }
            if (Dna?.Field2 != null) { return null; }
            return Serology?.Field1;
        }
        #endregion

        #region Field2
        private bool field2IsPrecalculated = false;
        private string precalculatedField2 = null;
        public string ReadField2(IHlaCategorisationService hlaCategoriser, ILogger logger)
        {
            if (!field2IsPrecalculated)
            {
                precalculatedField2 = CalculateField2(hlaCategoriser, logger);
                field2IsPrecalculated = true;
            }
            return precalculatedField2;
        }
        private string CalculateField2(IHlaCategorisationService hlaCategoriser, ILogger logger)
        {
            var standardisedDnaField2 = StandardiseDnaField(Dna?.Field2, hlaCategoriser, logger);
            if (standardisedDnaField2 != null)
            {
                return standardisedDnaField2;
            }

            var standardisedDnaField1 = StandardiseDnaField(Dna?.Field1, hlaCategoriser, logger);
            if (standardisedDnaField1 != null)
            {
                // If Field2 is not Specified, but Field1 IS, then interpret that as a homozygous record and return Field1. The reverse is NOT valid.
                logger.SendTrace("Interpreted Dna Data as implicitly homozygous", LogLevel.Verbose,
                    new Dictionary<string, string> { { "DonorCode", "XXX" }, { "HLA", "YYY" } });
                return standardisedDnaField1;
            }

            if (Serology?.Field2 != null)
            {
                return Serology?.Field2;
            }

            if (Serology?.Field1 != null)
            {
                // If Field2 is not Specified, but Field1 IS, then interpret that as a homozygous record and return Field1. The reverse is NOT valid.
                logger.SendTrace("Interpreted Serology Data as implicitly homozygous", LogLevel.Verbose,
                    new Dictionary<string, string> { { "DonorCode", "XXX" }, { "HLA", "YYY" } });
                return Serology?.Field1;
            }

            return null;
        }
        #endregion
#pragma warning restore 618

        private string StandardiseDnaField(string dnaField1, IHlaCategorisationService hlaCategoriser, ILogger logger)
        {
            if (dnaField1 == null)
            {
                return null;
            }

            var needsStar = hlaCategoriser.IsRecognisableHla(dnaField1);
            var hasStar = dnaField1.StartsWith('*');
            if (needsStar && !hasStar)
            {
                logger.SendTrace("Prepended * to non-standard donor hla.", LogLevel.Verbose,
                    new Dictionary<string, string> {{"DonorCode", "XXX"}, {"HLA", "YYY"}, {"Location", "DNA Field1"}});
                return "*" + dnaField1;
            }

            return dnaField1;
        }

    }

    internal class DnaLocus : TwoFieldDefaultingStringData
    {
    }

    internal class SerologyLocus : TwoFieldDefaultingStringData
    {
    }

    internal abstract class TwoFieldDefaultingStringData
    {
        [JsonIgnore]
        private string raw1;
        public string Field1 { get => raw1; set => raw1 = string.IsNullOrEmpty(value) ? null : value; }

        [JsonIgnore]
        private string raw2;
        public string Field2 { get => raw2; set => raw2 = string.IsNullOrEmpty(value) ? null : value; }
    }
}