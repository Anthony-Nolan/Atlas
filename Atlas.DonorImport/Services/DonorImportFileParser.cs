using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.Models.FileSchema;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorImportFileParser
    {
        /// <returns>A batch of parsed donor updates</returns>
        public IAsyncEnumerable<IEnumerable<DonorUpdate>> LazilyParseDonorUpdates(Stream stream, int batchSize = 10000);
    }

    internal class DonorImportFileParser : IDonorImportFileParser
    {
        public async IAsyncEnumerable<IEnumerable<DonorUpdate>> LazilyParseDonorUpdates(Stream stream, int batchSize)
        {
            using var streamReader = new StreamReader(stream);
            using var reader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();

            var donorBatch = new List<DonorUpdate>();
            
            // Loops through top level JSON
            while (await reader.ReadAsync())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString();
                    switch (propertyName)
                    {
                        case "updateMode":
                            // Read into property
                            await reader.ReadAsync();
                            var updateMode = serializer.Deserialize<string>(reader);
                            // We do not yet care about the update mode
                            break;
                        case "donors":
                            // Read into property
                            await reader.ReadAsync();
                            // Read into array. Do not deserialize to collection, as the collection can be very large and requires streaming.
                            await reader.ReadAsync();
                            // Loops through all donors in array
                            do
                            {
                                var donorOperation = serializer.Deserialize<DonorUpdate>(reader);
                                donorBatch.Add(donorOperation);
                                if (donorBatch.Count >= batchSize)
                                {
                                    yield return donorBatch;
                                    donorBatch = new List<DonorUpdate>();
                                }
                            } while (await reader.ReadAsync() && reader.TokenType != JsonToken.EndArray);

                            break;
                        default:
                            throw new Exception($"Unrecognised property: {propertyName} encountered in donor import file.");
                    }
                }
            }

            yield return donorBatch;
        }
    }
}