Feature: Scoring - Ranking (match grades)
  As a member of the search team
  I want search results to be returned in an appropriate order based on differing match grades

  Scenario: Match grade ranking - gDNA vs cDNA
    Given a patient has multiple matches with different match grades
    When I run a 10/10 search
    Then a full gDNA match should be returned above a full cDNA match

  Scenario: Match grade ranking - gDNA vs protein
    Given a patient has multiple matches with different match grades
    When I run a 10/10 search
    Then a full gDNA match should be returned above a full protein match

  Scenario: Match grade ranking - gDNA vs g-group
    Given a patient has multiple matches with different match grades
    When I run a 10/10 search
    Then a full gDNA match should be returned above a full g-group match

  Scenario: Match grade ranking - gDNA vs p-group
    Given a patient has multiple matches with different match grades
    When I run a 10/10 search
    Then a full gDNA match should be returned above a full p-group match

  Scenario: Match grade ranking - gDNA vs serology
    Given a patient has multiple matches with different match grades
    When I run a 10/10 search
    Then a full gDNA match should be returned above a full serology match