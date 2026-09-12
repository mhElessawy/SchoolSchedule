using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SchoolSchedule.Data;
using SchoolSchedule.Models;
using SchoolSchedule.Models.ViewModels;

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
                    // البحث عن ورقة "جدول المدرسي"
                    var worksheet = package.Workbook.Worksheets
                        .FirstOrDefault(ws => ws.Name.Trim().Contains("جدول المدرسي"));

                    if (worksheet == null)
                    {
                        result.Success = false;
                        result.Errors.Add("لم يتم العثور على ورقة 'جدول المدرسي' في الملف");
                        result.Errors.Add($"الأوراق الموجودة: {string.Join(", ", package.Workbook.Worksheets.Select(w => w.Name))}");
                        return result;
                    }

                    // حذف البيانات الموجودة إذا طُلب ذلك
                    if (deleteExisting)
                    {
                        var existingSchedules = await _context.Schedules
                            .Where(s => s.AcademicYear == academicYear && s.Semester == semester)
                            .ToListAsync();
                        _context.Schedules.RemoveRange(existingSchedules);
                        await _context.SaveChangesAsync();
                    }

                    // تحميل البيانات المرجعية
                    var subjects = await _context.Subjects.ToDictionaryAsync(s => s.SubjectName, s => s);
                    var days = await _context.SchoolDays.OrderBy(d => d.DayOrder).ToListAsync();
                    var periods = await _context.Periods.OrderBy(p => p.PeriodNumber).ToListAsync();
                    var classes = await _context.Classes.ToDictionaryAsync(c => c.ClassCode!, c => c);
                    var teachers = new Dictionary<string, Teacher>();

                    // تتبع الحصص المضافة (من قاعدة البيانات ومن هذا الاستيراد) لمنع التعارض مع الفهرس الفريد
                    var existingSlots = new HashSet<(int ClassId, int DayId, int PeriodId)>(
                        (await _context.Schedules
                            .Where(s => s.AcademicYear == academicYear && s.Semester == semester)
                            .Select(s => new { s.ClassId, s.DayId, s.PeriodId })
                            .ToListAsync())
                            .Select(s => (s.ClassId, s.DayId, s.PeriodId)));

                    // خطوة 1: بناء خريطة الأعمدة (أي عمود يمثل أي يوم وحصة)
                    var columnMap = new Dictionary<int, (int DayId, int PeriodId)>();
                    
                    // قراءة الصف 8 لمعرفة أرقام الحصص
                    int col = 5; // البداية من العمود 5 (E)
                    foreach (var day in days)
                    {
                        for (int periodNum = 1; periodNum <= periods.Count; periodNum++)
                        {
                            // البحث عن العمود الذي يحتوي على رقم الحصة
                            while (col <= worksheet.Dimension.End.Column)
                            {
                                var headerValue = worksheet.Cells[8, col].Value;
                                if (headerValue != null && int.TryParse(headerValue.ToString(), out int num) && num == periodNum)
                                {
                                    var period = periods.FirstOrDefault(p => p.PeriodNumber == periodNum);
                                    if (period != null)
                                    {
                                        columnMap[col] = (day.DayId, period.PeriodId);
                                    }
                                    col++;
                                    break;
                                }
                                col++;
                            }
                        }
                    }

                    // خطوة 2: قراءة البيانات من Excel
                    int currentRow = 9; // البداية من الصف 9 (بعد الترويسة)
                    Subject? currentSubject = null;

                    while (currentRow <= worksheet.Dimension.End.Row)
                    {
                        // قراءة القيم من الصف
                        var rowNumber = worksheet.Cells[currentRow, 1].Value;
                        var subjectName = worksheet.Cells[currentRow, 2].Text?.Trim();
                        var teacherName = worksheet.Cells[currentRow, 3].Text?.Trim();

                        // إذا كانت الخلية الأولى فارغة أو 0، انتقل للصف التالي
                        if (rowNumber == null || rowNumber.ToString() == "0")
                        {
                            currentRow++;
                            continue;
                        }

                        // التحقق من وجود مادة جديدة
                        if (!string.IsNullOrEmpty(subjectName) && subjects.ContainsKey(subjectName))
                        {
                            currentSubject = subjects[subjectName];
                        }

                        // إذا كانت المادة محددة وهناك اسم معلم
                        if (currentSubject != null && !string.IsNullOrEmpty(teacherName))
                        {
                            // البحث عن المعلم أو إنشاؤه
                            Teacher teacher;
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
                                    var notes = worksheet.Cells[currentRow, 40].Text?.Trim();
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

                                var cellValue = worksheet.Cells[currentRow, colNum].Value;
                                var classCode = cellValue?.ToString()?.Trim();

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
                                        // التحقق من عدم وجود تعارض (في القاعدة أو في هذا الاستيراد نفسه)
                                        var slot = (classEntity.ClassId, dayId, periodId);

                                        if (existingSlots.Add(slot))
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
                                        else
                                        {
                                            result.Warnings.Add(
                                                $"تعارض في الجدول: الصف {finalClassCode} محجوز مسبقاً في نفس اليوم والحصة (الصف {currentRow}, المعلم: {teacherName})");
                                        }
                                    }
                                }
                            }

                            // حفظ كل 50 حصة لتحسين الأداء
                            if (result.SchedulesAdded % 50 == 0)
                            {
                                await _context.SaveChangesAsync();
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
                var detail = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    detail += " -> " + inner.Message;
                    inner = inner.InnerException;
                }
                result.Errors.Add($"خطأ في الاستيراد: {detail}");
            }

            return result;
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
