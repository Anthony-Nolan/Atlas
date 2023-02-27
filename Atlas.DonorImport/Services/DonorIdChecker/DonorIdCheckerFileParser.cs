using System;
using System.Collections.Generic;
using System.IO;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services.DonorIdChecker
{
    internal interface IDonorIdCheckerFileParser
    {
        public ILazilyParsingDonorIdFile PrepareToLazilyParsingDonorIdFile(Stream stream);
    }

    internal class DonorIdCheckerFileParser : IDonorIdCheckerFileParser
    {
        public ILazilyParsingDonorIdFile PrepareToLazilyParsingDonorIdFile(Stream stream) => new LazilyParsingDonorIdFile(stream);
    }

    internal interface ILazilyParsingDonorIdFile
    {
        IEnumerable<string> ReadLazyDonorIds();
    }

    internal class LazilyParsingDonorIdFile : ILazilyParsingDonorIdFile
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

                    if (propertyName != nameof(DonorIdCheckerRequest.recordIds))
                    {
                        throw new MalformedDonorFileException("recordIds property must be the first property provided in donor id JSON file.");
                    }

                    TryRead(reader);

                    while (TryRead(reader) &&  reader.TokenType != JsonToken.EndArray)
                    {
                        var donorId = reader.Value?.ToString();

                        yield return donorId;
                    }
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
