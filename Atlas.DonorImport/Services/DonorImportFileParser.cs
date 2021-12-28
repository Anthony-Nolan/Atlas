using System.Collections.Generic;
using System.IO;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.Models.FileSchema;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorImportFileParser
    {
        /// <returns>A batch of parsed donor updates</returns>
        public LazilyParsingDonorFile PrepareToLazilyParseDonorUpdates(Stream stream);
    }

    internal class DonorImportFileParser : IDonorImportFileParser
    {
        public LazilyParsingDonorFile PrepareToLazilyParseDonorUpdates(Stream stream)
        {
            return new LazilyParsingDonorFile(stream);
        }
    }

    internal class LazilyParsingDonorFile
    {
        public LazilyParsingDonorFile(Stream stream)
        {
            underlyingDataStream = stream;
        }
        private readonly Stream underlyingDataStream;

        public int ParsedDonorCount { get; private set; }
        public string LastSuccessfullyParsedDonorCode { get; private set; }

        public IEnumerable<DonorUpdate> ReadLazyDonorUpdates()
        {
            if (underlyingDataStream == null)
            {
                throw new EmptyDonorFileException();
            }
            
            using (var streamReader = new StreamReader(underlyingDataStream))
            using (var reader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                UpdateMode? updateMode = null;
                // Loops through top level JSON
                while (TryRead(reader))
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var propertyName = reader.Value?.ToString();
                        switch (propertyName)
                        {
                            case nameof(DonorImportFileSchema.updateMode):
                                // Read into property
                                TryRead(reader);
                                try
                                {
                                    updateMode = serializer.Deserialize<UpdateMode>(reader);
                                }
                                catch (JsonSerializationException e)
                                {
                                    throw new DonorFormatException(e);
                                }
                                break;
                            case nameof(DonorImportFileSchema.donors):
                                TryRead(reader); // Read into property
                                TryRead(reader); // Read into array. Do not deserialize to collection, as the collection can be very large and requires streaming.
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
                                        donorOperation = serializer.Deserialize<DonorUpdate>(reader);
                                    }
                                    catch (JsonSerializationException e)
                                    {
                                        throw new DonorFormatException(e);
                                    }
                                    
                                    if (donorOperation == null)
                                    {
                                        throw new MalformedDonorFileException("Donor in array of input file could not be parsed.");
                                    }

                                    if (updateMode.Value == UpdateMode.Full && donorOperation.ChangeType != ImportDonorChangeType.Create)
                                    {
                                        throw new MalformedDonorFileException($"File Update mode is '{UpdateMode.Full}, but a record in the file is of ChangeType '{donorOperation.ChangeType}'. Only 'New' records / Creations are permitted in a '{UpdateMode.Full}' file.");
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
                                } while (TryRead(reader) && reader.TokenType != JsonToken.EndArray);

                                break;
                            default:
                                throw new MalformedDonorFileException($"Unrecognised property: {propertyName} encountered in donor import file.");
                        }
                    }
                }
            }
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
    }
}