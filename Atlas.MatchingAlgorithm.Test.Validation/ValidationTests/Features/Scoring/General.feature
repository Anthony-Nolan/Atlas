Feature: Scoring - general
  As a member of the search team
  I want searches to complete successfully when scoring is enabled

  Scenario: [Regression] Donor has an ambiguous molecular typing that only expands to null alleles (A*01:04)
    Given a patient has a match
    And the matching donor has the following HLA:
    | A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 |
    | *01:01 | *01:04 | *41:01 | *57:01 | *07:01 | *13:XX |
    And the patient has the following HLA:
    | A_1    | A_2    | B_1    | B_2    | DRB1_1 | DRB1_2 |
    | *01:01 | *01:01 | *41:01 | *57:01 | *07:01 | *13:XX |
    And scoring is enabled at locus A
    When I run a 6/6 search
    Then the results should contain the specified donor