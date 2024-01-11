/*
 *	Queries to identify patients or donors that could not be explained by their referenced HF set
 */ 

SELECT DISTINCT si.ExternalId AS UnrepresentedPatient
FROM MatchedDonors md
JOIN SearchRequests sr
ON md.SearchRequestRecord_Id = sr.Id
JOIN SubjectInfo si
ON si.Id = sr.PatientId
WHERE md.WasPatientRepresented = 0
ORDER BY UnrepresentedPatient

SELECT DISTINCT md.DonorCode AS UnrepresentedDonor
FROM MatchedDonors md
WHERE md.WasDonorRepresented = 0
ORDER BY UnrepresentedDonor