Feature: Scoring - Is Antigen Match?
  As a member of the search team
  I want search results to report whether a position is antigen matched or not.

  Scenario: Patient and donor are allele-level matched
    Given a patient has a match
    And scoring is enabled at all loci
    When I run a 6/6 search
    Then antigen match should be true at all loci at both positions

  Scenario: Patient has broad serology and donor has the same broad
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1 |B_2 |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |16  |16  |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1 |B_2 |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |16  |16  |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true at locus B at both positions

  Scenario: Patient has broad serology and donor has split of that broad
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1 |B_2 |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38  |39  |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1 |B_2 |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |16  |16  |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true at locus B at both positions

  Scenario: Patient has broad serology and donor has associated of that broad
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |3901  |3902  |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |16    |16    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true at locus B at both positions

  Scenario: Patient has split serology and donor has the same split
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1 |B_2 |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |39  |39  |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1 |B_2 |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |39  |39  |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true at locus B at both positions

  Scenario: Patient has split serology and donor has associated of that split
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |3901  |3902  |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |39    |39    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true at locus B at both positions

  Scenario: Patient has split serology and donor has broad of that split
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |16    |16    |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |39    |39    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true at locus B at both positions

  Scenario: Patient has split serology and donor has sibling of that split
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |38    |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |39    |39    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be false at locus B at both positions

  Scenario: Patient has associated serology and donor has same associated
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |3901  |3901  |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |3901  |3901  |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true at locus B at both positions

  Scenario: Patient has associated serology and donor has split of that associated
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |39    |39    |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |3901  |3901  |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true at locus B at both positions

  Scenario: Patient has associated serology and donor has broad of that associated
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |16    |16    |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |3901  |3901  |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true at locus B at both positions

  Scenario: Patient has associated serology and donor has sibling of that associated
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |3902  |3902  |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |3901  |3901  |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be false at locus B at both positions

  Scenario: Patient has associated serology and donor has sibling of the split of that associated
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |38    |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |3901  |3901  |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be false at locus B at both positions

  Scenario: Patient has one antigen mismatch to donor in direct orientation
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |52    |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |51    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true in position 1 and false in position 2 of locus B

  Scenario: Patient has one antigen mismatch to donor in cross orientation (patient B_1 vs donor B_2)
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |52    |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |51    |38    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be false in position 1 and true in position 2 of locus B

  Scenario: Patient has one antigen mismatch to donor in cross orientation (patient B_2 vs donor B_1)
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |52    |38    |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |51    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true in position 1 and false in position 2 of locus B

  Scenario: Donor has Unknown match grade at DPB1
    Given a patient has a match
    And the matching donor is untyped at locus DPB1
    And scoring is enabled at locus DPB1
    When I run a 6/6 search
    Then antigen match should be empty at locus DPB1 at both positions