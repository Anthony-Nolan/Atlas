using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.Models.FileSchema;
using Newtonsoft.Json;
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Atlas.DonorImport.Services
{
    public interface IDonorFileImporter
    {
        Task ImportDonorFile(Stream fileStream);
    }

    public class DonorFileImporter : IDonorFileImporter
    {
        private readonly IDonorOperationApplier donorOperationApplier;

        public DonorFileImporter(IDonorOperationApplier donorOperationApplier)
        {
            this.donorOperationApplier = donorOperationApplier;
        }
        
        public async Task ImportDonorFile(Stream fileStream)
        {
            using var streamReader = new StreamReader(fileStream);
            using var reader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();

            var inDonorsProperty = false;
            var inDonorUpdateArray = false;
            while (await reader.ReadAsync())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName when reader.Value?.ToString() == "donors":
                        inDonorsProperty = true;
                        break;
                    case JsonToken.StartArray when inDonorsProperty:
                        inDonorUpdateArray = true;
                        break;
                    case JsonToken.StartObject when inDonorUpdateArray:
                        var donorOperation = serializer.Deserialize<DonorUpdate>(reader);
                        // TODO: ATLAS-167: Batch donor updates before applying
                        await donorOperationApplier.ApplyDonorOperationBatch(new List<DonorUpdate> {donorOperation});
                        break;
                    case JsonToken.EndArray when inDonorUpdateArray:
                        inDonorUpdateArray = false;
                        break;
                    case JsonToken.EndObject when !inDonorUpdateArray && inDonorsProperty:
                        inDonorsProperty = false;
                        break;
                }
            }
        }
    }
}