Feature: Six Out Of Six Search
  As a member of the search team
  I want to be able to run an 6/6 search

  Scenario: 6/6 Search
    Given a patient has a match
    When I run a 6/6 search
    Then the results should contain the specified donor  

  Scenario: 6/6 Search with untyped patient at C
    Given a patient has a match
	And the patient is untyped at Locus C
    When I run a 6/6 search
    Then the results should contain the specified donor

  Scenario: 6/6 Search with untyped patient at DQB1
    Given a patient has a match
    And the patient is untyped at Locus DQB1
    When I run a 6/6 search
    Then the results should contain the specified donor
	
  Scenario: 6/6 Search with untyped patient at C and DQB1
    Given a patient has a match
	And the patient is untyped at Locus C
    And the patient is untyped at Locus DQB1
    When I run a 6/6 search
    Then the results should contain the specified donor