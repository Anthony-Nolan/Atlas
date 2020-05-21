using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.Common.Notifications.MessageModels;
using Atlas.DonorImport.Models.FileSchema;
using Newtonsoft.Json;

// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Atlas.DonorImport.Services
{
    public interface IDonorFileImporter
    {
        Task ImportDonorFile(Stream fileStream, string fileName);
    }

    public class DonorFileImporter : IDonorFileImporter
    {
        private readonly IDonorOperationApplier donorOperationApplier;
        private readonly INotificationsClient notificationsClient;
        private readonly int batchSize;
        private const UpdateMode DefaultUpdateMode = UpdateMode.Differential;

        public DonorFileImporter(
            IDonorOperationApplier donorOperationApplier,
            INotificationsClient notificationsClient,
            int batchSize = 10000)
        {
            this.donorOperationApplier = donorOperationApplier;
            this.notificationsClient = notificationsClient;
            this.batchSize = batchSize;
        }

        public async Task ImportDonorFile(Stream fileStream, string fileName)
        {
            try
            {
                using var streamReader = new StreamReader(fileStream);
                using var reader = new JsonTextReader(streamReader);
                var serializer = new JsonSerializer();
                var updateMode = DefaultUpdateMode;
                var donorBatch = new List<DonorUpdate>();
                var inDonorsProperty = false;
                var inDonorUpdateArray = false;
                while (await reader.ReadAsync())
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.PropertyName when reader.Value?.ToString() == "updateMode":
                            await reader.ReadAsync();
                            updateMode = serializer.Deserialize<UpdateMode>(reader);
                            break;
                        case JsonToken.PropertyName when reader.Value?.ToString() == "donors":
                            inDonorsProperty = true;
                            break;
                        case JsonToken.StartArray when inDonorsProperty:
                            inDonorUpdateArray = true;
                            break;
                        case JsonToken.StartObject when inDonorUpdateArray:
                            var donorOperation = serializer.Deserialize<DonorUpdate>(reader);
                            donorBatch.Add(donorOperation);
                            if (donorBatch.Count >= batchSize)
                            {
                                await donorOperationApplier.ApplyDonorOperationBatch(updateMode, donorBatch);
                                donorBatch = new List<DonorUpdate>();
                            }

                            break;
                        case JsonToken.EndArray when inDonorUpdateArray:
                            await donorOperationApplier.ApplyDonorOperationBatch(updateMode, donorBatch);
                            inDonorUpdateArray = false;
                            break;
                        case JsonToken.EndObject when !inDonorUpdateArray && inDonorsProperty:
                            inDonorsProperty = false;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                var summary = $"Donor Import Failed: {fileName}";
                var description = $"Importing donors for file: {fileName} has failed. With exception {e.Message}. If there were more than " +
                                  $"{batchSize} donor updates in the file, the file may have been partially imported - manual investigation is " +
                                  $"recommended. See Application Insights for more information.";
                var alert = new Alert(summary, description, Priority.Medium);
                
                await notificationsClient.SendAlert(alert);
                
                throw;
            }
        }
    }
}