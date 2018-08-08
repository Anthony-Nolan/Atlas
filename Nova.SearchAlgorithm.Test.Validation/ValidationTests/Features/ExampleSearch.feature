Feature: Example Search
  As a member of the search team
  I want to be able to search for matching donors

  Scenario: 6/6 Search for a matching donor
    Given I search for exact donor hla
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 6/6 search
    Then The result should contain at least one donor

  Scenario: 6/6 aligned registries search
    Given I search for exact donor hla
    And the search type is adult
    And The search is run for aligned registries
    When I run a 6/6 search
    Then The result should contain at least one donor

  Scenario: 8/8 Search for a matching donor
    Given I search for exact donor hla
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/8 search
    Then The result should contain at least one donor

  Scenario: 10/10 Search for a matching donor
    Given I search for exact donor hla
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 10/10 search
    Then The result should contain at least one donor
    
  Scenario: 9/10 Search for a matching donor
    Given I search for exact donor hla
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run a 9/10 search at locus A
    Then The result should contain no donors   
    
  Scenario: 8/10 Search for a matching donor
    Given I search for exact donor hla
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search at locus A
    Then The result should contain no donors

  Scenario: 6/6 Search for a matching cord
    Given I search for exact donor hla
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 6/6 search
    Then The result should contain at least one donor
    
  Scenario: 4/8 Search for a matching cord
    Given I search for exact donor hla
    And the search type is cord
    And the search is run against the Anthony Nolan registry only
    When I run a 4/8 search
    Then The result should contain at least one donor
    
  Scenario: 4/8 Search for a cord at multiple registries
    Given I search for exact donor hla
    And the search type is cord
    And The search is run for the registry: AN
    And The search is run for the registry: NHSBT
    And The search is run for the registry: NMDP
    And The search is run for the registry: France
    And The search is run for the registry: Italy
    When I run a 4/8 search
    Then The result should contain at least one donor
