-- Remove all saved MyCareersFuture job applications from SmartJobTrackerDB
-- Run once against: Server=.\SQLEXPRESS;Database=SmartJobTrackerDB
-- MyCareersFuture is restricted to Singapore citizens/PRs — not applicable for international applicants.

USE SmartJobTrackerDB;
GO

-- Preview what will be deleted (run SELECT first to confirm)
SELECT Id, Title, Company, Source, AppliedAt
FROM ExternalJobApplications
WHERE Source = 'MyCareersFuture';
GO

-- Delete all MyCareersFuture entries
DELETE FROM ExternalJobApplications
WHERE Source = 'MyCareersFuture';

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' MyCareersFuture record(s) deleted.';
GO
