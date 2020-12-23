# Pre-processed HLA normalisation

## Status

Accepted.

## Context

The data structure of pGroup:donorId key value pairs lead to a significantly large processed data set. For the 30Million donor case, we were seeing database sizes approaching 1TB, 
which start to get very expensive in the cloud. 

While this may be the most efficient data structure for data lookup, the cost of data storage is prohibitively high in Azure SQL for the target dataset size.
(Or, more accurately, the cost of a database powerful enough to perform the necessary search processing - smaller pricing tiers have a max storage size, so we needed to overpay for 
processing to allow enough data storage, even if memory itself isn't that expensive.)

A lot of this data is not very normalised - e.g. some HLA expand to a large number of P-Groups, and are very prevalent, e.g. XX-Code typed alleles. As such, this dataset can be shrunk significantly. 

## Investigation

Work was performed to store pGroup:hla relations directly, as well as hla:donorId lookups.
Data refresh was sped up significantly, and the dataset size was significantly shrunk. Search times were investigated and shown to increase, but not drastically.

## Decision

- The pre-processed data in SQL will be stored as two lookups -> pGroup:hla:donorId, rather than the flat pGroup:donorId
- As part of this change, we need to ensure that null-expressing alleles are stored with a composite key - as the matching logic for null alleles is dependant on the expressing allele at the same locus 

## Consequences

- Data refresh time is several times shorter
- Data set is many times smaller, and scales better with more donors (it scales linearly with donors, and only exponentially with new HLA names, which are limited. Previously could scale exponentially with donors.)
- Search time is marginally slower in the 2M donor environment (small searches e.g 10-100 donors didn't significantly change. Larger searches ~10,000 donors roughly doubled from ~20s to ~40s in 2M donor environment.)
- Null allele handling is slightly more complex to understand and support, due to introducing the need to care about the "paired" expressing allele name as well as its p-groups.