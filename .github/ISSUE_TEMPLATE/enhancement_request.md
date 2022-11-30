---
name: Enhancement Request
about: Submit a request for new feature or an improvement to an existing feature
title: ''
labels: enhancement
assignees: ''

---

**Title**
Phrase as a _user story_, i.e., a short, simple description of a feature told from the perspective of the person who desires the new capability. [[ref](https://www.mountaingoatsoftware.com/agile/user-stories)]
- Template: As a \<type of user\>, I want \<some goal\> so that \<some reason\>.
- E.g., "As a _support agent_, I want to be _alerted of a failed donor import_ so I can _re-upload the file_"

**Description**
Explain the enhancement in more detail.

**Component(s) to enhance (if known)**
- Donor Import
- HLA Metadata Dictionary
- Matching Algorithm
- Match Prediction Algorithm
- MAC Dictionary
- Automated tests
- Support alerts & notifications
- Logging

**Business value (select one or more):**
- Improved user experience
- Improved supportability
- Improved resilience
- Improved security
- Improved performance
- Reduced running costs
- Other (please describe)

**How would this enhancement provide this value?**
E.g., "alerting reduces support turn-around-time and reduces the risk of data loss"

**Acceptance Criteria**
A checklist of requirements and outcomes that means the enhancement was completed successfully.
- It is useful for criteria to be phrased as "Given, When, Then" statements.
- E.g., 
"_Given_ a donor import file with invalid donor data,
_When_ the file is added to the azure blob storage container for upload,
_Then_ a failure alert message should be sent out that includes the name of the file".

**Required By Date (if any)**
Please explain the significance of the date, if provided.
- E.g., "Required by \<date\> ahead of compliance inspection"