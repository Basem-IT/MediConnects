UPDATE Patients
SET ReferenceCode = 'PAT-' + RIGHT('0000' + CAST(PatientID AS VARCHAR(10)), 4)
WHERE ReferenceCode IS NULL OR ReferenceCode = '';