<#
.SYNOPSIS
    Turns the genotype set read records of one refresh cycle into the list ATL-347 replays against LRU caches (ATL-571).

.DESCRIPTION
    Search batches record the donor inputs they would read precomputed genotype sets for, as JSON blobs under
    "{refresh record id}/..." (setting GenotypeSetReadRecording:Enabled). This script:

      1. checks that the refresh record is still the active one: its transient database still holds the rows the
         searches would have read. Once the next Data Refresh completes, that database is wiped and the records of
         this cycle can no longer be resolved, so run the script before then.
      2. downloads the record blobs of that refresh record,
      3. loads their (donor input, donor id) rows into a temp table on the active transient database,
      4. joins them to DonorSubjectGenotypeSets and SubjectGenotypeSetValues to find each donor input's value key
         and payload size, and
      5. writes one CSV row per donor input.

    Read-only on the databases: it creates only a session temp table. It reads DATALENGTH of the payloads, so run it
    off-peak on LIVE.

    Needs: Azure CLI, logged in ("az login") with read access to the storage account (Storage Blob Data Reader) and an
    Entra ID login on the SQL server that can read the persistent and transient databases.

.OUTPUTS
    CSV columns:
      RefreshRecordId, RecordedAtUtc, SearchRequestId, UsePrecomputedGenotypeSets, AllowedLociKey, DonorIdCount,
      Status     - Stored (a value row exists), NotStored (not stored by design, ATL-478: computed live, never cached),
                   NoRow (no assignment row for the donor ids)
      ValueKeyHash - SHA-256 of "HlaTypingKey|HaplotypeFrequencySetId|AllowedLociKey"; empty unless Stored
      SizeBytes  - DATALENGTH of the stored payload; 0 unless Stored

.EXAMPLE
    ./Resolve-GenotypeSetReadRecords.ps1 -RefreshRecordId 37 -StorageAccountName devatlasstorage `
        -SqlServer dev-atlas-sql.database.windows.net -PersistentDatabase dev-atlas-matching-persistent `
        -TransientDatabaseA dev-atlas-matching-a -TransientDatabaseB dev-atlas-matching-b -OutFile ./reads-37.csv
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [int] $RefreshRecordId,
    # Not needed with -RecordsFolder.
    [string] $StorageAccountName,
    [string] $Container = 'genotype-set-read-records',
    [Parameter(Mandatory)] [string] $SqlServer,
    [Parameter(Mandatory)] [string] $PersistentDatabase,
    [Parameter(Mandatory)] [string] $TransientDatabaseA,
    [Parameter(Mandatory)] [string] $TransientDatabaseB,
    [Parameter(Mandatory)] [string] $OutFile,
    [string] $WorkFolder = (Join-Path ([System.IO.Path]::GetTempPath()) "genotype-set-reads-$RefreshRecordId"),
    [int] $CommandTimeoutSeconds = 3600,
    # Read the records from this folder (holding "{RefreshRecordId}/...") instead of downloading them. For local tests.
    [string] $RecordsFolder,
    # Windows login instead of an Entra ID token from the Azure CLI. For a local SQL Server.
    [switch] $IntegratedSecurity
)

$ErrorActionPreference = 'Stop'

# The record has the loci match prediction ran on; the stored rows are keyed by the AllowedLociKey name
# (Atlas.MatchingAlgorithm.Data AllowedLociKey). Loci in Locus enum order, as GenotypeSetReadRecorder writes them.
$allowedLociKeys = @{
    'A,B,C,Dqb1,Drb1' = 'ABCDrb1Dqb1'
    'A,B,C,Drb1'      = 'ABCDrb1'
    'A,B,Dqb1,Drb1'   = 'ABDrb1Dqb1'
    'A,B,Drb1'        = 'ABDrb1'
}

function Open-SqlConnection([string] $database) {
    if ($IntegratedSecurity) {
        $local = New-Object System.Data.SqlClient.SqlConnection(
            "Server=$SqlServer;Database=$database;Integrated Security=True;TrustServerCertificate=True;")
        $local.Open()
        return $local
    }

    $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
    if ($LASTEXITCODE -ne 0) { throw 'Could not get a SQL access token. Run "az login" first.' }

    $connection = New-Object System.Data.SqlClient.SqlConnection(
        "Server=tcp:$SqlServer,1433;Database=$database;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;")
    $connection.AccessToken = $token
    $connection.Open()
    return $connection
}

# 1. The refresh record must still be the active one: the last successful refresh, as the search reader decides it.
$persistent = Open-SqlConnection $PersistentDatabase
try {
    $command = $persistent.CreateCommand()
    $command.CommandText = @'
SELECT TOP 1 Id, [Database]
FROM DataRefreshHistory
WHERE RefreshEndUtc IS NOT NULL AND WasSuccessful = 1
ORDER BY RefreshEndUtc DESC
'@
    $reader = $command.ExecuteReader()
    if (-not $reader.Read()) { throw 'No successful data refresh record found.' }
    $activeId = $reader.GetInt32(0)
    $activeDatabase = $reader.GetString(1)
    $reader.Close()
}
finally {
    $persistent.Dispose()
}

if ($activeId -ne $RefreshRecordId) {
    throw "Refresh record $RefreshRecordId is no longer active (active: $activeId). Its database has been replaced, so its records cannot be resolved."
}

$transientDatabase = switch ($activeDatabase) {
    'DatabaseA' { $TransientDatabaseA }
    'DatabaseB' { $TransientDatabaseB }
    default { throw "Unknown transient database '$activeDatabase'." }
}
Write-Host "Refresh record $RefreshRecordId is active, on $activeDatabase ($transientDatabase)."

# 2. Download this refresh record's blobs.
if ($RecordsFolder) {
    $WorkFolder = $RecordsFolder
}
else {
    if (-not $StorageAccountName) { throw 'Give -StorageAccountName, or -RecordsFolder.' }
    New-Item -ItemType Directory -Force -Path $WorkFolder | Out-Null
    az storage blob download-batch --account-name $StorageAccountName --auth-mode login --source $Container `
        --pattern "$RefreshRecordId/*" --destination $WorkFolder --overwrite true --only-show-errors | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Blob download failed.' }
}

$files = @(Get-ChildItem -Path (Join-Path $WorkFolder $RefreshRecordId) -Recurse -Filter '*.json' -ErrorAction SilentlyContinue)
Write-Host "Found $($files.Count) record blobs."
if ($files.Count -eq 0) { throw "No records found under $Container/$RefreshRecordId/." }

# 3. Load (donor input, donor id) rows into a session temp table, one bulk copy per blob.
$transient = Open-SqlConnection $transientDatabase
try {
    $command = $transient.CreateCommand()
    $command.CommandTimeout = $CommandTimeoutSeconds
    $command.CommandText = @'
CREATE TABLE #DonorInputs (
    DonorInputId bigint NOT NULL PRIMARY KEY,
    RecordedAtUtc datetimeoffset NOT NULL,
    SearchRequestId nvarchar(100) NOT NULL,
    UsePrecomputedGenotypeSets bit NOT NULL,
    AllowedLociKey nvarchar(20) NOT NULL,
    DonorIdCount int NOT NULL);
CREATE TABLE #Reads (
    DonorInputId bigint NOT NULL,
    DonorId int NOT NULL,
    AllowedLociKey nvarchar(20) NOT NULL);
'@
    $command.ExecuteNonQuery() | Out-Null

    $donorInputId = 0L
    $uncoveredInputs = 0L
    foreach ($file in $files) {
        $record = Get-Content -Raw -Path $file.FullName | ConvertFrom-Json

        $allowedLociKey = $record.AllowedLociKey
        if (-not $allowedLociKey) { $allowedLociKey = $allowedLociKeys[$record.AllowedLoci] }
        if (-not $allowedLociKey) {
            # Not one of the four precomputed combinations: never read from the stored rows, so never a cache candidate.
            $uncoveredInputs += @($record.DonorInputs).Count
            continue
        }

        $inputs = New-Object System.Data.DataTable
        [void]$inputs.Columns.Add('DonorInputId', [long])
        [void]$inputs.Columns.Add('RecordedAtUtc', [DateTimeOffset])
        [void]$inputs.Columns.Add('SearchRequestId', [string])
        [void]$inputs.Columns.Add('UsePrecomputedGenotypeSets', [bool])
        [void]$inputs.Columns.Add('AllowedLociKey', [string])
        [void]$inputs.Columns.Add('DonorIdCount', [int])

        $reads = New-Object System.Data.DataTable
        [void]$reads.Columns.Add('DonorInputId', [long])
        [void]$reads.Columns.Add('DonorId', [int])
        [void]$reads.Columns.Add('AllowedLociKey', [string])

        # Windows PowerShell keeps the date as text; PowerShell 7 turns it into a DateTime.
        $recordedAt = if ($record.RecordedAtUtc -is [DateTime]) {
            [DateTimeOffset]::new($record.RecordedAtUtc.ToUniversalTime())
        }
        else {
            [DateTimeOffset]::Parse($record.RecordedAtUtc, [Globalization.CultureInfo]::InvariantCulture)
        }
        foreach ($donorIds in $record.DonorInputs) {
            $donorInputId++
            $ids = @($donorIds)
            [void]$inputs.Rows.Add($donorInputId, $recordedAt, $record.SearchRequestId, [bool]$record.UsePrecomputedGenotypeSets, $allowedLociKey, $ids.Count)
            foreach ($donorId in $ids) {
                [void]$reads.Rows.Add($donorInputId, [int]$donorId, $allowedLociKey)
            }
        }

        foreach ($pair in @(@('#DonorInputs', $inputs), @('#Reads', $reads))) {
            $bulkCopy = New-Object System.Data.SqlClient.SqlBulkCopy($transient)
            $bulkCopy.DestinationTableName = $pair[0]
            $bulkCopy.BulkCopyTimeout = $CommandTimeoutSeconds
            $bulkCopy.WriteToServer($pair[1])
        }
    }
    Write-Host "Loaded $donorInputId donor inputs; skipped $uncoveredInputs donor inputs with uncovered loci."

    # 4. One value per donor input: the donor ids of one input share a typing, so they point at the same value.
    #    Value id 0 is the "not stored by design" sentinel (ATL-478).
    $command.CommandText = @'
CREATE INDEX IX_Reads ON #Reads (DonorId, AllowedLociKey);

WITH InputValues AS (
    SELECT r.DonorInputId, MAX(a.SubjectGenotypeSetValueId) AS ValueId
    FROM #Reads r
    LEFT JOIN DonorSubjectGenotypeSets a
        ON a.DonorId = r.DonorId
       AND a.AllowedLociKey = r.AllowedLociKey
    GROUP BY r.DonorInputId
)
SELECT
    i.RecordedAtUtc,
    i.SearchRequestId,
    i.UsePrecomputedGenotypeSets,
    i.AllowedLociKey,
    i.DonorIdCount,
    CASE WHEN iv.ValueId IS NULL THEN 'NoRow' WHEN iv.ValueId = 0 THEN 'NotStored' ELSE 'Stored' END AS Status,
    CASE WHEN v.Id IS NULL THEN '' ELSE CONVERT(varchar(64), HASHBYTES('SHA2_256',
        CONCAT(v.HlaTypingKey, '|', v.HaplotypeFrequencySetId, '|', v.AllowedLociKey)), 2) END AS ValueKeyHash,
    ISNULL(DATALENGTH(v.SubjectGenotypeSetData), 0) AS SizeBytes
FROM #DonorInputs i
INNER JOIN InputValues iv ON iv.DonorInputId = i.DonorInputId
LEFT JOIN SubjectGenotypeSetValues v
    ON iv.ValueId > 0
   AND v.Id = iv.ValueId
   AND v.Bucket = CAST(iv.ValueId % 16 AS tinyint)
ORDER BY i.RecordedAtUtc, i.DonorInputId;
'@

    # 5. Stream the rows to the CSV.
    $reader = $command.ExecuteReader()
    $writer = New-Object System.IO.StreamWriter($OutFile, $false, (New-Object System.Text.UTF8Encoding($false)))
    try {
        $writer.WriteLine('RefreshRecordId,RecordedAtUtc,SearchRequestId,UsePrecomputedGenotypeSets,AllowedLociKey,DonorIdCount,Status,ValueKeyHash,SizeBytes')
        $counts = @{ Stored = 0L; NotStored = 0L; NoRow = 0L }
        while ($reader.Read()) {
            $status = $reader.GetString(5)
            $counts[$status]++
            $writer.WriteLine(('{0},{1},{2},{3},{4},{5},{6},{7},{8}' -f
                $RefreshRecordId,
                $reader.GetDateTimeOffset(0).ToString('o'),
                $reader.GetString(1),
                [int]$reader.GetBoolean(2),
                $reader.GetString(3),
                $reader.GetInt32(4),
                $status,
                $reader.GetString(6),
                $reader.GetValue(7)))
        }
    }
    finally {
        $writer.Dispose()
        $reader.Close()
    }

    Write-Host ("Wrote {0}: Stored {1}, NotStored {2}, NoRow {3}." -f $OutFile, $counts.Stored, $counts.NotStored, $counts.NoRow)
}
finally {
    $transient.Dispose()
}
