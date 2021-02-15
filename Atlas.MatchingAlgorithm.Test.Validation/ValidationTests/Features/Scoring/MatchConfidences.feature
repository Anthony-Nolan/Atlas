Feature: Scoring - Match Confidences
  As a member of the search team
  I want search results to have an appropriate match confidence

  Scenario: Definite match at each locus - patient and donor unambiguously typed
    Given a patient has a match
	And the matching donor is unambiguously typed at each locus
	And the patient is unambiguously typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match confidence should be Definite at each locus at both positions

  Scenario: Exact match at each locus - patient unambiguously typed and donor ambiguously (single P group) typed
    Given a patient has a match
    And the matching donor is ambiguously (single P group) typed at each locus
	And the patient is unambiguously typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at each locus at both positions

  Scenario: Exact match at each locus - patient ambiguously (single P group) typed and donor unambiguously typed
    Given a patient has a match
	And the matching donor is unambiguously typed at each locus
    And the patient is ambiguously (single P group) typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at each locus at both positions

  Scenario: Exact match at each locus - patient and donor ambiguously (single P group) typed
    Given a patient has a match
    And the matching donor is ambiguously (single P group) typed at each locus
    And the patient is ambiguously (single P group) typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at each locus at both positions

  Scenario: Potential match at each locus - patient unambiguously typed and donor ambiguously (multiple P groups) typed
    Given a patient has a match
    And the matching donor is ambiguously (multiple P groups) typed at each locus
	And the patient is unambiguously typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match at each locus - patient ambiguously (multiple P groups) typed and donor unambiguously typed
    Given a patient has a match
	And the matching donor is unambiguously typed at each locus
    And the patient is ambiguously (multiple P groups) typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match at each locus - patient ambiguously (single P group) typed and donor ambiguously (multiple P groups) typed
    Given a patient has a match
    And the matching donor is ambiguously (multiple P groups) typed at each locus
    And the patient is ambiguously (single P group) typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match at each locus - patient ambiguously (multiple P groups) typed and donor ambiguously (single P group) typed
    Given a patient has a match
    And the matching donor is ambiguously (single P group) typed at each locus
    And the patient is ambiguously (multiple P groups) typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match at each locus - patient and donor ambiguously (multiple P groups) typed
    Given a patient has a match
    And the matching donor is ambiguously (multiple P groups) typed at each locus
    And the patient is ambiguously (multiple P groups) typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at each locus at both positions

  Scenario: Potential match - donor untyped at C
    Given a patient has a match
    And the matching donor is untyped at locus C
    And scoring is enabled at locus C
    When I run a 6/6 search
    Then the match confidence should be Potential at C at both positions

  Scenario: Potential match - patient untyped at C
    Given a patient has a match
    And the patient is untyped at locus C
    And scoring is enabled at locus C
    When I run a 6/6 search
    Then the match confidence should be Potential at C at both positions

  Scenario: Potential match - patient and donor untyped at C
    Given a patient has a match
    And the matching donor is untyped at locus C
    And the patient is untyped at locus C
    And scoring is enabled locus C
    When I run a 6/6 search
    Then the match confidence should be Potential at C at both positions

  Scenario: Mismatch confidence - double mismatch at locus A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And scoring is enabled at locus A
    When I run an 8/10 search
    Then the match confidence should be Mismatch at A at both positions

  Scenario: Donor typing is a split mismatch to the patient at locus B
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |C_1    |C_2    |DQB1_1 |DQB1_2 |
    |*01:01 |*01:02 |52     |37     |*15:03 |*03:01 |*06:02 |*16:02 |*05:02 |*02:01 |
    And the patient has the following HLA:
    |A_1    |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |C_1    |C_2    |DQB1_1 |DQB1_2 |
    |*01:01 |*01:02 |*51:01 |*37:01 |*15:03 |*03:01 |*06:02 |*16:02 |*05:02 |*02:01 |
    And scoring is enabled at locus B
    When I run a 9/10 search at locus B
    Then the match confidence should be Mismatch at B at position 1

  Scenario: Potential match - patient and donor untyped at Dpb1
    Given a patient has a match
    And the patient is untyped at locus DPB1
    And the matching donor is untyped at locus DPB1
    And scoring is enabled at locus DPB1
    When I run a 10/10 search
    Then the match confidence should be Potential at Dpb1 at both positions
