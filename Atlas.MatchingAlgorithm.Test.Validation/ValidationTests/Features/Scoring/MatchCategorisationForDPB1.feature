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

Scenario: Permissive mismatch - Donor has DPB1 allele string wherein one allele has a matching TCE assignment, but other has no TCE assignment
	Given a patient has a match
	And the matching donor has the following HLA:
		| A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | DPB1_1 | DPB1_2         |
		| *01:01 | *66:01 | *57:01 | *41:01 | *13:XX | *07:01 | *01:01 | *191:01/192:01 |
	And the patient has the following HLA:
		| A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | DPB1_1 | DPB1_2 |
		| *01:01 | *66:01 | *57:01 | *41:01 | *13:XX | *07:01 | *01:01 | *02:01 |
	And scoring is enabled at locus DPB1
	When I run a 6/6 search
	Then the locus match category should be PermissiveMismatch at locus Dpb1