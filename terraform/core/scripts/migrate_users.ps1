$variablesArray = 
"matchingUsername=matchingUser",
"matchingPassword='$(TF_VAR_MATCHING_DATABASE_PASSWORD)'",
"matchPredictionUsername=matchPredictionUser",
"matchPredictionPassword='$(TF_VAR_MATCH_PREDICTION_DATABASE_PASSWORD)'",
"donorImportUsername=donorImportUser",
"donorImportPassword='$(TF_VAR_DONOR_DATABASE_PASSWORD)'"


Invoke-Sqlcmd -InputFile "sql/matchPrediction.sql" -ServerInstance $(TERRAFORM.sql-server) -Database $(TERRAFORM.match-prediction-database-name) -Username $(TERRAFORM.sql-server-admin-login) -Password $(TERRAFORM.sql-server-admin-login-password) -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/matchingTransient.sql" -ServerInstance $(TERRAFORM.sql-server) -Database $(TERRAFORM.matching-algorithm-a-name) -Username $(TERRAFORM.sql-server-admin-login) -Password $(TERRAFORM.sql-server-admin-login-password) -Variable $variablesArray
Invoke-Sqlcmd -InputFile "sql/matchingTransient.sql" -ServerInstance $(TERRAFORM.sql-server) -Database $(TERRAFORM.matching-algorithm-b-name) -Username $(TERRAFORM.sql-server-admin-login) -Password $(TERRAFORM.sql-server-admin-login-password) -Variable $variablesArray
Invoke-Sqlcmd -InputFile "sql/matchingPersistent.sql" -ServerInstance $(TERRAFORM.sql-server) -Database $(TERRAFORM.matching-algorithm-persistent-db-name) -Username $(TERRAFORM.sql-server-admin-login) -Password $(TERRAFORM.sql-server-admin-login-password) -Variable $variablesArray

Invoke-Sqlcmd -InputFile "sql/donorImport.sql" -ServerInstance $(TERRAFORM.sql-server) -Database $(TERRAFORM.donor-import-database-name) -Username $(TERRAFORM.sql-server-admin-login) -Password $(TERRAFORM.sql-server-admin-login-password) -Variable $variablesArray
