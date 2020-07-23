Projects dedicated to manual, non-automated testing of various aspects of the Atlas solution.
This code is designed to be run locally; it is not production quality and cannot be deployed "as-is" to remote environments.

## Projects
- `Atlas.MatchingAlgorithm.Performance`
  - A rudimentary harness for collating performance data of search times.
  - Relies on locally-run MatchingAlgorithm API.

- `Atlas.MatchPrediction.Test.Verification`
  - Web API project for manual verification of the Match Prediction Algorithm (MPA).
  - Verification involves generating a harness of simulated patients and donors, running it through search requests,
	and collating the final results to determine the accuracy of match probabilities.
  - Includes minimal suite of unit tests, with sufficient coverage only to ensure the reliability of the generated data.