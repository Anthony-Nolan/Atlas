Feature: Ten Out Of Ten Aligned Registry Search
  As a member of the search team
  I want to be able to run a 10/10 aligned registry search
  
  Scenario: 10/10 Aligned Registry Search - Anthony Nolan
    Given a patient has a match
    And the matching donor is in registry: Anthony Nolan
    And the search is run for aligned registries
    When I run a 10/10 search
    Then the results should contain the specified donor  
    
  Scenario: 10/10 Aligned Registry Search - DKMS
    Given a patient has a match
    And the matching donor is in registry: DKMS
    And the search is run for aligned registries
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Aligned Registry Search - BBMR
    Given a patient has a match
    And the matching donor is in registry: BBMR
    And the search is run for aligned registries
    When I run a 10/10 search
    Then the results should contain the specified donor
    
  Scenario: 10/10 Aligned Registry Search - WBMDR
    Given a patient has a match
    And the matching donor is in registry: WBMDR
    And the search is run for aligned registries
    When I run a 10/10 search
    Then the results should contain the specified donor