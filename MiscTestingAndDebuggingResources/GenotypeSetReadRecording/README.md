# Genotype set read recording (ATL-571)

A temporary measurement. It collects the data for the cache hit-rate check in ATL-347: would a Redis cache in front of
the precomputed genotype set read get enough hits?

## What is recorded

When the setting is on, each match prediction search batch writes one JSON blob with:

- the time, the search request id and `MatchingAlgorithmDataRefreshRecordId`
- the loci match prediction ran on, and the allowed-loci key (when the stored rows were read)
- whether the batch used the stored sets (`UsePrecomputedGenotypeSets`) and the batch-wide fallback reason, if any
- the donor ids of each donor input

The batch is recorded on both the stored and the live path. No patient information is written.

The value key is **not** recorded: in 4.1 the search does not build it. `Resolve-GenotypeSetReadRecords.ps1` finds it
later from the donor ids.

Blobs go to the container `genotype-set-read-records` in the match prediction results storage account, named
`{refresh record id}/{date}/{search request id}/{time}-{guid}.json`.

Recording is best effort: it runs in the background, and a failure is logged as the exception event
`Genotype set read recording failed`. It never fails or delays the search.

## Turn it on and off

| Where | Setting |
| --- | --- |
| Terraform / release variable | `MATCH_PREDICTION_RECORD_GENOTYPE_SET_READS` (default `false`) |
| Match prediction worker (container app) | `GenotypeSetReadRecording__Enabled` |
| Atlas Functions app | `MatchPrediction:GenotypeSetReadRecording:Enabled` |

Both apps run search batches, so set both (the Terraform variable does).

## Resolve the records

Run `Resolve-GenotypeSetReadRecords.ps1` once for each refresh record seen in the container, **while that refresh
record is still the active one**. When the next Data Refresh completes, the old database is wiped and the records of
that cycle can no longer be resolved. The script refuses to run for a record that is no longer active.

```powershell
az login
./Resolve-GenotypeSetReadRecords.ps1 -RefreshRecordId <id> -StorageAccountName <account> `
    -SqlServer <server>.database.windows.net -PersistentDatabase <persistent db> `
    -TransientDatabaseA <db A> -TransientDatabaseB <db B> -OutFile ./reads-<id>.csv
```

It needs read access to the storage account and the SQL databases. It reads payload sizes, so run it off-peak on LIVE.

The output has one row per donor input: `RefreshRecordId, RecordedAtUtc, SearchRequestId, UsePrecomputedGenotypeSets,
AllowedLociKey, DonorIdCount, Status, ValueKeyHash, SizeBytes`.

- `Stored`: a value row exists. These are the cache candidates.
- `NoRow`: no assignment row for the donor ids. These are never cache hits. They include the donors that are not stored
  by design (ATL-478, unambiguous typing): after the refresh those donors have no row either, so the script cannot tell
  them apart.

Blobs under `unknown/` come from requests with no refresh record id. They cannot be resolved.

For a local test, `-RecordsFolder <folder>` reads the records from disk instead of downloading them, and
`-IntegratedSecurity` uses a Windows login.

## After the measurement

Turn the setting off. If ATL-347 decides not to build the cache, remove the recording code, this folder, the
Terraform variable and the blob container.
