Feature: Eight Out Of Eight Search - mismatches
  As a member of the search team
  I want to be able to run an 8/8 search
  And see no mismatches at specified loci in the results

  Scenario: 8/8 Search with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    When I run an 8/8 search
    Then the results should not contain the specified donor  
  
  Scenario: 8/8 Search with a doubly mismatched donor at A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    When I run an 8/8 search
    Then the results should not contain the specified donor

  Scenario: 8/8 Search with a mismatched donor at B
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    When I run an 8/8 search
    Then the results should not contain the specified donor

  Scenario: 8/8 Search with a mismatched donor at Drb1
    Given a patient and a donor
    And the donor has a single mismatch at locus Drb1
    When I run an 8/8 search
    Then the results should not contain the specified donor
  
  Scenario: 8/8 Search with a mismatched donor at C
    Given a patient and a donor
    And the donor has a single mismatch at locus C
    When I run an 8/8 search
    Then the results should not contain the specified donor

  Scenario: 8/8 Search with a mismatched donor at Dqb1
    Given a patient has a match
    And the donor has a single mismatch at locus Dqb1
    When I run an 8/8 search
    Then the results should contain the specified donor
    
  Scenario: 8/8 Search with a mismatched donor at Dpb1
    Given a patient has a match
    And the donor has a single mismatch at locus Dpb1
    When I run an 8/8 search
    Then the results should contain the specified donor