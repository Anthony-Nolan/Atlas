Feature: Nine Out Of Ten Search - full matches
  As a member of the search team
  I want to be able to run a 9/10 search
  And not see full 10/10 matches in the results

  Scenario: 9/10 Search at A with a matching donor
    Given a patient has a match
    And the matching donor is a 10/10 match
    When I run a 9/10 search at locus A
    Then the results should not contain the specified donor