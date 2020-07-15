Feature: Scoring - Match Categorisation
  As a member of the search team
  I want search results to have an appropriate overall match category

  Scenario: Definite match at each locus
    Given a patient has a match
    And the matching donor is unambiguously typed at each locus
    And the patient is unambiguously typed at each locus
    And scoring includes all loci
    When I run a 10/10 search
    Then the match category should be Definite

  Scenario: Exact match at each locus
    Given a patient has a match
    And the matching donor is ambiguously (single P group) typed at each locus
    And the patient is unambiguously typed at each locus
    And scoring includes all loci
    When I run a 10/10 search
    Then the match category should be Exact

  Scenario: Potential match at each locus
    Given a patient has a match
    And the matching donor is ambiguously (multiple P groups) typed at each locus
    And the patient is unambiguously typed at each locus
    And scoring includes all loci
    When I run a 10/10 search
    Then the match category should be Potential

  Scenario: Mismatch at one locus
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And scoring includes all loci
    When I run an 8/10 search
    Then the match category should be Mismatch

  Scenario: Permissive Mismatch at DPB1
    Given a patient has a match
    And the patient and donor have mismatched DPB1 alleles with the same TCE group assignments
    And scoring includes all loci
    When I run a 6/6 search
    Then the match category should be Permissive Mismatch

  Scenario: Excluded Permissive Mismatch at DPB1
    Given a patient has a match
    And the matching donor is ambiguously (multiple P groups) typed at each locus
    And the patient and donor have mismatched DPB1 alleles with the same TCE group assignments
    And locus DPB1 is excluded from aggregate scoring
    And scoring includes all loci
    When I run a 6/6 search
    Then the match category should be Potential

  Scenario: Excluded Mismatch at DPB1
    Given a patient has a match
    And the matching donor is ambiguously (multiple P groups) typed at each locus
    And the patient and donor have mismatched DPB1 alleles with no TCE group assignments
    And scoring includes all loci
    And locus DPB1 is excluded from aggregate scoring
    When I run a 6/6 search
    Then the match category should be Potential