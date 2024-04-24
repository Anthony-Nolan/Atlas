# Atlas.Debug.Client.Models

## Description
This package contains client models utilised by the Atlas debug endpoints.

## Changelog
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### 2.1.0
* Creation of new library, `Atlas.Debug.Client.Models`, that contains models used in debug endpoints.
* Moved following models to new project:
    * `DebugDonorResult` to `DonorImport` namespace.
    * `PeekServiceBusMessagesRequest` and `PeekedServiceBusMessage<T>` to `ServiceBus` namespace.
* Added new models:
    * `DonorUpdateFailureInfo` and `DonorImportRequest` to `DonorImport` namespace.