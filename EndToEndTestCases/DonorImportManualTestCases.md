# Donor Import Manual Test Cases

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

- Donor Import file content example (deletion mode), should be used in step 3 for added in this test donors deletion

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
| 2. Run donor import function debug donor endpoint (look at test data section), use `string[]` array in it's body with donor record ids. Use in body `recordIds` used in Donor import file from step 1 | - Response code: 200 <br>`"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.<br> - Donors metadata and hla data in respose equals to donors metadata and hla data in Donor import file used in step 1. |
|3. [**Clean Up**] Delete imported donors from DB.<br>Upload donor import file in deletion mode (example in test data section) with `recordIds` uploaded in step 1|Donors uploaded in step 1 deleted from DB. |
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
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`:<br> - Select one or few donors in `<env>atlas` DB, `Donors.Donors` table:<br> - Use it/theirs `ExternalDonorCode` values as `"recordIds"` in import file | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint, use `string[]` array in it's body with donor record ids. Use in body `recordIds` used in Donor import file from step 1 | - Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to `0`.<br> - `"absent"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.|
---

<br></br>

### 3. Donor Import [`Create or Update`] when some donors in import file should be created and some of them shoud be updated (they exist in DB)

<b>Description:</b> Check donor import process in case of using `"NU"`(new or update) `"changeType"` in donor import file and some donors which exist in DB (update them) and doesn't exist (create them).

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
| 1. Select one or few donors in `<env>atlas` DB, `Donors.Donors` table:<br> - Note it/theirs `ExternalDonorCode` values and HLA data | - `ExternalDonorCode` of one or few donors noted.<br> - HLA data for this donor/donors noted |
| 2. Upload Donor Import .json file with some donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`:<br> - One or few of donors should exist in DB, you should use `ExternalDonorCode` noted in step 1 as `"recordId"` for this donor/donors (<b>HLA data in Donor Import file for this donors should be different from noted in step 1</b> - to check that HLA data in DB could be updated)<br> - Rest few donors and their `"recordId"` should be unique.<br> - `"changeType"` should be `"NU"` (Create or Update), donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 3. Run donor import function debug donor endpoint (look at test data section), use `string[]` array in it's body with donor record ids. Use in body `recordIds` used in Donor import file from step 2 | - Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.<br> - Donors metadata and hla data in response equals to donors metadata and hla data in Donor import file used in step 2. |
|4. [**Clean Up**] Delete imported donors from DB.<br>Upload donor import file in deletion mode (example in test data section) with `recordIds` uploaded in step 1|Donors uploaded in step 1 deleted from DB|
---
<br>

## <u>Negative Scenarios</u>

### 4. Donor Import [`Create`] when some donors in import file are already exist in db

<b>Description:</b> Check that donor import process isn't terminated in case when one of donors in import file already exist in DB, import of this donor should be terminated, error should be logged, but all other donors in import file should be processed and added to Atalas db

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

- Donor import function debug donor endpoint request

```
POST https://<env>-atlas-donor-import-function.azurewebsites.net/api/debug/donors
```
Request body: `["recordId_value1","recordId_value2"]`

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`:<br> - Select one donor in `<env>atlas` DB, `Donors.Donors` table:<br> - Use it `ExternalDonorCode` values as `"recordIds"` in import file.<br><br>`"changeType"` should be `"N"` (Create), donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint (look at test data section), use `string[]` array in it's body with donor record ids. Use in body `recordIds` used in Donor import file from step 1 | - Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.<br> - Donors metadata and hla data in respose equals to donors metadata and hla data in Donor import file used in step 1. |
|3. Go to Atlas Application Insights Logs ("`<ENV>`-ATLAS") within Azure web portal and check donor import logs by next query:<br>`traces`<br>`\| where message contains "Failed to import"`| There are logs in `<ENV>-ATLAS` where you could find info about failed and imported donors|
|4. Check the `alerts` topic on `<env>-atlas` service bus | There should **not** be an alert message for this import file |
|5. [**Clean Up**] Delete imported donors from DB.<br>Upload donor import file in deletion mode (example in test data section) with `recordIds` uploaded in step 1|Donors uploaded in step 1 deleted from DB|
---
<br>

### 5. Donor Import [`Delete`] when some donors in import file are already deleted from db

<b>Description:</b> Check that donor import process isn't terminated in case when one of donors in import file already deleted from DB, import of this donor should be terminated, error should be logged, but all other donors in import file should be processed and deleted from Atalas db

<b>Preconditions:</b> There are some donors imported into Atlas db

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

- Donor import function debug donor endpoint request

```
POST https://<env>-atlas-donor-import-function.azurewebsites.net/api/debug/donors
```
Request body: `["recordId_value1","recordId_value2"]`

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`(<b>one of donors should be deleted from DB, rest should exist in DB</b>):<br> - Select one or few donors in `<env>atlas` DB, `Donors.Donors` table:<br> - Use it/theirs `ExternalDonorCode` values as `"recordIds"` in import file.<br><br>`"changeType"` should be `"D"` (Delete), donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint (look at test data section), use `string[]` array in it's body with donor record ids. Use in body `recordIds` used in Donor import file from step 1 | Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to `0`.<br> - `"absent"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1. |
|3. Go to Atlas Application Insights Logs ("`<ENV>`-ATLAS") within Azure web portal and check donor import logs by next query:<br>`traces`<br>`\| where message contains "Failed to import"`| There are logs in `<ENV>-ATLAS` where you could find info about failed and imported donors|
|4. Check the `alerts` topic on `<env>-atlas` service bus | There should **not** be an alert message for this import file |
---