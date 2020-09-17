/*
 *
 *	USEFUL QUERIES TO RUN AGAINST VERIFICATION DATABASE AFTER SUBMITTING SEARCH REQUESTS
 *
 */

 -- Replace with value of interest
 DECLARE @runId INT = (SELECT TOP 1 Id FROM VerificationRuns WHERE TestHarness_Id = (
		SELECT TOP 1 TestHarness_Id
		FROM TestDonorExportRecords
		WHERE WasDataRefreshSuccessful = 1
		ORDER BY Id DESC)
	ORDER BY Id DESC)

/*
 *	VERIFICATION RUN RECORD
 */

  SELECT * FROM VerificationRuns WHERE Id = @runId

/*
 *	SEARCH REQUESTS AWAITING THEIR SEARCH RESULTS
 */

SELECT s.SimulatedHlaTypingCategory as cat, r.*
FROM VerificationRuns v
JOIN SearchRequests r
ON v.Id = r.VerificationRun_Id
join Simulants s
on r.PatientSimulant_Id = s.Id
WHERE v.Id = @runId AND r.SearchResultsRetrieved = 0

/*
 *	FAILED SEARCH REQUESTS
 */

SELECT r.*
FROM VerificationRuns v
JOIN SearchRequests r
ON v.Id = r.VerificationRun_Id
WHERE v.Id = @runId AND r.SearchResultsRetrieved = 1 AND r.WasSuccessful = 0

/*
 *	MATCH PREDICTION TIMES
 */

SELECT
	CAST(MIN(r.MatchPredictionTimeInMs/60000) AS DECIMAL(10,1)) AS MinTimeInMins,
	CAST(MAX(r.MatchPredictionTimeInMs/60000) AS DECIMAL(10,1)) AS MaxTimeInMins,
	CAST(AVG(r.MatchPredictionTimeInMs/60000) AS DECIMAL(10,1)) AS MeanAvgTimeInMins
FROM VerificationRuns v
JOIN SearchRequests r
ON v.Id = r.VerificationRun_Id
WHERE v.Id = @runId AND r.WasSuccessful = 1