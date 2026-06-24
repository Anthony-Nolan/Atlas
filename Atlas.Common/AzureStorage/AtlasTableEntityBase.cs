using Azure;
using Azure.Data.Tables;
using System;

namespace Atlas.Common.AzureStorage;

public class AtlasTableEntityBase : ITableEntity
{
    public string PartitionKey { set; get; }
    public string RowKey { set; get; }
    public DateTimeOffset? Timestamp { set; get; }
    public ETag ETag { set; get; }
}