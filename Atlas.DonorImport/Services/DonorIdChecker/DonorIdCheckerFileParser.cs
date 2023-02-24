using System;
using System.Collections.Generic;
using System.IO;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services.DonorIdChecker
{
    public interface IDonorIdCheckerFileParser
    {
        public LazilyParsingDonorIdFile PrepareToLazilyParsingDonorIdFile(Stream stream);
    }

    public class DonorIdCheckerFileParser : IDonorIdCheckerFileParser
    {
        public LazilyParsingDonorIdFile PrepareToLazilyParsingDonorIdFile(Stream stream) => new(stream);
    }

    public class LazilyParsingDonorIdFile
    {
        private readonly Stream underlyingDataStream;

        public LazilyParsingDonorIdFile(Stream stream)
        {
            underlyingDataStream = stream;
        }

        public IEnumerable<string> ReadLazyDonorIds()
        {
            if (underlyingDataStream == null)
            {
                throw new EmptyDonorFileException();
            }

            using (var streamReader = new StreamReader(underlyingDataStream))
            using (var reader = new JsonTextReader(streamReader))
            {
                while (TryRead(reader))
                {
                    if (reader.TokenType != JsonToken.PropertyName)
                    {
                        continue;
                    }

                    var propertyName = reader.Value?.ToString();

                    if (propertyName != nameof(DonorIdCheckerRequest.RecordIds))
                    {
                        throw new MalformedDonorFileException("RecordIds property must be the first property provided in donor id JSON file.");
                    }

                    TryRead(reader); // Read into property
                    TryRead(reader); // Read into array

                    do
                    {
                        var donorId = reader.Value.ToString();

                        yield return donorId;
                    } while (TryRead(reader) && reader.TokenType != JsonToken.EndArray);
                }
            }
        }

        private static bool TryRead(JsonReader reader)
        {
            try
            {
                return reader.Read();
            }
            catch (JsonException)
            {
                throw new MalformedDonorFileException("Invalid JSON was encountered");
            }
        }
    }
}
