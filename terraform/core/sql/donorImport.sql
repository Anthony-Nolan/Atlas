IF USER_ID('$(matchingUsername)') IS NULL
BEGIN 
	CREATE USER $(matchingUsername) WITH PASSWORD = '$(matchingPassword)';
  ALTER ROLE db_datareader ADD MEMBER $(matchPredictionUsername)
END

ELSE

BEGIN
	ALTER USER $(matchingUsername) WITH PASSWORD = '$(matchingPassword)';
END



IF USER_ID('$(donorImportUsername)') IS NULL
BEGIN 
	CREATE USER $(donorImportUsername) WITH PASSWORD = '$(donorImportPassword)'
  ALTER ROLE db_datareader ADD MEMBER $(donorImportUsername)
  ALTER ROLE db_datawriter ADD MEMBER $(donorImportUsername)
END

ELSE

BEGIN
	ALTER USER $(donorImportUsername) WITH PASSWORD = '$(donorImportPassword)'
END

