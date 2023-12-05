# Search Test Cases

## <u>Positive Scenarios</u>

### 1. Adult Search Request (10/10 - no mismatch, batching: off)

<b>Description:</b> Check Search results in `<env>atlasstorage`

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
    "runMatchPrediction": true,
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
| 2. After a few minutes, check the `matching-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br> - `SearchRequest`: matching the original request submitted in step 1,<br> - `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: matching-algorithm-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 3. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list (same number as listed in step 2 result message), and the original Search Request data from step 1. |
| 4. After a few more minutes, check the `search-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br>- `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: atlas-search-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 5. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some donors in `Results` list, and Search Request data. |
---

<br></br>

### 2. Adult Search Request (9/10 - 1 mismatch, batching: off)

<b>Description:</b> Check Search results in `<env>atlasstorage`

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
    "runMatchPrediction": true,
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
| 2. After a few minutes, check the `matching-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br> - `SearchRequest`: matching the original request submitted in step 1,<br> - `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: matching-algorithm-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 3. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list (same number as listed in step 2 result message), and the original Search Request data from step 1. |
| 4. After a few more minutes, check the `search-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br>- `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: atlas-search-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 5. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some donors in `Results` list, and Search Request data. |
---

<br></br>

### 3. Adult Search Request (8/10 - 2 mismatches, batching: off)

<b>Description:</b> Check Search results in `<env>atlasstorage`

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
| 1. Run Search Request, but set in request body:<br> - `"DonorMismatchCount"` value to `2`<br> - Two HLA types mismatch values in `"LocusMismatchCriteria"` should be `2`(e.g. `"A"` and `"B"`, so mismatches of this HLA types would be allowed)<br> - `"includeBetterMatches"` value should be `false` (not to include 10/10 or 9/10 mathed donors) | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. After a few minutes, check the `matching-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br> - `SearchRequest`: matching the original request submitted in step 1,<br> - `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: matching-algorithm-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 3. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list (same number as listed in step 2 result message), and the original Search Request data from step 1. |
| 4. After a few more minutes, check the `search-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br>- `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: atlas-search-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 5. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some donors in `Results` list, and Search Request data. |
---

<br></br>

### 4. Adult Search Request (batching: on)

<b>Description:</b> Check Search results in `<env>atlasstorage`

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
    "runMatchPrediction": true,
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
| 2. After a few minutes, check the `matching-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br> - `SearchRequest`: matching the original request submitted in step 1,<br> - `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: matching-algorithm-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 3. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list (same number as listed in step 2 result message), and the original Search Request data from step 1. |
| 4. After a few more minutes, check the `search-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br>- `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: atlas-search-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 5. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some donors in `Results` list, and Search Request data. |
---

<br></br>

### 5. Cord Search Request (4/8 - no mismatch, batching: off)

<b>Description:</b> Check Search results in `<env>atlasstorage`

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
            "A": 2,
            "B": 2,
            "C": 2,
            "Dpb1": null,
            "Dqb1": null,
            "Drb1": 2
        },
        "includeBetterMatches": true
    },
    "ScoringCriteria": {
        "LociToScore": [],
        "LociToExcludeFromAggregateScore": []   
    },
    "PatientEthnicityCode": null,
    "PatientRegistryCode": null,
    "runMatchPrediction": true,
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
| 1. Run Search Request, but set in request body:<br> - `"DonorMismatchCount"` value to `2`<br> - Mismatch values in `"LocusMismatchCriteria"` for `A`, `B`, `C`, `DRB1` should be `2`; `DQB1` and `DPB1` should be `null` | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. After a few minutes, check the `matching-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br> - `SearchRequest`: matching the original request submitted in step 1,<br> - `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: matching-algorithm-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 3. Check Search results in `<env>atlasstorage`: `matching-algorithm-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `matching-algorithm-results`.<br> - Search results file contains some donors in `Results` list (same number as listed in step 2 result message), and the original Search Request data from step 1. |
| 4. After a few more minutes, check the `search-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br>- `SearchRequestId`: matching the ID returned from step 1,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: atlas-search-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>.json` |
| 5. Check Search results in `<env>atlasstorage`: `atlas-search-results` blob container (use `searchIdentifier` from step 1) | - There is a .json file with name equal to `searchIdentifier` from step 1 in `atlas-search-results`.<br> - Search results file contains some cords in `Results` list, and Search Request data. |
---

<br></br>