using System.Collections.Generic;
using System.IO;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services.DonorChecker
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
        string DonorPool { get; }
        ImportDonorType DonorType { get; }
    }

    internal class LazilyParsingDonorIdFile : ILazilyParsingDonorIdFile
    {
        private readonly Stream underlyingDataStream;
        private ImportDonorType? convertedDonorType;

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

                    switch (propertyName)
                    {
                        case nameof(DonorIdCheckerRequest.donPool):
                            TryRead(reader);
                            DonorPool = reader.Value?.ToString();
                            break;
                        case nameof(DonorIdCheckerRequest.donorType):
                            TryRead(reader);
                            try
                            {
                                convertedDonorType = new JsonSerializer().Deserialize<ImportDonorType>(reader);
                            }
                            catch (JsonSerializationException e)
                            {
                                throw new MalformedDonorFileException($"Error parsing {nameof(DonorIdCheckerRequest.donorType)}.");
                            }
                            break;
                        case nameof(DonorIdCheckerRequest.donors):
                            if (string.IsNullOrEmpty(DonorPool))
                            {
                                throw new MalformedDonorFileException($"{nameof(DonorIdCheckerRequest.donPool)} property must be defined before list of donors and cannot be null.");
                            }

                            if (!convertedDonorType.HasValue)
                            {
                                throw new MalformedDonorFileException($"{nameof(DonorIdCheckerRequest.donorType)} property must be defined before list of donors and cannot be null.");
                            }

                            TryRead(reader);
                            while (TryRead(reader) && reader.TokenType != JsonToken.EndArray)
                            {
                                var donorId = reader.Value?.ToString();

                                yield return donorId;
                            }
                            break;
                        default:
                            throw new MalformedDonorFileException($"Unexpected property '{propertyName}' in the file.");
                    }
                }
            }
        }
        
        public string DonorPool { get; private set; }

        public ImportDonorType DonorType => convertedDonorType.GetValueOrDefault();

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
