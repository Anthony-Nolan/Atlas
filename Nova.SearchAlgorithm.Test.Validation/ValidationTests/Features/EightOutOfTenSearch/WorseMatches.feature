Feature: Eight Out Of Ten Search - worse matches
  As a member of the search team
  I want to be able to run a 8/10 search
  And not see worse matches than 8/10 in the results

  Scenario: 8/10 Search with three mismatches across two loci
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a double mismatch at locus B
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should not contain the specified donor

  Scenario: 8/10 Search with three mismatches across three loci
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus C
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should not contain the specified donor

  Scenario: 8/10 Search with mismatches at all loci
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus C
    And the donor has a single mismatch at locus DRB1
    And the donor has a single mismatch at locus DQB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should not contain the specified donor