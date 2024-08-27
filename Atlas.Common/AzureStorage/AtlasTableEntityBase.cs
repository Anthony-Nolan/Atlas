using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Common.AzureStorage
{
    public class AtlasTableEntityBase : ITableEntity
    {
        public string PartitionKey { set; get; }
        public string RowKey { set; get; }
        public DateTimeOffset? Timestamp { set; get; }
        public ETag ETag { set; get; }
    }
}
