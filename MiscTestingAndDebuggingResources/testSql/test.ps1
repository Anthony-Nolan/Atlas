$variablesArray = "matchingTransientA=dev-atlas-matching-a", 
"matchingTransientB=dev-atlas-matching-b", 
"matchingPersistent=dev-atlas-matching-persistent", 
"matchPrediction=dev-atlas-match-prediction", 
"donorImport=dev-atlas-donors",
"matchingUsername=matchingUser",
"matchingPassword='PZSgg4yM'",
"matchPredictionUsername=matchPredictionUser",
"matchPredictionPassword='1WuSXTfY'",
"donorImportUsername=donorImportUser",
"donorImportPassword='4IYnnN76'"


Invoke-Sqlcmd -InputFile "./matchPrediction.sql" -ServerInstance "dev-atlas-sql-server.database.windows.net" -Database "dev-atlas-match-prediction" -Username "atlas-admin" -Password "fSE6tLQk85E}/H'e" -Variable $variablesArray

Invoke-Sqlcmd -InputFile "./matchingTransient.sql" -ServerInstance "dev-atlas-sql-server.database.windows.net" -Database "dev-atlas-matching-a" -Username "atlas-admin" -Password "fSE6tLQk85E}/H'e" -Variable $variablesArray
Invoke-Sqlcmd -InputFile "./matchingTransient.sql" -ServerInstance "dev-atlas-sql-server.database.windows.net" -Database "dev-atlas-matching-b" -Username "atlas-admin" -Password "fSE6tLQk85E}/H'e" -Variable $variablesArray
Invoke-Sqlcmd -InputFile "./matchingPersistent.sql" -ServerInstance "dev-atlas-sql-server.database.windows.net" -Database "dev-atlas-matching-persistent" -Username "atlas-admin" -Password "fSE6tLQk85E}/H'e" -Variable $variablesArray

Invoke-Sqlcmd -InputFile "./donorImport.sql" -ServerInstance "dev-atlas-sql-server.database.windows.net" -Database "dev-atlas-donors" -Username "atlas-admin" -Password "fSE6tLQk85E}/H'e" -Variable $variablesArray

