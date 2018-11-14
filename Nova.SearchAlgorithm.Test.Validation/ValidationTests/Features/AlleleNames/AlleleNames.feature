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