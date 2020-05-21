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
        public IEnumerable<DonorUpdate> LazilyParseDonorUpdates(Stream stream);

        public UpdateMode ParseUpdateMode(Stream stream);
    }

    internal class DonorImportFileParser : IDonorImportFileParser
    {
        private const string UpdateModePropertyName = "updateMode";
        private const string DonorsPropertyName = "donors";

        public IEnumerable<DonorUpdate> LazilyParseDonorUpdates(Stream stream)
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
                        case UpdateModePropertyName:
                            // Read into property
                            reader.Read();
                            var updateMode = serializer.Deserialize<string>(reader);
                            // Update mode is parsed independently
                            break;
                        case DonorsPropertyName:
                            reader.Read(); // Read into property
                            reader.Read(); // Read into array. Do not deserialize to collection, as the collection can be very large and requires streaming.
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

        public UpdateMode ParseUpdateMode(Stream stream)
        {
            using var streamReader = new StreamReader(stream);
            using var reader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == UpdateModePropertyName)
                {
                    // Read into property
                    reader.Read();
                    return serializer.Deserialize<UpdateMode>(reader);
                }
            }
            
            throw new Exception($"Expected property: {UpdateModePropertyName} was not found in donor import file.");
        }
    }
}