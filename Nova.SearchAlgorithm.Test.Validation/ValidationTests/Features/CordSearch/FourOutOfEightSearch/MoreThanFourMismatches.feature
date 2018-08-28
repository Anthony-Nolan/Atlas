Feature: Four out of eight Search - more than four mismatches
  As a member of the search team
  I want to be able to run a 4/8 cord search
  And not see donors with more than four mismatches in the results

  Scenario: 4/8 Search with five mismatches 
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And the donor has a double mismatch at locus B
    And the donor has a single mismatch at locus DRB1
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should not contain the specified donor
    
  Scenario: 4/8 Search with six mismatches 
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And the donor has a double mismatch at locus B
    And the donor has a double mismatch at locus C
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should not contain the specified donor