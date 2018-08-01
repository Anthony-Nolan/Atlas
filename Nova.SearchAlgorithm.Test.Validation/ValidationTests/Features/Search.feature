Feature: Search
	As a member of the search team
	I want to be able to search for matching donors

@mytag
Scenario: Search for a matching donor
	Given I search for recognised hla
	And The search type is adult
	And The search is run for Anthony Nolan's registry only
	When I run a 6/6 search
	Then The result should contain at least one donor
