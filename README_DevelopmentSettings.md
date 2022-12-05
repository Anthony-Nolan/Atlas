# Development Settings

Explanation of the kinds of application and database settings that are used to configure Atlas, how to override them locally, and how to extend them when developing.

## Non Azure-Functions Settings

Settings for each non-functions project are defined in the `appsettings.json` file.

In some cases these settings will need overriding locally - either for secure values (e.g. api keys), or if you want to use a different service (e.g. azure storage account, service bus)

This is achieved with User Secrets: <https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-2.2&tabs=windows>

### Cloud Tables
  - The `StorageConnectionString` setting in `appsettings.json` determines the connection to Azure storage.
    - The default is to use local emulated storage, for which the `Azurite` (previously known as, `Azure Storage Emulator`) will need to be running.
  - To run against development storage (e.g. if performance of the emulator is not good enough, or the emulator is unavailable), this connection string can be overridden using user secrets to point at the DEV storage account.

## Azure-Functions Settings

Azure functions requires a different settings configuration. With the "Values" object in `local.settings.json`, it expects a collection of string app settings - these reflect a 1:1 mapping with the app settings configured in Azure for deployed environments

> Warning! Attempting to use nested objects in this configuration file will prevent the app settings from loading, with no warning from the functions host!

In order to allow checking in of non-secret default settings, while dissuading accidental check-in of secrets, the following pattern is used: 

- A `local.settings.template.json` is checked in with all default settings values populated. 
    - Any new app settings should be added to these files
    - Any secrets (e.g. service bus connection strings) should be checked in with an obviously dummy value (e.g. override-this)
- On build of the functions projects, this template will be copied to a gitignored `local.settings.json`
    - This file can be safely edited locally to override any secret settings without risk of accidental check-in
    - This copying is done by manually amending the csproj and adding the following code: 
    ```
      <Target Name="Scaffold local settings file" BeforeTargets="BeforeCompile" Condition="!EXISTS('$(ProjectDir)\local.settings.json')">
          <Copy SourceFiles="$(ProjectDir)\local.settings.template.json" DestinationFiles="$(ProjectDir)\local.settings.json" />
      </Target>
    ```
- When someone else has added new settings, you will need to either: 
    - (a) Add the new setting manually to `local.settings.json`
    - (b) Delete `local.settings.json` and allow it to regenerate on build. Any local secret settings will then need to be re-applied. 
        - *Warning* When deleting through an IDE, it may remove the "Copy always" functionality in the csproj, at which point the file 
        will not be copied to the build folder. 
        - Either delete via file explorer, or remember to mark as "copy always" on recreation

*Warning* - when running a functions app for the first time on a machine, this copying may not have happened in time. If no settings are found, try rebuilding and running again. 

## Options Pattern

To enable a shared configuration pattern across both the functions project, and api used for testing, a modification of the 
[Options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-2.2) is used:

- Top level entry-points (e.g. functions apps, ASP.NET API) are responsible for providing the expected settings to logical component projects.
    - Within these apps, the standard options pattern is followed, by registering settings as `IOptions<TSettings>`
- Within the component projects, settings are re-registered as `TSettings` directly
    - This allows for decoupling of the components and the source of their settings
    - Do not attempt to re-register `IOptions<TSettings>` within component projects, or declare a dependency on them.