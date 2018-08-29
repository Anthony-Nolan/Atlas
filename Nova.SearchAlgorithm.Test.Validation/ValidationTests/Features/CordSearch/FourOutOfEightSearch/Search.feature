Feature: Four out of eight Search
  As a member of the search team
  I want to be able to run a 4/8 cord search

  Scenario: 4/8 Search with an exact match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type cord
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Cord Search with an exact adult match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should not contain the specified donor
    