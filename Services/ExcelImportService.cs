using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SchoolSchedule.Data;
using SchoolSchedule.Models;
using SchoolSchedule.Models.ViewModels;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolSchedule.Services
{
    public class ExcelImportService
    {
        private readonly ApplicationDbContext _context;

        public ExcelImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ImportResult> ImportScheduleFromExcel(
            Stream excelStream, 
            string academicYear, 
            int semester, 
            bool deleteExisting = false)
        {
            var result = new ImportResult { Success = true };

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(excelStream))
                {
                    if (!package.Workbook.Worksheets.Any())
                    {
                        result.Success = false;
                        result.Errors.Add("ملف Excel لا يحتوي على أي أوراق عمل");
                        return result;
                    }

                    // البحث عن ورقة الجدول الرئيسية مع تجاهل اختلافات الهمزات والمسافات.
                    var worksheet = package.Workbook.Worksheets
                        .FirstOrDefault(ws => NormalizeArabic(ws.Name).Contains(NormalizeArabic("جدول المدرسي")));

                    if (worksheet == null)
                    {
                        result.Success = false;
                        result.Errors.Add("لم يتم العثور على ورقة 'جدول المدرسي' في الملف");
                        result.Errors.Add($"الأوراق الموجودة: {string.Join(", ", package.Workbook.Worksheets.Select(w => w.Name))}");
                        return result;
                    }

                    var header = DetectScheduleHeader(worksheet);
                    if (header == null)
                    {
                        result.Success = false;
                        result.Errors.Add("لم يتم العثور على صف عناوين الجدول. يجب أن يحتوي على الأعمدة: المادة، اسم المعلم، وأيام الأسبوع.");
                        return result;
                    }

                    // حذف البيانات الموجودة إذا طُلب ذلك
                    if (deleteExisting)
                    {
                        var schedulesToDelete = await _context.Schedules
                            .Where(s => s.AcademicYear == academicYear && s.Semester == semester)
                            .ToListAsync();
                        _context.Schedules.RemoveRange(schedulesToDelete);
                        await _context.SaveChangesAsync();
                    }

                    // تحميل البيانات المرجعية
                    var subjects = await _context.Subjects.ToListAsync();
                    var subjectLookup = BuildSubjectLookup(subjects);
                    var days = await _context.SchoolDays.OrderBy(d => d.DayOrder).ToListAsync();
                    var periods = await _context.Periods.OrderBy(p => p.PeriodNumber).ToListAsync();
                    var periodByNumber = periods.ToDictionary(p => p.PeriodNumber, p => p);
                    var classes = await _context.Classes.ToDictionaryAsync(c => c.ClassCode!, c => c);
                    var teachers = new Dictionary<string, Teacher>();

                    var existingSchedules = await _context.Schedules
                        .Where(s => s.AcademicYear == academicYear && s.Semester == semester)
                        .ToListAsync();

                    var existingSchedulesByKey = existingSchedules
                        .ToDictionary(s => (s.ClassId, s.DayId, s.PeriodId), s => s);
                    var importedScheduleKeys = new HashSet<(int ClassId, int DayId, int PeriodId)>();

                    // خطوة 1: بناء خريطة الأعمدة من عناوين الأيام وأرقام الحصص الموجودة فعلاً.
                    var columnMap = BuildColumnMap(worksheet, header.Value.HeaderRow, header.Value.PeriodRow, days, periodByNumber);

                    if (!columnMap.Any())
                    {
                        result.Success = false;
                        result.Errors.Add("لم يتم التعرف على أعمدة الأيام والحصص داخل ورقة الجدول المدرسي");
                        return result;
                    }

                    // خطوة 2: قراءة البيانات من Excel
                    int currentRow = header.Value.PeriodRow + 1;
                    Subject? currentSubject = null;
                    var warnedSubjects = new HashSet<string>();
                    var lastSavedSchedulesCount = 0;

                    while (currentRow <= worksheet.Dimension.End.Row)
                    {
                        // قراءة القيم من الصف
                        var rowNumber = worksheet.Cells[currentRow, 1].Value;
                        var subjectName = GetCellText(worksheet, currentRow, 2);
                        var teacherName = GetCellText(worksheet, currentRow, 3);

                        // إذا كانت الخلية الأولى فارغة أو 0، انتقل للصف التالي
                        if (rowNumber == null || rowNumber.ToString() == "0" || !int.TryParse(rowNumber.ToString(), out _))
                        {
                            currentRow++;
                            continue;
                        }

                        // التحقق من وجود مادة جديدة
                        if (!string.IsNullOrEmpty(subjectName))
                        {
                            currentSubject = ResolveSubject(subjectName, subjectLookup, subjects);
                            if (currentSubject == null && warnedSubjects.Add(subjectName))
                            {
                                result.Warnings.Add($"لم يتم العثور على مادة مطابقة في قاعدة البيانات: {subjectName}");
                            }
                        }

                        // إذا كانت المادة محددة وهناك اسم معلم
                        if (currentSubject != null && !string.IsNullOrEmpty(teacherName))
                        {
                            // البحث عن المعلم أو إنشاؤه
                            Teacher? teacher;
                            if (teachers.ContainsKey(teacherName))
                            {
                                teacher = teachers[teacherName];
                            }
                            else
                            {
                                teacher = await _context.Teachers
                                    .FirstOrDefaultAsync(t => t.TeacherName == teacherName);

                                if (teacher == null)
                                {
                                    // استخراج الملاحظات من العمود 40
                                    var notes = GetCellText(worksheet, currentRow, 40);
                                    bool isHeadOfDept = notes?.Contains("رئيس قسم") ?? false;
                                    bool isSupervisor = notes?.Contains("مشرف") ?? false;

                                    teacher = new Teacher
                                    {
                                        TeacherName = teacherName,
                                        SubjectId = currentSubject.SubjectId,
                                        IsHeadOfDepartment = isHeadOfDept,
                                        IsSupervisor = isSupervisor,
                                        Notes = notes
                                    };

                                    _context.Teachers.Add(teacher);
                                    await _context.SaveChangesAsync();
                                    result.TeachersAdded++;
                                }

                                teachers[teacherName] = teacher;
                            }

                            // قراءة الحصص من الأعمدة المحددة في columnMap
                            foreach (var kvp in columnMap)
                            {
                                int colNum = kvp.Key;
                                int dayId = kvp.Value.DayId;
                                int periodId = kvp.Value.PeriodId;
                                int periodNumber = kvp.Value.PeriodNumber;

                                var classCode = GetCellText(worksheet, currentRow, colNum);

                                if (!string.IsNullOrEmpty(classCode) &&
                                    classCode != "م.إ" &&  // تجاهل "م.إ"
                                    !classCode.Contains("اجتم") && // تجاهل "اجتماع"
                                    int.TryParse(classCode, out int code) &&
                                    code >= 16 && code <= 99) // من صف 6-1 (16) إلى صف 9-9 (99)
                                {
                                    // تحويل رمز الصف
                                    string finalClassCode = classCode;
                                    if (classCode.Length == 2)
                                    {
                                        int firstDigit = code / 10;
                                        int secondDigit = code % 10;
                                        if (firstDigit >= 1 && firstDigit <= 9 &&
                                            secondDigit >= 6 && secondDigit <= 9)
                                        {
                                            finalClassCode = $"{secondDigit}{firstDigit}";
                                        }
                                    }

                                    if (classes.TryGetValue(finalClassCode, out Class? classEntity))
                                    {
                                        var scheduleKey = (classEntity.ClassId, dayId, periodId);

                                        // التحقق من عدم وجود تعارض مكرر داخل نفس الملف.
                                        if (!importedScheduleKeys.Add(scheduleKey))
                                        {
                                            result.Warnings.Add($"تم تجاهل تعارض داخل الملف للصف {finalClassCode} في اليوم {dayId} الحصة {periodNumber}");
                                        }
                                        else if (existingSchedulesByKey.TryGetValue(scheduleKey, out var existingSchedule))
                                        {
                                            existingSchedule.SubjectId = currentSubject.SubjectId;
                                            existingSchedule.TeacherId = teacher.TeacherId;
                                            existingSchedule.UpdatedDate = DateTime.Now;
                                            result.SchedulesAdded++;
                                        }
                                        else
                                        {
                                            var schedule = new Schedule
                                            {
                                                ClassId = classEntity.ClassId,
                                                SubjectId = currentSubject.SubjectId,
                                                TeacherId = teacher.TeacherId,
                                                DayId = dayId,
                                                PeriodId = periodId,
                                                AcademicYear = academicYear,
                                                Semester = semester
                                            };

                                            _context.Schedules.Add(schedule);
                                            result.SchedulesAdded++;
                                        }
                                    }
                                    else
                                    {
                                        result.Warnings.Add($"تم تجاهل كود صف غير موجود في قاعدة البيانات: {finalClassCode}");
                                    }
                                }
                            }

                            // حفظ كل 50 حصة لتحسين الأداء
                            if (result.SchedulesAdded > 0 && result.SchedulesAdded - lastSavedSchedulesCount >= 50)
                            {
                                await _context.SaveChangesAsync();
                                lastSavedSchedulesCount = result.SchedulesAdded;
                            }
                        }

                        currentRow++;
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"خطأ في الاستيراد: {ex.Message}");
            }

            return result;
        }

        private static (int HeaderRow, int PeriodRow)? DetectScheduleHeader(ExcelWorksheet worksheet)
        {
            for (int row = 1; row <= Math.Min(worksheet.Dimension.End.Row, 30); row++)
            {
                var hasSubject = false;
                var hasTeacher = false;
                var hasDay = false;

                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    var text = NormalizeArabic(GetCellText(worksheet, row, col));
                    if (text == NormalizeArabic("المادة"))
                    {
                        hasSubject = true;
                    }
                    else if (text == NormalizeArabic("اسم المعلم"))
                    {
                        hasTeacher = true;
                    }
                    else if (IsKnownDayHeader(text))
                    {
                        hasDay = true;
                    }
                }

                if (hasSubject && hasTeacher && hasDay)
                {
                    return (row, row + 1);
                }
            }

            return null;
        }

        private static Dictionary<int, (int DayId, int PeriodId, int PeriodNumber)> BuildColumnMap(
            ExcelWorksheet worksheet,
            int headerRow,
            int periodRow,
            List<SchoolDay> days,
            Dictionary<int, Period> periodByNumber)
        {
            var map = new Dictionary<int, (int DayId, int PeriodId, int PeriodNumber)>();
            var dayStarts = new List<(int Column, SchoolDay Day)>();
            var daysByName = days.ToDictionary(d => NormalizeArabic(d.DayName), d => d);

            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var headerText = NormalizeArabic(GetCellText(worksheet, headerRow, col));
                if (daysByName.TryGetValue(headerText, out var day))
                {
                    dayStarts.Add((col, day));
                }
            }

            for (int i = 0; i < dayStarts.Count; i++)
            {
                var start = dayStarts[i].Column;
                var end = i + 1 < dayStarts.Count
                    ? dayStarts[i + 1].Column - 1
                    : worksheet.Dimension.End.Column;

                for (int col = start; col <= end; col++)
                {
                    var periodValue = worksheet.Cells[periodRow, col].Value;
                    if (periodValue != null &&
                        int.TryParse(periodValue.ToString(), out var periodNumber) &&
                        periodByNumber.TryGetValue(periodNumber, out var period))
                    {
                        map[col] = (dayStarts[i].Day.DayId, period.PeriodId, period.PeriodNumber);
                    }
                }
            }

            return map;
        }

        private static Dictionary<string, Subject> BuildSubjectLookup(List<Subject> subjects)
        {
            var lookup = new Dictionary<string, Subject>();
            foreach (var subject in subjects)
            {
                lookup[NormalizeSubject(subject.SubjectName)] = subject;
                if (!string.IsNullOrWhiteSpace(subject.SubjectNameEn))
                {
                    lookup[NormalizeSubject(subject.SubjectNameEn)] = subject;
                }
            }

            return lookup;
        }

        private static Subject? ResolveSubject(string subjectName, Dictionary<string, Subject> lookup, List<Subject> subjects)
        {
            var normalized = NormalizeSubject(subjectName);
            if (lookup.TryGetValue(normalized, out var subject))
            {
                return subject;
            }

            if (normalized.Contains("موسيقي"))
            {
                return subjects.FirstOrDefault(s => NormalizeSubject(s.SubjectName).Contains("موسيقي"));
            }

            if (normalized.Contains("دراسات"))
            {
                return subjects.FirstOrDefault(s => NormalizeSubject(s.SubjectName).Contains("دراسات"));
            }

            return subjects.FirstOrDefault(s =>
                NormalizeSubject(s.SubjectName).Contains(normalized) ||
                normalized.Contains(NormalizeSubject(s.SubjectName)));
        }

        private static bool IsKnownDayHeader(string text)
        {
            return text is "الاحد" or "الاثنين" or "الثلاثاء" or "الاربعاء" or "الخميس";
        }

        private static string? GetCellText(ExcelWorksheet worksheet, int row, int col)
        {
            var cell = worksheet.Cells[row, col];
            return (cell.Text ?? cell.Value?.ToString())?.Trim();
        }

        private static string NormalizeSubject(string value)
        {
            var normalized = NormalizeArabic(value);
            normalized = Regex.Replace(normalized, @"\(.+?\)", "");
            normalized = normalized.Replace("التربيه", "");
            normalized = normalized.Replace("اللغه", "");
            normalized = normalized.Replace(" ", "");
            return normalized;
        }

        private static string NormalizeArabic(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                builder.Append(ch switch
                {
                    'أ' or 'إ' or 'آ' => 'ا',
                    'ى' => 'ي',
                    'ة' => 'ه',
                    'ـ' => '\0',
                    _ => ch
                });
            }

            return builder.ToString().Replace("\0", "").Normalize(NormalizationForm.FormC);
        }

        public async Task<byte[]> ExportScheduleToExcel(string academicYear, int semester)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("الجدول المدرسي");

                // إعدادات الورقة
                worksheet.View.RightToLeft = true;
                worksheet.Cells.Style.Font.Name = "Arial";
                worksheet.Cells.Style.Font.Size = 11;

                // عنوان المدرسة
                var settings = await _context.SchoolSettings.FirstAsync();
                worksheet.Cells[1, 1].Value = "وزارة التربية";
                worksheet.Cells[1, 2].Value = settings.SchoolName;
                worksheet.Cells[1, 3].Value = $"الفصل الدراسي {semester} {academicYear}";

                worksheet.Cells[3, 1].Value = settings.EducationalZone;

                // تحميل البيانات
                var days = await _context.SchoolDays.OrderBy(d => d.DayOrder).ToListAsync();
                var periods = await _context.Periods.OrderBy(p => p.PeriodNumber).ToListAsync();
                var subjects = await _context.Subjects.ToListAsync();

                int currentRow = 7;

                // ترويسة الجدول
                worksheet.Cells[currentRow, 1].Value = "م";
                worksheet.Cells[currentRow, 2].Value = "المادة";
                worksheet.Cells[currentRow, 3].Value = "اسم المعلم";
                worksheet.Cells[currentRow, 4].Value = "عدد الحصص";

                int col = 5;
                foreach (var day in days)
                {
                    worksheet.Cells[currentRow, col].Value = day.DayName;
                    col += periods.Count;
                }
                worksheet.Cells[currentRow, col].Value = "ملاحظات";

                currentRow++;

                // أرقام الحصص
                col = 5;
                foreach (var day in days)
                {
                    foreach (var period in periods)
                    {
                        worksheet.Cells[currentRow, col].Value = period.PeriodNumber;
                        col++;
                    }
                }

                currentRow++;

                // بيانات المعلمين والحصص
                foreach (var subject in subjects)
                {
                    var teachers = await _context.Teachers
                        .Where(t => t.SubjectId == subject.SubjectId && t.IsActive)
                        .ToListAsync();

                    if (teachers.Any())
                    {
                        int subjectStartRow = currentRow;

                        foreach (var teacher in teachers)
                        {
                            worksheet.Cells[currentRow, 2].Value = subject.SubjectName;
                            worksheet.Cells[currentRow, 3].Value = teacher.TeacherName;

                            // الحصص
                            col = 5;
                            foreach (var day in days)
                            {
                                foreach (var period in periods)
                                {
                                    var schedule = await _context.Schedules
                                        .Include(s => s.Class)
                                        .FirstOrDefaultAsync(s =>
                                            s.TeacherId == teacher.TeacherId &&
                                            s.DayId == day.DayId &&
                                            s.PeriodId == period.PeriodId &&
                                            s.AcademicYear == academicYear &&
                                            s.Semester == semester);

                                    if (schedule != null)
                                    {
                                        worksheet.Cells[currentRow, col].Value = schedule.Class.ClassCode;
                                    }

                                    col++;
                                }
                            }

                            // الملاحظات
                            if (!string.IsNullOrEmpty(teacher.Notes))
                            {
                                worksheet.Cells[currentRow, col].Value = teacher.Notes;
                            }

                            currentRow++;
                        }
                    }
                }

                // تنسيق الخلايا
                worksheet.Cells.AutoFitColumns();

                return package.GetAsByteArray();
            }
        }
    }
}
