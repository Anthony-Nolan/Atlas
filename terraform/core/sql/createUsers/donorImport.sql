IF USER_ID('$(matchingUsernameForDonorDB)') IS NULL
    BEGIN
        CREATE USER $(matchingUsernameForDonorDB) WITH PASSWORD = '$(matchingPasswordForDonorDB)', DEFAULT_SCHEMA = $(donorImportSchema);
        ALTER ROLE db_datareader ADD MEMBER $(matchingUsernameForDonorDB)
		ALTER ROLE db_datawriter ADD MEMBER $(matchingUsernameForDonorDB)
    END

ELSE

    BEGIN
        ALTER USER $(matchingUsernameForDonorDB) WITH PASSWORD = '$(matchingPasswordForDonorDB)', DEFAULT_SCHEMA = $(donorImportSchema);
        ALTER ROLE db_datareader ADD MEMBER $(matchingUsernameForDonorDB)
		ALTER ROLE db_datawriter ADD MEMBER $(matchingUsernameForDonorDB)
    END


IF USER_ID('$(donorImportUsername)') IS NULL
    BEGIN
        CREATE USER $(donorImportUsername) WITH PASSWORD = '$(donorImportPassword)', DEFAULT_SCHEMA = $(donorImportSchema)
        ALTER ROLE db_datareader ADD MEMBER $(donorImportUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(donorImportUsername)
    END

ELSE

    BEGIN
        ALTER USER $(donorImportUsername) WITH PASSWORD = '$(donorImportPassword)', DEFAULT_SCHEMA = $(donorImportSchema)
        ALTER ROLE db_datareader ADD MEMBER $(donorImportUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(donorImportUsername)
    END

