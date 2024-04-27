/*
 * SELECT SUBJECTS WITH MISSING REQUIRED HLA
 */

-- Patients
SELECT DISTINCT hs.SetName, pdp.PatientId
FROM HomeworkSets hs
JOIN PatientDonorPairs pdp
ON hs.Id = pdp.HomeworkSet_Id
WHERE pdp.DidPatientHaveMissingHla = 1
ORDER BY hs.SetName, pdp.PatientId

-- Donors
SELECT DISTINCT hs.SetName, pdp.DonorId
FROM HomeworkSets hs
JOIN PatientDonorPairs pdp
ON hs.Id = pdp.HomeworkSet_Id
WHERE pdp.DidDonorHaveMissingHla = 1
ORDER BY hs.SetName, pdp.DonorId