/*
 *
 *	USEFUL QUERIES TO RUN AGAINST VERIFICATION DATABASE AFTER SUBMITTING SEARCH REQUESTS
 *
 */

 -- Replace with value of interest
 DECLARE @testHarnessId INT = (SELECT TestHarness_Id FROM TestDonorExportRecords WHERE Id = (
	SELECT MAX(id)
	FROM TestDonorExportRecords
	WHERE WasDataRefreshSuccessful = 1))

/*
 *	VERIFICATION RUN RECORD
 */

  SELECT * FROM VerificationRuns WHERE Id = @testHarnessId

/*
 *	SEARCH REQUESTS AWAITING THEIR SEARCH RESULTS
 */

SELECT r.*
FROM VerificationRuns v
JOIN SearchRequests r
ON v.Id = r.VerificationRun_Id
WHERE v.TestHarness_Id = @testHarnessId AND r.SearchResultsRetrieved = 0