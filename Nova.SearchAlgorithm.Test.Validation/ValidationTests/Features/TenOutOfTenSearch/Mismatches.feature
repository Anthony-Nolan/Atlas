Feature: Ten Out Of Ten Search - mismatches
  As a member of the search team
  I want to be able to run a 10/10 search
  And see no mismatches at specified loci in the results

  Scenario: 10/10 Search with a mismatched donor at A
    Given a patient has a match
    And the matching donor has a single mismatch at locus A
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should not contain the specified donor

  Scenario: 10/10 Search with a mismatched donor at B
    Given a patient has a match
    And the matching donor has a single mismatch at locus B
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should not contain the specified donor

  Scenario: 10/10 Search with a mismatched donor at C
    Given a patient has a match
    And the matching donor has a single mismatch at locus C
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should not contain the specified donor

  Scenario: 10/10 Search with a mismatched donor at Dqb1
    Given a patient has a match
    And the matching donor has a single mismatch at locus Dqb1
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should not contain the specified donor

  Scenario: 10/10 Search with a mismatched donor at Drb1
    Given a patient has a match
    And the matching donor has a single mismatch at locus Drb1
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should not contain the specified donor

  Scenario: 10/10 Search with a mismatched donor at Dpb1
    Given a patient has a match
    And the matching donor has a single mismatch at locus Dpb1
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor