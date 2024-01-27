Changes in Atlas.Common.Public.Models:
 delete mode 100644 .github/workflows/automate-rc-admin-tasks.yml
 delete mode 100644 Atlas.Client.Models/changelogs/CHANGELOG_Atlas.Client.Models-v1.2.3.md
 delete mode 100644 Atlas.Common/Debugging/DebugDonorsHelper.cs
 create mode 100644 Atlas.Common/Debugging/Donors/DebugDonorsHelper.cs
 rename {Atlas.Debug.Client.Models/DonorImport => Atlas.Common/Debugging/Donors}/DebugDonorsResult.cs (72%)
 rename {Atlas.Client.Models/SupportMessages => Atlas.Common/Notifications/MessageModels}/Alert.cs (84%)
 rename {Atlas.Client.Models/SupportMessages => Atlas.Common/Notifications/MessageModels}/BaseNotificationsMessage.cs (90%)
 rename {Atlas.Client.Models/SupportMessages => Atlas.Common/Notifications/MessageModels}/Notification.cs (80%)
 rename {Atlas.Client.Models/SupportMessages => Atlas.Common/Notifications}/Priority.cs (65%)
 rename Atlas.Common/ServiceBus/{ => BatchReceiving}/MessageReceiverFactory.cs (96%)
 rename {Atlas.Common.Public.Models/ServiceBus => Atlas.Common/ServiceBus/Models}/ServiceBusMessage.cs (56%)
 delete mode 100644 Atlas.Common/ServiceBus/ServiceBusPeeker.cs
 delete mode 100644 Atlas.Debug.Client.Models/Atlas.Debug.Client.Models.csproj
 delete mode 100644 Atlas.Debug.Client.Models/CHANGELOG_DebugClientModels.md
 delete mode 100644 Atlas.Debug.Client.Models/DonorImport/DonorImportFileContents.cs
 delete mode 100644 Atlas.Debug.Client.Models/DonorImport/DonorImportRequest.cs
 delete mode 100644 Atlas.Debug.Client.Models/DonorImport/DonorUpdateFailureInfo.cs
 delete mode 100644 Atlas.Debug.Client.Models/ServiceBus/PeekServiceBusMessagesRequest.cs
 delete mode 100644 Atlas.Debug.Client.Models/ServiceBus/PeekedServiceBusMessage.cs
 delete mode 100644 Atlas.DonorImport.Functions/Functions/Debug/DonorImportFunctions.cs
 delete mode 100644 Atlas.DonorImport.Functions/Functions/Debug/DonorUpdateFunctions.cs
 delete mode 100644 Atlas.DonorImport.Functions/Models/Debug/DonorImportFailureExtensions.cs
 delete mode 100644 Atlas.DonorImport.Functions/Models/Debug/DonorRequest.cs
 delete mode 100644 Atlas.DonorImport.Test.Integration/IntegrationTests/Import/FullModeDonorImportTests.cs
 delete mode 100644 Atlas.DonorImport.Test.Integration/TestHelpers/DonorImportFailuresInspectionRepository.cs
 rename Atlas.DonorImport.Test/{Services => }/DonorImportMessageSenderTests.cs (92%)
 delete mode 100644 Atlas.DonorImport.Test/Services/DonorFileImporterTests.cs
 delete mode 100644 Atlas.DonorImport.Test/Services/DonorUpdates/DonorUpdatesSaveTests.cs
 rename Atlas.DonorImport/ExternalInterface/Settings/{DonorImportSettings.cs => StalledFileSettings.cs} (64%)
 delete mode 100644 Atlas.DonorImport/Helpers/SearchableDonorUpdateMapper.cs
 delete mode 100644 Atlas.DonorImport/Services/Debug/DonorImportBlobStorageClient.cs
 delete mode 100644 Atlas.DonorImport/Services/Debug/DonorImportResultsPeeker.cs
 delete mode 100644 Atlas.Functions/Functions/Debug/SupportFunctions.cs
 delete mode 100644 Atlas.Functions/Services/Debug/SupportMessagesPeeker.cs
 delete mode 100644 Atlas.Functions/Settings/NotficationsDebugSettings.cs
 delete mode 100644 Atlas.ManualTesting.Common/Contexts/IDonorExportData.cs
 delete mode 100644 Atlas.ManualTesting.Common/Contexts/ISearchData.cs
 rename Atlas.ManualTesting.Common/{Services => }/FileReader.cs (97%)
 delete mode 100644 Atlas.ManualTesting.Common/Models/DonorTypeExtensions.cs
 delete mode 100644 Atlas.ManualTesting.Common/Models/Entities/LocusMatchDetails.cs
 delete mode 100644 Atlas.ManualTesting.Common/Models/Entities/MatchedDonor.cs
 delete mode 100644 Atlas.ManualTesting.Common/Models/Entities/MatchedDonorProbability.cs
 delete mode 100644 Atlas.ManualTesting.Common/Models/Entities/SearchRequestRecord.cs
 delete mode 100644 Atlas.ManualTesting.Common/Models/ImportedSubject.cs
 delete mode 100644 Atlas.ManualTesting.Common/Models/SuccessfulSearchRequestInfo.cs
 delete mode 100644 Atlas.ManualTesting.Common/Repositories/IProcessedResultsRepository.cs
 delete mode 100644 Atlas.ManualTesting.Common/Repositories/ISearchRequestsRepository.cs
 delete mode 100644 Atlas.ManualTesting.Common/Repositories/LocusMatchDetailsRepository.cs
 delete mode 100644 Atlas.ManualTesting.Common/Repositories/MatchProbabilitiesRepository.cs
 delete mode 100644 Atlas.ManualTesting.Common/Repositories/MatchedDonorsRepository.cs
 delete mode 100644 Atlas.ManualTesting.Common/Repositories/SearchRequestsRepositoryBase.cs
 delete mode 100644 Atlas.ManualTesting.Common/Repositories/TestDonorExportRepository.cs
 delete mode 100644 Atlas.ManualTesting.Common/Services/AtlasPreparer.cs
 delete mode 100644 Atlas.ManualTesting.Common/Services/MessageSender.cs
 delete mode 100644 Atlas.ManualTesting.Common/Services/Storers/LocusMatchDetailsStorer.cs
 delete mode 100644 Atlas.ManualTesting.Common/Services/TestDonorExporter.cs
 delete mode 100644 Atlas.ManualTesting.Common/Settings/DataRefreshSettings.cs
 create mode 100644 Atlas.ManualTesting.Common/SubjectImport/ImportedSubject.cs
 rename Atlas.Common/Utils/Http/HttpRequestExtensions.cs => Atlas.ManualTesting/Helpers/RequestExtension.cs (81%)
 rename Atlas.ManualTesting/Models/{PeekRequests.cs => PeekRequest.cs} (54%)
 delete mode 100644 Atlas.ManualTesting/Services/ServiceBus/DeadLettersPeeker.cs
 create mode 100644 Atlas.ManualTesting/Services/ServiceBus/ServiceBusPeeker.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Integration/TestHelpers/HaplotypeFrequencyImporter.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231017183400_AddNewColumn_ExternalHFSetId.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231017183400_AddNewColumn_ExternalHFSetId.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231101184037_AddDonorExportData.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231101184037_AddDonorExportData.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231102141557_ChangeColumnDataType.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231102141557_ChangeColumnDataType.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231221161218_AddSearchTables.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231221161218_AddSearchTables.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231221205016_AmendSearchSetTable.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231221205016_AmendSearchSetTable.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231221214440_AmendSearchResultsRetrieved.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231221214440_AmendSearchResultsRetrieved.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231223172423_RenameMatchCountsTable.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231223172423_RenameMatchCountsTable.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231223172956_NewLocusScoreColumns.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231223172956_NewLocusScoreColumns.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231223175948_AddAntigenMatchColumns.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231223175948_AddAntigenMatchColumns.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231223204203_ChangeToDonorCode.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231223204203_ChangeToDonorCode.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231224125031_AddPopulationIds.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231224125031_AddPopulationIds.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231224125622_AddProbabilityAsPercentage.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20231224125622_AddProbabilityAsPercentage.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20240109190030_NewIndexes.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Migrations/20240109190030_NewIndexes.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Models/MatchedDonorBuilder.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Models/SearchSet.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Models/ValidationSearchRequestRecord.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Repositories/SearchRequestsRepository.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation.Data/Repositories/SearchSetRepository.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation/Functions/Exercise3Functions.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation/Functions/Exercise4Functions.cs
 rename Atlas.MatchPrediction.Test.Validation/{Settings => Models}/ValidationAzureStorageSettings.cs (60%)
 delete mode 100644 Atlas.MatchPrediction.Test.Validation/Models/ValidationSearchRequest.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation/Services/Exercise3/MatchPredictionLocationSender.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation/Services/Exercise4/SearchRequester.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation/Services/Exercise4/SearchResultNotificationSender.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation/Services/Exercise4/SearchResultSetProcessor.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Validation/Services/Exercise4/ValidationAtlasPreparer.cs
 create mode 100644 Atlas.MatchPrediction.Test.Validation/Services/MessageSender.cs
 rename Atlas.MatchPrediction.Test.Validation/Services/{Exercise3 => }/ResultsProcessor.cs (79%)
 rename Atlas.MatchPrediction.Test.Validation/Services/{Exercise3/MatchPredictionRequester.cs => ValidationRunner.cs} (90%)
 delete mode 100644 Atlas.MatchPrediction.Test.Validation/Settings/ValidationSearchSettings.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231101134247_RefactorDbForExercise4Changes.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231101134247_RefactorDbForExercise4Changes.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231221143334_RenamePatientIdColumn.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231221143334_RenamePatientIdColumn.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231221145130_RenameDonorIdColumn.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231221145130_RenameDonorIdColumn.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231221153305_RebuildIndex.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231221153305_RebuildIndex.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231223172627_RenameMatchCountsTable.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231223172627_RenameMatchCountsTable.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231223172912_NewLocusScoreColumns.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231223172912_NewLocusScoreColumns.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231223180125_AddAntigenMatchColumns.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231223180125_AddAntigenMatchColumns.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231223194808_RemovePerformanceChangeToDonorCode.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231223194808_RemovePerformanceChangeToDonorCode.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231224125123_AddPopulationIds.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231224125123_AddPopulationIds.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231224125709_AddProbabilityAsPercentage.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20231224125709_AddProbabilityAsPercentage.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20240109190148_NewIndexes.Designer.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Migrations/20240109190148_NewIndexes.cs
 rename {Atlas.ManualTesting.Common/Models/Entities => Atlas.MatchPrediction.Test.Verification.Data/Models/Entities/TestHarness}/TestDonorExportRecord.cs (52%)
 create mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Models/Entities/Verification/LocusMatchCount.cs
 rename {Atlas.ManualTesting.Common/Models/Entities => Atlas.MatchPrediction.Test.Verification.Data/Models/Entities/Verification}/MatchProbability.cs (54%)
 create mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Models/Entities/Verification/MatchedDonor.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Models/Entities/Verification/MatchedDonorBuilder.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Models/Entities/Verification/SearchRequestRecord.cs
 delete mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Models/Entities/Verification/VerificationSearchRequestRecord.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Models/SuccessfulSearchRequestInfo.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Repositories/IProcessedResultsRepository.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Repositories/MatchCountsRepository.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Repositories/MatchProbabilitiesRepository.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Repositories/MatchedDonorsRepository.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification.Data/Repositories/TestDonorExportRepository.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification/Services/AtlasPreparer.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification/Services/TestDonorExporter.cs
 rename {Atlas.ManualTesting.Common/Services => Atlas.MatchPrediction.Test.Verification/Services/Verification/ResultsProcessing}/ResultSetProcessor.cs (58%)
 create mode 100644 Atlas.MatchPrediction.Test.Verification/Services/Verification/ResultsProcessing/Storers/MatchCountsStorer.cs
 rename Atlas.ManualTesting.Common/Services/Storers/MatchedDonorProbabilitiesStorer.cs => Atlas.MatchPrediction.Test.Verification/Services/Verification/ResultsProcessing/Storers/MatchedProbabilitiesStorer.cs (65%)
 rename {Atlas.ManualTesting.Common/Services => Atlas.MatchPrediction.Test.Verification/Services/Verification/ResultsProcessing}/Storers/ResultsStorer.cs (68%)
 rename {Atlas.ManualTesting.Common/Services => Atlas.MatchPrediction.Test.Verification/Services/Verification/ResultsProcessing}/Storers/SearchResultDonorStorer.cs (67%)
 delete mode 100644 Atlas.MatchPrediction.Test.Verification/Services/VerificationAtlasPreparer.cs
 create mode 100644 Atlas.MatchPrediction.Test.Verification/Settings/VerificationDataRefreshSettings.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Common/Models/Scoring/LocusScoreResult.cs
 create mode 100644 Atlas.MatchingAlgorithm.Common/Models/Scoring/MatchGradeResult.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Data.Persistent/Migrations/20231212191758_UpdateMatchGradesTable.Designer.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Data.Persistent/Migrations/20231212191758_UpdateMatchGradesTable.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Data/Migrations/20240116210024_IndexOnExternalDonorCode.Designer.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Data/Migrations/20240116210024_IndexOnExternalDonorCode.cs
 create mode 100644 Atlas.MatchingAlgorithm.Test.Integration/IntegrationTests/Search/NullAlleleScoring/ScoringTestsForNullAlleleInString.cs
 create mode 100644 Atlas.MatchingAlgorithm.Test.Integration/IntegrationTests/Search/NullAlleleScoring/ScoringTestsForSingleNullAllele.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Integration/IntegrationTests/Search/ScoringTestsForNullAllele.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/IsAntigenMatch.feature
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/IsAntigenMatch.feature.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/LocusMatchCategory.feature
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/LocusMatchCategory.feature.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/LocusScoreCount.feature
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/LocusScoreCount.feature.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/MatchConfidence.feature
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/MatchConfidence.feature.cs
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/MatchGrade.feature
 delete mode 100644 Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/MatchGrade.feature.cs
 rename Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/{Search.feature => NullAlleles.feature} (100%)
 rename Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/NullAlleles/{Search.feature.cs => NullAlleles.feature.cs} (99%)
 create mode 100644 Atlas.MatchingAlgorithm.Test/Services/Donors/DonorHelperTests.cs
 create mode 100644 Atlas.MatchingAlgorithm/Services/Donors/DonorHelper.cs
 delete mode 100644 Atlas.MatchingAlgorithm/Services/Search/Scoring/PositionalScorerBase.cs
 rename MiscTestingAndDebuggingResources/ManualTesting/MatchPredictionValidation/{exercise3 => }/HaplotypeFrequenciesConverter.js (100%)
 delete mode 100644 MiscTestingAndDebuggingResources/ManualTesting/MatchPredictionValidation/exercise4/HaplotypeFrequenciesConverter.js
 delete mode 100644 MiscTestingAndDebuggingResources/ManualTesting/MatchPredictionValidation/exercise4/SQL_IncompleteOrFailedSearches.sql
 delete mode 100644 MiscTestingAndDebuggingResources/ManualTesting/MatchPredictionValidation/exercise4/SQL_ReportAllResults.sql
 delete mode 100644 MiscTestingAndDebuggingResources/ManualTesting/MatchPredictionValidation/exercise4/SQL_Unrepresented.sql
 create mode 100644 terraform/core/modules/matching_algorithm/app_configuration.tf
