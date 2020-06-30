# Options Pattern

## Status

Accepted.

## Context

- We would like to use the options pattern as provided by Microsoft for application settings.
- We would also like to decouple logical C# modules (e.g. matching algorithm, mac lookup) from the functions apps in which they are called
    - This is important as some modules will be used in several functions apps e.g. HLA metadata dictionary, MAC lookup

## Investigation

- The easiest way to set up the options pattern is to use Microsoft's provided configuration, which converts from settings files
to IOptions<Settings> objects.
- Registering a dependency on IOptions<Settings> from within a module would mean usage of the module requires the functions app to register
IOptions<Settings>, which is more coupled than we'd like.
- Attempting to re-register IOptions<Settings> within a module causes an infinite loop.

## Decision

- Individual modules will require concrete settings objects (rather than IOptions<Settings>)
- This will allow functions apps to set up IOptions<Settings> as usual, and pass in the concrete object to its modules.

## Consequences

- ATLAS modules are sufficiently decoupled from their entry points, where application settings are actually set up
- We still get all the benefits of the options pattern
- Our settings dependency injection code may be confusing to maintainers familiar with the standard IOptions pattern.