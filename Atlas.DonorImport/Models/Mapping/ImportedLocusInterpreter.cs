using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.DonorImport.Models.FileSchema;
using MoreLinq;

namespace Atlas.DonorImport.Models.Mapping
{
    internal interface IImportedLocusInterpreter
    {
        /// <summary>
        /// Take the locus representation as found in the import files, and
        /// convert it into a standardised HLA representation.
        /// Standardisation currently consists of
        /// * using NULLs for missing data
        /// * pre-pending Molecular HLAs with '*'
        /// * interpreting missing 2nd Fields
        /// * determining whether Molecular or Serological data should be used.
        /// </summary>
        LocusInfo<string> Interpret(ImportedLocus locusData, Dictionary<string, string> loggingContext);
    }

    internal class ImportedLocusInterpreter : IImportedLocusInterpreter
    {
        private readonly IHlaCategorisationService categoriser;
        private readonly ILogger logger;
        private Dictionary<string, string> currentInterpretationContext;
        private const string contextHlaKey = "HLA";
        private const string contextPositionKey = "Position";

        public ImportedLocusInterpreter(IHlaCategorisationService hlaCategoriser, ILogger logger)
        {
            categoriser = hlaCategoriser;
            this.logger = logger;
        }

        /// <inheritdoc />
        public LocusInfo<string> Interpret(ImportedLocus locusData, Dictionary<string, string> loggingContext)
        {
            currentInterpretationContext = loggingContext?.ToDictionary() ?? new Dictionary<string, string>();

            if (locusData == null)
            {
                return NewNullLocusInfo();
            }

            var dna = locusData.Dna;
            var serology = locusData.Serology;

            if (IsBlank(dna) && IsBlank(serology))
            {
                return NewNullLocusInfo();
            }

            if (IsBlank(dna))
            {
                return InterpretFrom(serology);
            }

            var standardisedDna = StandardiseDna(dna);
            return InterpretFrom(standardisedDna);
        }

        public LocusInfo<string> InterpretFrom(TwoFieldStringData locusData)
        {
            var field1 = NullIfBlank(locusData.Field1);
            var field2 = NullIfBlank(locusData.Field2);

            if ((field1 ?? field2) == null)
            {
                return NewNullLocusInfo();
            }

            if (field2 == null)
            {
                // If Field2 is not Specified, but Field1 IS, then interpret that as a homozygous record and return Field1. The reverse is NOT valid.
                currentInterpretationContext[contextHlaKey] = field1;
                logger.SendTrace("Interpreted Locus Data as implicitly homozygous", LogLevel.Verbose, currentInterpretationContext);

                return new LocusInfo<string>(field1);
            }

            return new LocusInfo<string>(field1, field2);
        }

        private LocusInfo<string> NewNullLocusInfo() => new LocusInfo<string>(null as string);
        private bool IsBlank(TwoFieldStringData input) => input == null || (IsBlank(input.Field1) && IsBlank(input.Field2));
        private bool IsBlank(string input) => string.IsNullOrEmpty(input);
        private string NullIfBlank(string input) => IsBlank(input) ? null : input;

        /// <summary>
        /// Currently this only covers ensuring that Molecular HLAs are pre-pended by a '*'
        /// </summary>
        /// <param name="dnaData"></param>
        /// <returns></returns>
        public TwoFieldStringData StandardiseDna(TwoFieldStringData dnaData)
        {
            return new TwoFieldStringData
            {
                Field1 = StandardiseDnaField(dnaData.Field1, "1"),
                Field2 = StandardiseDnaField(dnaData.Field2, "2")
            };
        }

        private string StandardiseDnaField(string dnaField, string positionLabel)
        {
            if (IsBlank(dnaField))
            {
                return null;
            }
            
            var hasStar = dnaField.StartsWith('*');
            if (!hasStar)
            {
                var needsStar = categoriser.ConformsToValidHlaFormat(dnaField); // Only do this regex if it doesn't already start with a '*'.
                if (needsStar)
                {
                    currentInterpretationContext[contextHlaKey] = dnaField;
                    currentInterpretationContext[contextPositionKey] = positionLabel;
                    logger.SendTrace("Prepended * to non-standard donor hla.", LogLevel.Verbose, currentInterpretationContext);
                    currentInterpretationContext.Remove(contextPositionKey);

                    return "*" + dnaField;
                }
            }

            return dnaField;
        }
    }
}