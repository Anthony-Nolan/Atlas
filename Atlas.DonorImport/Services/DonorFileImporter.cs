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
        private readonly int batchSize;

        public DonorFileImporter(IDonorOperationApplier donorOperationApplier, int batchSize = 10000)
        {
            this.donorOperationApplier = donorOperationApplier;
            this.batchSize = batchSize;
        }

        public async Task ImportDonorFile(Stream fileStream)
        {
            using var streamReader = new StreamReader(fileStream);
            using var reader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();

            var donorBatch = new List<DonorUpdate>();

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
                        donorBatch.Add(donorOperation);
                        if (donorBatch.Count >= batchSize)
                        {
                            await donorOperationApplier.ApplyDonorOperationBatch(donorBatch);
                            donorBatch = new List<DonorUpdate>();
                        }

                        break;
                    case JsonToken.EndArray when inDonorUpdateArray:
                        await donorOperationApplier.ApplyDonorOperationBatch(donorBatch);
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