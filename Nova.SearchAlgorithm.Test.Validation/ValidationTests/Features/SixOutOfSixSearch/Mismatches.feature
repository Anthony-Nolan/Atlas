Feature: Six Out Of Six Search - mismatches
  As a member of the search team
  I want to be able to run a 6/6 search
  And see no mismatches at specified loci in the results

  Scenario: 6/6 Search with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    When I run a 6/6 search
    Then the results should not contain the specified donor  
  
  Scenario: 6/6 Search with a doubly mismatched donor at A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    When I run a 6/6 search
    Then the results should not contain the specified donor

  Scenario: 6/6 Search with a mismatched donor at B
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    When I run a 6/6 search
    Then the results should not contain the specified donor

  Scenario: 6/6 Search with a mismatched donor at Drb1
    Given a patient and a donor
    And the donor has a single mismatch at locus Drb1
    When I run a 6/6 search
    Then the results should not contain the specified donor
  
  Scenario: 6/6 Search with a mismatched donor at C
    Given a patient has a match
    And the donor has a single mismatch at locus C
    When I run a 6/6 search
    Then the results should contain the specified donor

  Scenario: 6/6 Search with a mismatched donor at Dqb1
    Given a patient has a match
    And the donor has a single mismatch at locus Dqb1
    When I run a 6/6 search
    Then the results should contain the specified donor
    
  Scenario: 6/6 Search with a mismatched donor at Dpb1
    Given a patient has a match
    And the donor has a single mismatch at locus Dpb1
    When I run a 6/6 search
    Then the results should contain the specified donor