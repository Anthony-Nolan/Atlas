/*
 *	SELECT INFO BY PATIENT AND DONOR ID
 */

-- SET THE FOLLOWING VARS
DECLARE @PatientId varchar(16) = 'P000820'
DECLARE @DonorId varchar(16) = 'D908661'
DECLARE @MatchCountDenominator int = 10 -- i.e., "10" for x/10 search, "8" for x/8 search, etc.


/*
 * QUERIES
 */

DECLARE @PatientImputationId int = (SELECT Id FROM ImputationSummaries WHERE ExternalSubjectId = @PatientId)
DECLARE @DonorImputationId int = (SELECT Id FROM ImputationSummaries WHERE ExternalSubjectId = @DonorId)

-- select imputation summaries for patient and donor
SELECT si.*, i.*
FROM ImputationSummaries i
JOIN SubjectInfo si
ON i.ExternalSubjectId = si.ExternalId
WHERE i.Id = @PatientImputationId

SELECT si.*, i.*
FROM ImputationSummaries i
JOIN SubjectInfo si
ON i.ExternalSubjectId = si.ExternalId
WHERE i.Id = @DonorImputationId

-- select distinct genotypes for patient
SELECT DISTINCT
	Patient_A_1, Patient_A_2,
	Patient_B_1, Patient_B_2,
	Patient_C_1, Patient_C_2,
	Patient_DQB1_1, Patient_DQB1_2,
	Patient_DRB1_1, Patient_DRB1_2,
	Patient_Likelihood
FROM MatchingGenotypes mg
JOIN ImputationSummaries i
ON mg.Patient_ImputationSummary_Id = i.Id
WHERE i.Id = @PatientImputationId

-- select distinct genotypes for donors
SELECT DISTINCT
	Donor_A_1, Donor_A_2,
	Donor_B_1, Donor_B_2,
	Donor_C_1, Donor_C_2,
	Donor_DQB1_1, Donor_DQB1_2,
	Donor_DRB1_1, Donor_DRB1_2,
	Donor_Likelihood
FROM MatchingGenotypes mg
JOIN ImputationSummaries i
ON mg.Donor_ImputationSummary_Id = i.Id
WHERE i.Id = @DonorImputationId

-- select matching genotypes
SELECT mg.*
FROM MatchingGenotypes mg
JOIN ImputationSummaries p
ON mg.Patient_ImputationSummary_Id = p.Id
JOIN ImputationSummaries d
ON mg.Donor_ImputationSummary_Id = d.Id
WHERE p.Id = @PatientImputationId and d.Id = @DonorImputationId

-- select match probabilities
DECLARE @PatientLikelihoodSum decimal = (SELECT SumOfLikelihoods FROM ImputationSummaries WHERE Id = @PatientImputationId)
DECLARE @DonorLikelihoodSum decimal = (SELECT SumOfLikelihoods FROM ImputationSummaries WHERE Id = @DonorImputationId)
DECLARE @MatchProbabilityDenominator decimal = (@PatientLikelihoodSum * @DonorLikelihoodSum)

SELECT
	@MatchCountDenominator - TotalCount AS MismatchCount_Total,
	100 * SUM(Patient_Likelihood * Donor_Likelihood) / @MatchProbabilityDenominator AS ProbabilityPercentage
FROM MatchingGenotypes
WHERE Patient_ImputationSummary_Id = @PatientImputationId and Donor_ImputationSummary_Id = @DonorImputationId
GROUP BY TotalCount

SELECT
	2 - A_Count AS MismatchCount_A,
	100 * SUM(Patient_Likelihood * Donor_Likelihood) / @MatchProbabilityDenominator AS ProbabilityPercentage
FROM MatchingGenotypes
WHERE Patient_ImputationSummary_Id = @PatientImputationId and Donor_ImputationSummary_Id = @DonorImputationId
GROUP BY A_Count

SELECT
	2 - B_Count AS MismatchCount_B,
	100 * SUM(Patient_Likelihood * Donor_Likelihood) / @MatchProbabilityDenominator AS ProbabilityPercentage
FROM MatchingGenotypes
WHERE Patient_ImputationSummary_Id = @PatientImputationId and Donor_ImputationSummary_Id = @DonorImputationId
GROUP BY B_Count

SELECT
	2 - C_Count AS MismatchCount_C,
	100 * SUM(Patient_Likelihood * Donor_Likelihood) / @MatchProbabilityDenominator AS ProbabilityPercentage
FROM MatchingGenotypes
WHERE Patient_ImputationSummary_Id = @PatientImputationId and Donor_ImputationSummary_Id = @DonorImputationId
GROUP BY C_Count

SELECT
	2 - DQB1_Count AS MismatchCount_DQB1,
	100 * SUM(Patient_Likelihood * Donor_Likelihood) / @MatchProbabilityDenominator AS ProbabilityPercentage
FROM MatchingGenotypes
WHERE Patient_ImputationSummary_Id = @PatientImputationId and Donor_ImputationSummary_Id = @DonorImputationId
GROUP BY DQB1_Count

SELECT
	2 - DRB1_Count AS MismatchCount_DRB1,
	100 * SUM(Patient_Likelihood * Donor_Likelihood) / @MatchProbabilityDenominator AS ProbabilityPercentage
FROM MatchingGenotypes
WHERE Patient_ImputationSummary_Id = @PatientImputationId and Donor_ImputationSummary_Id = @DonorImputationId
GROUP BY DRB1_Count