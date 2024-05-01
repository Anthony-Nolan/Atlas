/*
 *	SELECT INFO BY PATIENT AND DONOR ID
 */
DECLARE @PatientId varchar(16) = 'P000820'
DECLARE @DonorId varchar(16) = 'D448673'

-- select imputation summaries for patient and donor
SELECT si.*, i.*
FROM ImputationSummaries i
RIGHT JOIN SubjectInfo si
ON i.ExternalSubjectId = si.ExternalId
WHERE ExternalId = @PatientId

SELECT si.*, i.*
FROM ImputationSummaries i
RIGHT JOIN SubjectInfo si
ON i.ExternalSubjectId = si.ExternalId
WHERE ExternalId = @DonorId

-- select distinct genotypes for patient
SELECT DISTINCT
	Patient_A_1, Patient_A_2,
	Patient_B_1, Patient_B_2,
	Patient_C_1, Patient_C_2,
	Patient_DQB1_1, Patient_DQB1_2,
	Patient_DRB1_1, Patient_DRB1_2,
	Patient_Likelihood
FROM MatchingGenotypes mg
JOIN ImputationSummaries p
ON mg.Patient_ImputationSummary_Id = p.Id
WHERE p.ExternalSubjectId = @PatientId

-- select distinct genotypes for donors
SELECT DISTINCT
	Donor_A_1, Donor_A_2,
	Donor_B_1, Donor_B_2,
	Donor_C_1, Donor_C_2,
	Donor_DQB1_1, Donor_DQB1_2,
	Donor_DRB1_1, Donor_DRB1_2,
	Donor_Likelihood
FROM MatchingGenotypes mg
JOIN ImputationSummaries d
ON mg.Donor_ImputationSummary_Id = d.Id
WHERE d.ExternalSubjectId = @DonorId

-- select matching genotypes
SELECT mg.*
FROM MatchingGenotypes mg
JOIN ImputationSummaries p
ON mg.Patient_ImputationSummary_Id = p.Id
JOIN ImputationSummaries d
ON mg.Donor_ImputationSummary_Id = d.Id
WHERE p.ExternalSubjectId = @PatientId and d.ExternalSubjectId = @DonorId