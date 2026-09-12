-- سكريبت للتحقق من وجود الجداول
USE SchoolScheduleDB;
GO

-- التحقق من الجداول الموجودة
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

-- عرض عدد السجلات في كل جدول
SELECT 'Subjects' AS TableName, COUNT(*) AS RecordCount FROM Subjects
UNION ALL
SELECT 'Teachers', COUNT(*) FROM Teachers
UNION ALL
SELECT 'Classes', COUNT(*) FROM Classes
UNION ALL
SELECT 'SchoolDays', COUNT(*) FROM SchoolDays
UNION ALL
SELECT 'Periods', COUNT(*) FROM Periods
UNION ALL
SELECT 'Schedule', COUNT(*) FROM Schedule
UNION ALL
SELECT 'SchoolSettings', COUNT(*) FROM SchoolSettings;
GO

-- إذا كان اسم الجدول خطأ، استخدم هذا لإعادة التسمية
-- EXEC sp_rename 'Schedules', 'Schedule';
