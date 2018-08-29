Feature: Four out of eight Search - three mismatches
  As a member of the search team
  I want to be able to run a 4/8 cord search
  And see results with three mismatches

  Scenario: 4/8 Search with two mismatches at A, and one at B
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And the donor has a single mismatch at locus B
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Search with two mismatches at A, and one at DRB1
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And the donor has a single mismatch at locus DRB1
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Search with two mismatches at A, and one at C
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And the donor has a single mismatch at locus C
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with two mismatches at B, and one at A
    Given a patient and a donor
    And the donor has a double mismatch at locus B
    And the donor has a single mismatch at locus A
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Search with two mismatches at B, and one at DRB1
    Given a patient and a donor
    And the donor has a double mismatch at locus B
    And the donor has a single mismatch at locus DRB1
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Search with two mismatches at B, and one at C
    Given a patient and a donor
    And the donor has a double mismatch at locus B
    And the donor has a single mismatch at locus C
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with two mismatches at DRB1, and one at A
    Given a patient and a donor
    And the donor has a double mismatch at locus DRB1
    And the donor has a single mismatch at locus A
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Search with two mismatches at DRB1, and one at B
    Given a patient and a donor
    And the donor has a double mismatch at locus DRB1
    And the donor has a single mismatch at locus B
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Search with two mismatches at DRB1, and one at C
    Given a patient and a donor
    And the donor has a double mismatch at locus DRB1
    And the donor has a single mismatch at locus C
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with two mismatches at C, and one at A
    Given a patient and a donor
    And the donor has a double mismatch at locus C
    And the donor has a single mismatch at locus A
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Search with two mismatches at C, and one at B
    Given a patient and a donor
    And the donor has a double mismatch at locus C
    And the donor has a single mismatch at locus B
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
    
  Scenario: 4/8 Search with two mismatches at C, and one at DRB1
    Given a patient and a donor
    And the donor has a double mismatch at locus C
    And the donor has a single mismatch at locus DRB1
    And the donor is of type cord
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus A, B, and C
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus C
    And the donor is TGS typed at each locus
    And the donor is of type cord
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus A, B, and DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus DRB1
    And the donor is TGS typed at each locus
    And the donor is of type cord
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus A, C, and DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus C
    And the donor has a single mismatch at locus DRB1
    And the donor is TGS typed at each locus
    And the donor is of type cord
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor

  Scenario: 4/8 Search with one mismatch each at locus B, C, and DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    And the donor has a single mismatch at locus C
    And the donor has a single mismatch at locus DRB1
    And the donor is TGS typed at each locus
    And the donor is of type cord
    And the donor is in registry: Anthony Nolan
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then the results should contain the specified donor
