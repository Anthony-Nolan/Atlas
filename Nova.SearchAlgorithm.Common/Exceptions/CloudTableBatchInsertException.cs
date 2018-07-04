using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace Nova.SearchAlgorithm.Common.Exceptions
{
    public class CloudTableBatchInsertException : Exception
    {
        public CloudTableBatchInsertException(IEnumerable<TableEntity> tableEntities, Exception inner)
            : base(TableEntitiesToString(tableEntities), inner)
        {
        }

        private static string TableEntitiesToString(IEnumerable<TableEntity> entities)
        {
            var entitiesCollection = entities.ToList();
            var message = "Failed to insert batch: [";

            for (var i = 0; i < entitiesCollection.Count; i++)
            {
                var entity = entitiesCollection[i];
                message += $"{i}: {entity.PartitionKey}, {entity.RowKey}; ";
            }

            return message + "]";
        }
    }
}