Feature: Scoring
  As a member of the search team
  I want search results to have an appropriate score

  Scenario: P-group match at all loci
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is p-group
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be p-group at all loci at both positions
  
  Scenario: G-group match at all loci
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is g-group
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be g-group at all loci at both positions