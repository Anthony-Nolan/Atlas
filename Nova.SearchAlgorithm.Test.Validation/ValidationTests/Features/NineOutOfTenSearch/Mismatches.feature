Feature: Nine Out Of Ten Search - mismatches
  As a member of the search team
  I want to be able to run a 9/10 search
  And see single mismatches at specified loci in the results

  Scenario: 9/10 Search at A with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus A
    Then the results should contain the specified donor  
  
  Scenario: 9/10 Search at A with a doubly mismatched donor at A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus A
    Then the results should not contain the specified donor

  Scenario: 9/10 Search at A with a mismatched donor at B
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus A
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at B with a mismatched donor at B
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus B
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at B with a doubly mismatched donor at B
    Given a patient and a donor
    And the donor has a double mismatch at locus B
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus B
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at B with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus B
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at DRB1 with a mismatched donor at DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus DRB1
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus DRB1
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at DRB1 with a doubly mismatched donor at DRB1
    Given a patient and a donor
    And the donor has a double mismatch at locus DRB1
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus DRB1
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at DRB1 with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus DRB1
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at C with a mismatched donor at C
    Given a patient and a donor
    And the donor has a single mismatch at locus C
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus C
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at C with a doubly mismatched donor at C
    Given a patient and a donor
    And the donor has a double mismatch at locus C
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus C
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at C with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus C
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at DQB1 with a mismatched donor at DQB1
    Given a patient and a donor
    And the donor has a single mismatch at locus DQB1
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus DQB1
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at DQB1 with a doubly mismatched donor at DQB1
    Given a patient and a donor
    And the donor has a double mismatch at locus DQB1
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus DQB1
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at DQB1 with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor is of type adult
    And the donor is TGS typed at each locus
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus DQB1
    Then the results should not contain the specified donor