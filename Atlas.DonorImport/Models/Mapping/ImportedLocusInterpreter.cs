using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Models.FileSchema
{
    internal interface IImportedLocusInterpreter
    {
        LocusInfo<string> Interpret(ImportedLocus locus);
        /// <summary>
        /// Store contextual information for use with logging warnings.
        /// </summary>
        /// <param name="fileUpdate"></param>
        void SetDonorContext(DonorUpdate fileUpdate);
    }

    internal class ImportedLocusInterpreter : IImportedLocusInterpreter
    {
        private readonly IHlaCategorisationService storedHlaCategoriser;
        private readonly ILogger storedLogger;
        private DonorUpdate currentDonorFileData = null;
        private TwoFieldStringData Dna;
        private TwoFieldStringData Serology;


        public ImportedLocusInterpreter(IHlaCategorisationService hlaCategoriser, ILogger logger)
        {
            this.storedHlaCategoriser = hlaCategoriser;
            this.storedLogger = logger;
        }

        public LocusInfo<string> Interpret(ImportedLocus locus)
        {
#pragma warning disable 618
            Dna = MergeEmptyToNull(locus?.Dna);
            Serology = MergeEmptyToNull(locus?.Serology);
            field1IsPrecalculated = false;
            field2IsPrecalculated = false;
#pragma warning restore 618

            return new LocusInfo<string>
            {
                Position1 = ReadField1(storedHlaCategoriser, storedLogger),
                Position2 = ReadField2(storedHlaCategoriser, storedLogger)
            };
        }

        private string ToNullIfEmpty(string input) => string.IsNullOrEmpty(input) ? null : input;
        private TwoFieldStringData MergeEmptyToNull(TwoFieldStringData input) => new TwoFieldStringData { Field1 = ToNullIfEmpty(input?.Field1), Field2 = ToNullIfEmpty(input?.Field2) };

        /// <inheritdoc />
        public void SetDonorContext(DonorUpdate fileUpdate)
        {
            this.currentDonorFileData = fileUpdate;
        }

#pragma warning disable 618 // Dna & Serology are not Obsolete, but would be considered private if not for deserialization to this class

        #region Field1
        private bool field1IsPrecalculated = false;
        private string precalculatedField1 = null;
        private string ReadField1(IHlaCategorisationService hlaCategoriser, ILogger logger)
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

            if (standardisedDnaField1 != null)
            {
                return standardisedDnaField1;
            }

            if (Dna?.Field2 != null)
            {
                return null;
            }

            return Serology?.Field1;
        }
        #endregion

        #region Field2
        private bool field2IsPrecalculated = false;
        private string precalculatedField2 = null;

        private string ReadField2(IHlaCategorisationService hlaCategoriser, ILogger logger)
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

        private string StandardiseDnaField(string dnaField, IHlaCategorisationService hlaCategoriser, ILogger logger)
        {
            if (dnaField == null)
            {
                return null;
            }

            var needsStar = hlaCategoriser.ConformsToValidHlaFormat(dnaField);
            var hasStar = dnaField.StartsWith('*');
            if (needsStar && !hasStar)
            {
                logger.SendTrace("Prepended * to non-standard donor hla.", LogLevel.Verbose,
                    new Dictionary<string, string> {{"DonorCode", "XXX"}, {"HLA", "YYY"}, {"Location", "DNA Field1"}});
                return "*" + dnaField;
            }

            return dnaField;
        }

    }
}