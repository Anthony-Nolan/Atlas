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
            '3390',
            1)
GO

