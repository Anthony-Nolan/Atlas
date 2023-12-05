# Haplotype Frequency Set Import Test Cases

## <u>Positive Scenarios</u>

### 1. Haplotype Frequency Set Successful Import

<b>Description:</b> Check Haplotype Frequency (HF) set successful import scenario

<b>Test Data:</b>

- Atlas DB `HlaNomenclatureVersion` value request

```sql
SELECT TOP 1 HlaNomenclatureVersion
FROM MatchingAlgorithmPersistent.DataRefreshHistory
WHERE WasSuccessful = 1 ORDER BY Id DESC
```
- App Insights log request
```sql
customEvents
| where operation_Name == "ImportHaplotypeFrequencySet"
| project
    timestamp,
    name,
    fileName = customDimensions["FileName"],
    customDimensions
| order by timestamp desc
```




| Test step | Expected Result |
| --------- | --------------- |
| 1. Run SQL request from <b>Test Data</b> section to `<env>-atlas` db to get HlaNomenclatureVersion value | HlaNomenclatureVersion value noted|
| 2. Download `initial-hf-set.json` file from `<gitRoot>/MiscTestingAndDebuggingResources/MatchPrediction/` | HF set import json file downloaded|
| 3. Edit and save downloaded `initial-hf-set.json` file: <br> - change filename (to make it unique) <br> - replace `"nomenclatureVersion"` value in file with noted one in step 1 | HF set import file edited and saved successfuly |
| 4. Upload HF set import file to `haplotype-frequency-set-import` blob storage of `<env>atlasstorage`| HF set import file uploaded to `haplotype-frequency-set-import` blob storage of `<env>atlasstorage` |
| 5. Download `initial-donors-hf-set-metadata.json` file from `<gitRoot>/MiscTestingAndDebuggingResources/MatchPrediction/`| HF set metadata file downloaded |
| 6. Connect to `<env>-atlas.servicebus` service bus and: <br> - find `haplotype-frequency-file-uploads` topic <br> - open `Send message` window for this topic <br> - paste HF set metadata file content from previous step in `Message Text` field <br> - edit HF set import file filename in `"subject"` and `"url"` values (use filename from step 3) <br> - Click on `Start` button| Message successfuly sent |
| 7. Check `notifications` topic, `audit` subscription | There is a new message with <b>"Summary":</b>`"Haplotype Frequency Set Import Succeeded"` |
| 8. Check logs in App Insights of `<ENV>-ATALS`, use App Insights logs request in <b>Test Data</b> section | There are `Haplotype Frequency Set Import Succeeded` logs in App Insights |
---

<br></br>

## <u>Negative Scenarios</u>

### 2. Haplotype Frequency Set Import Failure

<b>Description:</b> Check Haplotype Frequency (HF) set import failure

<b>Test Data:</b>

- App Insights log request
```sql
customEvents
| where operation_Name == "ImportHaplotypeFrequencySet"
| project
    timestamp,
    name,
    fileName = customDimensions["FileName"],
    customDimensions
| order by timestamp desc
```




| Test step | Expected Result |
| --------- | --------------- |
| 1. Download `initial-hf-set.json` file from `<gitRoot>/MiscTestingAndDebuggingResources/MatchPrediction/` | HF json file downloaded|
| 2. Edit and save downloaded `initial-hf-set.json` file: <br> - change filename (to make it unique) <br> - replace `"nomenclatureVersion"` value in file with unexisting value (e.g. `111111`) | HF set import file edited and saved successfuly |
| 3. Upload HF set import file to `haplotype-frequency-set-import` blob storage of `<env>atlasstorage`| HF set import file uploaded to `haplotype-frequency-set-import` blob storage of `<env>atlasstorage` |
| 4. Download `initial-donors-hf-set-metadata.json` file from `<gitRoot>/MiscTestingAndDebuggingResources/MatchPrediction/`| HF set metadata file downloaded |
| 5. Connect to `<env>-atlas.servicebus` service bus and: <br> - find `haplotype-frequency-file-uploads` topic <br> - open `Send message` window for this topic <br> - paste HF set metadata file content from previous step in `Message Text` field <br> - edit HF set import file filename in `"subject"` and `"url"` values (use filename from step 3) <br> - Click on `Start` button| Message successfuly sent |
| 6. Check `notifications` topic, `audit` subscription | There is no new message with <b>"Summary":</b>`"Haplotype Frequency Import Succeeded"` |
| 7. Check `alerts` topic, `audit` subscription | There is a new messages with <b>"Summary":</b>`"Haplotype Frequency Set Import Failure"` |
| 8. Check logs in App Insights of `<ENV>-ATALS`, use App Insights logs request in <b>Test Data</b> section | - There are no new `Haplotype Frequency Set Import Succeeded` logs in App Insights <br> - There are new `Haplotype Frequency Set Import Failure` logs in App Insights |
---

<br></br>