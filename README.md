# Nova.SearchAlgorithm
Service for AN's HSC Search Algorithm.

## Terminology

The following terms are assumed domain knowledge when used in the code:

* Homozygous locus
  - The typings at both positions for a locus are the same
  - e.g. *A\*01:01,01:01*
* Heterozygous locus
  - The typings at both positions for a locus are not the same
  - e.g. *A\*01:01,02:01*
* GvH = Graft vs. Host 
* HvG = Host vs Graft
  - Refers to the direction of matching if one party has a homozygous locus type
  - e.g. patient (host) is *A\*01:01,02:01*; donor (graft) is *A\*01:01,01:01* - There is one mismatch im the GvH direction (but none in the HvG)
  - e.g. patient (host) is *A\*01:01,01:01*; donor (graft) is *A\*01:01,02:01* - There is one mismatch in the HvG direction, (but none in the GvH)
