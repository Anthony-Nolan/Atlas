Feature: Four out of eight Search
  As a member of the search team
  I want to be able to run a 4/8 cord search

  Scenario: 4/8 Search with an exact match
    Given a patient has a match
    And the matching donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Cord Search with an exact adult match
    Given a patient has a match
    And the matching donor is of type adult
    And the search type is cord
    When I run a 4/8 search
    Then the results should not contain the specified donor
    