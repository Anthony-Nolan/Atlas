using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace Atlas.HlaMetadataDictionary.Exceptions
{
    public class CloudTableBatchInsertException : Exception
    {
        public CloudTableBatchInsertException(IEnumerable<TableEntity> tableEntities, Exception inner)
            : base(GetErrorMessage(tableEntities), inner)
        {
        }

        private static string GetErrorMessage(IEnumerable<TableEntity> entities)
        {
            var entitiesAsStrings = entities.Select((entity, i) => $"{i}: {entity.PartitionKey}, {entity.RowKey}");
            return $"Failed to insert batch: [{string.Join(";", entitiesAsStrings)}]";
        }
    }
}