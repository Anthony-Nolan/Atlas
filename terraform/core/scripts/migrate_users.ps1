$variablesArray = 
"matchingUsername=$env:matchingUser",
"matchingPassword=$env:matchingPassword",
"matchPredictionUsername=$env:matchPredictionUser",
"matchPredictionPassword=$env:matchPredictionPassword",
"donorImportUsername=$env:donorImportUser",
"donorImportPassword=$env:donorImportPassword"

Write-Host $variablesArray

Invoke-Sqlcmd -InputFile "sql/createUsers/matchPrediction.sql" -ServerInstance $env:sqlServer -Database $env:matchPredictionDatabaseName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/createUsers/matchingTransient.sql" -ServerInstance $env:sqlServer -Database $env:matchingAlgorithmDatabaseTransientAName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray
Invoke-Sqlcmd -InputFile "sql/createUsers/matchingTransient.sql" -ServerInstance $env:sqlServer -Database $env:matchingAlgorithmDatabaseTransientBName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray
Invoke-Sqlcmd -InputFile "sql/createUsers/matchingPersistent.sql" -ServerInstance $env:sqlServer -Database $env:matchingAlgorithmDatabasePersistentName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/createUsers/donorImport.sql" -ServerInstance $env:sqlServer -Database $env:donorImportDatabase -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray
