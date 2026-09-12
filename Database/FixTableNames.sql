-- سكريبت إصلاح سريع لأسماء الجداول
USE SchoolScheduleDB;
GO

-- التحقق من وجود جدول Schedules بدلاً من Schedule
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Schedules')
BEGIN
    PRINT 'تم العثور على جدول Schedules - سيتم إعادة تسميته إلى Schedule';
    
    -- حذف القيود أولاً
    DECLARE @sql NVARCHAR(MAX) = '';
    
    SELECT @sql = @sql + 'ALTER TABLE [' + OBJECT_NAME(parent_object_id) + '] DROP CONSTRAINT [' + name + '];'
    FROM sys.foreign_keys
    WHERE referenced_object_id = OBJECT_ID('Schedules');
    
    EXEC sp_executesql @sql;
    
    -- إعادة تسمية الجدول
    EXEC sp_rename 'Schedules', 'Schedule';
    
    PRINT 'تم إعادة تسمية الجدول بنجاح';
END
ELSE IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Schedule')
BEGIN
    PRINT 'الجدول Schedule موجود بالاسم الصحيح';
END
ELSE
BEGIN
    PRINT 'خطأ: لم يتم العثور على جدول Schedule أو Schedules';
    PRINT 'يرجى تشغيل سكريبت إنشاء قاعدة البيانات SchoolScheduleDB.sql';
END
GO
