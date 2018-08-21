Feature: Ten Out Of Ten Search - Multiple patients
  As a member of the search team
  I want to be able to run multiple 10/10 search
  For different patients

  Scenario: 10/10 Search for a selection of 4-field TGS typed donors
    Given a set of 50 patients with matching donors
    And each matching donor is a 10/10 match
    And each matching donor is of type adult
    And each matching donor is TGS (four field) typed at each locus
    And each matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search for each patient
    Then each set of results should contain the specified donor
