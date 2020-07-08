using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Utils.Extensions;
using CosmosEntity = Microsoft.Azure.Cosmos.Table.TableEntity;
using WindowsAzureEntity = Microsoft.WindowsAzure.Storage.Table.TableEntity;

namespace Atlas.Common.AzureStorage.TableStorage
{
    internal class AzureTableBatchInsertException : Exception
    {
        public AzureTableBatchInsertException(IEnumerable<CosmosEntity> tableEntities, Exception inner)
            : base(GetErrorMessage(tableEntities), inner)
        { }

        public AzureTableBatchInsertException(IEnumerable<WindowsAzureEntity> tableEntities, Exception inner)
            : base(GetErrorMessage(tableEntities), inner)
        { }

        private static string GetErrorMessage(IEnumerable<CosmosEntity> entities) => GetErrorMessage(entities, e => e.PartitionKey, e => e.RowKey);
        private static string GetErrorMessage(IEnumerable<WindowsAzureEntity> entities) => GetErrorMessage(entities, e => e.PartitionKey, e => e.RowKey);

        private static string GetErrorMessage<T>(IEnumerable<T> entities, Func<T, string> partitionKeyFor, Func<T, string> rowKeyFor)
        {
            var entitiesAsStrings = entities.Select((entity, i) => $"{i}: {partitionKeyFor(entity)}, {rowKeyFor(entity)}");
            return $"Failed to insert batch: [{entitiesAsStrings.StringJoin(";")}]";
        }
    }
}