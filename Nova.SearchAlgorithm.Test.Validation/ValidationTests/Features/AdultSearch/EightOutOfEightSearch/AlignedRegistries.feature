Feature: Eight out of Eight Aligned Registry Search
  As a member of the search team
  I want to be able to run a 8/8 aligned registry search
  
  Scenario: 8/8 Aligned Registry Search - Anthony Nolan
    Given a patient has a match
    And the matching donor is a 8/8 match
    And the matching donor is in registry: Anthony Nolan
    And the search is run for aligned registries
    When I run an 8/8 search
    Then the results should contain the specified donor  
    
  Scenario: 8/8 Aligned Registry Search - DKMS
    Given a patient has a match
    And the matching donor is a 8/8 match
    And the matching donor is in registry: DKMS
    And the search is run for aligned registries
    When I run an 8/8 search
    Then the results should contain the specified donor
    
  Scenario: 8/8 Aligned Registry Search - BBMR
    Given a patient has a match
    And the matching donor is a 8/8 match
    And the matching donor is in registry: BBMR
    And the search is run for aligned registries
    When I run an 8/8 search
    Then the results should contain the specified donor
    
  Scenario: 8/8 Aligned Registry Search - WBMDR
    Given a patient has a match
    And the matching donor is a 8/8 match
    And the matching donor is in registry: WBMDR
    And the search is run for aligned registries
    When I run an 8/8 search
    Then the results should contain the specified donor