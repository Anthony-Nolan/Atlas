Feature: Ten Out Of Ten Search - Multiple patients
  As a member of the search team
  I want to be able to run multiple 10/10 search
  For different patients

  Scenario: 10/10 Search for a selection of 4-field 'TGS derived data' typed donors
    Given a set of 10 patients with matching donors
    And each matching donor is 'TGS derived data at four-field resolution' typed at each locus
    When I run a 10/10 search for each patient
    Then each set of results should contain the specified donor

  Scenario: 10/10 Search for a selection of 3-field 'TGS derived data' typed donors
    Given a set of 10 patients with matching donors
    And each matching donor is 'TGS derived data at three-field resolution' typed at each locus
    When I run a 10/10 search for each patient
    Then each set of results should contain the specified donor

  Scenario: 10/10 Search for a selection of 2-field 'TGS derived data' typed donors
    Given a set of 10 patients with matching donors
    And each matching donor is 'TGS derived data at two-field resolution' typed at each locus
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search for each patient
    Then each set of results should contain the specified donor

  Scenario: 10/10 Search for a selection of 'TGS derived data' typed donors
    Given a set of 10 patients with matching donors
    And each matching donor is 'TGS derived data' typed at each locus
    When I run a 10/10 search for each patient
    Then each set of results should contain the specified donor

  Scenario: 10/10 Search for a selection of variously typed donors
    Given a set of 100 patients with matching donors
    And each matching donor is arbitrarily typed at each locus
    When I run a 10/10 search for each patient
    Then each set of results should contain the specified donor
