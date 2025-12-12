Feature: Ten Out Of Ten Search - mismatches
  As a member of the search team
  I want to be able to run a 10/10 search
  And see no mismatches at specified loci in the results

  Scenario: 10/10 Search with a mismatched donor at A
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    When I run a 10/10 search
    Then the results should not contain the specified donor  
  
  Scenario: 10/10 Search with a doubly mismatched donor at A
    Given a patient and a donor
    And the donor has a double mismatch at locus A
    When I run a 10/10 search
    Then the results should not contain the specified donor

  Scenario: 10/10 Search with a mismatched donor at B
    Given a patient and a donor
    And the donor has a single mismatch at locus B
    When I run a 10/10 search
    Then the results should not contain the specified donor

  Scenario: 10/10 Search with a mismatched donor at DRB1
    Given a patient and a donor
    And the donor has a single mismatch at locus DRB1
    When I run a 10/10 search
    Then the results should not contain the specified donor

  Scenario: 10/10 Search with a mismatched donor at C
    Given a patient and a donor
    And the donor has a single mismatch at locus C
    When I run a 10/10 search
    Then the results should not contain the specified donor
    
  Scenario: 10/10 Search with a mismatched donor at DQB1
    Given a patient and a donor
    And the donor has a single mismatch at locus DQB1
    When I run a 10/10 search
    Then the results should not contain the specified donor

  Scenario: 10/10 Search with a mismatched donor at DPB1
    Given a patient has a match
    And the donor has a single mismatch at locus DPB1
    When I run a 10/10 search
    Then the results should contain the specified donor

  Scenario: 10/10 Search - Donor has a NEW allele at locus DPB1
    Given a patient and a donor
    And the matching donor has the following HLA:
    |A_1    |A_2      |B_1    |B_2    |DRB1_1 |DRB1_2 |C_1    |C_2    |DQB1_1 |DQB1_2 |DPB1_1 |DPB1_2 |
    |*33:01 |*68:01   |*44:02 |*14:02 |*03:01 |*04:04 |*07:04 |*08:02 |*02:01 |*03:02 |NEW	  |*04:01 |
    And the patient has the following HLA:
    |A_1    |A_2      |B_1    |B_2    |DRB1_1 |DRB1_2 |C_1    |C_2    |DQB1_1 |DQB1_2 |DPB1_1 |DPB1_2 |
    |*33:01 |*68:01   |*44:02 |*14:02 |*03:01 |*04:04 |*07:04 |*08:02 |*02:01 |*03:02 |*02:01 |*04:01 |
    When I run a 10/10 search
    Then the results should contain the specified donor