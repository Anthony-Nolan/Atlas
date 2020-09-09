IF USER_ID('$(matchPredictionUsername)') IS NULL
    BEGIN
        CREATE USER $(matchPredictionUsername) WITH PASSWORD = '$(matchPredictionPassword)'
        ALTER ROLE db_datareader ADD MEMBER $(matchPredictionUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(matchPredictionUsername)
    END

ELSE

    BEGIN
        ALTER USER $(matchPredictionUsername) WITH PASSWORD = '$(matchPredictionPassword)'
        ALTER ROLE db_datareader ADD MEMBER $(matchPredictionUsername)
        ALTER ROLE db_datawriter ADD MEMBER $(matchPredictionUsername)
    END

