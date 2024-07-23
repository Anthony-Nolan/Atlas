IF USER_ID('$(searchTrackingUsername)') IS NULL
    BEGIN
        CREATE USER $(searchTrackingUsername) WITH PASSWORD = '$(searchTrackingPassword)', DEFAULT_SCHEMA = $(searchTrackingSchema)
        ALTER ROLE db_datareader ADD MEMBER $(searchTrackingUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(searchTrackingUsername)
    END

ELSE

    BEGIN
        ALTER USER $(searchTrackingUsername) WITH PASSWORD = '$(searchTrackingPassword)', DEFAULT_SCHEMA = $(searchTrackingSchema)
        ALTER ROLE db_datareader ADD MEMBER $(searchTrackingUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(searchTrackingUsername)
    END

