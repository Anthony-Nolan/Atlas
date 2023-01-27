# Atlas.Common.Public.Models

## Description
This package contains models that are referenced by other Atlas NuGet packages, and are also utilised by other Atlas components, e.g., models of HLA typing data.
It is a dependency package; it is not intended to be installed in isolation.
NuGet package manager should automatically download it when installing other Atlas NuGet packages that have defined it as a dependency.

## Changelog
All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### 1.5.0
* Atlas.Common.Public.Models project created to hold models that are used by both the Client.Models project and by other Atlas components, so they can be published as a NuGet package.

### Pre-1.5.0
* This project did not exist; all "common" models were held in the main Atlas.Common project.
