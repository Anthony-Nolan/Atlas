$variablesArray = 
"matchingUsername=matchingUser",
"matchingPassword={$env:matchingPassword}",
"matchPredictionUsername=matchPredictionUser",
"matchPredictionPassword={$env:matchPredictionPassword}",
"donorImportUsername=donorImportUser",
"donorImportPassword={$env:donorImportPassword}"

Write-Host $variablesArray

Invoke-Sqlcmd -InputFile "sql/matchPrediction.sql" -ServerInstance $env:sqlServer -Database $env:matchPredictionDatabaseName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/matchingTransient.sql" -ServerInstance $env:sqlServer -Database $env:matchingAlgorithmDatabaseTransientAName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray
Invoke-Sqlcmd -InputFile "sql/matchingTransient.sql" -ServerInstance $env:sqlServer -Database $env:matchingAlgorithmDatabaseTransientBName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray
Invoke-Sqlcmd -InputFile "sql/matchingPersistent.sql" -ServerInstance $env:sqlServer -Database $env:matchingAlgorithmDatabasePersistentName -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/donorImport.sql" -ServerInstance $env:sqlServer -Database $env:donorImportDatabase -Username $env:sqlServerLogin -Password $env:sqlServerLoginPassword -Variable $variablesArray
