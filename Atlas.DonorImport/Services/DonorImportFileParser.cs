using System;
using System.Collections.Generic;
using System.IO;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.FileSchema.Models;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorImportFileParser
    {
        /// <returns>A batch of parsed donor updates</returns>
        public ILazilyParsingDonorFile PrepareToLazilyParseDonorUpdates(Stream stream);
    }

    internal class DonorImportFileParser : IDonorImportFileParser
    {
        public ILazilyParsingDonorFile PrepareToLazilyParseDonorUpdates(Stream stream)
        {
            return new LazilyParsingDonorFile(stream);
        }
    }

    internal interface ILazilyParsingDonorFile : IDisposable
    {
        public int ParsedDonorCount { get; }
        public string LastSuccessfullyParsedDonorCode { get; }

        IEnumerable<DonorUpdate> ReadLazyDonorUpdates();
        /// <summary>
        /// Reads and returns update mode from stream. Can be called only before <see cref="ReadUpdateMode"/>.
        /// </summary>
        /// <returns></returns>
        UpdateMode ReadUpdateMode();
    }

    internal class LazilyParsingDonorFile : ILazilyParsingDonorFile, IDisposable
    {
        private readonly Stream underlyingDataStream;
        private readonly JsonSerializer serializer;

        private StreamReader streamReader;
        private JsonTextReader jsonReader;
        private UpdateMode? updateMode;

        public LazilyParsingDonorFile(Stream underlyingDataStream)
        {
            this.underlyingDataStream = underlyingDataStream;
            this.serializer = new JsonSerializer();

        }

        public int ParsedDonorCount { get; private set; }
        public string LastSuccessfullyParsedDonorCode { get; private set; }

        public UpdateMode ReadUpdateMode()
        {
            if (updateMode != null)
                throw new InvalidOperationException("Update mode was already read from input stream.");

            EnsureReadersCreated();

            // Loops through top level JSON
            while (TryRead(jsonReader))
            {
                if (jsonReader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = jsonReader.Value?.ToString();

                    switch (propertyName)
                    {
                        case nameof(DonorImportFileSchema.updateMode):
                            // Read into property
                            TryRead(jsonReader);
                            try
                            {
                                updateMode = serializer.Deserialize<UpdateMode>(jsonReader);
                                return updateMode.Value;
                            }
                            catch (JsonSerializationException e)
                            {
                                throw new DonorFormatException(e);
                            }

                        case nameof(DonorImportFileSchema.donors):
                            throw new MalformedDonorFileException("Update Mode must be provided before donor list in donor import JSON file.");

                        default:
                            throw new MalformedDonorFileException($"Unrecognised property: {propertyName} encountered in donor import file.");
                    }
                }
            }

            throw new MalformedDonorFileException("Update Mode must be provided in donor import JSON file.");
        }

        public IEnumerable<DonorUpdate> ReadLazyDonorUpdates()
        {
            EnsureReadersCreated();

            if (updateMode == null)
                ReadUpdateMode();

            // Continue loop through top level JSON
            while (TryRead(jsonReader))
            {
                if (jsonReader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = jsonReader.Value?.ToString();
                    switch (propertyName)
                    {
                        case nameof(DonorImportFileSchema.updateMode):
                            throw new MalformedDonorFileException("Update Mode must be provided just once in donor import JSON file.");

                        case nameof(DonorImportFileSchema.donors):
                            TryRead(jsonReader); // Read into property
                            TryRead(jsonReader); // Read into array. Do not deserialize to collection, as the collection can be very large and requires streaming.
                            // Loops through all donors in array
                            do
                            {
                                if (!updateMode.HasValue)
                                {
                                    throw new MalformedDonorFileException("Update Mode must be provided before donor list in donor import JSON file.");
                                }

                                DonorUpdate donorOperation = null;
                                try
                                {
                                    donorOperation = serializer.Deserialize<DonorUpdate>(jsonReader);
                                }
                                catch (JsonSerializationException e)
                                {
                                    throw new DonorFormatException(e);
                                }
                                    
                                if (donorOperation == null)
                                {
                                    throw new MalformedDonorFileException("Donor in array of input file could not be parsed.");
                                }

                                if (updateMode.Value == UpdateMode.Full &&
                                    donorOperation.ChangeType != ImportDonorChangeType.Create &&
                                    donorOperation.ChangeType != ImportDonorChangeType.Upsert)
                                {
                                    throw new MalformedDonorFileException(
                                        $"File Update mode is '{UpdateMode.Full}, but a record in the file is of ChangeType '{donorOperation.ChangeType}'. Only 'New'/'Upsert' records / Creations are permitted in a '{UpdateMode.Full}' file.");
                                }

                                donorOperation.UpdateMode = updateMode.Value;

                                //Record the successful parsing for diagnostics if subsequent records fail, before returning it to the caller.
                                ParsedDonorCount++;
                                LastSuccessfullyParsedDonorCode = donorOperation.RecordId;
                                if (donorOperation.Hla == null && donorOperation.ChangeType != ImportDonorChangeType.Delete)
                                {
                                    throw new MalformedDonorFileException("Donor property HLA cannot be null.");
                                }
                                if (donorOperation.RecordId == null)
                                {
                                    throw new MalformedDonorFileException("Donor property RecordId cannot be null.");
                                }
                                yield return donorOperation;
                            } while (TryRead(jsonReader) && jsonReader.TokenType != JsonToken.EndArray);

                            break;
                        default:
                            throw new MalformedDonorFileException($"Unrecognised property: {propertyName} encountered in donor import file.");
                    }
                }
            }
        }
        
        private void EnsureReadersCreated()
        {
            if (jsonReader != null)
            {
                return;
            }

            if (underlyingDataStream == null)
            {
                throw new EmptyDonorFileException();
            }

            this.streamReader = new StreamReader(underlyingDataStream);
            this.jsonReader = new JsonTextReader(streamReader);
        }

        private static bool TryRead(JsonReader reader){
            try
            {
                return reader.Read();
            }
            catch (JsonException)
            {
                throw new MalformedDonorFileException("Invalid JSON was encountered");
            }
        }

        public void Dispose()
        {
            ((IDisposable)jsonReader)?.Dispose();
            streamReader?.Dispose();
        }
    }
}