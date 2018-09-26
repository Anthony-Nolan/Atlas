Feature: Ten Out Of Ten Search - Null Alleles
  As a member of the search team
  I want to be able to run a 10/10 aligned registry search when the patient or donor has a potential null allele

  Scenario: Null Alleles - Same typing
    Given a patient has a match
    And the donor has a null allele at locus A at position 1
    When I run a 10/10 search
    Then the results should contain the specified donor  