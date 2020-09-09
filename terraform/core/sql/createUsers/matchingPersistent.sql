IF USER_ID('$(matchingUsername)') IS NULL
    BEGIN
        CREATE USER $(matchingUsername) WITH PASSWORD = '$(matchingPassword)'
        ALTER ROLE db_datareader ADD MEMBER $(matchingUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(matchingUsername)
        ALTER ROLE db_owner ADD MEMBER $(matchingUsername)
    END

ELSE

    BEGIN
        ALTER USER $(matchingUsername) WITH PASSWORD = '$(matchingPassword)'
        ALTER ROLE db_datareader ADD MEMBER $(matchingUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(matchingUsername)
        ALTER ROLE db_owner ADD MEMBER $(matchingUsername)
    END

