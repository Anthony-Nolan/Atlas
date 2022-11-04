Feature: Nine Out Of Ten Search - mismatches
  As a member of the search team
  I want to be able to run a 9/10 search
  And see single mismatches at specified loci in the results

  Scenario: 9/10 Search at A with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    When I run a 9/10 search at locus A
    Then the results should contain the specified donor  
  
  Scenario: 9/10 Search at A with a doubly mismatched donor at A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    When I run a 9/10 search at locus A
    Then the results should not contain the specified donor

  Scenario: 9/10 Search at A with a mismatched donor at B
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    When I run a 9/10 search at locus A
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at B with a mismatched donor at B
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    When I run a 9/10 search at locus B
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at B with a doubly mismatched donor at B
    Given a patient and a donor
    And the donor has a double mismatch at locus B
    When I run a 9/10 search at locus B
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at B with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    When I run a 9/10 search at locus B
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at DRB1 with a mismatched donor at DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus DRB1
    When I run a 9/10 search at locus DRB1
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at DRB1 with a doubly mismatched donor at DRB1
    Given a patient and a donor
    And the donor has a double mismatch at locus DRB1
    When I run a 9/10 search at locus DRB1
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at DRB1 with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    When I run a 9/10 search at locus DRB1
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at C with a mismatched donor at C
    Given a patient and a donor
    And the donor has a single mismatch at locus C
    When I run a 9/10 search at locus C
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at C with a doubly mismatched donor at C
    Given a patient and a donor
    And the donor has a double mismatch at locus C
    When I run a 9/10 search at locus C
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at C with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    When I run a 9/10 search at locus C
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at DQB1 with a mismatched donor at DQB1
    Given a patient and a donor
    And the donor has a single mismatch at locus DQB1
    When I run a 9/10 search at locus DQB1
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at DQB1 with a doubly mismatched donor at DQB1
    Given a patient and a donor
    And the donor has a double mismatch at locus DQB1
    When I run a 9/10 search at locus DQB1
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at DQB1 with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    When I run a 9/10 search at locus DQB1
    Then the results should not contain the specified donor
    
  Scenario: 9/10 Search at A with a mismatched donor at DPB1
    Given a patient and a donor
    And the donor has a single mismatch at locus DPB1
    When I run a 9/10 search at locus A
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at A with a doubly mismatched donor at DPB1
    Given a patient and a donor
    And the donor has a double mismatch at locus DPB1
    When I run a 9/10 search at locus A
    Then the results should contain the specified donor
    
  Scenario: 9/10 Search at A with mismatches at A and DPB1
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And the donor has a single mismatch at locus DPB1
    When I run a 9/10 search at locus A
    Then the results should contain the specified donor

  Scenario: 9/10 Search at A - Donor has a MAC at locus A that includes a deleted expression letter allele (*03:200Q)
    Given a patient and a donor
    And the matching donor has the following HLA:
    |A_1    |A_2      |B_1    |B_2    |DRB1_1 |DRB1_2 |C_1    |C_2    |DQB1_1 |DQB1_2 |
    |*01:01 |*03:JUAA |*57:01 |*41:01 |*13:01 |*07:01 |*01:02 |*01:02 |*05:02 |*02:01 |
    And the patient has the following HLA:
    |A_1    |A_2      |B_1    |B_2    |DRB1_1 |DRB1_2 |C_1    |C_2    |DQB1_1 |DQB1_2 |
    |*01:01 |*66:01   |*57:01 |*41:01 |*13:01 |*07:01 |*01:02 |*01:02 |*05:02 |*02:01 |
    When I run a 9/10 search at locus A
    Then the results should contain the specified donor