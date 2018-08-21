Feature: Eight Out Of Eight Search
  As a member of the search team
  I want to be able to run an 8/8 search
    
  Scenario: 8/8 Search with untyped patient at Dqb1
    Given a patient has a match
	And the patient is untyped at Locus Dqb1
    And the matching donor is a 8/8 match    
    And the matching donor is TGS typed at each locus
    And the matching donor is of type adult
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/8 search
    Then the results should contain the specified donor
