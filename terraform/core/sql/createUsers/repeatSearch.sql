IF USER_ID('$(repeatSearchUsername)') IS NULL
    BEGIN
        CREATE USER $(repeatSearchUsername) WITH PASSWORD = '$(repeatSearchPassword)', DEFAULT_SCHEMA = $(repeatSearchSchema)
        ALTER ROLE db_datareader ADD MEMBER $(repeatSearchUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(repeatSearchUsername)
    END

ELSE

    BEGIN
        ALTER USER $(repeatSearchUsername) WITH PASSWORD = '$(repeatSearchPassword)', DEFAULT_SCHEMA = $(repeatSearchSchema)
        ALTER ROLE db_datareader ADD MEMBER $(repeatSearchUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(repeatSearchUsername)
    END

