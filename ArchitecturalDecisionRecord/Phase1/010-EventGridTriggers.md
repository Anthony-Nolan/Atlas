# Event Grid Triggers

## Status

Accepted.

## Context

Having initially implemented blob storage triggers for file uploads, issues were observed making them not fit for purpose.

## Investigation

- Uploading a Blob appears to consistently not trigger a blob triggered function if the functions app is asleep.
- Perceived cases in which Blob were uploaded and the Function never triggered (despite having logged a 'receipt' to say that it did)
- Some instances of the Trigger skipping the 2nd Dequeue of the 'Trigger Message'

## Decision

As recommended by Microsoft for blob-storage triggers where scaling or performance is a concern, we are migrating to EventGrid triggered functions.

## Consequences

- File based workflows become a lot snappier and reliable.
- Terraform setup becomes a little more complex. As EventGrid triggers are webhook based, the functions apps must exist for a handshake 
request during EventGrid subscription setup. 
    - In practice this means two terraform scripts - one to set up the functions apps (and all other infrastructure), and another to set up 
    Webhooks post deployment to the functions app 