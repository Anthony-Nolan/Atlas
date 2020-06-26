Feature: Ten Out Of Ten Search - Typing Resolutions
  As a member of the search team
  I want to be able to run a 10/10 search
  For a variety of different typing resolutions

  // TODO: ATLAS-57 - Add P & G group tests

  Scenario: 10/10 Search with a 'TGS derived data at four-field resolution' typed match
    Given a patient has a match
    And the matching donor is 'TGS derived data at four-field resolution' typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with a 'TGS derived data at three-field resolution' typed match
    Given a patient has a match
    And the matching donor is 'TGS derived data at three-field resolution' typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with a 'TGS derived data at two-field resolution' typed match
    Given a patient has a match
    And the matching donor is 'TGS derived data at three-field resolution' typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor
     
  Scenario: 10/10 Search with a 'TGS derived data' typed match
    Given a patient has a match
    And the matching donor is 'TGS derived data' typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with untyped donor at C
    Given a patient has a match
    And the matching donor is untyped at locus C
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with untyped donor at DQB1
    Given a patient has a match
    And the matching donor is untyped at locus DQB1
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with untyped donor at C and DQB1
    Given a patient has a match
    And the matching donor is untyped at locus C
    And the matching donor is untyped at locus DQB1
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a three field truncated allele typed match
    Given a patient has a match
    And the matching donor is three field truncated allele typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a two field truncated allele typed match
    Given a patient has a match
    And the matching donor is two field truncated allele typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a XX code typed match
    Given a patient has a match
    And the matching donor is XX code typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a NMDP code typed match
    Given a patient has a match
    And the matching donor is NMDP code typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with an allele string (of names) typed match
    Given a patient has a match
    And the matching donor is allele string (of names) typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with an allele string (of names) with different antigen groups
    Given a patient has a match
    And the matching donor is allele string (of names) typed at each locus
    And the matching donor's allele string contains different antigen groups at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with an allele string (of subtypes) typed match
    Given a patient has a match
    And the matching donor is allele string (of subtypes) typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a serology typed match
    Given a patient has a match
    And the matching donor is serology typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a mixed typing resolution match
    Given a patient has a match
    And the matching donor is differently typed at each locus
    When I run a 10/10 search
    Then the results should contain the specified donor
 
  Scenario: 10/10 Search with matches at multiple resolutions
    Given a patient has multiple matches at different typing resolutions
    When I run a 10/10 search
    Then the results should contain all specified donors