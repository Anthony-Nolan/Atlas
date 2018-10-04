Feature: Ten Out Of Ten Search
  As a member of the search team
  I want to be able to run a 10/10 search
  
  Scenario: 10/10 Search with a homozygous donor
    Given a patient has a match
    And the matching donor is homozygous at locus A
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a homozygous patient
    Given a patient has a match
    And the patient is homozygous at locus A
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a p group match
    Given a patient has a match
    And the match level is p-group
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with a match with differing fourth field
    Given a patient has a match
    And the matching donor is 'TGS derived data at four-field resolution' typed at each locus
    And the match level is three field (different fourth field)
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with a match with differing third field
    Given a patient has a match
    And the matching donor is 'TGS derived data at three-field resolution' typed at each locus
    And the match level is two field (different third field)
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a match with an expression suffix
    Given a patient has a match
    And the matching donor has an allele with any (non-null) expression suffix at locus A
    And the matching donor has an allele with any (non-null) expression suffix at locus B
    And the matching donor has an allele with any (non-null) expression suffix at locus C
    And the matching donor has an allele with any (non-null) expression suffix at locus DQB1
    And the matching donor has an allele with any (non-null) expression suffix at locus DPB1
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a cross match
    Given a patient has a match
    And the match orientation is cross at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor
 
  Scenario: 10/10 Search with a direct match
    Given a patient has a match
    And the match orientation is direct at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor