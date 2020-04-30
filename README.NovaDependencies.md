# Nova Dependencies

This tool was originally developped by Anthony Nolan and depended upon various of their internal utilities.

As part of preparing the tool for use by the WMDA, the source code for all of those dependencies was pulled directly into the codebase.

For historical reference, some notes on that process:

## Packages & Versions

* `Nova.DonorService.Client.Models` was depended on:
  * at version 4.1.1, by:
    * `Atlas.MatchingAlgorithm`
    * `Atlas.MatchingAlgorithm.Functions`
    * `Atlas.MatchingAlgorithm.Test.Integration`
* `Nova.HLAService.Client` was depended on:
  * at version 4.0.0, by:
    * `Atlas.MatchingAlgorithm`
    * `Atlas.MatchingAlgorithm.Dictionary`
    * `Atlas.MatchingAlgorithm.Test.Integration`
* `Nova.Utils` was depended on:
  * at version 10.0.2, by `Nova.HLAService.Client` (* Actual dependence not confirmed)
  * at version 12.0.12, by `Nova.DonorService.Client.Models` (* Actual dependence not confirmed)
  * at version 14.3.0, by:
    * `Atlas.MatchingAlgorithm.Data`
    * `Atlas.MatchingAlgorithm.Client`
    * `Atlas.MatchingAlgorithm.Client.Models`
    * `Atlas.MatchingAlgorithm.Test.Integration`
* `Nova.Utils.Client` was depended on:
  * at version 10.0.2, by `Nova.HLAService.Client` (* Actual dependence not confirmed)
* `Nova.Utils.Storage` was depended on:
  * at version 14.3.0, by `Atlas.MatchingAlgorithm`
* `Nova.Utils.Notifications` was depended on:
  * at version 14.3.0, by `Atlas.MatchingAlgorithm`
* `Nova.Utils.ServiceBus` was depended on:
  * at version 14.3.0, by:
    * `Atlas.MatchingAlgorithm`
    * `Atlas.MatchingAlgorithm.Functions.DonorManagement`
    * `Atlas.MatchingAlgorithm.Test`
* `Nova.Utils.ServiceClient` was depended on:
  * at version 14.3.0, by:
    * `Atlas.MatchingAlgorithm.Api`
    * `Atlas.MatchingAlgorithm.Client`
    * `Atlas.MatchingAlgorithm.Functions`
    * `Atlas.MatchingAlgorithm.Functions.DonorManagement`
    * `Atlas.MatchingAlgorithm.Test`
    * `Atlas.MatchingAlgorithm.Test.Integration`
    * `Atlas.MatchingAlgorithm.Test.Performance`
    * `Atlas.MatchingAlgorithm.Test.Validation`

## Repositories & Commits

* `Nova.Utils.*` v14.3.0 appears to have been packaged from commit `67c23aafc6dac6ddb151597c7f8eb1373e99bd1c` of <https://github.com/Anthony-Nolan/Nova.Utils>.
* `Nova.Utils.*` v12.0.12 appears to have been packaged from commit `qq` of <https://github.com/Anthony-Nolan/Nova.Utils>. (Will be determined if we establish that we need it!)
* `Nova.Utils.*` v10.0.2 appears to have been packaged from commit `qq` of <https://github.com/Anthony-Nolan/Nova.Utils>. (Will be determined if we establish that we need it!)
* `Nova.DonorService.Client.Models` v4.1.1 appears to have been packaged from commit `8033bc642d08de525516abe11ff08012c1e76785` of <https://github.com/Anthony-Nolan/Nova.Donor>.
* `Nova.HLAService.Client` v4.0.0 appears to have been packaged from commit `5035fd29856f1540270098c831a3649c2f60ed81` of <https://github.com/Anthony-Nolan/Nova.HLA>.
