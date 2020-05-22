using System;
using System.Collections.Generic;
using System.IO;
using Atlas.DonorImport.Models.FileSchema;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorImportFileParser
    {
        public IEnumerable<DonorUpdate> ParseDonorUpdates(Stream stream);
    }

    internal class DonorImportFileParser : IDonorImportFileParser
    {
        public IEnumerable<DonorUpdate> ParseDonorUpdates(Stream stream)
        {
            using var streamReader = new StreamReader(stream);
            using var reader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();

            // Loops through top level JSON
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value.ToString();
                    switch (propertyName)
                    {
                        case "updateMode":
                            // Read into property
                            reader.Read();
                            var updateMode = serializer.Deserialize<string>(reader);
                            // We do not yet care about the update mode
                            break;
                        case "donors":
                            // Read into property
                            reader.Read();
                            // Read into array. Do not deserialize to collection, as the collection can be very large and requires streaming.
                            reader.Read();
                            // Loops through all donors in array
                            do
                            {
                                var donorOperation = serializer.Deserialize<DonorUpdate>(reader);
                                yield return donorOperation;
                            } while (reader.Read() && reader.TokenType != JsonToken.EndArray);

                            break;
                        default:
                            throw new Exception($"Unrecognised property: {propertyName} encountered in donor import file.");
                    }
                }
            }
        }
    }
}