# Donor Import Manual Test Cases

## <u>Positive Scenarious</u>

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
            "updateMode": "Diff",
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
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
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
|3. Delete imported donors from DB.<br>Upload donor import file in deletion mode (example in test data section) with `recordIds` uploaded in step 1|Donors uploaded in step 1 deleted from DB. |
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
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
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
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`(<b>you should use recordIds of donors that exist in the db</b>),`"changeType"` should be `"D"` (Delete), donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint, use `string[]` array in it's body with donor record ids. Use in body `recordIds` used in Donor import file from step 1 | - Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to `0`.<br> - `"absent"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.|
---

<br></br>

### 3. Donor Import [`Create or Update`] when some donors in import file should be created and some of them shoul be updated (they exist in DB)

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
            "updateMode": "Diff",
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
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
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
| 3. Run donor import function debug donor endpoint (look at test data section), use `string[]` array in it's body with donor record ids. Use in body `recordIds` used in Donor import file from step 2 | - Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.<br> - Donors metadata and hla data in respose equals to donors metadata and hla data in Donor import file used in step 2. |
|4. Check updated donors HLA data in `<env>atlas` DB:<br> - `ExternalDonorCode` from step 1 and find donors.<br> - Compare their HLA data in DB to HLA data in uploaded Donor Import file in step 2. | Updated donors HLA data in DB mathes HLA data used in Donor Import file from step 2. |
|5. Delete imported donors from DB.<br>Upload donor import file in deletion mode (example in test data section) with `recordIds` uploaded in step 1|Donors uploaded in step 1 deleted from DB|
---
<br>

## <u>Negative Scenarious</u>

### 4. Donor Import [`Create`] when some donors in import file are already exist in db

<b>Description:</b> Check that donor import process is't terminated in case when one of donors in import file already exist in DB, import of this donor should be terminated, error should be logged, but all other donors in import file should be processed and added to Atalas db

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
            "updateMode": "Diff",
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
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
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
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`(<b>one of donors should exist in DB, rest should be unique</b>),`"changeType"` should be `"N"` (Create), donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint (look at test data section), use `string[]` array in it's body with donor record ids. Use in body `recordIds` used in Donor import file from step 1 | - Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1.<br> - Donors metadata and hla data in respose equals to donors metadata and hla data in Donor import file used in step 1. |
|3. Check Azure logs in `<ENV>-ATLAS`:<br>You could use `"FaildeDonorIds"` keyword to find them| There are logs in `<ENV>-ATLAS` where you could find info about failed and imported donors|
|4. Delete imported donors from DB.<br>Upload donor import file in deletion mode (example in test data section) with `recordIds` uploaded in step 1|Donors uploaded in step 1 deleted from DB|
---
<br>

### 5. Donor Import [`Delete`] when some donors in import file are already deleted from db

<b>Description:</b> Check that donor import process is't terminated in case when one of donors in import file already deleted from DB, import of this donor should be terminated, error should be logged, but all other donors in import file should be processed and deleted from Atalas db

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
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231707",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
      "hla": null
    },
    {
      "recordId": "3003231706",
      "changeType": "D",
      "donorType": "D",
      "donPool": null,
      "ethn": null,
      "updateMode": "Diff",
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
| 1. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`(<b>one of donors should be deleted from DB, rest should exist in DB</b>),`"changeType"` should be `"D"` (Delete), donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 2. Run donor import function debug donor endpoint (look at test data section), use `string[]` array in it's body with donor record ids. Use in body `recordIds` used in Donor import file from step 1 | Response code: 200 <br> - `"present"` value in `"donorCounts"` object of response equals to `0`.<br> - `"absent"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 1. |
---