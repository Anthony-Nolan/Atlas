# Donor Import Test Cases

## <u>Positive Scenarios</u>

### 1. Donor Import [`Create`]

<b>Description:</b> Check donors import in `"N" (Create)` mode

<b>Test Data:</b>

- Donor Import file content example

```json
{
    "updateMode": "Diff",
    "donors": [
        {
            "recordId": "0404231732",
            "changeType": "N",
            "donorType": "D",
            "donPool": "AN",
            "ethn": null,
            "hla": {
                "a": {
                    "dna": {
                        "field1": "*01:DXGWF",
                        "field2": "*01:DXGWF"
                    }
                },
                "b": {
                    "dna": {
                        "field1": "*07:DXFTK",
                        "field2": "*07:DXFTK"
                    }
                },
                "c": {
                    "dna": {
                        "field1": "*05:DUVRN",
                        "field2": "*07:BRXNC"
                    }
                },
                "dpb1": {
                    "dna": {
                        "field1": "*03:FYKD",
                        "field2": "*04:BYVXE"
                    }
                },
                "dqb1": {
                    "dna": {
                        "field1": "*03:BMSUA",
                        "field2": "*06:BSBZX"
                    }
                },
                "drb1": {
                    "dna": {
                        "field1": "*15:01:01",
                        "field2": "*12:JV"
                    }
                }
            }
        }
    ]
}
```
<br>

- Donor Import file content example (deletion mode), should be used in step 3 for deletion of test donors added during this test

```json
{
  "updateMode": "Diff",
  "donors": [
    {
      "recordId": "0404231732",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    }
  ]
}
```

- Donor import function debug donor endpoint request

```
POST https://<env>-atlas-donor-import-function.azurewebsites.net/api/debug/donors
```
Request body: `["recordId_value1","recordId_value2"]`

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`,`"changeType"` should be `"N"` (Create), donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint (see **test data** section): in request body, submit `string[]` of donor record ids taken from the `recordId` fields within the Donor import file from step 1. | - Response code: 200 <br>- `"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.<br> - Donors metadata and HLA data in response equals to donors metadata and HLA data in Donor import file used in step 1. |
|3. Check the `donor-import-results` topic on `<env>-atlas` service bus | There should be a success donor import message with next properties:<br> - FileName,<br> - WasSuccessful = true,<br> - ImportedDonorCount  = total number of donors in the test file,<br> - FailedDonorCount = 0 |
|4. [**Clean Up**] Delete imported donors from DB.<br>Upload donor import file in deletion mode (example in test data section) with `recordIds` uploaded in step 1|Donors uploaded in step 1 deleted from DB. |
---

<br></br>

### 2. Donor Import [`Delete`]

<b>Description:</b> Check donors import in `"D" (Delete)` mode

<b>Preconditions:</b> There are some donors imported into Atlas db

<b>Test Data:</b>

- Donor Import file content example (deletion mode)

```json
{
  "updateMode": "Diff",
  "donors": [
    {
      "recordId": "0404231732",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    }
  ]
}
```

- Donor import function debug donor endpoint request

```
POST https://<env>-atlas-donor-import-function.azurewebsites.net/api/debug/donors
```
Request body: `["recordId_value1","recordId_value2"]`

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`:<br> - Select one or few donors in `<env>atlas` DB, `Donors.Donors` table:<br> - Use `ExternalDonorCode` values as `"recordId"` in import file | Donor Import file uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint (see **test data** section): in request body, submit `string[]` of donor record ids taken from the `recordId` fields within the Donor import file from step 1. | - Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to `0`.<br> - `"absent"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.|
| 3. Check the `donor-import-results` topic on `<env>-atlas` service bus | There should be a success donor import message with next properties:<br> - FileName,<br> - WasSuccessful = true,<br> - ImportedDonorCount  = total number of donors in the test file,<br> - FailedDonorCount = 0 |
---

<br></br>

### 3. Donor Import [`Create or Update`]

<b>Description:</b> Check donor import process in case of using `"NU"`(new or update) `"changeType"` in donor import file. Test that new donors in the import file are created and existing donors are updated.

<b>Preconditions:</b> There are some donors imported into Atlas db

<b>Test Data:</b>

- Donor Import file content example

```json
{
    "updateMode": "Diff",
    "donors": [
        {
            "recordId": "0404231732",
            "changeType": "NU",
            "donorType": "D",
            "donPool": "AN",
            "ethn": null,
            "hla": {
                "a": {
                    "dna": {
                        "field1": "*01:DXGWF",
                        "field2": "*01:DXGWF"
                    }
                },
                "b": {
                    "dna": {
                        "field1": "*07:DXFTK",
                        "field2": "*07:DXFTK"
                    }
                },
                "c": {
                    "dna": {
                        "field1": "*05:DUVRN",
                        "field2": "*07:BRXNC"
                    }
                },
                "dpb1": {
                    "dna": {
                        "field1": "*03:FYKD",
                        "field2": "*04:BYVXE"
                    }
                },
                "dqb1": {
                    "dna": {
                        "field1": "*03:BMSUA",
                        "field2": "*06:BSBZX"
                    }
                },
                "drb1": {
                    "dna": {
                        "field1": "*15:01:01",
                        "field2": "*12:JV"
                    }
                }
            }
        }
    ]
}
```
<br>

- Donor Import file content example (deletion mode)

```json
{
  "updateMode": "Diff",
  "donors": [
    {
      "recordId": "0404231732",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    }
  ]
}
```

- Donor import function debug donor endpoint request

```
POST https://<env>-atlas-donor-import-function.azurewebsites.net/api/debug/donors
```
Request body: `["recordId_value1","recordId_value2"]`

| Test step | Expected Result |
| --------- | --------------- |
| 1. Select one or few donors in `<env>atlas` DB, `Donors.Donors` table, noting the `ExternalDonorCode` values and HLA data | |
| 2. Upload Donor Import .json file with some donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`:<br> - Existing donors: At least one donor should exist in DB, you should use `ExternalDonorCode` noted in step 1 as `"recordId"` for these updates; however, **HLA data for these donors should be different** from that noted in step 1 in order to check the update is applied to the DB.<br> - New donors: Remaining donor updates in the file should have new, unique `"recordId"`.<br> - `"changeType"` for all updates should be `"NU"` (Create or Update) <br> - Donor import file name should be unique. | Donor Import file uploaded to `donors` folder of `<env>atlasstorage`|
| 3. Run donor import function debug donor endpoint (see **test data** section): in request body, submit `string[]` of donor record ids taken from the `recordId` fields within the Donor import file from step 1. | - Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.<br> - Donors metadata and HLA data in response equals to donors metadata and HLA data in Donor import file used in step 2. |
|4. Check the `donor-import-results` topic on `<env>-atlas` service bus | There should be a success donor import message with next properties:<br> - FileName,<br> - WasSuccessful = true,<br> - ImportedDonorCount  = total number of donors in the test file,<br> - FailedDonorCount = 0 |
|5. [**Clean Up**] Delete imported donors from DB.<br>Upload donor import file in deletion mode (example in test data section) with `recordIds` uploaded in step 1|Donors uploaded in step 1 deleted from DB|
---
<br>

## <u>Negative Scenarios</u>

### 4. Donor Import [`Create`] when donor already exists

<b>Description:</b> Check that donor import process isn't terminated early when one of the donors in the import file already exists in the DB. Instead, the invalid donor update should not be imported, failure should be logged, and all other donors in the import file should be processed and added to Atlas db.

<b>Preconditions:</b> There are some donors imported into Atlas db

<b>Test Data:</b>

- Donor Import file content example

```json
{
    "updateMode": "Diff",
    "donors": [
        {
            "recordId": "0404231732",
            "changeType": "N",
            "donorType": "D",
            "donPool": "AN",
            "ethn": null,
            "hla": {
                "a": {
                    "dna": {
                        "field1": "*01:DXGWF",
                        "field2": "*01:DXGWF"
                    }
                },
                "b": {
                    "dna": {
                        "field1": "*07:DXFTK",
                        "field2": "*07:DXFTK"
                    }
                },
                "c": {
                    "dna": {
                        "field1": "*05:DUVRN",
                        "field2": "*07:BRXNC"
                    }
                },
                "dpb1": {
                    "dna": {
                        "field1": "*03:FYKD",
                        "field2": "*04:BYVXE"
                    }
                },
                "dqb1": {
                    "dna": {
                        "field1": "*03:BMSUA",
                        "field2": "*06:BSBZX"
                    }
                },
                "drb1": {
                    "dna": {
                        "field1": "*15:01:01",
                        "field2": "*12:JV"
                    }
                }
            }
        }
    ]
}
```
<br>

- Donor Import file content example (deletion mode)

```json
{
  "updateMode": "Diff",
  "donors": [
    {
      "recordId": "0404231732",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    }
  ]
}
```
<br>

- SQL request to check added rows in DonorImportFailures table

```sql
SELECT * FROM Donors.DonorImportFailures
WHERE ExternalDonorCode IN (<replace_with_record_id_1_from_import_file>, <replace_with_record_id_2_from_import_file>)
```

- Donor import function debug donor endpoint request

```
POST https://<env>-atlas-donor-import-function.azurewebsites.net/api/debug/donors
```
Request body: `["recordId_value1","recordId_value2"]`

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`:<br> - Select one donor in `<env>atlas` DB, `Donors.Donors` table:<br> - Use it `ExternalDonorCode` values as `"recordId"` in import file.<br>- `"changeType"` should be `"N"` (Create)<br>- Donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint (see **test data** section): in request body, submit `string[]` of donor record ids taken from the `recordId` fields within the Donor import file from step 1. | - Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.<br> - Donors metadata and HLA data in response equals to donors metadata and HLA data in Donor import file used in step 1. |
|3. Go to Atlas Application Insights Logs ("`<ENV>`-ATLAS") within Azure web portal and check donor import logs by next query:<br>`traces`<br>`\| where message contains "Failed to import"`| There are logs in `<ENV>-ATLAS` where you could find info about failed donors|
|4. Check the `donor-import-results` topic on `<env>-atlas` service bus | There should be a success donor import message with next properties:<br> - FileName,<br> - WasSuccessful = true,<br> - ImportedDonorCount  = total number of valid donor updates in the test file,<br> - FailedDonorCount = number of invalid donor updates |
|5. Check the `alerts` topic on `<env>-atlas` service bus | There should **not** be an alert message for this import file |
|6. Check that new rows were added to `Donors.DonorImportFailures` table of `<env>-atlas` db, one row for each failed donor update during import process, using request from <b>Test Data</b> section (add donors recordIds from donor import file to SQL query) | - New rows added to `Donors.DonorImportFailures` table <br> - Rows data corresponds to donor import file data <br> - FailureReason columns are filled with corresponding import failure reason |
|7. [**Clean Up**] Delete imported donors from DB.<br>Upload donor import file in deletion mode (example in test data section) with `recordIds` uploaded in step 1|Donors uploaded in step 1 deleted from DB|
---
<br>

### 5. Donor Import [`Delete`] when donor does not exist

<b>Description:</b> Check that donor import process isn't terminated early when one of donors in import file is absent from DB. The deletion update should not be applied, failure should be logged, and all other valid donor updates in the import file should be applied to Atlas db.

<b>Preconditions:</b> 
- There are some donors imported into Atlas db.
- App setting, `NotificationConfiguration:NotifyOnAttemptedDeletionOfUntrackedDonor`, on `<ENV>-ATLAS-DONOR-IMPORT-FUNCTION` should be set to `true` to test step 6.

<b>Test Data:</b>

- Donor Import file content example

```json
{
  "updateMode": "Diff",
  "donors": [
    {
      "recordId": "0404231732",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "hla": null
    }
  ]
}
```
<br>

- SQL request to check added rows in DonorImportFailures table

```sql
SELECT * FROM Donors.DonorImportFailures
WHERE ExternalDonorCode IN (<replace_with_record_id_1_from_import_file>, <replace_with_record_id_2_from_import_file>)
```

- Donor import function debug donor endpoint request

```
POST https://<env>-atlas-donor-import-function.azurewebsites.net/api/debug/donors
```
Request body: `["recordId_value1","recordId_value2"]`

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`(<b>one of donors should be absent from DB, rest should exist in DB</b>):<br> - Select one or few donors in `<env>atlas` DB, `Donors.Donors` table:<br> - Use `ExternalDonorCode` values as `"recordIds"` in import file.<br><br>`"changeType"` should be `"D"` (Delete), donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint (see **test data** section): in request body, submit `string[]` of donor record ids taken from the `recordId` fields within the Donor import file from step 1. | Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to `0`.<br> - `"absent"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1. |
|3. Go to Atlas Application Insights Logs ("`<ENV>`-ATLAS") within Azure web portal and check donor import logs by next query:<br>`traces`<br>`\| where message contains "Failed to import"`| There are logs in `<ENV>-ATLAS` where you could find info about failed donors|
|4. Check that new rows were added to `Donors.DonorImportFailures` table of `<env>-atlas` db for each donor failed during import process, using request from <b>Test Data</b> section (add donors recordIds from donor import file to SQL query) | - New rows added to `Donors.DonorImportFailures` table <br> - Rows data corresponds to donor import file data <br> - FailureReason columns are filled with corresponding import failure reason |
|5. Check the `alerts` topic on `<env>-atlas` service bus | There should **not** be an alert message for this import file |
|6. Check the `notifications` topic on `<env>-atlas` service bus. | There should be a notification message for this import file stating that Atlas attempted to delete missing donors. |
---