/*
 * CHECK THAT HOMEWORK SETS HAVE BEEN IMPORTED
 * Can also use this query to follow progress of the individual PDP processing.
 */

SELECT *
FROM HomeworkSets h
JOIN PatientDonorPairs pdp
ON h.Id = pdp.HomeworkSet_Id
ORDER BY HomeworkSet_Id, PatientId, DonorId