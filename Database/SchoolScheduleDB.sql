-- إنشاء قاعدة بيانات الجدول المدرسي
CREATE DATABASE SchoolScheduleDB;
GO

USE SchoolScheduleDB;
GO

-- جدول المواد الدراسية
CREATE TABLE Subjects (
    SubjectId INT PRIMARY KEY IDENTITY(1,1),
    SubjectName NVARCHAR(100) NOT NULL,
    SubjectNameEn NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- جدول المعلمين
CREATE TABLE Teachers (
    TeacherId INT PRIMARY KEY IDENTITY(1,1),
    TeacherName NVARCHAR(100) NOT NULL,
    SubjectId INT NOT NULL,
    IsHeadOfDepartment BIT DEFAULT 0,
    IsSupervisor BIT DEFAULT 0,
    Notes NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SubjectId) REFERENCES Subjects(SubjectId)
);

-- جدول الصفوف الدراسية
CREATE TABLE Classes (
    ClassId INT PRIMARY KEY IDENTITY(1,1),
    Grade INT NOT NULL, -- الصف (6, 7, 8, 9)
    Section INT NOT NULL, -- الشعبة (1, 2, 3, ...)
    ClassName AS (CAST(Grade AS VARCHAR) + '/' + CAST(Section AS VARCHAR)) PERSISTED,
    ClassCode AS (CAST(Grade AS VARCHAR) + CAST(Section AS VARCHAR)) PERSISTED,
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_Class_Grade_Section UNIQUE(Grade, Section)
);

-- جدول الأيام الدراسية
CREATE TABLE SchoolDays (
    DayId INT PRIMARY KEY IDENTITY(1,1),
    DayName NVARCHAR(50) NOT NULL,
    DayNameEn VARCHAR(50) NOT NULL,
    DayOrder INT NOT NULL,
    IsActive BIT DEFAULT 1,
    CONSTRAINT UQ_DayOrder UNIQUE(DayOrder)
);

-- جدول الحصص (الفترات)
CREATE TABLE Periods (
    PeriodId INT PRIMARY KEY IDENTITY(1,1),
    PeriodNumber INT NOT NULL,
    StartTime TIME,
    EndTime TIME,
    IsActive BIT DEFAULT 1,
    CONSTRAINT UQ_PeriodNumber UNIQUE(PeriodNumber)
);

-- جدول الجدول المدرسي (الحصص)
CREATE TABLE Schedule (
    ScheduleId INT PRIMARY KEY IDENTITY(1,1),
    ClassId INT NOT NULL,
    SubjectId INT NOT NULL,
    TeacherId INT NOT NULL,
    DayId INT NOT NULL,
    PeriodId INT NOT NULL,
    AcademicYear NVARCHAR(20),
    Semester INT, -- 1 or 2
    Notes NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME,
    FOREIGN KEY (ClassId) REFERENCES Classes(ClassId),
    FOREIGN KEY (SubjectId) REFERENCES Subjects(SubjectId),
    FOREIGN KEY (TeacherId) REFERENCES Teachers(TeacherId),
    FOREIGN KEY (DayId) REFERENCES SchoolDays(DayId),
    FOREIGN KEY (PeriodId) REFERENCES Periods(PeriodId),
    CONSTRAINT UQ_Schedule UNIQUE(ClassId, DayId, PeriodId, AcademicYear, Semester)
);

-- جدول إعدادات المدرسة
CREATE TABLE SchoolSettings (
    SettingId INT PRIMARY KEY IDENTITY(1,1),
    SchoolName NVARCHAR(200) NOT NULL,
    EducationalZone NVARCHAR(200),
    CurrentAcademicYear NVARCHAR(20),
    CurrentSemester INT,
    SemesterStartDate DATE,
    PeriodsPerDay INT DEFAULT 7,
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME
);

-- إدخال البيانات الأساسية

-- إعدادات المدرسة
INSERT INTO SchoolSettings (SchoolName, EducationalZone, CurrentAcademicYear, CurrentSemester, SemesterStartDate)
VALUES (N'مدرسة عبدالمحسن الخرافى المتوسطة للبنين', N'الادارة العامة لمنطقة حولى التعليمية', '2025/2026', 2, '2026-01-25');

-- الأيام الدراسية
INSERT INTO SchoolDays (DayName, DayNameEn, DayOrder) VALUES
(N'الأحد', 'Sunday', 1),
(N'الاثنين', 'Monday', 2),
(N'الثلاثاء', 'Tuesday', 3),
(N'الأربعاء', 'Wednesday', 4),
(N'الخميس', 'Thursday', 5);

-- الحصص (7 حصص يومياً)
INSERT INTO Periods (PeriodNumber, StartTime, EndTime) VALUES
(1, '07:30', '08:15'),
(2, '08:15', '09:00'),
(3, '09:00', '09:45'),
(4, '10:00', '10:45'),
(5, '10:45', '11:30'),
(6, '11:30', '12:15'),
(7, '12:15', '13:00');

-- المواد الدراسية
INSERT INTO Subjects (SubjectName, SubjectNameEn) VALUES
(N'التربية الإسلامية', 'Islamic Education'),
(N'اللغة العربية', 'Arabic Language'),
(N'اللغة الإنجليزية', 'English Language'),
(N'الرياضيات', 'Mathematics'),
(N'العلوم', 'Science'),
(N'الاجتماعيات', 'Social Studies'),
(N'الحاسوب', 'Computer'),
(N'التربية البدنية', 'Physical Education'),
(N'التربية الموسيقية', 'Music'),
(N'التربية الفنية', 'Art'),
(N'الدراسات العملية', 'Practical Studies');

-- الصفوف (من 6 إلى 9 مع 9 شعب لكل صف)
DECLARE @Grade INT = 6;
DECLARE @Section INT;

WHILE @Grade <= 9
BEGIN
    SET @Section = 1;
    WHILE @Section <= 9
    BEGIN
        INSERT INTO Classes (Grade, Section) VALUES (@Grade, @Section);
        SET @Section = @Section + 1;
    END
    SET @Grade = @Grade + 1;
END

-- Stored Procedures

-- إجراء للحصول على جدول صف معين
GO
CREATE PROCEDURE GetClassSchedule
    @ClassId INT,
    @AcademicYear NVARCHAR(20) = NULL,
    @Semester INT = NULL
AS
BEGIN
    IF @AcademicYear IS NULL
        SELECT @AcademicYear = CurrentAcademicYear FROM SchoolSettings WHERE SettingId = 1;
    
    IF @Semester IS NULL
        SELECT @Semester = CurrentSemester FROM SchoolSettings WHERE SettingId = 1;

    SELECT 
        s.ScheduleId,
        c.ClassName,
        c.Grade,
        c.Section,
        sub.SubjectName,
        t.TeacherName,
        d.DayName,
        d.DayOrder,
        p.PeriodNumber,
        p.StartTime,
        p.EndTime,
        s.Notes
    FROM Schedule s
    INNER JOIN Classes c ON s.ClassId = c.ClassId
    INNER JOIN Subjects sub ON s.SubjectId = sub.SubjectId
    INNER JOIN Teachers t ON s.TeacherId = t.TeacherId
    INNER JOIN SchoolDays d ON s.DayId = d.DayId
    INNER JOIN Periods p ON s.PeriodId = p.PeriodId
    WHERE s.ClassId = @ClassId
        AND s.AcademicYear = @AcademicYear
        AND s.Semester = @Semester
    ORDER BY d.DayOrder, p.PeriodNumber;
END
GO

-- إجراء للحصول على جدول معلم معين
CREATE PROCEDURE GetTeacherSchedule
    @TeacherId INT,
    @AcademicYear NVARCHAR(20) = NULL,
    @Semester INT = NULL
AS
BEGIN
    IF @AcademicYear IS NULL
        SELECT @AcademicYear = CurrentAcademicYear FROM SchoolSettings WHERE SettingId = 1;
    
    IF @Semester IS NULL
        SELECT @Semester = CurrentSemester FROM SchoolSettings WHERE SettingId = 1;

    SELECT 
        s.ScheduleId,
        c.ClassName,
        sub.SubjectName,
        t.TeacherName,
        d.DayName,
        d.DayOrder,
        p.PeriodNumber,
        p.StartTime,
        p.EndTime,
        s.Notes
    FROM Schedule s
    INNER JOIN Classes c ON s.ClassId = c.ClassId
    INNER JOIN Subjects sub ON s.SubjectId = sub.SubjectId
    INNER JOIN Teachers t ON s.TeacherId = t.TeacherId
    INNER JOIN SchoolDays d ON s.DayId = d.DayId
    INNER JOIN Periods p ON s.PeriodId = p.PeriodId
    WHERE s.TeacherId = @TeacherId
        AND s.AcademicYear = @AcademicYear
        AND s.Semester = @Semester
    ORDER BY d.DayOrder, p.PeriodNumber;
END
GO

-- إجراء لحساب عدد حصص المعلم
CREATE PROCEDURE GetTeacherWorkload
    @AcademicYear NVARCHAR(20) = NULL,
    @Semester INT = NULL
AS
BEGIN
    IF @AcademicYear IS NULL
        SELECT @AcademicYear = CurrentAcademicYear FROM SchoolSettings WHERE SettingId = 1;
    
    IF @Semester IS NULL
        SELECT @Semester = CurrentSemester FROM SchoolSettings WHERE SettingId = 1;

    SELECT 
        t.TeacherId,
        t.TeacherName,
        sub.SubjectName,
        COUNT(s.ScheduleId) AS TotalPeriods,
        t.IsHeadOfDepartment,
        t.IsSupervisor,
        t.Notes
    FROM Teachers t
    INNER JOIN Subjects sub ON t.SubjectId = sub.SubjectId
    LEFT JOIN Schedule s ON t.TeacherId = s.TeacherId 
        AND s.AcademicYear = @AcademicYear 
        AND s.Semester = @Semester
    WHERE t.IsActive = 1
    GROUP BY t.TeacherId, t.TeacherName, sub.SubjectName, t.IsHeadOfDepartment, t.IsSupervisor, t.Notes
    ORDER BY sub.SubjectName, t.TeacherName;
END
GO

-- إجراء للتحقق من تعارض الجدول
CREATE PROCEDURE CheckScheduleConflict
    @ClassId INT,
    @TeacherId INT,
    @DayId INT,
    @PeriodId INT,
    @AcademicYear NVARCHAR(20),
    @Semester INT,
    @ExcludeScheduleId INT = NULL
AS
BEGIN
    -- التحقق من تعارض الصف
    IF EXISTS (
        SELECT 1 FROM Schedule 
        WHERE ClassId = @ClassId 
            AND DayId = @DayId 
            AND PeriodId = @PeriodId
            AND AcademicYear = @AcademicYear
            AND Semester = @Semester
            AND (@ExcludeScheduleId IS NULL OR ScheduleId != @ExcludeScheduleId)
    )
    BEGIN
        SELECT 'Class Conflict' AS ConflictType, 'الصف لديه حصة في نفس الوقت' AS Message;
        RETURN;
    END

    -- التحقق من تعارض المعلم
    IF EXISTS (
        SELECT 1 FROM Schedule 
        WHERE TeacherId = @TeacherId 
            AND DayId = @DayId 
            AND PeriodId = @PeriodId
            AND AcademicYear = @AcademicYear
            AND Semester = @Semester
            AND (@ExcludeScheduleId IS NULL OR ScheduleId != @ExcludeScheduleId)
    )
    BEGIN
        SELECT 'Teacher Conflict' AS ConflictType, 'المعلم لديه حصة في نفس الوقت' AS Message;
        RETURN;
    END

    SELECT 'No Conflict' AS ConflictType, 'لا يوجد تعارض' AS Message;
END
GO

-- Views مفيدة

-- عرض الجدول الكامل
CREATE VIEW vw_CompleteSchedule AS
SELECT 
    s.ScheduleId,
    c.ClassName,
    c.Grade,
    c.Section,
    sub.SubjectName,
    t.TeacherName,
    d.DayName,
    d.DayOrder,
    p.PeriodNumber,
    FORMAT(p.StartTime, 'hh\:mm') AS StartTime,
    FORMAT(p.EndTime, 'hh\:mm') AS EndTime,
    s.AcademicYear,
    s.Semester,
    s.Notes
FROM Schedule s
INNER JOIN Classes c ON s.ClassId = c.ClassId
INNER JOIN Subjects sub ON s.SubjectId = sub.SubjectId
INNER JOIN Teachers t ON s.TeacherId = t.TeacherId
INNER JOIN SchoolDays d ON s.DayId = d.DayId
INNER JOIN Periods p ON s.PeriodId = p.PeriodId;
GO

-- عرض إحصائيات المعلمين
CREATE VIEW vw_TeacherStatistics AS
SELECT 
    t.TeacherId,
    t.TeacherName,
    sub.SubjectName,
    COUNT(DISTINCT s.ClassId) AS NumberOfClasses,
    COUNT(s.ScheduleId) AS TotalPeriods,
    t.IsHeadOfDepartment,
    t.IsSupervisor,
    t.Notes
FROM Teachers t
INNER JOIN Subjects sub ON t.SubjectId = sub.SubjectId
LEFT JOIN Schedule s ON t.TeacherId = s.TeacherId
WHERE t.IsActive = 1
GROUP BY t.TeacherId, t.TeacherName, sub.SubjectName, t.IsHeadOfDepartment, t.IsSupervisor, t.Notes;
GO

PRINT 'تم إنشاء قاعدة البيانات بنجاح!';
