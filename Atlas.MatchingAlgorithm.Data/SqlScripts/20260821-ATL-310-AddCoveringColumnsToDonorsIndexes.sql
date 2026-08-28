-------------------------------------------------------------------------------
-- ATL-310. Adds covering columns to the two Donors indexes that the donor join in
-- DonorSearchRepository.MatchAtLocusSql seeks.
--
-- WHY THIS IS A MANUAL SCRIPT
-- dbo.Donors holds about 44.6 million rows on live. An index build over that table is far too slow to
-- sit inside a release, so the build is done ahead of the release by this script, out of hours. Each
-- statement uses DROP_EXISTING = ON with ONLINE = ON, so the old copy of the index serves queries until
-- the swap at the end. The table is never without the index, and the index keeps its name.
--
-- Migration <timestamp>_Add_Covering_Columns_to_Donors_Indexes holds the same DDL behind the same shape
-- check. Where this script has run, that migration finds the work done and changes nothing.
--
-- WHERE TO RUN IT
-- On BOTH transient matching databases, A and B. Searches read from whichever one the persistent
-- database marks as active, and a data refresh can change which that is, so a fix applied to only one
-- of them is not a fix. Each section starts with SELECT DB_NAME() - read it before you go on. Attached
-- to the wrong database, these statements do nothing you want.
--
-- ONLINE = ON needs Azure SQL, or Enterprise or Developer edition.
--
-- Every section is re-runnable. The shape check compares the whole included-column list, so a partly
-- applied index is rebuilt rather than passed over.
-------------------------------------------------------------------------------


-------------------------------------------------------------------------------
-- 1  IX_DonorId  ->  key (DonorId) include (DonorType, RegistryCode)
--    Do this one first. It is the index the donor join seeks.
--    Watch it from a second session with section 3.
-------------------------------------------------------------------------------
SELECT ThisDatabase = DB_NAME();

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Donors') AND name = 'IX_DonorId')
    BEGIN
        -- There is no old copy to drop. This is not the expected state on a released database.
        PRINT 'IX_DonorId is missing. Creating it.';

        CREATE NONCLUSTERED INDEX [IX_DonorId] ON [dbo].[Donors] ([DonorId])
            INCLUDE ([DonorType], [RegistryCode])
            WITH (ONLINE = ON);
    END
ELSE IF ISNULL((
        SELECT STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY c.name)
        FROM sys.indexes i
                 INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                 INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID('dbo.Donors')
          AND i.name = 'IX_DonorId'
          AND ic.is_included_column = 1), '') <> 'DonorType,RegistryCode'
    BEGIN
        PRINT 'Rebuilding IX_DonorId. The old copy serves queries until the swap at the end.';

        CREATE NONCLUSTERED INDEX [IX_DonorId] ON [dbo].[Donors] ([DonorId])
            INCLUDE ([DonorType], [RegistryCode])
            WITH (DROP_EXISTING = ON, ONLINE = ON);
    END
ELSE
    PRINT 'IX_DonorId already includes DonorType and RegistryCode. Nothing to do.';

-------------------------------------------------------------------------------
-- 2  IX_Donors_DonorType_RegistryCode  ->  key (DonorType, RegistryCode) include (DonorId)
--    Start this only after section 1 has finished.
--    This restores the shape the index had before the RegistryCode column was dropped and re-added.
-------------------------------------------------------------------------------
SELECT ThisDatabase = DB_NAME();

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Donors') AND name = 'IX_Donors_DonorType_RegistryCode')
    BEGIN
        -- There is no old copy to drop. This is not the expected state on a released database.
        PRINT 'IX_Donors_DonorType_RegistryCode is missing. Creating it.';

        CREATE NONCLUSTERED INDEX [IX_Donors_DonorType_RegistryCode] ON [dbo].[Donors] ([DonorType], [RegistryCode])
            INCLUDE ([DonorId])
            WITH (ONLINE = ON);
    END
ELSE IF ISNULL((
        SELECT STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY c.name)
        FROM sys.indexes i
                 INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                 INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID('dbo.Donors')
          AND i.name = 'IX_Donors_DonorType_RegistryCode'
          AND ic.is_included_column = 1), '') <> 'DonorId'
    BEGIN
        PRINT 'Rebuilding IX_Donors_DonorType_RegistryCode. The old copy serves queries until the swap at the end.';

        CREATE NONCLUSTERED INDEX [IX_Donors_DonorType_RegistryCode] ON [dbo].[Donors] ([DonorType], [RegistryCode])
            INCLUDE ([DonorId])
            WITH (DROP_EXISTING = ON, ONLINE = ON);
    END
ELSE
    PRINT 'IX_Donors_DonorType_RegistryCode already includes DonorId. Nothing to do.';

-------------------------------------------------------------------------------
-- 3  Watch a build. Run this from a SECOND session, on the same database, while section 1 or 2 runs.
--    SQL Server does not always fill in percent_complete for an index build. A moving elapsed time with no
--    blocking session is the sign of a healthy build.
-------------------------------------------------------------------------------
SELECT
    r.session_id,
    r.command,
    r.status,
    PercentComplete = CAST(r.percent_complete AS decimal(5, 2)),
    ElapsedMinutes = r.total_elapsed_time / 60000,
    r.wait_type,
    r.blocking_session_id,
    Statement_ = t.text
FROM sys.dm_exec_requests r
         CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id <> @@SPID
  AND r.command LIKE '%INDEX%';

-------------------------------------------------------------------------------
-- 4  Verify. Both rows must read PASS, on both transient databases.
--     This is the same check as the integration test DonorIndexSchemaTests, and the same comparison as the
--     shape check in sections 1 and 2.
-------------------------------------------------------------------------------
SELECT ThisDatabase = DB_NAME();

SELECT
    Check_ = 'IX_DonorId includes DonorType and RegistryCode',
    Result = CASE WHEN ISNULL((
                           SELECT STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY c.name)
                           FROM sys.indexes i
                                    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                                    INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                           WHERE i.object_id = OBJECT_ID('dbo.Donors')
                             AND i.name = 'IX_DonorId'
                             AND ic.is_included_column = 1), '') = 'DonorType,RegistryCode'
                      THEN 'PASS' ELSE '### FAIL ###' END
UNION ALL
SELECT
    Check_ = 'IX_Donors_DonorType_RegistryCode includes DonorId',
    Result = CASE WHEN ISNULL((
                           SELECT STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY c.name)
                           FROM sys.indexes i
                                    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                                    INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                           WHERE i.object_id = OBJECT_ID('dbo.Donors')
                             AND i.name = 'IX_Donors_DonorType_RegistryCode'
                             AND ic.is_included_column = 1), '') = 'DonorId'
                      THEN 'PASS' ELSE '### FAIL ###' END;
