Feature: Scoring - Ranking (match counts)
  As a member of the search team
  I want search results to be returned in an appropriate order based on differing match counts

  Scenario: Search with multiple matches with different match counts - 10/8 vs 8/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run an 8/8 search
    Then a match at DQB1 should be returned above a mismatch at DQB1
  
  Scenario: Search with multiple matches with different match counts - 8/8 vs 7/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then an 8/8 result should be returned above a 7/8 result

  Scenario: Search with multiple matches with different match counts - 8/8 vs 6/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then an 8/8 result should be returned above a 6/8 result

  Scenario: Search with multiple matches with different match counts - 8/8 vs 5/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then an 8/8 result should be returned above a 5/8 result

  Scenario: Search with multiple matches with different match counts - 8/8 vs 4/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then an 8/8 result should be returned above a 4/8 result

  Scenario: Search with multiple matches with different match counts - 7/8 vs 6/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then a 7/8 result should be returned above a 6/8 result

  Scenario: Search with multiple matches with different match counts - 7/8 vs 5/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then a 7/8 result should be returned above a 5/8 result

  Scenario: Search with multiple matches with different match counts - 7/8 vs 4/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then a 7/8 result should be returned above a 4/8 result

  Scenario: Search with multiple matches with different match counts - 6/8 vs 5/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then a 6/8 result should be returned above a 5/8 result

  Scenario: Search with multiple matches with different match counts - 6/8 vs 4/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then a 6/8 result should be returned above a 4/8 result

  Scenario: Search with multiple matches with different match counts - 5/8 vs 4/8
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    And scoring includes all loci
    When I run a 4/8 search
    Then a 5/8 result should be returned above a 4/8 result
