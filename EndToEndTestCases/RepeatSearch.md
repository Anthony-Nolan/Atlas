# Donor Repeat Search Test Cases

## <u>Positive Scenarios</u>

### 1. Repeat Search Request for newly matched donors (10/10 - no mismatch, batching: off)

<b>Description:</b> Check Repeat Search request results in `<env>atlasstorage`

<b>Preconditions:</b><br>Repeat Search results shouldn't be batched:<br>
`<ENV>-ATLAS-REPEAT-SEARCH-FUNCTIONS` AzureStorage:SearchResultsBatchSize: `0`<br>
`<ENV>-ATLAS-FUNCTIONS` AtlasFunction:AzureStorage:ShouldBatchResults: `False`

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

<br>

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

- Donor import function debug donor endpoint request

```
POST https://<env>-atlas-donor-import-function.azurewebsites.net/api/debug/donors
```
Request body: `["recordId_value1","recordId_value2"]`

<br>

- Repeat Search request

```
POST https://<env>-atlas-api.azurewebsites.net/api/RepeatSearch
```
Request body example
```json
{
    "OriginalSearchId" : "ec58a9f6-cdd4-482c-bc20-3a8269f65855",
    "SearchCutoffDate" : "2021-03-09",
    "SearchRequest": {
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
            }
        },
        "ScoringCriteria": {
            "LociToScore": ["A", "B", "C", "DQB1", "DRB1", "DPB1"],
            "LociToExcludeFromAggregateScore": []
        },
        "PatientEthnicityCode": null,
        "PatientRegistryCode": null,
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
}
```

| Test step | Expected Result |
| --------- | --------------- |
| 1. Run Search Request, but set in request body:<br> - `"DonorMismatchCount"` value to `0`<br>- Note the `searchIdentifier` from request response | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 2. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`,`"changeType"` should be `"NU"` (Create or Update), donor import file name should be unique. | - Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 3. Run donor import function debug donor endpoint (see **test data** section): in request body, submit `string[]` of donor record ids taken from the `recordId` fields within the Donor import file from step 2. | - Response code: 200 <br>- `"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 2.|
| 4. Run Repeat Search Request, but set in request body:<br> - `"OriginalSearchId"` value to `searchIdentifier` value, noted in step 1. <br> - `"SearchCutoffDate"` value to today's date in format `"YYYY-MM-DD"`(in order to show in results only donors that were imported today).<br> - `SearchRequest` should be copied from the request body of the search submitted in step 1. <br> - Note `repeatSearchIdentifier` from request response | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format.<br> - `repeatSerchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 5. After a few minutes, check the `repeat-search-matching-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br> - `SearchRequest`: matching the original request submitted in step 1,<br> - `SearchRequestId`: matching the ID returned from step 1,<br> - `RepeatSearchRequestId`: matching ID returned from step 4,<br> - `IsRepeatSearch`: `true`,<br> - `WasSuccessful`: `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: repeat-search-matching-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>/<repeat-search-request-id>.json` |
| 6. Check repeat search results in `<env>atlasstorage`: `repeat-matching-algorithm-results` blob container | - There is a folder with name equal to `searchIdentifier` from step 1 in `repeat-search-results`.<br> - There is a .json file with name equal to `repeatSearchIdentifier` from step 4.<br> - This file contains donors imported in step 2 in `Results` list. |
| 7. After a few more minutes, check the `repeat-search-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br>- `SearchRequestId`: matching the ID returned from step 1,<br> - `RepeatSearchRequestId`: matching ID returned from step 4,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: repeat-search-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>/<repeat-search-request-id>.json` |
| 8. Check Repeat Search results in `<env>atlasstorage`: `repeat-search-results` blob container (use `searchIdentifier` from step 1) | - There is a folder with name equal to `searchIdentifier` from step 1 in `repeat-search-results`.<br> - There is a .json file with name equal to `repeatSearchIdentifier` from step 4.<br> - This file contains donors imported in step 2 in `Results` list.|
---

<br></br>

### 2. Repeat Search Request for newly matched donors (10/10 - no mismatch, batching: on)

<b>Description:</b> Check Repeat Search request results in `<env>atlasstorage`

<b>Preconditions:</b><br>Repeat Search results shouldn't be batched:<br>
`<ENV>-ATLAS-REPEAT-SEARCH-FUNCTIONS` AzureStorage:SearchResultsBatchSize: `2`<br>
`<ENV>-ATLAS-FUNCTIONS` AtlasFunction:AzureStorage:ShouldBatchResults: `True`

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

<br>

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

- Donor import function debug donor endpoint request

```
POST https://<env>-atlas-donor-import-function.azurewebsites.net/api/debug/donors
```
Request body: `["recordId_value1","recordId_value2"]`

<br>

- Repeat Search request

```
POST https://<env>-atlas-api.azurewebsites.net/api/RepeatSearch
```
Request body example
```json
{
    "OriginalSearchId" : "ec58a9f6-cdd4-482c-bc20-3a8269f65855",
    "SearchCutoffDate" : "2021-03-09",
    "SearchRequest": {
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
            }
        },
        "ScoringCriteria": {
            "LociToScore": ["A", "B", "C", "DQB1", "DRB1", "DPB1"],
            "LociToExcludeFromAggregateScore": []
        },
        "PatientEthnicityCode": null,
        "PatientRegistryCode": null,
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
}
```

| Test step | Expected Result |
| --------- | --------------- |
| 1. Run Search Request, but set in request body:<br> - `"DonorMismatchCount"` value to `0`<br> - All HLA mismatch values in `"LocusMismatchCriteria"` should be `0`<br> - Note `searchIdentifier` from request response | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format.<br> - `searchIdentifier` from request response noted |
| 2. Upload Donor Import .json file with few donors to `donors` folder of `<env>atlasstorage`, you can use donor import file content example from Test Data section, but you should change `"recordId"`,`"changeType"` should be `"NU"` (Create or Update), donor import file name should be unique. | Donor Import file with few donors uploaded to `donors` folder of `<env>atlasstorage`|
| 3. Run donor import function debug donor endpoint (see **test data** section): in request body, submit `string[]` of donor record ids taken from the `recordId` fields within the Donor import file from step 2. | - Response code: 200 <br>- `"present"` value in `"donorCounts"` object of response equals to quantity of donors in Donor import file used in step 2.|
| 4. Run Repeat Search Request, but set in request body:<br> - `"OriginalSearchId"` value to `searchIdentifier` value, noted in step 1. <br> - `"SearchCutoffDate"` value to today's date in format `"YYYY-MM-DD"`(in order to show in results only donors that were imported today).<br> - `SearchRequest` should be copied from the request body of the search submitted in step 1. <br> - Note `repeatSearchIdentifier` from request response | - Result code: 200.<br> - `searchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format.<br> - `repeatSerchIdentifier` in request response: has `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` format. |
| 5. After a few minutes, check the `repeat-search-matching-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br> - `SearchRequest`: matching the original request submitted in step 1,<br> - `SearchRequestId`: matching the ID returned from step 1,<br> - `RepeatSearchRequestId`: matching ID returned from step 4,<br> - `IsRepeatSearch`: `true`,<br> - `WasSuccessful`: `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: repeat-search-matching-results,<br>- `BlobStorageResultsFileName`: should be `<search-request-id>/<repeat-search-request-id>.json`<br> - `ResultsBatched`: true, <br> - `BatchFolderName`: `<search-request-id>/<repeat-search-request-id>` |
| 6. Check repeat search results in `<env>atlasstorage`: `repeat-matching-algorithm-results` blob container | - There is a folder with name equal to `searchIdentifier` from step 1 in `repeat-search-results`.<br> - There is a .json file with name equal to `repeatSearchIdentifier` from step 4.<br> - This file contains donors imported in step 2 in `Results` list. |
| 7. After a few more minutes, check the `repeat-search-results-ready` topic on `<env>-atlas` service bus. | There should be a success search message that includes the following properties:<br>- `SearchRequestId`: matching the ID returned from step 1,<br> - `RepeatSearchRequestId`: matching ID returned from step 4,<br> - `WasSuccessful`: should be `true`,<br>- `NumberOfResults`,<br>- `BlobStorageContainerName`: repeat-search-results,<br>- `BlobStorageResultsFileName`: `<search-request-id>/<repeat-search-request-id>.json`<br> - `ResultsBatched`: true, <br> - `BatchFolderName`: `<search-request-id>/<repeat-search-request-id>` |
| 8. Check Repeat Search results in `<env>atlasstorage`: `repeat-search-results` blob container (use `searchIdentifier` from step 1) | - There is a folder with name equal to `searchIdentifier` from step 1 in `repeat-search-results`.<br> - There is a .json file with name equal to `repeatSearchIdentifier` from step 3.<br> - This file contain only general information related to request (e.g. as `TotalResults` amount) and doesn't contain `Results` list with donors.<br> - There is also a folder with name equal to `repeat-search-results` from step 3 which contains .json files of donors divided to .json files according to `BatchSize` value defined in preconditions, where `BatchSize` value = `max amount` of donors in each .json file in this folder. |
---

<br></br>