# Donor Import Manual Test Cases

## <u>Positive Scenarious</u>

### 1. Donor Search Request (10/10 - no mismatch, batching: off)

<b>Description:</b> Check Search request results in `<env>atlasstorage`

<b>Preconditions:</b><br>Search results shouldn't be batched:<br>
`<ENV>-ATLAS-MATCHING-ALGORITHM-FUNCTIONS` AzureStorage:SearchResultsBatchSize: `0`<br>
`<ENV>-ATLAS-FUNCTIONS` AtlasFunction:AzureStorage:ShouldBatchResuls: `False`

<b>Test Data:</b>

- Search request

```
POST https://<env>-atlas-api.azurewebsites.net/api/Search
```
Request body example
```json
{
    "SearchDonorType": 1,
    "MatchCriteria": {
        "DonorMismatchCount": 0,
        "LocusMismatchCriteria": {
            "A": 0,
            "B": 0,
            "C": 0,
            "Dpb1": null,
            "Dqb1": 0,
            "Drb1": 0
        },
        "includeBetterMatches": true
    },
    "ScoringCriteria": {
        "LociToScore": [],
        "LociToExcludeFromAggregateScore": []   
    },
    "PatientEthnicityCode": null,
    "PatientRegistryCode": null,
    "runMatchPrediction": false,
    "SearchHlaData":{
        "A": {
            "Position1": "*01:DXGWF",
            "Position2": "*01:DXGWF"
        },
        "B": {
            "Position1": "*07:DXFTK",
            "Position2": "*07:DXFTK"
        },
        "C": {
            "Position1": "*05:DUVRN",
            "Position2": "*07:BRXNC"
        },
        "DPB1": {
            "Position1": "*03:FYKD",
            "Position2": "*04:BYVXE"
        },        
        "DQB1": {
            "Position1": "*03:BMSUA",
            "Position2": "*06:BSBZX"
        },
        "DRB1": {
            "Position1": "*15:01:01",
            "Position2": "*12:JV"
        }
    }
}
```

| Test step | Expected Result |
| --------- | --------------- |
| 1. Run Search Request, but set in request body:<br> - `"DonorMismatchCount"` value to `0`<br> - All HLA mismatch values in `"LocusMismatchCriteria"` should be `0` | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list and Search Request data. |
| 3. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some donors in `Results` list and Search Request data. |
---

<br></br>

### 2. Donor Search Request (9/10 - 1 mismatch, batching: off)

<b>Description:</b> Check Search request results in `<env>atlasstorage`

<b>Preconditions:</b><br>Search results shouldn't be batched:<br>
`<ENV>-ATLAS-MATCHING-ALGORITHM-FUNCTIONS` AzureStorage:SearchResultsBatchSize: `0`<br>
`<ENV>-ATLAS-FUNCTIONS` AtlasFunction:AzureStorage:ShouldBatchResuls: `False`

<b>Test Data:</b>

- Search request

```
POST https://<env>-atlas-api.azurewebsites.net/api/Search
```
Request body example
```json
{
    "SearchDonorType": 1,
    "MatchCriteria": {
        "DonorMismatchCount": 0,
        "LocusMismatchCriteria": {
            "A": 0,
            "B": 0,
            "C": 0,
            "Dpb1": null,
            "Dqb1": 0,
            "Drb1": 0
        },
        "includeBetterMatches": true
    },
    "ScoringCriteria": {
        "LociToScore": [],
        "LociToExcludeFromAggregateScore": []   
    },
    "PatientEthnicityCode": null,
    "PatientRegistryCode": null,
    "runMatchPrediction": false,
    "SearchHlaData":{
        "A": {
            "Position1": "*01:DXGWF",
            "Position2": "*01:DXGWF"
        },
        "B": {
            "Position1": "*07:DXFTK",
            "Position2": "*07:DXFTK"
        },
        "C": {
            "Position1": "*05:DUVRN",
            "Position2": "*07:BRXNC"
        },
        "DPB1": {
            "Position1": "*03:FYKD",
            "Position2": "*04:BYVXE"
        },        
        "DQB1": {
            "Position1": "*03:BMSUA",
            "Position2": "*06:BSBZX"
        },
        "DRB1": {
            "Position1": "*15:01:01",
            "Position2": "*12:JV"
        }
    }
}
```

| Test step | Expected Result |
| --------- | --------------- |
| 1. Run Search Request, but set in request body:<br> - `"DonorMismatchCount"` value to `1`<br> - One HLA mismatch value in `"LocusMismatchCriteria"` should be `1`(e.g. `"A"` or `"B"`, so mismatch of this HLA type would be allowed)<br> - `"includeBetterMatches"` value should be `false` (not to include 10/10 mathed donors) | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list and Search Request data. |
| 3. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some donors in `Results` list and Search Request data.<br> - Donors in results have mismatch corresponding to parameters of Search Request used in step 1 (in context of HLA type and mismatch quantity) |
---

<br></br>

### 3. Donor Search Request (8/10 - 2 mismatches, batching: off)

<b>Description:</b> Check Search request results in `<env>atlasstorage`

<b>Preconditions:</b><br>Search results shouldn't be batched:<br>
`<ENV>-ATLAS-MATCHING-ALGORITHM-FUNCTIONS` AzureStorage:SearchResultsBatchSize: `0`<br>
`<ENV>-ATLAS-FUNCTIONS` AtlasFunction:AzureStorage:ShouldBatchResuls: `False`

<b>Test Data:</b>

- Search request

```
POST https://<env>-atlas-api.azurewebsites.net/api/Search
```
Request body example
```json
{
    "SearchDonorType": 1,
    "MatchCriteria": {
        "DonorMismatchCount": 0,
        "LocusMismatchCriteria": {
            "A": 0,
            "B": 0,
            "C": 0,
            "Dpb1": null,
            "Dqb1": 0,
            "Drb1": 0
        },
        "includeBetterMatches": true
    },
    "ScoringCriteria": {
        "LociToScore": [],
        "LociToExcludeFromAggregateScore": []   
    },
    "PatientEthnicityCode": null,
    "PatientRegistryCode": null,
    "runMatchPrediction": false,
    "SearchHlaData":{
        "A": {
            "Position1": "*01:DXGWF",
            "Position2": "*01:DXGWF"
        },
        "B": {
            "Position1": "*07:DXFTK",
            "Position2": "*07:DXFTK"
        },
        "C": {
            "Position1": "*05:DUVRN",
            "Position2": "*07:BRXNC"
        },
        "DPB1": {
            "Position1": "*03:FYKD",
            "Position2": "*04:BYVXE"
        },        
        "DQB1": {
            "Position1": "*03:BMSUA",
            "Position2": "*06:BSBZX"
        },
        "DRB1": {
            "Position1": "*15:01:01",
            "Position2": "*12:JV"
        }
    }
}
```

| Test step | Expected Result |
| --------- | --------------- |
| 1. Run Search Request, but set in request body:<br> - `"DonorMismatchCount"` value to `2`<br> - Two HLA types mismatch values in `"LocusMismatchCriteria"` should be `1`(e.g. `"A"` and `"B"`, so mismatches of this HLA types would be allowed)<br> - `"includeBetterMatches"` value should be `false` (not to include 10/10 or 9/10 mathed donors) | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list and Search Request data. |
| 3. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some donors in `Results` list and Search Request data.<br> - Donors in results have mismatch corresponding to parameters of Search Request used in step 1 (in context of HLA type and mismatch quantity) |
---

<br></br>

### 4. Donor Search Request (batching: on)

<b>Description:</b> Check Search request results in `<env>atlasstorage`

<b>Preconditions:</b><br>Search results shouldn't be batched:<br>
`<ENV>-ATLAS-MATCHING-ALGORITHM-FUNCTIONS` AzureStorage:SearchResultsBatchSize: `3`<br>
`<ENV>-ATLAS-FUNCTIONS` AtlasFunction:AzureStorage:ShouldBatchResuls: `True`

<b>Test Data:</b>

- Search request

```
POST https://<env>-atlas-api.azurewebsites.net/api/Search
```
Request body example
```json
{
    "SearchDonorType": 1,
    "MatchCriteria": {
        "DonorMismatchCount": 0,
        "LocusMismatchCriteria": {
            "A": 0,
            "B": 0,
            "C": 0,
            "Dpb1": null,
            "Dqb1": 0,
            "Drb1": 0
        },
        "includeBetterMatches": true
    },
    "ScoringCriteria": {
        "LociToScore": [],
        "LociToExcludeFromAggregateScore": []   
    },
    "PatientEthnicityCode": null,
    "PatientRegistryCode": null,
    "runMatchPrediction": false,
    "SearchHlaData":{
        "A": {
            "Position1": "*01:DXGWF",
            "Position2": "*01:DXGWF"
        },
        "B": {
            "Position1": "*07:DXFTK",
            "Position2": "*07:DXFTK"
        },
        "C": {
            "Position1": "*05:DUVRN",
            "Position2": "*07:BRXNC"
        },
        "DPB1": {
            "Position1": "*03:FYKD",
            "Position2": "*04:BYVXE"
        },        
        "DQB1": {
            "Position1": "*03:BMSUA",
            "Position2": "*06:BSBZX"
        },
        "DRB1": {
            "Position1": "*15:01:01",
            "Position2": "*12:JV"
        }
    }
}
```

| Test step | Expected Result |
| --------- | --------------- |
| 1. Run Search Request | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json search result file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file doesn't contain `Results` list but contain Search Request data.<br> - There is a folder with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.|
| 3. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some donors in `Results` list and Search Request data. |
---

<br></br>

### 5. Donor Search Request (match prediction: on)

<b>Description:</b> Check Search request results in `<env>atlasstorage`

<b>Preconditions:</b><br>Search results shouldn't be batched:<br>
`<ENV>-ATLAS-MATCHING-ALGORITHM-FUNCTIONS` AzureStorage:SearchResultsBatchSize: `0`<br>
`<ENV>-ATLAS-FUNCTIONS` AtlasFunction:AzureStorage:ShouldBatchResuls: `False`

<b>Test Data:</b>

- Search request

```
POST https://<env>-atlas-api.azurewebsites.net/api/Search
```
Request body example
```json
{
    "SearchDonorType": 1,
    "MatchCriteria": {
        "DonorMismatchCount": 0,
        "LocusMismatchCriteria": {
            "A": 0,
            "B": 0,
            "C": 0,
            "Dpb1": null,
            "Dqb1": 0,
            "Drb1": 0
        },
        "includeBetterMatches": true
    },
    "ScoringCriteria": {
        "LociToScore": [],
        "LociToExcludeFromAggregateScore": []   
    },
    "PatientEthnicityCode": null,
    "PatientRegistryCode": null,
    "runMatchPrediction": false,
    "SearchHlaData":{
        "A": {
            "Position1": "*01:DXGWF",
            "Position2": "*01:DXGWF"
        },
        "B": {
            "Position1": "*07:DXFTK",
            "Position2": "*07:DXFTK"
        },
        "C": {
            "Position1": "*05:DUVRN",
            "Position2": "*07:BRXNC"
        },
        "DPB1": {
            "Position1": "*03:FYKD",
            "Position2": "*04:BYVXE"
        },        
        "DQB1": {
            "Position1": "*03:BMSUA",
            "Position2": "*06:BSBZX"
        },
        "DRB1": {
            "Position1": "*15:01:01",
            "Position2": "*12:JV"
        }
    }
}
```

| Test step | Expected Result |
| --------- | --------------- |
| 1. Run Search Request:<br> - Set `"RunMatchPrediction"` value to `true` in Search Request body | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json search result file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file doesn't contain `Results` list but contain Search Request data.<br> - There is a folder with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.|
| 3. Check Match prediction results in `<env>atlasstorage`: `match-prediction-results` blob container (use `searchIdentifier` from step 1) | - There is a folder with name equal to `searchIdentifier` from step 1 in `match-prediction-results`.<br> - Match prediction folder contains set of files, where each file corresponds to single donor match prediction in Match prediction folder.<br> - Number of match prediction files equals to amount of donors in Search request results |
---

<br></br>

### 6. Cord Search Request (10/10 - no mismatch, batching: off)

<b>Description:</b> Check Search request results in `<env>atlasstorage`

<b>Preconditions:</b><br>Search results shouldn't be batched:<br>
`<ENV>-ATLAS-MATCHING-ALGORITHM-FUNCTIONS` AzureStorage:SearchResultsBatchSize: `0`<br>
`<ENV>-ATLAS-FUNCTIONS` AtlasFunction:AzureStorage:ShouldBatchResuls: `False`

<b>Test Data:</b>

- Search request

```
POST https://<env>-atlas-api.azurewebsites.net/api/Search
```
Request body example
```json
{
    "SearchDonorType": 2,
    "MatchCriteria": {
        "DonorMismatchCount": 0,
        "LocusMismatchCriteria": {
            "A": 0,
            "B": 0,
            "C": 0,
            "Dpb1": null,
            "Dqb1": 0,
            "Drb1": 0
        },
        "includeBetterMatches": true
    },
    "ScoringCriteria": {
        "LociToScore": [],
        "LociToExcludeFromAggregateScore": []   
    },
    "PatientEthnicityCode": null,
    "PatientRegistryCode": null,
    "runMatchPrediction": false,
    "SearchHlaData":{
        "A": {
            "Position1": "*01:DXGWF",
            "Position2": "*01:DXGWF"
        },
        "B": {
            "Position1": "*07:DXFTK",
            "Position2": "*07:DXFTK"
        },
        "C": {
            "Position1": "*05:DUVRN",
            "Position2": "*07:BRXNC"
        },
        "DPB1": {
            "Position1": "*03:FYKD",
            "Position2": "*04:BYVXE"
        },        
        "DQB1": {
            "Position1": "*03:BMSUA",
            "Position2": "*06:BSBZX"
        },
        "DRB1": {
            "Position1": "*15:01:01",
            "Position2": "*12:JV"
        }
    }
}
```

| Test step | Expected Result |
| --------- | --------------- |
| 1. Run Search Request, but set in request body:<br> - `"DonorMismatchCount"` value to `0`<br> - All HLA mismatch values in `"LocusMismatchCriteria"` should be `0` | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list and Search Request data. |
| 3. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some cord-donors in `Results` list and Search Request data. |
---

<br></br>

### 6. Cord Search Request (8/10 - no mismatch, batching: off)

<b>Description:</b> Check Search request results in `<env>atlasstorage`

<b>Preconditions:</b><br>Search results shouldn't be batched:<br>
`<ENV>-ATLAS-MATCHING-ALGORITHM-FUNCTIONS` AzureStorage:SearchResultsBatchSize: `0`<br>
`<ENV>-ATLAS-FUNCTIONS` AtlasFunction:AzureStorage:ShouldBatchResuls: `False`

<b>Test Data:</b>

- Search request

```
POST https://<env>-atlas-api.azurewebsites.net/api/Search
```
Request body example
```json
{
    "SearchDonorType": 2,
    "MatchCriteria": {
        "DonorMismatchCount": 2,
        "LocusMismatchCriteria": {
            "A": 1,
            "B": 1,
            "C": 0,
            "Dpb1": null,
            "Dqb1": 0,
            "Drb1": 0
        },
        "includeBetterMatches": true
    },
    "ScoringCriteria": {
        "LociToScore": [],
        "LociToExcludeFromAggregateScore": []   
    },
    "PatientEthnicityCode": null,
    "PatientRegistryCode": null,
    "runMatchPrediction": false,
    "SearchHlaData":{
        "A": {
            "Position1": "*01:DXGWF",
            "Position2": "*01:DXGWF"
        },
        "B": {
            "Position1": "*07:DXFTK",
            "Position2": "*07:DXFTK"
        },
        "C": {
            "Position1": "*05:DUVRN",
            "Position2": "*07:BRXNC"
        },
        "DPB1": {
            "Position1": "*03:FYKD",
            "Position2": "*04:BYVXE"
        },        
        "DQB1": {
            "Position1": "*03:BMSUA",
            "Position2": "*06:BSBZX"
        },
        "DRB1": {
            "Position1": "*15:01:01",
            "Position2": "*12:JV"
        }
    }
}
```

| Test step | Expected Result |
| --------- | --------------- |
| 1. Run Search Request, but set in request body:<br> - `"DonorMismatchCount"` value to `0`<br> - All HLA mismatch values in `"LocusMismatchCriteria"` should be `0` | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list and Search Request data. |
| 3. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some cord-donors in `Results` list and Search Request data. |
---

<br></br>