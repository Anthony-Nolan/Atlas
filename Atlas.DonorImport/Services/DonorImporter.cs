using System.IO;
using Atlas.DonorImport.Models.FileSchema;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services
{
    public interface IDonorImporter
    {
        void ImportDonorFile(Stream fileStream);
    }

    public class DonorImporter : IDonorImporter
    {
        public void ImportDonorFile(Stream fileStream)
        {
            using var streamReader = new StreamReader(fileStream);
            using var reader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();


            var inDonorsProperty = false;
            var inDonorUpdateArray = false;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName when reader.Value.ToString() == "donors":
                        inDonorsProperty = true;
                        break;
                    case JsonToken.StartArray when inDonorsProperty:
                        inDonorUpdateArray = true;
                        break;
                    case JsonToken.StartObject when inDonorUpdateArray:
                        var donorOperation = serializer.Deserialize<DonorUpdate>(reader);
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