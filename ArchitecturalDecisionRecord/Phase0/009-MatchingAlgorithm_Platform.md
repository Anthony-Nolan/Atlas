# Matching Algorithm Platform

## Status

Accepted.

## Context

On what platform is the matching algorithm deployed.

## Investigation

The matching algorithm was initially deployed as an ASP.NET WebApi as an Azure App Service.

This was proved to not be fit for purpose due to a non-configurable 4 minute timeout of Azure's app service load balancer - 
any requests that take longer than 4 minutes will return an error response, and execution on the server is not guaranteed to complete.   

## Decision

Azure functions were chosen as a platform that does not have such a restriction. 
Additional benefits include ease of adding additional triggers - timer and service bus function triggers are available.

The ASP.NET API was kept, but only for two purposes: 
- Local debugging, in cases where an ASP.NET server is easier to run locally than an Azure Functions Host
- Automated testing: in which an in-memory ASP.NET server is used to run full system tests of the matching algorithm

## Consequences

The matching algorithm is an "Azure Functions" application, inheriting all the pros and cons of such.