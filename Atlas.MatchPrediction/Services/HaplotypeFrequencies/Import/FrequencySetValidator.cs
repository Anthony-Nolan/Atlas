using System.Linq;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    public interface IFrequencySetValidator
    { 
        void Validate(FrequencySetFileSchema frequencySetFile);
    }

    internal class FrequencySetValidator : IFrequencySetValidator
    {
        public void Validate(FrequencySetFileSchema frequencySetFile) 
        {
            if (!frequencySetFile.EthnicityCodes.IsNullOrEmpty())
            {
                if (frequencySetFile.RegistryCodes.IsNullOrEmpty())
                {
                    throw new MalformedHaplotypeFileException(
                        $"Cannot import set: Ethnicity codes ([{frequencySetFile.EthnicityCodes.StringJoin(", ")}]) provided but no registry");
                }

                if (frequencySetFile.EthnicityCodes.Length != frequencySetFile.EthnicityCodes.ToHashSet().Count)
                {
                    throw new MalformedHaplotypeFileException($"Cannot import set: Cannot import duplicate registry codes");
                }
            }

            if (!frequencySetFile.RegistryCodes.IsNullOrEmpty())
            {
                if (frequencySetFile.RegistryCodes.Contains(null))
                {
                    throw new MalformedHaplotypeFileException("Cannot import set: Invalid registry codes");
                }

                if (frequencySetFile.RegistryCodes.Length != frequencySetFile.RegistryCodes.ToHashSet().Count)
                {
                    throw new MalformedHaplotypeFileException($"Cannot import set: Cannot import duplicate registry codes");
                }
            }

            if (frequencySetFile.HlaNomenclatureVersion.IsNullOrEmpty())
            {
                throw new MalformedHaplotypeFileException("Cannot import set: Nomenclature version must be set");
            }

            foreach (var frequencyRecord in frequencySetFile.Frequencies)
            {
                if (frequencyRecord == null)
                {
                    throw new MalformedHaplotypeFileException("Set does not contain any frequencies");
                }

                if (frequencyRecord.Frequency == 0m)
                {
                    throw new MalformedHaplotypeFileException($"Haplotype frequency property frequency cannot be 0.");
                }

                if (frequencyRecord.A == null ||
                    frequencyRecord.B == null ||
                    frequencyRecord.C == null ||
                    frequencyRecord.Dqb1 == null ||
                    frequencyRecord.Drb1 == null)
                {
                    throw new MalformedHaplotypeFileException($"Haplotype frequency loci cannot be null.");
                }
            }
        }
    }
}
