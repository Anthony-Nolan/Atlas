Feature: Scoring - Match Confidences
  As a member of the search team
  I want search results to have an appropriate match confidence

  Scenario: Definite match at each locus - donor and patient TGS typed
    Given a patient has a match
    When I run a 10/10 search
    Then the match confidence should be Definite at each locus at both positions

  Scenario: Exact match at each locus - patient TGS typed and donor ambiguously (single P group) typed
    Given a patient has a match
    And the matching donor is ambiguously (single P group) typed at each locus
    When I run a 10/10 search
    Then the match confidence should be Exact at each locus at both positions

  Scenario: Exact match at each locus - patient ambiguously (single P group) typed and donor TGS typed
    Given a patient has a match
    And the patient is ambiguously (single P group) typed at each locus
    When I run a 10/10 search
    Then the match confidence should be Exact at each locus at both positions

  Scenario: Exact match at each locus - donor and patient ambiguously (single P group) typed
    Given a patient has a match
    And the matching donor is ambiguously (single P group) typed at each locus
    And the patient is ambiguously (single P group) typed at each locus
    When I run a 10/10 search
    Then the match confidence should be Exact at each locus at both positions

  Scenario: Potential match at each locus - patient TGS typed and donor ambiguously (multiple P groups) typed
    Given a patient has a match
    And the matching donor is ambiguously (multiple P groups) typed at each locus
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match at each locus - patient ambiguously (multiple P groups) typed and donor TGS typed
    Given a patient has a match
    And the patient is ambiguously (multiple P groups) typed at each locus
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match at each locus - patient ambiguously (single P group) typed and donor ambiguously (multiple P groups) typed
    Given a patient has a match
    And the matching donor is ambiguously (multiple P groups) typed at each locus
    And the patient is ambiguously (single P group) typed at each locus
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match at each locus - patient ambiguously (multiple P groups) typed and donor ambiguously (single P group) typed
    Given a patient has a match
    And the matching donor is ambiguously (single P group) typed at each locus
    And the patient is ambiguously (multiple P groups) typed at each locus
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match at each locus - donor and patient ambiguously (multiple P groups) typed
    Given a patient has a match
    And the matching donor is ambiguously (multiple P groups) typed at each locus
    And the patient is ambiguously (multiple P groups) typed at each locus
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match - donor untyped at C
    Given a patient has a match
    And the matching donor is untyped at Locus C
    When I run a 6/6 search
    Then the match confidence should be Potential at C at both positions

  Scenario: Potential match - patient untyped at C
    Given a patient has a match
    And the patient is untyped at Locus C
    When I run a 6/6 search
    Then the match confidence should be Potential at C at both positions

  Scenario: Potential match - donor and patient untyped at C
    Given a patient has a match
    And the matching donor is untyped at Locus C
    And the patient is untyped at Locus C
    When I run a 6/6 search
    Then the match confidence should be Potential at C at both positions

  Scenario: Mismatch confidence - double mismatch at locus A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    When I run an 8/10 search
    Then the match confidence should be Mismatch at A at both positions