Feature: Four out of eight Search - two mismatches
  As a member of the search team
  I want to be able to run a 4/8 cord search
  And see results with two mismatches

  Scenario: 4/8 Search with a double mismatch at A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with a double mismatch at B
    Given a patient and a donor
    And the donor has a double mismatch at locus B
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with a double mismatch at DRB1
    Given a patient and a donor
    And the donor has a double mismatch at locus DRB1
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with a double mismatch at C
    Given a patient and a donor
    And the donor has a double mismatch at locus C
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus A and B
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus B
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus A and C
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus C
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus A and DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus DRB1
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus B and C
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus C
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus B and DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus DRB1
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus C and DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus C
    And the donor has a single mismatch at locus DRB1
    And the donor is of type cord
    And the search type is cord
    When I run a 4/8 search
    Then the results should contain the specified donor