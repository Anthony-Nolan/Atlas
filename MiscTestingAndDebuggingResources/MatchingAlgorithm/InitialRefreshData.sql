USE [AtlasMatchingPersistent]
GO

INSERT INTO DataRefreshHistory (
            RefreshBeginUtc, RefreshEndUtc,
            [Database],
            WmdaDatabaseVersion,
            WasSuccessful)
     VALUES (
            GETDATE()-1, GETDATE(),
            'DatabaseA',
            '3390', -- Note that the Hardcoded test data is v3.3.0; but we don't expect this to cause problems in practice.
            1)
GO

