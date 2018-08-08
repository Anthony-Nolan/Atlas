Feature: Ten Out Of Ten Search
  As a member of the search team
  I want to be able to run a 10/10 search

  Scenario: 10/10 Search with a TGS typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
