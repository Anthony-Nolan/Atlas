Feature: Scoring - Ranking (match confidence)
  As a member of the search team
  I want search results to be returned in an appropriate order based on differing match confidences

  Scenario: Match grade ranking - Definite vs Exact
    Given a patient has multiple matches with different match confidences
    When I run an 8/8 search
    Then a full definite match should be returned above a full exact match

  Scenario: Match grade ranking - Definite vs Potential
    Given a patient has multiple matches with different match confidences
    When I run an 8/8 search
    Then a full definite match should be returned above a full potential match

  Scenario: Match grade ranking - Exact vs Potential
    Given a patient has multiple matches with different match confidences
    When I run an 8/8 search
    Then a full exact match should be returned above a full potential match