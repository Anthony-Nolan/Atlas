/*
 *
 *	USEFUL QUERIES TO RUN AGAINST VERIFICATION DATABASE AFTER SEARCH RESULTS HAVE BEEN RETRIEVED
 *
 */

 -- Replace with value of interest
 DECLARE @runId int = (SELECT MAX(Id) FROM VerificationRuns WHERE SearchRequestsSubmitted = 1);

/*
 *	ACTUAL VS. POTENTIAL QUERY FOR GENOTYPE VS. GENOTYPE PATIENT-DONOR PAIRS (PDPs)
 *  This is a good positive control for verification. Every genotype vs. genotype PDP should have a 100% probability for the number of actual mismatches they have.
 *  E.g., a PDP with one actual mismatch should have been assigned a P(1 mismatch) value of 100%, as well as 0% for P(0 mismatch) and P(2 mismatch).
 */

SELECT actual_mismatch_count, [P_0_mismatch], [P_1_mismatch], [P_2_mismatch], COUNT(*) AS PDP_count
FROM(
	SELECT 
		r.PatientSimulant_Id AS patient_sim_id,
		d.MatchedDonorSimulant_Id AS donor_sim_id,
		v.SearchLociCount*2 -d.TotalMatchCount AS actual_mismatch_count,
		'P_' + CAST(MismatchCount AS NVARCHAR(1)) + '_mismatch' AS Prediction,
		CAST(ROUND(p.Probability, 2)*100 AS INT) AS Probability
	FROM VerificationRuns v
	JOIN SearchRequests r
	ON v.Id = r.VerificationRun_Id
	JOIN MatchedDonors d
	ON r.Id = d.SearchRequestRecord_Id
	JOIN MatchProbabilities p
	ON d.Id = p.MatchedDonor_Id
	JOIN Simulants ps
	ON r.PatientSimulant_Id = ps.Id
	JOIN Simulants ds
	ON d.MatchedDonorSimulant_Id = ds.Id
	WHERE
		r.SearchResultsRetrieved = 1 AND 
		r.VerificationRun_Id = @runId AND 
		ps.SimulatedHlaTypingCategory = 'Genotype' AND 
		ds.SimulatedHlaTypingCategory = 'Genotype' AND
		p.Locus IS NULL) src
PIVOT (MAX(Probability) FOR Prediction IN ([P_0_mismatch], [P_1_mismatch], [P_2_mismatch])) AS pvt
GROUP BY actual_mismatch_count, [P_0_mismatch], [P_1_mismatch], [P_2_mismatch]
ORDER BY actual_mismatch_count