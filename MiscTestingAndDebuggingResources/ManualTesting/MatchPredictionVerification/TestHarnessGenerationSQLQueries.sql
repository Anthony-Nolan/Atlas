/*
 *
 *	USEFUL QUERIES TO RUN AGAINST VERIFICATION DATABASE AFTER TEST HARNESS GENERATION
 *
 */

 -- Replace with values of interest
 DECLARE @testHarnessId INT = (SELECT MAX(id) FROM TestHarnesses WHERE WasCompleted = 1)
 DECLARE @individualCategory NVARCHAR(10) = 'Donor'

 /*
  *	TEST HARNESS RECORD
  */

  SELECT * FROM TestHarnesses WHERE Id = @testHarnessId

/*
 *	PAIR SIMULATED GENOTYPE WITH ITS MASKED VERSION.
 *	Useful to compare HLA before and after masking.
 */

SELECT
	g.Id,
	g.[A_1],g.[A_2],g.[B_1],g.[B_2],g.[C_1],g.[C_2],g.[DQB1_1],g.[DQB1_2],g.[DRB1_1],g.[DRB1_2],
	m.[A_1],m.[A_2],m.[B_1],m.[B_2],m.[C_1],m.[C_2],m.[DQB1_1],m.[DQB1_2],m.[DRB1_1],m.[DRB1_2]
FROM (
  SELECT s.*
  FROM Simulants s
  JOIN TestHarnesses h
  ON h.Id = s.TestHarness_Id
  WHERE h.Id = @testHarnessId AND TestIndividualCategory = @individualCategory AND SimulatedHlaTypingCategory = 'Genotype') g
JOIN (
  SELECT s.*
  FROM Simulants s
  JOIN TestHarnesses h
  ON h.Id = s.TestHarness_Id
  WHERE h.Id = @testHarnessId AND TestIndividualCategory = @individualCategory AND SimulatedHlaTypingCategory = 'Masked') m
ON g.Id = m.SourceSimulantId
ORDER BY g.Id


/*
 *	MASKING PROPORTIONS BY LOCUS
 *	Compare the final masking proportions to those submitted in the original generation request.
 */

-- DO NOT MODIFY THESE VARS
DECLARE @TotalSimulantCount INT = (SELECT COUNT(*) FROM Simulants WHERE TestHarness_Id = @TestHarnessId AND TestIndividualCategory = @individualCategory)
DECLARE @Deleted NVARCHAR(10) = 'Deleted'

SELECT pvt.*, m.MaskingRequests AS SubmittedMaskingRequests
FROM(
	SELECT Locus, MaskingCategory, 100*COUNT(*)/@TotalSimulantCount AS Proportion
	FROM(
		SELECT
			Id,
			Hla,
			MaskingCategory = CASE
				WHEN Hla = @Deleted THEN Hla
				WHEN Hla LIKE '%:XX' THEN 'XXCode'
				WHEN Hla LIKE '%:[A-Z]%' THEN 'MAC'
				WHEN Hla LIKE '%P' THEN 'PGroup'
				WHEN Hla LIKE '%G' THEN 'GGroup'
				WHEN Hla LIKE '%:%' THEN 'TwoField'
				ELSE 'Serology'
				END,
			SUBSTRING(LocusPosition, 0, LEN(LocusPosition)-1) AS Locus 
		FROM (
			SELECT
				Id,
				A_1, A_2,
				B_1, B_2,
				ISNULL(C_1, @Deleted) AS C_1,
				ISNULL(C_2, @Deleted) AS C_2,
				ISNULL(DQB1_1, @Deleted) AS DQB1_1,
				ISNULL(DQB1_2, @Deleted) AS DQB1_2,
				DRB1_1, DRB1_2
			FROM Simulants
			WHERE TestHarness_Id = @TestHarnessId AND TestIndividualCategory = @individualCategory AND SimulatedHlaTypingCategory = 'Masked') src  
		UNPIVOT(Hla FOR LocusPosition IN (A_1, A_2, B_1, B_2, C_1, C_2, DQB1_1, DQB1_2, DRB1_1, DRB1_2))AS unpvt) MaskingStats
	GROUP BY Locus, MaskingCategory) src
PIVOT (MAX(Proportion) FOR MaskingCategory IN (GGroup, TwoField, PGroup, MAC, XXCode, Serology, Deleted)) AS pvt
JOIN MaskingRecords m
ON pvt.Locus = m.Locus
WHERE m.TestHarness_Id = @testHarnessId AND m.TestIndividualCategory = @individualCategory