Feature: Ten Out Of Ten Search - Typing Resolutions
  As a member of the search team
  I want to be able to run a 10/10 search
  For a variety of different typing resolutions

  Scenario: 10/10 Search with a TGS (4-field) typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS (four field) typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with a TGS (3-field) typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS (three field) typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with a TGS (2-field) typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS (two field) typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
     
  Scenario: 10/10 Search with a TGS typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with untyped donor at C
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is untyped at Locus C
    And the matching donor is of type adult
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with untyped donor at Dqb1
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is untyped at Locus Dqb1
    And the matching donor is of type adult
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with untyped donor at C and Dqb1
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is untyped at Locus C
    And the matching donor is untyped at Locus Dqb1
    And the matching donor is of type adult
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a three field truncated allele typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is three field truncated allele typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a two field truncated allele typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is two field truncated allele typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a XX code typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is XX code typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a NMDP code typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is NMDP code typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with an allele string (of names) typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is allele string (of names) typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Search with an allele string (of subtypes) typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is allele string (of subtypes) typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a serology typed match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is serology typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search with a mixed typing resolution match
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is differently typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the results should contain the specified donor
 
  Scenario: 10/10 Search with matches at multiple resolutions
    Given a patient has multiple matches at different typing resolutions
    And all matching donors are of type adult
    And all matching donors are in registry: Anthony Nolan
    When I run a 10/10 search
    Then the results should contain all specified donors

    
