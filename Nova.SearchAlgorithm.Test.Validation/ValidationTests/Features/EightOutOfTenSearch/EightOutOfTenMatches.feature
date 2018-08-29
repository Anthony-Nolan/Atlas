Feature: Eight Out Of Ten Search - matches
  As a member of the search team
  I want to be able to run a 8/10 search
  And see 8/10 matches in the results

  Scenario: 8/10 Search with a double mismatch at locus A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with a double mismatch at locus B
    Given a patient and a donor
    And the donor has a double mismatch at locus B
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with a double mismatch at locus C
    Given a patient and a donor
    And the donor has a double mismatch at locus C
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with a double mismatch at locus DRB1
    Given a patient and a donor
    And the donor has a double mismatch at locus DRB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with a double mismatch at locus DQB1
    Given a patient and a donor
    And the donor has a double mismatch at locus DQB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus A and B
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus B
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus A and C
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus C
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus A and DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus DRB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus A and DQB1
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus DQB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus B and C
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus C
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus B and DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus DRB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus B and DQB1
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus DQB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus C and DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus C
    And the donor has a single mismatch at locus DRB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus C and DQB1
    Given a patient and a donor
    And the donor has a single mismatch at locus C
    And the donor has a single mismatch at locus DQB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with one mismatch each at locus DRB1 and DQB1
    Given a patient and a donor
    And the donor has a single mismatch at locus DRB1
    And the donor has a single mismatch at locus DQB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should contain the specified donor