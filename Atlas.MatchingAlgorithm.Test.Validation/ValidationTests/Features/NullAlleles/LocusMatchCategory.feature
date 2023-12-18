Feature: Scoring - Expressing Typing vs. Null Allele - Locus Match Category
  As a member of the search team
  I want search results to correctly handle the scoring of a locus that contains a null allele.

  Scenario: Homozygous locus vs Null-allele containing locus are matched at expressing typing
    Given a patient has a match
    And the matching donor has the following HLA:
    | A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | C_1    | C_2     |
    | *01:01 | *02:01 | *08:01 | *08:01 | *07:01 | *07:01 | *01:02 | *01:02  |
    And the patient has the following HLA:
    | A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | C_1    | C_2     |
    | *01:01 | *02:01 | *08:01 | *08:01 | *07:01 | *07:01 | *01:02 | *02:52N |
    And scoring is enabled at locus C
    When I run a 6/6 search
    Then the locus match category should be Match at C

  Scenario: Homozygous locus vs Null-allele containing locus are allele mismatched at expressing typing
    Given a patient has a match
    And the matching donor has the following HLA:
    | A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | C_1    | C_2     |
    | *01:01 | *02:01 | *08:01 | *08:01 | *07:01 | *07:01 | *01:02 | *01:02  |
    And the patient has the following HLA:
    | A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | C_1    | C_2     |
    | *01:01 | *02:01 | *08:01 | *08:01 | *07:01 | *07:01 | *01:10 | *02:52N |
    And scoring is enabled at locus C
    When I run a 6/6 search
    Then the locus match category should be Mismatch at C

  Scenario: Homozygous locus vs Null-allele containing locus are antigen mismatched at expressing typing
    Given a patient has a match
    And the matching donor has the following HLA:
    | A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | C_1    | C_2     |
    | *01:01 | *02:01 | *08:01 | *08:01 | *07:01 | *07:01 | *01:02 | *01:02  |
    And the patient has the following HLA:
    | A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | C_1    | C_2     |
    | *01:01 | *02:01 | *08:01 | *08:01 | *07:01 | *07:01 | *03:03 | *02:52N |
    And scoring is enabled at locus C
    When I run a 6/6 search
    Then the locus match category should be Mismatch at C

  Scenario: Homozygous DPB1 vs Null-allele containing DPB1 are permissively mismatched at expressing typing
	Given a patient has a match
	And the matching donor has the following HLA:
	| A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | DPB1_1 | DPB1_2  |
	| *01:01 | *02:01 | *08:01 | *08:01 | *07:01 | *07:01 | *03:01 | *03:01  |
	And the patient has the following HLA:
	| A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 | DPB1_1 | DPB1_2  |
	| *01:01 | *02:01 | *08:01 | *08:01 | *07:01 | *07:01 | *08:01 | *64:01N |
	And scoring is enabled at locus DPB1
	When I run a 6/6 search
	Then the locus match category should be PermissiveMismatch at locus Dpb1