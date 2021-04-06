Feature: Scoring - Match Grades
  As a member of the search team
  I want search results to have an appropriate match grade

  Scenario: gDNA match at all loci
    Given a patient has a match
    And the matching donor is 'TGS derived data at four-field resolution' typed at each locus
    And the match level is gDNA
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match grade should be gDNA at all loci at both positions

  Scenario: cDNA match at all loci
    Given a patient has a match
    And the match level is cDNA
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match grade should be cDNA at all loci at both positions

  Scenario: Three field (not fourth field) match at all loci
    Given a patient has a match
    And the matching donor is 'TGS derived data at four-field resolution' typed at each locus
    And the match level is three field (different fourth field)
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match grade should be cDNA at all loci at both positions

  Scenario: Protein match at all loci
    Given a patient has a match
    And the match level is protein
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match grade should be protein at all loci at both positions

  Scenario: G-group match at all loci
    Given a patient has a match
    And the match level is g-group
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match grade should be g-group at all loci at both positions
    
  Scenario: P-group match at all loci
    Given a patient has a match
    And the match level is p-group
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match grade should be p-group at all loci at both positions
        
  Scenario: Serology match at all loci - donor serology typed
    Given a patient has a match
    And the matching donor is serology typed at each locus
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match grade should be serology at all loci except DPB1 at both positions  
        
  Scenario: Serology match at all loci - patient serology typed
    Given a patient has a match
    And the patient is serology typed at all loci
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match grade should be serology at all loci except DPB1 at both positions  

  Scenario: Serology match at all loci - donor and patient serology typed
    Given a patient has a match
    And the matching donor is serology typed at each locus
    And the patient is serology typed at all loci
    And scoring is enabled at all loci
    When I run a 10/10 search
    Then the match grade should be serology at all loci except DPB1 at both positions  

  Scenario: P-group match - donor untyped at C
    Given a patient has a match
    And the matching donor is untyped at locus C
    And scoring is enabled at locus C
    When I run a 6/6 search
    Then the match grade should be p-group at C at both positions

  Scenario: P-group match - patient untyped at C
    Given a patient has a match
    And the patient is untyped at locus C
    And scoring is enabled at locus C
    When I run a 6/6 search
    Then the match grade should be p-group at C at both positions

  Scenario: P-group match - patient and donor untyped at C
    Given a patient has a match
    And the matching donor is untyped at locus C
    And the patient is untyped at locus C
    And scoring is enabled at locus C
    When I run a 6/6 search
    Then the match grade should be p-group at C at both positions

  Scenario: Mismatch grade - double mismatch at locus A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    And scoring is enabled at locus A
    When I run an 8/10 search
    Then the match grade should be mismatch at A at both positions

  Scenario: Permissive Mismatch grade - patient and donor mismatched at DPB1, same TCE groups
    Given a patient has a match
    And the patient and donor have mismatched DPB1 alleles with the same TCE group assignments
    And scoring is enabled at locus DPB1
    When I run a 6/6 search
    Then the match grade should be permissive mismatch at DPB1 at both positions

  Scenario: Mismatch grade - patient and donor mismatched at DPB1, different TCE groups
    Given a patient has a match
    And the patient and donor have mismatched DPB1 alleles with different TCE group assignments
    And scoring is enabled at locus DPB1
    When I run a 6/6 search
    Then the match grade should be mismatch at DPB1 at both positions

  Scenario: Mismatch grade - patient and donor mismatched at DPB1, no TCE groups
    Given a patient has a match
    And the patient and donor have mismatched DPB1 alleles with no TCE group assignments
    And scoring is enabled at locus DPB1
    When I run a 6/6 search
    Then the match grade should be mismatch at DPB1 at both positions
    
  Scenario: Unknown match - only donor untyped at DPB1
    Given a patient has a match
    And the matching donor is untyped at locus DPB1
    And scoring is enabled at locus DPB1
    When I run a 6/6 search
    Then the match grade should be unknown at DPB1 at both positions

  Scenario: Unknown match - only patient untyped at DPB1
    Given a patient has a match
    And the patient is untyped at locus DPB1
    And scoring is enabled at locus DPB1
    When I run a 6/6 search
    Then the match grade should be unknown at DPB1 at both positions
    
  Scenario: Unknown match - patient and donor untyped at DPB1
    Given a patient has a match
    And the patient is untyped at locus DPB1
    And the matching donor is untyped at locus DPB1
    And scoring is enabled at locus DPB1
    When I run a 6/6 search
    Then the match grade should be unknown at DPB1 at both positions

  Scenario: Serology match - Patient has serology and Donor has MAC that only expands to 3+ field alleles
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1    |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*01:AC |*66:01 |*57:01 |*41:01 |*13:XX |*07:01 |
    And the patient has the following HLA:
    |A_1 |A_2     |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |1   |*66:01  |*57:01 |*41:01 |*13:XX |*07:01 |
    And scoring is enabled at locus A
    When I run a 6/6 search
    Then the match grade should be serology at A at position 1

  Scenario: Serology match - Patient has serology and Donor has allele string that only expands to 3+ field alleles
    Given a patient has a match
    And the matching donor has the following HLA:
    |A_1       |A_2    |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |*01:01/03 |*66:01 |*57:01 |*41:01 |*13:XX |*07:01 |
    And the patient has the following HLA:
    |A_1 |A_2     |B_1    |B_2    |DRB1_1 |DRB1_2 |
    |1   |*66:01  |*57:01 |*41:01 |*13:XX |*07:01 |
    And scoring is enabled at locus A
    When I run a 6/6 search
    Then the match grade should be serology at A at position 1