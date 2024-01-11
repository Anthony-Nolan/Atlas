/*
 *	Select out MV4 results in the required format
 */

DECLARE @searchSetIds varchar(MAX) = '7,8,9' -- replace these IDs AS required

SELECT
-- subject ids
	pi.ExternalId AS PATIENT_ID,
	md.DonorCode AS DONOR_ID,
-- scores
	grade.A1, grade.A2,
	grade.B1, grade.B2,
	grade.C1, grade.C2,
	grade.DRB11, grade.DRB12,
	grade.DQB11, grade.DQB12,
	10 - md.TotalMatchCount AS Count_MM,
-- Predictions
	matchPr.P8_10,
	matchPr.P9_10,
	matchPr.P10_10,
-- M.P. metadata
	md.PatientHfSetPopulationId AS HF_SET_PATIENT,
	CASE md.WasPatientRepresented WHEN 0 THEN 'N' ELSE 'Y' END AS EXPLAINED_PATIENT,
	md.DonorHfSetPopulationId AS HF_SET_DONOR,
	CASE md.WasDonorRepresented WHEN 0 THEN 'N' ELSE 'Y' END AS EXPLAINED_DONOR
FROM SearchRequests sr
JOIN SubjectInfo pi
ON sr.PatientId = pi.Id
JOIN MatchedDonors md
ON sr.Id = md.SearchRequestRecord_Id
JOIN (
	SELECT 
		SRID,
		MatchedDonor_Id,
		-- Map Atlas score to MV4 match grade
		CASE 
			WHEN Grade = 'Mismatch0' THEN 'M'
			WHEN Grade = 'Mismatch1' THEN 'L'
			WHEN Grade LIKE 'Potential%' THEN 'P'
			ELSE 'A' END AS Grade,
		CASE WHEN Position = 'Pos1' THEN Locus+'1' ELSE Locus+'2' END AS GradeName
	FROM (
		SELECT
			sr.Id AS SRID,
			MatchedDonor_Id,
			Locus,
			MatchConfidence_1 + CONVERT(varchar(1),ISNULL(IsAntigenMatch_1, 0)) AS Pos1,
			MatchConfidence_2 + CONVERT(varchar(1),ISNULL(IsAntigenMatch_2, 0)) AS Pos2
		FROM SearchRequests sr
		JOIN MatchedDonors md
		ON sr.Id = md.SearchRequestRecord_Id
		JOIN LocusMatchDetails lmd
		ON md.Id = lmd.MatchedDonor_Id
		WHERE 
			sr.SearchSet_Id IN (SELECT CONVERT(int, value) FROM string_split(@searchSetIds, ',')) AND
			sr.WasSuccessful = 1) src
	UNPIVOT (Grade FOR Position IN (Pos1, Pos2)) AS unpvt) AS src
PIVOT (MAX(Grade) FOR GradeName IN ([A1],[A2],[B1],[B2],[C1],[C2],[DRB11],[DRB12],[DQB11],[DQB12])) AS grade
ON grade.MatchedDonor_Id = md.Id AND grade.SRID = sr.Id
JOIN (
	SELECT
		sr.Id AS SRID,
		MatchedDonor_Id,
		CASE mp.MismatchCount
			WHEN 0 THEN 'P10_10'
			WHEN 1 THEN 'P9_10'
			WHEN 2 THEN 'P8_10'
			END AS Prediction,
		ISNULL(ProbabilityAsPercentage,-1) AS Prob
	FROM SearchRequests sr
	JOIN MatchedDonors md
	ON sr.Id = md.SearchRequestRecord_Id
	JOIN MatchProbabilities mp
	ON md.Id = mp.MatchedDonor_Id
	WHERE 
		sr.SearchSet_Id IN (SELECT CONVERT(int, value) FROM string_split(@searchSetIds, ',')) AND
		sr.WasSuccessful = 1 AND 
		mp.Locus IS NULL)src
PIVOT (MAX(Prob) FOR Prediction IN ([P10_10],[P9_10],[P8_10])) AS matchPr
ON matchPr.MatchedDonor_Id = md.Id AND matchPr.SRID = sr.Id
WHERE 
	sr.SearchSet_Id IN (SELECT CONVERT(int, value) FROM string_split(@searchSetIds, ',')) AND 
	sr.WasSuccessful = 1
ORDER BY PATIENT_ID, DONOR_ID