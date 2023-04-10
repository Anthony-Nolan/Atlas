using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Atlas.DonorImport.Models;
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
        (string registryCode, DatabaseDonorType donorType) ReadRegistryCodeAndDonorType();
        IEnumerable<string> ReadLazyDonorIds();
    }

    internal class LazilyParsingDonorIdFile : ILazilyParsingDonorIdFile
    {
        private readonly Stream underlyingDataStream;

        public LazilyParsingDonorIdFile(Stream stream)
        {
            underlyingDataStream = stream;
        }

        public (string registryCode, DatabaseDonorType donorType) ReadRegistryCodeAndDonorType()
        {
            if (underlyingDataStream == null)
            {
                throw new EmptyDonorFileException();
            }

            var registryCode = string.Empty;
            ImportDonorType? donorType = default;

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

                    switch (propertyName)
                    {
                        case nameof(DonorIdCheckerRequest.donPool):
                            TryRead(reader);
                            registryCode = reader.Value?.ToString();
                            break;
                        case nameof(DonorIdCheckerRequest.donorType):
                            TryRead(reader);
                            try
                            {
                                donorType = new JsonSerializer().Deserialize<ImportDonorType>(reader);
                            }
                            catch (JsonSerializationException  e)
                            {
                                throw new MalformedDonorFileException($"Error parsing {nameof(DonorIdCheckerRequest.donorType)}.");
                            }
                            break;
                        default:
                            throw new MalformedDonorFileException($"{nameof(DonorIdCheckerRequest.donPool)} and {nameof(DonorIdCheckerRequest.donorType)} must be first properties in the file.");
                    }

                    if (!string.IsNullOrEmpty(registryCode) && donorType.HasValue)
                    {
                        return (registryCode, donorType.Value.ToDatabaseType());
                    }
                }
            }

            if (string.IsNullOrEmpty(registryCode))
            {
                throw new MalformedDonorFileException($"{nameof(DonorIdCheckerRequest.donPool)} cannot be null.");
            }

            if (!donorType.HasValue)
            {
                throw new MalformedDonorFileException($"{nameof(DonorIdCheckerRequest.donorType)} cannot be null.");
            }

            return (registryCode, donorType.Value.ToDatabaseType());
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
                    
                    if (propertyName != nameof(DonorIdCheckerRequest.donors))
                    {
                        continue;
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
