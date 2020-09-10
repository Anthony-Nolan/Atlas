using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.Services.AzureStorage;
using EnumStringValues;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows
{
    internal class HlaMetadataTableRow : TableEntity
    {
        public string LocusAsString { get; set; }
        public Locus Locus => LocusAsString.ParseToEnum<Locus>();
        public string TypingMethodAsString { get; set; }
        public TypingMethod TypingMethod => TypingMethodAsString.ParseToEnum<TypingMethod>();
        public string LookupName { get; set; }
        public string SerialisedHlaInfoType { get; set; }
        public string SerialisedHlaInfo { get; set; }

        public HlaMetadataTableRow()
        {
        }

        public HlaMetadataTableRow(ISerialisableHlaMetadata metadata)
            : base(HlaMetadataTableKeyManager.GetPartitionKey(metadata.Locus),
                HlaMetadataTableKeyManager.GetRowKey(metadata.LookupName, metadata.TypingMethod))
        {
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
    }
}