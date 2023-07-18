Feature: Scoring - Positional Orientation

  As a member of the search team
  I want search results to report scoring results in the correct orientation wrt to locus position.

 Scenario: Patient has one mismatch grade to donor in direct orientation
    Given a patient has a match
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |51    |*07:01 |*07:01 |
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |52    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then the match grade should be serology in position 1 and mismatch in position 2 of locus B

 Scenario: Patient has one mismatch grade to donor in cross orientation (patient B_1 vs donor B_2)
    Given a patient has a match
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |51    |38    |*07:01 |*07:01 |
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |52    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then the match grade should be serology in position 1 and mismatch in position 2 of locus B

  Scenario: Patient has one mismatch grade to donor in cross orientation (patient B_2 vs donor B_1)
    Given a patient has a match
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |51    |*07:01 |*07:01 |
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |52    |38    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then the match grade should be mismatch in position 1 and serology in position 2 of locus B

 Scenario: Patient has one mismatch confidence to donor in direct orientation
    Given a patient has a match
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |51    |*07:01 |*07:01 |
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |52    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then the match confidence should be potential in position 1 and mismatch in position 2 of locus B

 Scenario: Patient has one mismatch confidence to donor in cross orientation (patient B_1 vs donor B_2)
    Given a patient has a match
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |51    |38    |*07:01 |*07:01 |
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |52    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then the match confidence should be potential in position 1 and mismatch in position 2 of locus B

  Scenario: Patient has one mismatch confidence to donor in cross orientation (patient B_2 vs donor B_1)
    Given a patient has a match
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |51    |*07:01 |*07:01 |
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |52    |38    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then the match confidence should be mismatch in position 1 and potential in position 2 of locus B

 Scenario: Patient has one antigen mismatch to donor in direct orientation
    Given a patient has a match
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |51    |*07:01 |*07:01 |
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |52    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true in position 1 and false in position 2 of locus B

 Scenario: Patient has one antigen mismatch to donor in cross orientation (patient B_1 vs donor B_2)
    Given a patient has a match
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |51    |38    |*07:01 |*07:01 |
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |52    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be true in position 1 and false in position 2 of locus B

  Scenario: Patient has one antigen mismatch to donor in cross orientation (patient B_2 vs donor B_1)
    Given a patient has a match
    And the patient has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |38    |51    |*07:01 |*07:01 |
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1   |B_2   |DRB1_1 |DRB1_2 |
    |*01:01 |*01:01 |52    |38    |*07:01 |*07:01 |
    And scoring is enabled at locus B
    When I run a 6/6 search
    Then antigen match should be false in position 1 and true in position 2 of locus B