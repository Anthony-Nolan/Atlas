# Service Plans and Functions Apps Structure

## Status

Proposed.

## Context

We have run into concurrency issues when running significant load through the Atlas search process.

This is due to concurrency restrictions of the matching algorithm SQL database - at S3, it cannot handle more than 400 concurrent connections.

We need to be able to limit concurrency of the matching algorithm to prevent this limit being exceeded - without reducing the capacity for match prediction, which we 
expect to be able to horizontally scale significantly, as it is almost all CPU bound work. 

## Investigation

Read through durable functions documentation, as well as asking Stack Overflow and Microsoft Support - I have not found any way to limit concurrency on a per-function basis, only per functions-app.
As such we will need to deploy both to different functions apps, thus losing the ability to run matching as part of the durable function framework - instead we shall go back to manually 
managing queuing via service bus / storage for results. 

While on the subject, I took the time to review all Atlas entry points and ensure that the scalability of the relevant functions app was appropriate.

## Decision

Here is a table of the proposed functions apps, along with their service plan, scaling expectations, and details of any changes from the current architecture.

| Functions App                        | Responsibilities                        | Service Plan | Scalability                                            | Changes                                  | Consequences                                       |
|--------------------------------------|-----------------------------------------|--------------|--------------------------------------------------------|------------------------------------------|----------------------------------------------------|
| Donor Import                         | Ingesting donor update files            | Consumption  | Can horizontally scale under load.                     | App Service -> Consumption Plan          | Reduced Cost.                                      |
|                                      |                                         |              | Ordered delivery already not guaranteed by EventGrid   |                                          | Increased maximum load due to horizontal scaling.  |
|                                      |                                         |              |                                                        |                                          | Slightly higher cold start time.                   |
|                                      |                                         |              |                                                        |                                          |                                                    |
| Matching Algorithm Donor Management  | Ongoing donor updates via service bus   | App Service  | No scaling - preserve order                            | N/A                                      | N/A                                                |
|                                      |                                         |              |                                                        |                                          |                                                    |
| Match Prediction                     | HF Set Import                           | Consumption  | Can horizontally scale under load.                     | App Service -> Consumption Plan          | Reduced Cost.                                      |
|                                      | Manual match prediction requests        |              |                                                        |                                          | Increased maximum load due to horizontal scaling.  |
|                                      | *NOT* MPA as part of search             |              |                                                        |                                          | Slightly higher cold start time.                   |
|                                      |                                         |              |                                                        |                                          |                                                    |
| Matching Algorithm                   | Matching Data Refresh                   | Elastic      | Horizontally scalable under load                       | App service -> Elastic Plan              | Searches manually queued.                          |
|                                      | Runs matching as part of search         |              | Concurrency limited to prevent overloading SQL DB      | Now queues and runs searches             | Prevents failures from too many SQL connections.   |
|                                      |                                         |              |                                                        |                                          |                                                    |
| Atlas Functions                      | Orchestrate search post-matching        | Elastic      | Horizontally scalable under load                       | No longer runs matching                  | Matching removed from durable functions            |
|                                      | Includes running MPA fan-out/fan-in     |              | Higher concurrent load allowed than matching           | Now triggered by service bus             | Search entry point removed from durable functions  |
|                                      | MAC Import                              |              |                                                        | Will need to download matching results   |                                                    |
|                                      |                                         |              |                                                        |                                          |                                                    |
| Atlas Public API                     | Search initiation and ID generation     | Elastic      | Horizontally scalable under load                       | New functions app                        | Search initiation slows down less under load       |
|                                      |                                         |              | Does not perform any algorithmic work, ensuring high   |                                          |                                                    |
|                                      |                                         |              | availability and quick response times for search init  |                                          |                                                    |
|                                      |                                         |              |                                                        |                                          |                                                    |

## Consequences

See table.