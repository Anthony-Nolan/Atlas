Feature: Allele Names 
  As a member of the search team
  When a search involves renamed or deleted hla
  I want to see results for the out of date allele name

  Scenario: Donor has deleted allele
    Given a patient has a match
    And the matching donor has a deleted allele
    When I run a 6/6 search
    Then the results should contain the specified donor

  Scenario: Patient has deleted allele
    Given a patient has a match
    And the patient has a deleted allele
    When I run a 6/6 search
    Then the results should contain the specified donor
    
  Scenario: Donor has a renamed allele
    Given a patient has a match
    And the matching donor has an old version of a renamed allele
    When I run a 6/6 search
    Then the results should contain the specified donor

  Scenario: Patient has a renamed allele
    Given a patient has a match
    And the patient has an old version of a renamed allele
    When I run a 6/6 search
    Then the results should contain the specified donor
    
  Scenario: Donor has a specific renamed allele
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*02:09 |*01:01 |*15:01 |*15:11 |*15:03 |*03:01 |
    And the patient has the following HLA:
    |A_1          |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*02:09:01:01 |*01:01 |*15:01 |*15:11 |*15:03 |*03:01 |
    When I run a 6/6 search
    Then the results should contain the specified donor