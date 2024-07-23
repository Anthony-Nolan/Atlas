# Schema names are hardcoded in the EF context of each database - if changed, they will need to be changed in both places

$variablesArray =
"matchingUsername=$env:matchingUser",
"matchingPassword=$env:matchingPassword",
"matchingPersistentSchema=MatchingAlgorithmPersistent",
"matchPredictionUsername=$env:matchPredictionUser",
"matchPredictionPassword=$env:matchPredictionPassword",
"matchPredictionSchema=MatchPrediction",
"donorImportUsername=$env:donorImportUser",
"donorImportPassword=$env:donorImportPassword",
"donorImportSchema=Donors",
"matchingUsernameForDonorDB=$env:matchingUsernameForDonorDB",
"matchingPasswordForDonorDB=$env:matchingPasswordForDonorDB",
"repeatSearchUsername=$env:repeatSearchUsername",
"repeatSearchPassword=$env:repeatSearchPassword",
"repeatSearchSchema=RepeatSearch"
"searchTrackingUsername=$env:searchTrackingUsername",
"searchTrackingPassword=$env:searchTrackingPassword",
"searchTrackingSchema=SearchTracking"

Write-Host $variablesArray

Invoke-Sqlcmd -InputFile "sql/createUsers/matchPrediction.sql" -ServerInstance $env:sqlServer -Database $env:matchPredictionDatabaseName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/createUsers/matchingTransient.sql" -ServerInstance $env:sqlServer -Database $env:matchingAlgorithmDatabaseTransientAName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray
Invoke-Sqlcmd -InputFile "sql/createUsers/matchingTransient.sql" -ServerInstance $env:sqlServer -Database $env:matchingAlgorithmDatabaseTransientBName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray
Invoke-Sqlcmd -InputFile "sql/createUsers/matchingPersistent.sql" -ServerInstance $env:sqlServer -Database $env:matchingAlgorithmDatabasePersistentName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/createUsers/donorImport.sql" -ServerInstance $env:sqlServer -Database $env:donorImportDatabase -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/createUsers/repeatSearch.sql" -ServerInstance $env:sqlServer -Database $env:repeatSearchDatabase -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/createUsers/searchTracking.sql" -ServerInstance $env:sqlServer -Database $env:searchTrackingDatabase -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray