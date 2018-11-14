Feature: Allele Names 
  As a member of the search team
  When a search involves renamed or deleted hla
  I want to see results for the out of date allele name

  Scenario: Donor has deleted allele
    Given a patient and a donor
    And the matching donor has a deleted allele
    When I run a 6/6 search
    Then the results should contain the specified donor