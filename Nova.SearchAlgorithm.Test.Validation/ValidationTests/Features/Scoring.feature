Feature: Scoring
  As a member of the search team
  I want search results to have an appropriate score

  Scenario: gDNA match at all loci
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS (four field) typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is gDNA
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be gDNA at all loci at both positions
    
  Scenario: P-group match at all loci
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is p-group
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be p-group at all loci at both positions
  
  Scenario: G-group match at all loci
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is g-group
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be g-group at all loci at both positions
    
  Scenario: cDNA match at all loci
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is cDNA
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be cDNA at all loci at both positions  
        
  Scenario: Protein match at all loci
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is protein
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be protein at all loci at both positions  
        
  Scenario: Serology match at all loci - donor serology typed
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is serology typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be serology at all loci at both positions  
        
  Scenario: Serology match at all loci - patient serology typed
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the patient is serology typed at all loci
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be serology at all loci at both positions  
        
  Scenario: Serology match at all loci - donor and patient serology typed
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is serology typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the patient is serology typed at all loci
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be serology at all loci at both positions  
    
  Scenario: Three field (not fourth field) match at all loci
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is of type adult
    And the matching donor is TGS (four field) typed at each locus
    And the matching donor is in registry: Anthony Nolan
    And the match level is three field (different fourth field)
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then the match grade should be cDNA at all loci at both positions