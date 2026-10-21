using System.Threading;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Azure.Data.Tables;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;

/// <summary>
/// The one, process-wide client for the MAC table.
/// </summary>
/// <remarks>
/// <see cref="MacRepository"/> is scoped, and match prediction's parallel path opens a scope per donor. Creating the client
/// (and checking that the table exists) in the repository's constructor therefore cost one blocking Table Storage round trip
/// per donor - about 41k of them in one ATL-474 test run. A <see cref="TableClient"/> is thread-safe and meant to be reused,
/// so it is built once here, and the table is checked once, asynchronously, on first use.
/// </remarks>
internal interface IMacTableClientProvider
{
    /// <summary>
    /// The MAC table client, once the table is known to exist. Safe to call from many threads; only the first call checks
    /// the table. A failed check is not remembered, so the next call tries again.
    /// </summary>
    Task<TableClient> GetTable();
}

/// <inheritdoc />
internal sealed class MacTableClientProvider : IMacTableClientProvider
{
    private readonly TableClient table;
    private readonly SemaphoreSlim createGate = new(1, 1);
    private volatile bool tableExists;

    public MacTableClientProvider(MacDictionarySettings macDictionarySettings)
    {
        //TODO: ATLAS-485. Combine this with the CloudTableFactory in HMD.
        table = new TableClient(macDictionarySettings.AzureStorageConnectionString, macDictionarySettings.TableName);
    }

    /// <inheritdoc />
    public async Task<TableClient> GetTable()
    {
        if (tableExists)
        {
            return table;
        }

        await createGate.WaitAsync();
        try
        {
            if (!tableExists)
            {
                //TODO: ATLAS-512. Is there any mileage in using the "Lazy" Indexing Policy? (Apparently requires "Gateway" mode on the table?)
                await table.CreateIfNotExistsAsync();
                tableExists = true;
            }

            return table;
        }
        finally
        {
            createGate.Release();
        }
    }
}
