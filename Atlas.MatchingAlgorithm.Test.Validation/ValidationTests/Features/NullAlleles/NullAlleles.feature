Feature: Ten Out Of Ten Search - Null Alleles - patient has single null allele
  As a member of the search team
  When patient has a single null allele
  I want to be able to run a search when the patient or donor has a potential null allele

#  TODO: ATLAS-749: Ensure card is raised to make sure these tests are suitable - they didn't catch a null allele bug from this card
  
  Scenario: Null Alleles - Same typing
    Given a patient has a match
    And the donor has a null allele at locus A at position 1
    And the match orientation is direct at locus A
    When I run a 10/10 search
    Then the results should contain the specified donor 
    
  Scenario: Null Alleles - Same expressing allele, different null allele
    Given a patient has a match
    And the donor has a null allele at locus A at position 1
    And the patient has a different null allele at locus A at position 1
    And the match orientation is direct at locus A
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: Null Alleles - Homozygous donor
    Given a patient has a match
    And the donor is homozygous at locus A
    And the patient has a null allele at locus A at position 1
    And the match orientation is direct at locus A
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: Null Alleles - Same null allele, different expressing allele
    Given a patient has a match
    And the donor has a null allele at locus A at position 1
    And the donor has a mismatch at locus A at position 2
    And the match orientation is direct at locus A
    When I run a 9/10 search at locus A
    Then the results should not contain the specified donor
    
  Scenario: Null Alleles - Different null allele, different expressing allele
    Given a patient has a match
    And the donor has a null allele at locus A at position 1
    And the donor has a mismatch at locus A at position 2
    And the patient has a different null allele at locus A at position 1
    And the match orientation is direct at locus A
    When I run a 9/10 search at locus A
    Then the results should not contain the specified donor