Feature: Scoring - Match Confidences
  As a member of the search team
  I want search results to have an appropriate match confidence

  Scenario: Definite match at all loci - donor and patient TGS typed
    Given a patient has a match
	And the matching donor is TGS typed at each locus
	And the patient is TGS typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Definite at all loci at both positions

  Scenario: Exact match at all loci - donor allele string typed
    Given a patient has a match
	And the matching donor is allele string (single P group) typed at each locus
	And the patient is TGS typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at all loci at both positions

  Scenario: Exact match at all loci - patient allele string typed
    Given a patient has a match
	And the matching donor is TGS typed at each locus
	And the patient is allele string (single P group) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at all loci at both positions

  Scenario: Exact match at all loci - donor and patient allele string typed
    Given a patient has a match
	And the matching donor is allele string (single P group) typed at each locus
	And the patient is allele string (single P group) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at all loci at both positions

  Scenario: Exact match at all loci - donor NMDP code typed
    Given a patient has a match
	And the matching donor is NMDP code (single P group) typed at each locus
	And the patient is TGS typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at all loci at both positions

  Scenario: Exact match at all loci - patient NMDP code typed
    Given a patient has a match
	And the matching donor is TGS typed at each locus
	And the patient is NMDP code (single P group) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at all loci at both positions

  Scenario: Exact match at all loci - donor and patient NMDP code typed
    Given a patient has a match
	And the matching donor is NMDP code (single P group) typed at each locus
	And the patient is NMDP code (single P group) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at all loci at both positions

  Scenario: Exact match at all loci - donor NMDP code typed and patient allele string typed
    Given a patient has a match
	And the matching donor is NMDP code (single P group) typed at each locus
	And the patient is allele string (single P group) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at all loci at both positions

  Scenario: Exact match at all loci - donor allele string typed and patient NMDP code typed
    Given a patient has a match
	And the matching donor is allele string (single P group) at each locus
	And the patient is NMDP code (single P group) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Exact at all loci at both positions

  Scenario: Potential match at all loci - patient TGS typed
    Given a patient has a match
	And the matching donor is low resolution (multiple P groups) typed at each locus
	And the patient is TGS typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at all loci at both positions

  Scenario: Potential match at all loci - donor TGS typed
    Given a patient has a match
	And the matching donor is TGS typed at each locus
	And the patient is low resolution (multiple P groups) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at all loci at both positions

  Scenario: Potential match at all loci - patient allele string typed
    Given a patient has a match
	And the matching donor is low resolution (multiple P groups) typed at each locus
	And the patient is allele string (single P group) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at all loci at both positions

  Scenario: Potential match at all loci - donor allele string typed
    Given a patient has a match
	And the matching donor is allele string (single P group) typed at each locus
	And the patient is low resolution (multiple P groups) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at all loci at both positions

  Scenario: Potential match at all loci - patient NMDP code typed
    Given a patient has a match
	And the matching donor is low resolution (multiple P groups) typed at each locus
	And the patient is NMDP code (single P group) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at all loci at both positions

  Scenario: Potential match at all loci - donor NMDP code typed
    Given a patient has a match
	And the matching donor is NMDP code (single P group) typed at each locus
	And the patient is low resolution (multiple P groups) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at all loci at both positions

  Scenario: Potential match at all loci - donor and patient low resolution typed
    Given a patient has a match
	And the matching donor is low resolution (multiple P groups) typed at each locus
	And the patient is low resolution (multiple P groups) typed at all loci
    When I run a 10/10 search
    Then the match confidence should be Potential at all loci at both positions

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