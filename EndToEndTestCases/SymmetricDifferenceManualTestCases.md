# Symmetric Difference Manual Test Cases

## <u>Positive Scenarios</u>

### 1. Atlas set of donors equals to list of donors in submitted file

<b>Description:</b> Check that result of submitted file processing, in case when Atlas set of donors (in DB) equals to list on donors in submitted file, return 0 absent donor records (don't exist in DB) and 0 orphaned donor records (don't exist in submitted file)

<b>Preconditions:</b> To make testing easier in Atlas DB should exist short list of donors in specific "test" registry (e.g. "ST" aka SymmetricTest)

<b>Test Data:</b>

- Donor-id-checker request file content example

```json
{
    "donPool": "ST",
    "donorType": "D",
    "donors":
    [
        "2404231618",
        "2404231619",
        "2404231620",
        "2404231621",
        "2404231622"
    ]
}
```
<br>

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload donor-id-checker request file to `donors/donor-id-checker/requests` directory of `devatlasstorage`.<br> Uploaded file should have <b>unique name</b> and contain:<br> - `"donPool"` value equal to specific registry (e.g. `"AN"` or `"GB"`),<br> - `"donors"` list should contain same set of donor ids as presented in DB for specific registry defined in `"donPool"` parameter. | Donor-id-checker request file uploaded to `donors/donor-id-checker/requests` directory of `devatlasstorage` |
| 2. Check `donor-id-checker-results` topic of `dev-atlas` service bus | - No new deadlettered messages,<br> - There is a new message which contain: `"RequestFileLocation"` value with uploaded file filename, `"ResultsCount"` (should be `0`) and `"ResultsFilename"` (should be empty - `""`) |
---

<br></br>

### 2. Atlas set of donors has missing donor ids comparing to uploaded donor-id-checker request file

<b>Description:</b> Check that result of submitted file processing, in case when Atlas set of donors (in DB) has missing donor ids comparing to uploaded donor-id-checker request file, return X absent donor records (don't exist in DB) and 0 orphaned donor records (don't exist in submitted file)

<b>Preconditions:</b> To make testing easier in Atlas DB should exist short list of donors in specific "test" registry (e.g. "ST" aka SymmetricTest)

<b>Test Data:</b>

- Donor-id-checker request file content example

```json
{
    "donPool": "ST",
    "donorType": "D",
    "donors":
    [
        "2404231618",
        "2404231619",
        "2404231620",
        "2404231621",
        "2404231622"
    ]
}
```
<br>

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload donor-id-checker request file to `donors/donor-id-checker/requests` directory of `devatlasstorage`.<br> Uploaded file should have <b>unique name</b> and contain:<br> - `"donPool"` value equal to specific registry (e.g. `"AN"` or `"GB"`),<br> - `"donors"` list should contain same set of donor ids as presented in DB for specific registry defined in `"donPool"` parameter and <b>have few additional donor ids</b> which absent in DB | Donor-id-checker request file uploaded to `donors/donor-id-checker/requests` directory of `devatlasstorage` |
| 2. Check `donors/donor-id-checker/requests` topic of `dev-atlas` service bus | - No new deadlettered messages,<br> - There is a new message which contain: `"RequestFileLocation"` value with uploaded file filename, `"ResultsCount"` and `"ResultsFilename"` |
| 3. Check processing result file in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - To identify needed file you could use `"ResultsFilename"` from previous step | - Result file presents in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - `"AbsentRecordIds"` list contains donor ids which are missing in DB but present in uploaded donor-id-checker request file |
---

<br></br>

### 3. Uploaded donor-id-checker request file has missing donor ids comparing to Atlas set of donors

<b>Description:</b> Check that result of submitted file processing, in case when uploaded donor-id-checker request file has missing donor ids comparing to Atlas set of donors (in DB), return 0 absent donor records (don't exist in DB) and X orphaned donor records (don't exist in submitted file)

<b>Preconditions:</b> To make testing easier in Atlas DB should exist short list of donors in specific "test" registry (e.g. "ST" aka SymmetricTest)

<b>Test Data:</b>

- Donor-id-checker request file content example

```json
{
    "donPool": "ST",
    "donorType": "D",
    "donors":
    [
        "2404231618",
        "2404231619",
        "2404231620",
        "2404231621",
        "2404231622"
    ]
}
```
<br>

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload donor-id-checker request file to `donors/donor-id-checker/requests` directory of `devatlasstorage`.<br> Uploaded file should have <b>unique name</b> and contain:<br> - `"donPool"` value equal to specific registry (e.g. `"AN"` or `"GB"`),<br> - `"donors"` list should contain same set of donor ids as presented in DB for specific registry defined in `"donPool"` parameter but <b>without few additional donor ids</b> which present in DB | Donor-id-checker request file uploaded to `donors/donor-id-checker/requests` directory of `devatlasstorage` |
| 2. Check `donors/donor-id-checker/requests` topic of `dev-atlas` service bus | - No new deadlettered messages,<br> - There is a new message which contain: `"RequestFileLocation"` value with uploaded file filename, `"ResultsCount"` and `"ResultsFilename"` |
| 3. Check processing result file in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - To identify needed file you could use `"ResultsFilename"` from previous step | - Result file presents in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - `"OrphanedRecordIds"` list contains donor ids which are missing in uploaded donor-id-checker request file but present in DB |
---

<br></br>

### 4. Uploaded donor-id-checker request file has missing donor ids comparing to Atlas set of donors but Atlas set of donors also has missing donor ids comparing to uploaded donor-id-checker request file

<b>Description:</b> Check that result of submitted file processing, in case when uploaded donor-id-checker request file has missing donor ids comparing to Atlas set of donors (in DB) but Atlas set of donors also has missing donor ids comparing to uploaded donor-id-checker request file, return X absent donor records (don't exist in DB) and X orphaned donor records (don't exist in submitted file)

<b>Preconditions:</b> To make testing easier in Atlas DB should exist short list of donors in specific "test" registry (e.g. "ST" aka SymmetricTest)

<b>Test Data:</b>

- Donor-id-checker request file content example

```json
{
    "donPool": "ST",
    "donorType": "D",
    "donors":
    [
        "2404231618",
        "2404231619",
        "2404231620",
        "2404231621",
        "2404231622"
    ]
}
```
<br>

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload donor-id-checker request file to `donors/donor-id-checker/requests` directory of `devatlasstorage`.<br> Uploaded file should have <b>unique name</b> and contain:<br> - `"donPool"` value equal to specific registry (e.g. `"AN"` or `"GB"`),<br> - `"donors"` list should contain same set of donor ids as presented in DB for specific registry defined in `"donPool"` parameter but <b>without few additional donor ids</b> which present in DB, and also it <b>should contain few donor ids which absent in DB</b> | Donor-id-checker request file uploaded to `donors/donor-id-checker/requests` directory of `devatlasstorage` |
| 2. Check `donors/donor-id-checker/requests` topic of `dev-atlas` service bus | - No new deadlettered messages,<br> - There is a new message which contain: `"RequestFileLocation"` value with uploaded file filename, `"ResultsCount"` and `"ResultsFilename"` |
| 3. Check processing result file in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - To identify needed file you could use `"ResultsFilename"` from previous step | - Result file presents in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - `"AbsentRecordIds"` list contains donor ids which are missing in DB but present in uploaded donor-id-checker request file <br> - `"OrphanedRecordIds"` list contains donor ids which are missing in uploaded donor-id-checker request file but present in DB |
---

<br></br>

### 5. Uploaded donor-id-checker request file has empty donor ids list but Atlas set contains donors

<b>Description:</b> Check that result of submitted file processing, in case when uploaded donor-id-checker request file has empty donor ids list but Atlas set contain donors (in DB), return 0 absent donor records (don't exist in DB) and X orphaned donor records (don't exist in submitted file)

<b>Preconditions:</b> To make testing easier in Atlas DB should exist short list of donors in specific "test" registry (e.g. "ST" aka SymmetricTest)

<b>Test Data:</b>

- Donor-id-checker request file content example

```json
{
    "donPool": "ST",
    "donorType": "D",
    "donors":
    [
        "2404231618",
        "2404231619",
        "2404231620",
        "2404231621",
        "2404231622"
    ]
}
```
<br>

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload donor-id-checker request file to `donors/donor-id-checker/requests` directory of `devatlasstorage`.<br> Uploaded file should have <b>unique name</b> and contain:<br> - `"donPool"` value equal to specific registry (e.g. `"AN"` or `"GB"`),<br> - `"donors"` list should be empty | Donor-id-checker request file uploaded to `donors/donor-id-checker/requests` directory of `devatlasstorage` |
| 2. Check `donors/donor-id-checker/requests` topic of `dev-atlas` service bus | - No new deadlettered messages,<br> - There is a new message which contain: `"RequestFileLocation"` value with uploaded file filename, `"ResultsCount"` and `"ResultsFilename"` |
| 3. Check processing result file in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - To identify needed file you could use `"ResultsFilename"` from previous step | - Result file presents in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - `"AbsentRecordIds"` list doen't contain donor ids<br> - `"OrphanedRecordIds"` list contains donor ids which are missing in uploaded donor-id-checker request file but present in DB |
---

<br></br>

### 6. Uploaded donor-id-checker request file has empty donor ids list and Atlas set also doen't contain donors

<b>Description:</b> Check that result of submitted file processing, in case when uploaded donor-id-checker request file has empty donor ids list and Atlas set also doesn't contain donors (in DB), return 0 absent donor records (don't exist in DB) and 0 orphaned donor records (don't exist in submitted file)

<b>Preconditions:</b> There is no donors in DB for registry that will be used in donor-id-checker request file

<b>Test Data:</b>

- Donor-id-checker request file content example

```json
{
    "donPool": "ST",
    "donorType": "D",
    "donors":
    [
        "2404231618",
        "2404231619",
        "2404231620",
        "2404231621",
        "2404231622"
    ]
}
```
<br>

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload donor-id-checker request file to `donors/donor-id-checker/requests` directory of `devatlasstorage`.<br> Uploaded file should have <b>unique name</b> and contain:<br> - `"donPool"` value equal to specific registry (e.g. `"AN"` or `"GB"`),<br> - `"donors"` list should be empty | Donor-id-checker request file uploaded to `donors/donor-id-checker/requests` directory of `devatlasstorage` |
| 2. Check `donor-id-checker-results` topic of `dev-atlas` service bus | - No new deadlettered messages,<br> - There is a new message which contain: `"RequestFileLocation"` value with uploaded file filename, `"ResultsCount"` (should be `0`) and `"ResultsFilename"` (should be empty - `""`) |
---

<br></br>

### 7. Atlas set of donors has missing donor ids comparing to uploaded donor-id-checker request file, donor-id-checker request file contain unrequired "checkingMode" parameter

<b>Description:</b> Check that result of submitted file processing, in case when Atlas set of donors (in DB) has missing donor ids comparing to uploaded donor-id-checker request file and donor-id-checker request file contain unrequired "checkingMode" parameter, return X absent donor records (don't exist in DB) and 0 orphaned donor records (don't exist in submitted file)

<b>Preconditions:</b> To make testing easier in Atlas DB should exist short list of donors in specific "test" registry (e.g. "ST" aka SymmetricTest)

<b>Test Data:</b>

- Donor-id-checker request file content example

```json
{
    "checkingMode":"test",
    "donPool": "ST",
    "donorType": "D",
    "donors":
    [
        "2404231618",
        "2404231619",
        "2404231620",
        "2404231621",
        "2404231622"
    ]
}
```
<br>

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload donor-id-checker request file to `donors/donor-id-checker/requests` directory of `devatlasstorage`.<br> Uploaded file should have <b>unique name</b> and contain:<br> - Unrequired parameter `"checkingMode"` (look for example in Test Data section) <br> - `"donPool"` value equal to specific registry (e.g. `"AN"` or `"GB"`),<br> - `"donors"` list should contain same set of donor ids as presented in DB for specific registry defined in `"donPool"` parameter and <b>have few additional donor ids</b> which absent in DB | Donor-id-checker request file uploaded to `donors/donor-id-checker/requests` directory of `devatlasstorage` |
| 2. Check `donors/donor-id-checker/requests` topic of `dev-atlas` service bus | - No new deadlettered messages,<br> - There is a new message which contain: `"RequestFileLocation"` value with uploaded file filename, `"ResultsCount"` and `"ResultsFilename"` |
| 3. Check processing result file in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - To identify needed file you could use `"ResultsFilename"` from previous step | - Result file presents in `donors/donor-id-checker/requests` directory of `devatlasstorage`<br> - `"AbsentRecordIds"` list contains donor ids which are missing in DB but present in uploaded donor-id-checker request file |
---

<br></br>

## <u>Negative Scenarios</u>

### 8. Uploaded donor-id-checker request file has invalid schema

<b>Description:</b> Check that result of submitted file processing, in case when uploaded donor-id-checker request file has empty donor ids list and Atlas set also doesn't contain donors (in DB), return 0 absent donor records (don't exist in DB) and 0 orphaned donor records (don't exist in submitted file)

<b>Preconditions:</b> There is no donors in DB for registry that will be used in donor-id-checker request file

<b>Test Data:</b>

- Donor-id-checker request file content example

```json
{
    "donPool": "ST",
    "donorType": "D",
    "donors":
    [
        "2404231618",
        "2404231619",
        "2404231620",
        "2404231621",
        "2404231622"
    ]
}
```
<br>

| Test step | Expected Result |
| --------- | --------------- |
| 1. Upload donor-id-checker request file to `donors/donor-id-checker/requests` directory of `devatlasstorage`.<br> - Uploaded file should have <b>unique name</b> and invalid schema (e.g. name if required value, as `"donPool"`, could contain errors or file could have scherma of another type of the file) | Donor-id-checker request file uploaded to `donors/donor-id-checker/requests` directory of `devatlasstorage` |
| 2. Check `alerts` topic of `dev-atlas` service bus | - No new deadlettered messages,<br> - There is a new message which contain: `"Priority"`, `"Summary"` (error summary), `"Description"`, `"Originator"`. |
