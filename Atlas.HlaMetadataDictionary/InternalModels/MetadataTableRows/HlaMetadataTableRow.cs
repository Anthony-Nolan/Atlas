using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.Services.AzureStorage;
using Azure;
using Azure.Data.Tables;
using EnumStringValues;
using MoreLinq;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows
{
    /// <summary>
    /// Any new properties added to this table entity class will need to be manually added to the <see cref="WriteEntity"/> and <see cref="ReadEntity"/>
    /// methods used for writing and reading to/from Azure tables. 
    /// </summary>
    internal class HlaMetadataTableRow
    {
        private const int MaximumRowSize = 32_000;
        // Used to track whether serialised data has to be split across multiple columns.
        private const string IsSerialisedDataSplitProperty = "IsSerialisedDataSplit";
        public string LocusAsString { get; set; }
        public Locus Locus => LocusAsString.ParseToEnum<Locus>();
        public string TypingMethodAsString { get; set; }
        public TypingMethod TypingMethod => TypingMethodAsString.ParseToEnum<TypingMethod>();
        public string LookupName { get; set; }
        public string SerialisedHlaInfoType { get; set; }
        public string SerialisedHlaInfo { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset?Timestamp { get; set; }
        public ETag ETag { get; set; }

        public HlaMetadataTableRow()
        {
        }

        public HlaMetadataTableRow(ISerialisableHlaMetadata metadata)
        {
            PartitionKey = HlaMetadataTableKeyManager.GetPartitionKey(metadata.Locus);
            RowKey = HlaMetadataTableKeyManager.GetRowKey(metadata.LookupName, metadata.TypingMethod);

            LocusAsString = metadata.Locus.ToString();
            TypingMethodAsString = metadata.TypingMethod.ToString();
            LookupName = metadata.LookupName;
            SerialisedHlaInfoType = metadata.SerialisedHlaInfoType;
            SerialisedHlaInfo = SerialiseHlaInfo(metadata.HlaInfoToSerialise);
        }

        private static string SerialiseHlaInfo(object hlaInfo)
        {
            return JsonConvert.SerializeObject(hlaInfo);
        }

        public T GetHlaInfo<T>()
        {
            // Alas "nameof(T)", which would be compile-time constant, and thus compatible with a switch, doesn't give values that always match with typeof(T).Name
            // So we have to calculate these ourselves. `.Name` doesn't qualify Generic Types, and `.FullName` is incredibly verbose for Generic Types, hence using this helper.
            var typeName = typeof(T).GetNeatCSharpName();
            if (typeName != SerialisedHlaInfoType
                // If the serialised value is null, no type will be stored. In this case we do not want to prevent deserialisation to a null
                && SerialisedHlaInfoType != null
            )
            {
                throw new InvalidOperationException(
                    $"Expected to find '{typeName}' data to be deserialised. But actually the data is labeled as '{SerialisedHlaInfoType}'. Unable to proceed.");
            }

            return JsonConvert.DeserializeObject<T>(SerialisedHlaInfo);
        }
        
        public virtual void ReadEntity(TableEntity source)
        {
            LocusAsString = source.GetString(nameof(LocusAsString));
            TypingMethodAsString = source.GetString(nameof(TypingMethodAsString));
            LookupName = source.GetString(nameof(LookupName));
            SerialisedHlaInfoType = source.GetString(nameof(SerialisedHlaInfoType));

            // If we know that a row has not had its serialised data split, do not waste time with string comparisons of properties 
            if (source.GetBoolean(IsSerialisedDataSplitProperty) ?? false)
            {
                var sb = new StringBuilder();
                foreach (var splitSerialisedPropertyKey in source
                    .Keys
                    .Where(p => p.StartsWith($"{nameof(SerialisedHlaInfo)}"))
                    .Where(p => p != nameof(SerialisedHlaInfoType))
                    .OrderBy(p => p.Split(nameof(SerialisedHlaInfo))[1]))
                {
                    sb.Append(source.GetString(splitSerialisedPropertyKey));
                }

                SerialisedHlaInfo = sb.ToString();
            }
            else
            {
                SerialisedHlaInfo = source.GetString(nameof(SerialisedHlaInfo));
            }
        }

        public virtual void WriteEntity(TableEntity destination)
        {
            destination.RowKey = RowKey;
            destination.PartitionKey = PartitionKey;
            
            destination[nameof(LocusAsString)] = LocusAsString;
            destination[nameof(TypingMethodAsString)] = this.TypingMethodAsString;
            destination[nameof(LookupName)] = LookupName;
            destination[nameof(SerialisedHlaInfoType)] = SerialisedHlaInfoType;


            var splitSerialisedInfo = SerialisedHlaInfo
                .Batch(MaximumRowSize)
                .Select(characterBatch => new string(characterBatch.ToArray()))
                .ToList();
            var numberOfSubstrings = splitSerialisedInfo.Count;
            
            if (numberOfSubstrings > 1)
            {
                destination[IsSerialisedDataSplitProperty] = true;
            }
            
            // This code exists to work around the 32K character limit per string stored in Azure Table Storage:
            // If the serialised hla info would exceed this character limit (e.g. A:02:01 scoring info), instead we add additional characters to extra properties. 
            // These additional properties are combined again on read, so any consumers of this class in code still see the serialised data as a single string.
            for (var i = 0; i < numberOfSubstrings; i++)
            {
                var subString = splitSerialisedInfo[i];
                var subStringPropertyName = i == 0 ? nameof(SerialisedHlaInfo) : nameof(SerialisedHlaInfo) + i;
                destination[subStringPropertyName] = subString;
            }
        }

        public TableEntity ToTableEntity()
        {
            var entity = new TableEntity();
            WriteEntity(entity);
            return entity;
        }
    }
}