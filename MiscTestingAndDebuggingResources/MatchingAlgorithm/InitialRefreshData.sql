INSERT INTO DataRefreshHistory (
            RefreshBeginUtc, RefreshEndUtc,
            [Database],
            WmdaDatabaseVersion,
            WasSuccessful)
     VALUES (
            GETDATE()-1, GETDATE(),
            'DatabaseA',
            '3330', -- Note that this matches the Hardcoded test data; not the latest version of the dictionary.
            1)
GO

