Feature: Six out of Six Aligned Registry Search
  As a member of the search team
  I want to be able to run a 6/6 aligned registry search
  
  Scenario: 6/6 Aligned Registry Search - Anthony Nolan
    Given a patient has a match
    And the matching donor is in registry: Anthony Nolan
    And the search is run for aligned registries
    When I run a 6/6 search
    Then the results should contain the specified donor  
    
  Scenario: 6/6 Aligned Registry Search - DKMS
    Given a patient has a match
    And the matching donor is in registry: DKMS
    And the search is run for aligned registries
    When I run a 6/6 search
    Then the results should contain the specified donor
    
  Scenario: 6/6 Aligned Registry Search - BBMR
    Given a patient has a match
    And the matching donor is in registry: BBMR
    And the search is run for aligned registries
    When I run a 6/6 search
    Then the results should contain the specified donor
    
  Scenario: 6/6 Aligned Registry Search - WBMDR
    Given a patient has a match
    And the matching donor is in registry: WBMDR
    And the search is run for aligned registries
    When I run a 6/6 search
    Then the results should contain the specified donor