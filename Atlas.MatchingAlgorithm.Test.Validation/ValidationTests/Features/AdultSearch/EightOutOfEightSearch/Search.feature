﻿Feature: Eight Out Of Eight Search
  As a member of the search team
  I want to be able to run an 8/8 search

  Scenario: 8/8 Search
    Given a patient has a match
    And the matching donor is a 8/8 match
    When I run an 8/8 search
    Then the results should contain the specified donor
    
  Scenario: 8/8 Search with untyped patient at DQB1
    Given a patient has a match
	And the patient is untyped at Locus DQB1
    And the matching donor is a 8/8 match
    When I run an 8/8 search
    Then the results should contain the specified donor
