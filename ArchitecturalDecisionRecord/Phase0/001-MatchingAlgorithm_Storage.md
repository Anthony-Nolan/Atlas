# Matching Algorithm Storage

## Status

Accepted

## Context

The matching algorithm works by maintaining an up to date store of donors' HLA information, with any ambiguous HLA 
pre-broken down into possible p-groups for said HLA. Both the donor information and pre-processed p-group data should be stored in an
appropriate database. 

The database must allow for: 
- performant reads while running a search
- performant bulk insert on initial data processing 
- reasonable pricing 

## Investigation

Primarily three database options were investigated - Cosmos, Azure Table Storage, and Azure SQL. 

Both Cosmos and Table Storage proved to have insufficient write capacity when importing the initial dataset of donors, and were abandoned 
based on this, without testing their performance for searches.

SQL was shown to have sufficient write throughput (when run at an appropriate pricing tier on Azure), and the search performance was 
acceptable for the use case of Anthony Nolan search.

Google BigQuery was also briefly considered, but only after development was nearly complete on the SQL implementation. 
It proved to perform similarly on the test dataset (~1.6 million donors), but was hoped to scale better than linearly to much larger datasets.

Unlike Cosmos and Table Storage, bulk insert of data performed reasonably in BigQuery - the biggest unresolved question was how to handle 
ongoing updates, as BigQuery doesn't allow updates of data - and donor data updates are a required feature. 

A decision on how to handle updates in BigQuery was never concluded, as SQL was proven to be fit for purpose by this stage.  

## Decision

Azure SQL was chosen as the data store for the matching algorithm

## Consequences

- There is a chance that for significantly larger datasets, another database provider may be more suitable - further 
investigation may be required if Azure SQL is proven to be insufficient for larger datasets.  