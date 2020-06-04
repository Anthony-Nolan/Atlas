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
    }

    internal class DonorImportFileParser : IDonorImportFileParser
    {
        public IEnumerable<DonorUpdate> LazilyParseDonorUpdates(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                UpdateMode? updateMode = null;
                // Loops through top level JSON
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var propertyName = reader.Value?.ToString();
                        switch (propertyName)
                        {
                            case nameof(DonorImportFileSchema.updateMode):
                                // Read into property
                                reader.Read();
                                updateMode = serializer.Deserialize<UpdateMode>(reader);
                                break;
                            case nameof(DonorImportFileSchema.donors):
                                reader.Read(); // Read into property
                                reader.Read(); // Read into array. Do not deserialize to collection, as the collection can be very large and requires streaming.
                                // Loops through all donors in array
                                do
                                {
                                    if (!updateMode.HasValue)
                                    {
                                        throw new Exception("Update Mode must be provided before donor list in donor import JSON file.");
                                    }

                                    var donorOperation = serializer.Deserialize<DonorUpdate>(reader);
                                    if (donorOperation == null)
                                    {
                                        throw new Exception("Donor in array of input file could not be parsed.");
                                    }

                                    donorOperation.UpdateMode = updateMode.Value;
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
}