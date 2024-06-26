/*
 *	Query to identify failed or incomplete searches.
 */

SELECT DISTINCT i.*
FROM SearchRequests r
JOIN SubjectInfo i
ON r.PatientId = i.Id
WHERE 
	-- Searches that failed to be sent
	AtlasSearchIdentifier != 'FAILED-SEARCH' OR 
	-- Failed Search
	(SearchResultsRetrieved = 1 AND (ISNULL(WasSuccessful,0) = 0) OR
	-- Search result has not yet been downloaded
	SearchResultsRetrieved = 0)
ORDER BY ExternalId
