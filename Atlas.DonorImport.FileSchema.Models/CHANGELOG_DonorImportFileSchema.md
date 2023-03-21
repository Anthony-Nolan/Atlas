# Atlas.DonorImport.FileSchema.Models

## Description
This package contains models that define the Atlas Donor Import file.

## Changelog
All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versions

### 1.6.0
* Models have been added for new `Donor Checker` functionality.
* Donor import enum, UpdateMode, has been extended with a new option: `check`, to be used when info in the import file should only be checked against the donor store but not imported.

### 1.5.0
* No outward change to schema definition.
* Models moved from main Atlas.DonorImport project to new, standalone project, with versioning & changelog, to allow publishing as NuGet package.
