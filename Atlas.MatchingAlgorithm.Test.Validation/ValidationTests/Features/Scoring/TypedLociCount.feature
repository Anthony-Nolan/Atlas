Feature: Scoring - Typed Loci Count
  As a member of the search team
  I want search results to reveal the number of typed loci of a donor

  Scenario: Fully Typed Donor with no loci excluded from aggregate scoring
    Given a patient has a match
    And the matching donor is unambiguously typed at each locus
    And scoring includes all loci
    When I run a 10/10 search
    Then the typed loci count should be 6

  Scenario: Fully Typed Donor with DPB1 excluded from aggregate scoring
    Given a patient has a match
    And the matching donor is untyped at locus DPB1
    And locus DPB1 is excluded from aggregate scoring
    And scoring includes all loci
    When I run a 10/10 search
    Then the typed loci count should be 5

  Scenario: Poorly Typed Donor
    Given a patient has a match
    And the matching donor is untyped at locus DPB1
    And the matching donor is untyped at locus DQB1
    And the matching donor is untyped at locus C
    And scoring includes all loci
    When I run a 10/10 search
    Then the typed loci count should be 3