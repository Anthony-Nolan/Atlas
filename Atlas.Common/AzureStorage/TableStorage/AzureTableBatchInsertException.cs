﻿using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Utils.Extensions;
using Azure.Data.Tables;

namespace Atlas.Common.AzureStorage.TableStorage
{
    public class AzureTableBatchInsertException : Exception
    {
        public AzureTableBatchInsertException(IEnumerable<ITableEntity> tableEntities, Exception inner)
            : base(GetErrorMessage(tableEntities), inner)
        { }

        private static string GetErrorMessage(IEnumerable<ITableEntity> entities) => GetErrorMessage(entities, e => e.PartitionKey, e => e.RowKey);

        private static string GetErrorMessage<T>(IEnumerable<T> entities, Func<T, string> partitionKeyFor, Func<T, string> rowKeyFor)
        {
            var entitiesAsStrings = entities.Select((entity, i) => $"{i}: {partitionKeyFor(entity)}, {rowKeyFor(entity)}");
            return $"Failed to insert batch: [{entitiesAsStrings.StringJoin(";")}]";
        }
    }
}