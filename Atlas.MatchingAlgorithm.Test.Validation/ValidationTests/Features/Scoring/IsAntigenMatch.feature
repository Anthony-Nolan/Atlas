Feature: Scoring - Is Antigen Match?
  As a member of the search team
  I want search results to report whether a position is antigen matched or not.

  Scenario: Patient and donor are matched at A and have assigned serologies
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*02:01 |*02:01 |*08:01 |*08:01 |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*02:01 |*02:01 |*08:01 |*08:01 |*07:01 |*07:01 |
    And scoring is enabled at locus A
    When I run a 6/6 search
    Then antigen match should be true at locus A at both positions

  Scenario: Patient and donor are mismatched at A and have assigned serologies
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*02:01 |*02:01 |*08:01 |*08:01 |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*03:01 |*03:01 |*08:01 |*08:01 |*07:01 |*07:01 |
    And scoring is enabled at locus A
    When I run a 4/6 search
    Then antigen match should be false at locus A at both positions

  Scenario: Patient and donor are matched at A and one allele is non-expressing
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1           |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*01:01:01:02N |*02:01 |*08:01 |*08:01 |*07:01 |*07:01 |
    And the patient has the following HLA:
    |A_1           |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*01:01:01:02N |*02:01 |*08:01 |*08:01 |*07:01 |*07:01 |
    And scoring is enabled at locus A
    When I run a 6/6 search
    Then antigen match should be false in position 1 and true in position 2 of locus A

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

  Scenario: Donor has Unknown match grade at DPB1
    Given a patient has a match
    And the matching donor is untyped at locus DPB1
    And scoring is enabled at locus DPB1
    When I run a 6/6 search
    Then antigen match should be empty at locus DPB1 at both positions

  Scenario: [Regression] Patient and donor are completely allele mismatched at DRB1 but have one antigen match
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1     |A_2      |B_1      |B_2    |DRB1_1   |DRB1_2   |
    |*01:YAG |*03:ANPZ |*44:AJVH |*52:01 |*04:AFNC |*15:APKE |
    And the patient has the following HLA:
    |A_1      |A_2     |B_1      |B_2    |DRB1_1   |DRB1_2   |
    |*01:YAG |*03:ANPZ |*44:AJVH |*52:01 |*03:YMP  |*04:AMCV |
    And scoring is enabled at locus DRB1
    When I run a 4/6 search
    Then antigen match should be true in position 1 and false in position 2 of locus DRB1