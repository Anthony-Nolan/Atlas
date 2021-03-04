Feature: Eight Out Of Ten Search - better matches
  As a member of the search team
  I want to be able to run a 8/10 search
  And optionally see better matches in the results

  Scenario: 8/10 Search with a fully matching donor, with better matches allowed
    Given a patient has a match
    And the matching donor is a 10/10 match
    And better matches are allowed
    When I run an 8/10 search
    Then the results should contain the specified donor

  Scenario: 8/10 Search with a single mismatch, with better matches allowed
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And better matches are allowed
    When I run an 8/10 search
    Then the results should contain the specified donor
    
  Scenario: 8/10 Search with mismatches at locus A and DPB1, with better matches allowed
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And better matches are allowed
    And the donor has a single mismatch at locus DPB1
    When I run an 8/10 search
    Then the results should contain the specified donor
    
  Scenario: 8/10 Search with double mismatch at locus DPB1, with better matches allowed
    Given a patient and a donor
    And the donor has a double mismatch at locus DPB1
    And better matches are allowed
    When I run an 8/10 search
    Then the results should contain the specified donor
    
  Scenario: 8/10 Search with a fully matching donor, with better matches disallowed
    Given a patient has a match
    And the matching donor is a 10/10 match
    And better matches are disallowed 
    When I run an 8/10 search
    Then the results should not contain the specified donor

  Scenario: 8/10 Search with a single mismatch, with better matches disallowed
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And better matches are disallowed 
    When I run an 8/10 search
    Then the results should not contain the specified donor
    
  Scenario: 8/10 Search with mismatches at locus A and DPB1, with better matches disallowed
    Given a patient and a donor
    And the donor has a single mismatch at locus A
    And better matches are disallowed 
    And the donor has a single mismatch at locus DPB1
    When I run an 8/10 search
    Then the results should not contain the specified donor
    
  Scenario: 8/10 Search with double mismatch at locus DPB1, with better matches disallowed
    Given a patient and a donor
    And the donor has a double mismatch at locus DPB1
    And better matches are disallowed 
    When I run an 8/10 search
    Then the results should not contain the specified donor