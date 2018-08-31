Feature: Scoring - Ranking
  As a member of the search team
  I want search results to be returned in an appropriate order

  Scenario: Search with multiple matches with different match counts
    Given a patient has multiple matches with different match counts
    And all matching donors are of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the 8/8 result should be returned above the 7/8 result