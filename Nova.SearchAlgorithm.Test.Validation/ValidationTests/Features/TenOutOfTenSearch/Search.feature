Feature: Ten Out Of Ten Search
  As a member of the search team
  I want to be able to run a 10/10 search
  
  Scenario: 10/10 Search with a homozygous donor
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is homozygous at locus A
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a homozygous patient
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the patient is homozygous at locus A
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a p group match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is p-group
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with a match with differing fourth field
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS (four field) typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is three field (different fourth field)
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with a match with differing third field
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS (three field) typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is two field (different third field)
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a match with an expression suffix
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor has an allele with any (non-null) expression suffix at locus A
    And the matching donor has an allele with any (non-null) expression suffix at locus B
    And the matching donor has an allele with any (non-null) expression suffix at locus C
    And the matching donor has an allele with any (non-null) expression suffix at locus DQB1
    And the matching donor has an allele with any (non-null) expression suffix at locus DPB1
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
