using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Atlas.RepeatSearch.Services.ResultSetTracking
{
    /// <summary>
    /// Reads the donor codes of a serialised result set as a stream, without deserialising the result set:
    /// only one token of the file is held in memory at a time, whatever the number of results.
    /// </summary>
    internal static class DonorCodeStreamReader
    {
        private const string ResultsPropertyName = "Results";
        private const string DonorCodePropertyName = "DonorCode";

        /// <returns>The <c>DonorCode</c> of each item of the top-level <c>Results</c> array, in file order.</returns>
        public static async IAsyncEnumerable<string> ReadDonorCodes(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var streamReader = new StreamReader(stream);
            using var reader = new JsonTextReader(streamReader)
            {
                // Values that are skipped are not parsed as dates.
                DateParseHandling = DateParseHandling.None
            };

            if (!await reader.ReadAsync(cancellationToken) || reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonReaderException("The result set file does not start with a JSON object.");
            }

            while (await reader.ReadAsync(cancellationToken) && reader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = (string)reader.Value;
                await reader.ReadAsync(cancellationToken);

                if (!IsProperty(propertyName, ResultsPropertyName))
                {
                    await reader.SkipAsync(cancellationToken);
                    continue;
                }

                if (reader.TokenType != JsonToken.StartArray)
                {
                    // No results, for example "Results": null.
                    yield break;
                }

                while (await reader.ReadAsync(cancellationToken) && reader.TokenType != JsonToken.EndArray)
                {
                    if (reader.TokenType != JsonToken.StartObject)
                    {
                        await reader.SkipAsync(cancellationToken);
                        continue;
                    }

                    var donorCode = await ReadDonorCodeOfResult(reader, cancellationToken);
                    if (donorCode != null)
                    {
                        yield return donorCode;
                    }
                }

                // The rest of the file is not needed.
                yield break;
            }
        }

        /// <summary>
        /// Reads one result object, from its start to its end, and returns its donor code.
        /// </summary>
        private static async Task<string> ReadDonorCodeOfResult(JsonReader reader, CancellationToken cancellationToken)
        {
            string donorCode = null;

            while (await reader.ReadAsync(cancellationToken) && reader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = (string)reader.Value;
                await reader.ReadAsync(cancellationToken);

                if (IsProperty(propertyName, DonorCodePropertyName) && reader.TokenType == JsonToken.String)
                {
                    donorCode = (string)reader.Value;
                }
                else
                {
                    await reader.SkipAsync(cancellationToken);
                }
            }

            return donorCode;
        }

        // Json.NET matches property names without case when it deserialises, so this does the same.
        private static bool IsProperty(string propertyName, string expectedName) =>
            string.Equals(propertyName, expectedName, StringComparison.OrdinalIgnoreCase);
    }
}
