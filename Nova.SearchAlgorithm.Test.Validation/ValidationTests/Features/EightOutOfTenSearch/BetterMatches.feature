Feature: Eight Out Of Ten Search - better matches
  As a member of the search team
  I want to be able to run a 8/10 search
  And not see better matches in the results

  Scenario: 8/10 Search with a fully matching donor
    Given a patient has a match
    And the matching donor is a 10/10 match
    And the matching donor is TGS typed at each locus
    And the matching donor is of type adult
    And the matching donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should not contain the specified donor

  Scenario: 8/10 Search with a single mismatch
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should not contain the specified donor
    
  Scenario: 8/10 Search with mismatches at locus A and DPB1
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus DPB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should not contain the specified donor
    
  Scenario: 8/10 Search with double mismatch at locus DPB1
    Given a patient and a donor
    And the donor has a double mismatch at locus DPB1
    And the donor is TGS typed at each locus
    And the donor is of type adult
    And the donor is in registry: Anthony Nolan
    And the search type is adult
    And the search is run against the Anthony Nolan registry only
    When I run an 8/10 search
    Then the results should not contain the specified donor