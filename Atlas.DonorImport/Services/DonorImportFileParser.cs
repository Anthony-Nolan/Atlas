using System;
using System.Collections.Generic;
using System.IO;
using Atlas.DonorImport.Models.FileSchema;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorImportFileParser
    {
        /// <returns>A batch of parsed donor updates</returns>
        public IEnumerable<IEnumerable<DonorUpdate>> LazilyParseDonorUpdates(Stream stream, int batchSize = 10000);
    }

    internal class DonorImportFileParser : IDonorImportFileParser
    {
        public IEnumerable<IEnumerable<DonorUpdate>> LazilyParseDonorUpdates(Stream stream, int batchSize)
        {
            using var streamReader = new StreamReader(stream);
            using var reader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();

            var donorBatch = new List<DonorUpdate>();
            
            // Loops through top level JSON
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString();
                    switch (propertyName)
                    {
                        case "updateMode":
                            // Read into property
                            reader.Read();
                            var updateMode = serializer.Deserialize<string>(reader);
                            // We do not yet care about the update mode
                            break;
                        case "donors":
                            reader.Read(); // Read into property
                            reader.Read(); // Read into array. Do not deserialize to collection, as the collection can be very large and requires streaming.

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
                            } while (reader.Read() && reader.TokenType != JsonToken.EndArray);

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