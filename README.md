# Atlas - Donor Search Algorithm As a Service

>_Atlas is licensed under the GNU GPL-v3.0 (or later) license. For details see the [license file](LICENSE)._

There are several HLA matching algorithms in use worldwide, but Atlas is unique in being the only one that is free to use and open source. It uses cutting-edge cloud technology to implement the latest agreed-upon clinical guidelines for unrelated, hematopoietic stem cell donor matching and selection. It is based on [Anthony Nolan](https://www.anthonynolan.org/)’s own donor search algorithm and has been further enhanced for use in [WMDA's Search & Match Service](https://wmda.info/professionals/optimising-search-match-connect/programme-services/) for the benefit of the global, stem cell transplant community.

## Documentation & Support

- Atlas has extensive [README documentation](#readme-index) which is mainly aimed at technically proficient developers and support agents.
- If you have **questions about Atlas** - technical or otherwise - please browse the Q&A section of the [Discussions board](https://github.com/Anthony-Nolan/Atlas/discussions), and feel free to post your own questions.
- If you wish to be **updated on changes to Atlas**, please [subscribe to the repo](https://docs.github.com/en/account-and-profile/managing-subscriptions-and-notifications-on-github) and consult the [Atlas Changelogs](#changelog-index).
- Atlas is an open-source project and we welcome feedback, ideas, and contributions.
  - If you wish to **report an issue**, such as a bug or an enhancement request, please use the [Issues board](https://github.com/Anthony-Nolan/Atlas/issues).
    - Use the search tool to see if your issue has already been created.
    - If not, [submit a New Issue](https://github.com/Anthony-Nolan/Atlas/issues/new/choose), ideally, using the templates provided.
    - Submitted issues will be triaged according to criticality, priority, and business value, as appropriate, and updates will be posted via the issue comments section.
  - If you are a developer and wish to **contribute to the project**, please consult the [Contribution and Versioning README](README_Contribution_Versioning.md). 

## README Index

Due to the size and complexity of the project, the README has been split into various small chunks. Other READMEs have been linked to where appropriate, but here is a comprehensive list:

- [Core README (You Are Here)](README.md)

- Guides for developers working on Atlas
  - [Architectural Overview](README_ArchitecturalOverview.md)
  - [Development Start Up Guide ("Zero To Hero")](README_DevelopmentStartUpGuide.md)
  - [Development Settings](README_DevelopmentSettings.md)
  - [Contribution and Versioning](README_Contribution_Versioning.md)
  - [Test and Debug Resources](MiscTestingAndDebuggingResources/README_TestAndDebug.md)
  - [Manual Testing](README_ManualTesting.md)

- Guides for the installation, setup, and support of the deployed ATLAS system 
    - [Deployment](README_Deployment.md) - deploying the resources needed to run Atlas
    - [Configuration](README_Configuration.md) - configuring resources / settings of Atlas to fine tune your installation
    - [Integration](README_Integration.md) - processing reference data, and integrating your non-Atlas systems into Atlas
    - [Support](README_Support.md) - resources for assisting with any issues when running an installation of Atlas

- Component READMEs
    - [Donor Import](README_DonorImport.md) 
    - [HLA Metadata Dictionary](README_HlaMetadataDictionary.md) 
    - [Matching Algorithm](README_MatchingAlgorithm.md)
        - [Matching Algorithm Validation Test Suite (i.e., non-Technical BDD Testing)](Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/README_MatchingValidationTests.md)
    - [Match Prediction Algorithm](README_MatchPredictionAlgorithm.md) 
    - [MAC Dictionary](README_MultipleAlleleCodeDictionary.md)

## CHANGELOG Index

### API changelogs
- [Client](Atlas.Client.Models/CHANGELOG_Client.md)
- [Atlas Product](Atlas.Functions.PublicApi/CHANGELOG_Atlas.md)

### Database Changelogs
  - [Matching - Persistent](Atlas.MatchingAlgorithm.Data.Persistent/CHANGELOG_Data.md)
  - [Matching - Transient](Atlas.MatchingAlgorithm.Data/CHANGELOG_Data.md)
  - [Match Prediction](Atlas.MatchPrediction.Data/CHANGELOG_Data.md)
  - [Repeat Search](Atlas.RepeatSearch.Data/CHANGELOG_Data.md)
  - [Donors](Atlas.DonorImport.Data/CHANGELOG_Data.md)