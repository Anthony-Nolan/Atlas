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
        // TODO: ATLAS-287: Clean up internals of this class
        public IEnumerable<DonorUpdate> ParseDonorUpdates(Stream stream)
        {
            using var streamReader = new StreamReader(stream);
            using var reader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();

            var readerIsInDonorsProperty = false;
            var readerIsInDonorUpdateArray = false;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName when reader.Value.ToString() == "donors":
                        readerIsInDonorsProperty = true;
                        break;
                    case JsonToken.StartArray when readerIsInDonorsProperty:
                        readerIsInDonorUpdateArray = true;
                        break;
                    case JsonToken.StartObject when readerIsInDonorUpdateArray:
                        var donorOperation = serializer.Deserialize<DonorUpdate>(reader);
                        yield return donorOperation;
                        break;
                    case JsonToken.EndArray when readerIsInDonorUpdateArray:
                        readerIsInDonorUpdateArray = false;
                        break;
                    case JsonToken.EndObject when !readerIsInDonorUpdateArray && readerIsInDonorsProperty:
                        readerIsInDonorsProperty = false;
                        break;
                }
            }
        }
    }
}