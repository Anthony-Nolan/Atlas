Feature: Four out of eight Search - single mismatch
  As a member of the search team
  I want to be able to run a 4/8 cord search
  And see results with a single mismatch

  Scenario: 4/8 Search with a single mismatch at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with a single mismatch at B
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with a single mismatch at DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus DRB1
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with a single mismatch at C
    Given a patient and a donor
    And the donor has a single mismatch at locus C
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    