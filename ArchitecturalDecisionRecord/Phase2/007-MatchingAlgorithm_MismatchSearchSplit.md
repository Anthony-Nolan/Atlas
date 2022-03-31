# Matching Algorithm - Mismatch Search Split

## Status

Accepted.

## Context

In large Atlas environments (~15M donors), mismatch searches were shown to be a problem:

* Mismatch searches would take significantly longer than non-mismatch searches, particularly when mismatches are allowed at multiple loci
* When running a realistic amount of load through the system, even searches that were previously shown to be very quick were slowing down significantly
  * This was only observed when mismatch searches were included in the test load. 

## Investigation

The observed slowdown when under load from mismatch searches was shown to come from the database. There was no sign of obvious resource contention (e.g. CPU, memory, Log IO),
but there was still a noticeable slowdown in the database when running under load. 

This slowdown was postulated to be due to the nature of the matching algorithm - matching is performed locus-by-locus, with possible donors from the first matched 
locus passed through to the next. With a large enough data set, allowing any mismatches at the *first* matched locus will cause a *significant* amount more donors to be returned 
from the first locus matching request, and the sheer volume of data returned and passed into later loci seems to be the cause of the observed slowdown. 

We found that as long as one required locus (A/B/DRB1) is required to have zero mismatches, the first locus' results will be limited to a more reasonable number, 
and the slowdown is significantly reduced. 

We also postulate that it is logically possible to break down any search (with fewer than 5 allowed mismatches) into a series of smaller searches, that, when combined, 
will give the exact same result set. 

____________________

As an example: 

A mismatch search at A/B/DRB, allowing for a total of 1 mismatch. For shorthand we will write this in the format `Total(A/B/DRB1)`, i.e. 1(1/1/1).

Run as-is, this search will trigger the slowdown as seen above, as none of the three required loci are running without allowed mismatches.

This search can be broken up as follows: 

1(0/0/1) & 1(0/1/0) & 1(1/0/0)

As only one mismatch is allowed overall, the final results must either have a mismatch at A OR one at B OR one at DRB1. i.e. they must appear in one of the 
result sets for the three sub-searches above. 

As our requirement was merely to have one locus with no allowed mismatches, this search can then be broken down further into: 

1(0/0/1) & 1(1/1/0)

allowing fewer overall searches to be run. 

(It has not been investigated as to whether it's more efficient to run fewer searches, or more searches allowing more loci to require 0 mismatches in this case. 
The fewest searches approach has been taken by default, but this could be changed later if such an investigation proved that the three searches were more efficient here)

## Decision

Mismatch searches will be broken up into a series of "sub-searches", all of which must allow zero mismatches at one required locus.

These sub-searches will have their results combined (and de-duplicated, to cover the case where "better matches" are allowed) before returning a result set to the consumer.

## Consequences

* Mismatch searches will be significantly quicker to run (with some caveats, below)
  * Mismatch searches with limited loci allowed to have mismatches will not be improved, as they were not affected by the slowdown in the first place (e.g. 1(1/0/0))
  * Mismatch searches with more than 4 allowed total mismatches across required loci cannot be broken down into an ideal set of sub-searches, and will continue to see the slowdown described above.
* Mismatch searches meeting the above criteria will no longer cause significant slowdown on the database affecting other searches run in parallel.