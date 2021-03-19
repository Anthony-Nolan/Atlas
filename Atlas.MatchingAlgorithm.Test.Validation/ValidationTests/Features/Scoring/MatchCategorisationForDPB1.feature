Feature: MatchCategorisationForDPB1
  As a member of the search team
  I want search results to have an appropriate DPB1 match category

Scenario: DPB1 Mismatch
Given a patient has a match
And the matching donor is unambiguously typed at each locus
And the patient is unambiguously typed at each locus
And the patient and donor have mismatched DPB1 alleles with different TCE group assignments
And scoring is enabled at locus DPB1
When I run a 6/6 search
Then the locus match category should be Mismatch at locus Dpb1

Scenario: DPB1 Permissive Mismatch
Given a patient has a match
And the matching donor is unambiguously typed at each locus
And the patient is unambiguously typed at each locus
And the patient and donor have mismatched DPB1 alleles with the same TCE group assignments
And scoring is enabled at locus DPB1
When I run a 6/6 search
Then the locus match category should be PermissiveMismatch at locus Dpb1

Scenario: DPB1 Match
Given a patient has a match
And the matching donor is unambiguously typed at each locus
And the patient is unambiguously typed at each locus
And scoring is enabled at locus DPB1
When I run a 10/10 search
Then the locus match category should be Match at locus Dpb1

Scenario: DPB1 Unknown
Given a patient has a match
And the matching donor is untyped at locus DPB1
And scoring is enabled at locus DPB1
When I run a 10/10 search
Then the locus match category should be Unknown at locus Dpb1