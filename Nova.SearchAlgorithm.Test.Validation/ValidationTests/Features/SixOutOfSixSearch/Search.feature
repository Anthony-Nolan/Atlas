Feature: Six Out Of Six Search
  As a member of the search team
  I want to be able to run an 6/6 search

  Scenario: 6/6 Search with untyped patient at C
    Given a patient has a match
	And the patient is untyped at Locus C
    And the matching donor is a 6/6 match    
    And the matching donor is TGS typed at each locus
    And the matching donor is of type adult
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 6/6 search
    Then the results should contain the specified donor

  Scenario: 6/6 Search with untyped patient at Dqb1
    Given a patient has a match
    And the patient is untyped at Locus Dqb1
    And the matching donor is a 6/6 match    
    And the matching donor is TGS typed at each locus
    And the matching donor is of type adult
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 6/6 search
    Then the results should contain the specified donor
	
  Scenario: 6/6 Search with untyped patient at C and Dqb1
    Given a patient has a match
	And the patient is untyped at Locus C
    And the patient is untyped at Locus Dqb1
    And the matching donor is a 6/6 match    
    And the matching donor is TGS typed at each locus
    And the matching donor is of type adult
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 6/6 search
    Then the results should contain the specified donor